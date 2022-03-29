using Microsoft.VisualBasic;
using Microsoft.VisualBasic.Devices;
using System;
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
        public static bool IsBusy;


        public static void ReleaseMemory()
        {
            // Check privilege
            //if (!SetIncreasePrivilege("SeDebugPrivilege"))
            //{
            //    return;
            //}

            Task.Run(delegate
            {
                IsBusy = true;
                foreach (Process process in Process.GetProcesses().Where(process => process != null))
                {
                    try
                    {
                        using (process)
                        {
                            if (!process.HasExited && EmptyWorkingSet(process.Handle) == 0)
                                Program.Write(process.ProcessName + ": " + Marshal.GetLastWin32Error());
                        }
                    }
                    catch { }
                }
                IsBusy = false;
            });
            //GC.Collect();
        }
        public static void ReleaseMemory(Process process)
        {

            try
            {
                EmptyWorkingSet(process.Handle);
                GC.Collect();
            }
            catch
            {
            }
        }

        internal static bool SetIncreasePrivilege(string privilegeName)
        {
            using (WindowsIdentity current = WindowsIdentity.GetCurrent(TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges | TokenAccessLevels.AllAccess))
            {
                TokenPrivileges newState;
                newState.Count = 1;
                newState.Luid = 0L;
                newState.Attr = 2;  // PrivilegeEnabled;

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
            var memoryUse = new PerformanceCounter("Process", "Working Set - Private", process.ProcessName).RawValue;
            return (memoryUse > 1048576L ? memoryUse : process.WorkingSet64);
        }

        #endregion
    }
}