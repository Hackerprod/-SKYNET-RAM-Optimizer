using Microsoft.VisualBasic;
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;

namespace SKYNET
{
    public class MemoryHelper
    {
        private const int MEMORY_RELEASE_COOLDOWN_SECONDS = 10;
        private const long BYTES_TO_MB = 1048576L;
        private const int SE_PRIVILEGE_ENABLED = 2;

        public static bool IsBusy;
        private static DateTime CleanedTime = DateTime.MinValue;
        public static long TotalMemoryFreed = 0;  // Total memory freed in bytes
        public static int TotalProcessesOptimized = 0;  // Total number of processes optimized

        // Critical system processes that should not be optimized
        private static readonly HashSet<string> ExcludedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "csrss",
            "dwm",
            "explorer",
            "lsass",
            "services",
            "smss",
            "System",
            "svchost",
            "wininit",
            "winlogon",
            "audiodg",
            "conhost",
            "ntoskrnl"
        };


        public static void ReleaseMemory()
        {
            var span = DateTime.Now - CleanedTime;
            if (span.Seconds < MEMORY_RELEASE_COOLDOWN_SECONDS)
            {
                return;
            }

            Task.Run(delegate
            {
                IsBusy = true;
                int processesOptimized = 0;
                long memoryFreed = 0;

                foreach (Process process in Process.GetProcesses().Where(process => process != null))
                {
                    try
                    {
                        // Check if we can access this process
                        if (process.HasExited)
                        {
                            continue;
                        }

                        // Skip excluded critical system processes
                        if (ExcludedProcesses.Contains(process.ProcessName))
                        {
                            continue;
                        }

                        long beforeMemory = 0;
                        IntPtr handle = IntPtr.Zero;

                        try
                        {
                            beforeMemory = process.WorkingSet64;
                            handle = process.Handle;
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            // Access denied or 32/64 bit mismatch - skip this process
                            continue;
                        }
                        catch (InvalidOperationException)
                        {
                            // Process exited - skip
                            continue;
                        }

                        if (handle != IntPtr.Zero && !process.HasExited)
                        {
                            if (EmptyWorkingSet(handle) != 0)
                            {
                                // Success - track statistics
                                try
                                {
                                    process.Refresh();
                                    long afterMemory = process.WorkingSet64;
                                    long freed = beforeMemory - afterMemory;
                                    if (freed > 0)
                                    {
                                        memoryFreed += freed;
                                        processesOptimized++;
                                    }
                                }
                                catch
                                {
                                    // Process exited during refresh, count it anyway
                                    processesOptimized++;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but continue with other processes
                        if (!(ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException))
                        {
                            Program.Write("Error releasing memory for process: " + ex.Message);
                        }
                    }
                    finally
                    {
                        try
                        {
                            process?.Dispose();
                        }
                        catch { }
                    }
                }

                TotalMemoryFreed += memoryFreed;
                TotalProcessesOptimized += processesOptimized;
                Program.Write($"Optimization complete: {processesOptimized} processes optimized, {modCommon.LongToMbytes(memoryFreed)} freed");

                IsBusy = false;
            });

            frmMain.frm.ReleasedTimes++;
            CleanedTime = DateTime.Now;
        }

        internal static bool SetIncreasePrivilege(string privilegeName)
        {
            using (WindowsIdentity current = WindowsIdentity.GetCurrent(TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges | TokenAccessLevels.AllAccess))
            {
                TokenPrivileges newState;
                newState.Count = 1;
                newState.Luid = 0L;
                newState.Attr = SE_PRIVILEGE_ENABLED;

                // Retrieves the LUID used on a specified system to locally represent the specified privilege name
                if (LookupPrivilegeValue(null, privilegeName, ref newState.Luid))
                {
                    // Enables or disables privileges in a specified access token
                    int result = AdjustTokenPrivileges(current.Token, false, ref newState, 0, IntPtr.Zero, IntPtr.Zero) ? 1 : 0;

                    return result != 0;
                }
            }

            return false;
        }

        #region Native Methods

        [DllImport("psapi")]
        public static extern int EmptyWorkingSet(IntPtr handle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref long lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, [MarshalAs(UnmanagedType.Bool)]bool disableAllPrivileges, ref TokenPrivileges newState, int bufferLength, IntPtr previousState, IntPtr returnLength);


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TokenPrivileges
        {
            internal int Count;

            internal long Luid;

            internal int Attr;
        }

        internal static long GetUsedMemory(Process process)
        {
            try
            {
                using (var counter = new PerformanceCounter("Process", "Working Set - Private", process.ProcessName))
                {
                    var memoryUse = counter.RawValue;
                    return (memoryUse > BYTES_TO_MB ? memoryUse : process.WorkingSet64);
                }
            }
            catch
            {
                return process.WorkingSet64;
            }
        }

        #endregion
    }
}
