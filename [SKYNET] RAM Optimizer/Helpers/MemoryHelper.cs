using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SKYNET
{
    public sealed class MemoryReleaseResult
    {
        public int ProcessesOptimized { get; internal set; }
        public long EstimatedBytesFreed { get; internal set; }
        public bool WasSkipped { get; internal set; }
    }

    public static class MemoryHelper
    {
        private const long MinimumCandidateWorkingSet = 50L * 1024 * 1024;
        private const int MaximumProcessesPerRun = 10;
        private static readonly SemaphoreSlim ReleaseGate = new SemaphoreSlim(1, 1);
        private static readonly object ActivityLock = new object();
        private static readonly Dictionary<int, ProcessActivity> Activity = new Dictionary<int, ProcessActivity>();

        public static bool IsBusy { get { return ReleaseGate.CurrentCount == 0; } }
        public static long TotalMemoryFreed;
        public static int TotalProcessesOptimized;

        private static readonly HashSet<string> ExcludedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "audiodg", "csrss", "dwm", "explorer", "fontdrvhost", "lsass", "memory compression",
            "registry", "services", "sihost", "smss", "spoolsv", "startmenuexperiencehost", "system",
            "svchost", "wininit", "winlogon", "searchhost", "shellexperiencehost", "taskhostw"
        };

        // Called periodically. A process needs two observations before it can be considered idle.
        public static void CaptureProcessActivity()
        {
            DateTime now = DateTime.UtcNow;
            var seen = new HashSet<int>();
            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    if (process.HasExited) continue;
                    seen.Add(process.Id);
                    lock (ActivityLock)
                    {
                        ProcessActivity previous;
                        if (Activity.TryGetValue(process.Id, out previous) && previous.StartTime == process.StartTime)
                        {
                            previous.LastCpu = process.TotalProcessorTime;
                            previous.LastSeen = now;
                        }
                        else
                        {
                            Activity[process.Id] = new ProcessActivity { StartTime = process.StartTime, FirstSeen = now, LastCpu = process.TotalProcessorTime, LastSeen = now };
                        }
                    }
                }
                catch { /* inaccessible and exiting processes are never candidates */ }
                finally { process.Dispose(); }
            }

            lock (ActivityLock)
            {
                foreach (int id in Activity.Keys.Where(id => !seen.Contains(id)).ToArray()) Activity.Remove(id);
            }
        }

        public static async Task<MemoryReleaseResult> ReleaseMemoryAsync(long targetBytes, CancellationToken cancellationToken)
        {
            if (!await ReleaseGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
                return new MemoryReleaseResult { WasSkipped = true };

            try
            {
                return await Task.Run(() => ReleaseIdleProcesses(targetBytes, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
            finally { ReleaseGate.Release(); }
        }

        private static MemoryReleaseResult ReleaseIdleProcesses(long targetBytes, CancellationToken cancellationToken)
        {
            int currentProcessId = Process.GetCurrentProcess().Id;
            int currentSessionId = Process.GetCurrentProcess().SessionId;
            int foregroundProcessId = GetForegroundProcessId();
            var candidates = new List<Process>();

            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    if (IsSafeCandidate(process, currentProcessId, currentSessionId, foregroundProcessId)) candidates.Add(process);
                    else process.Dispose();
                }
                catch { process.Dispose(); }
            }

            var result = new MemoryReleaseResult();
            try
            {
                foreach (Process process in candidates.OrderByDescending(p => SafeWorkingSet(p)).Take(MaximumProcessesPerRun))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    long before = SafeWorkingSet(process);
                    if (before == 0) continue;

                    try
                    {
                        if (EmptyWorkingSet(process.Handle) != 0)
                        {
                            process.Refresh();
                            long freed = Math.Max(0, before - SafeWorkingSet(process));
                            if (freed > 0)
                            {
                                result.ProcessesOptimized++;
                                result.EstimatedBytesFreed += freed;
                                if (result.EstimatedBytesFreed >= targetBytes) break;
                            }
                        }
                    }
                    catch { /* a process can exit or deny access between checks */ }
                }
            }
            finally
            {
                foreach (Process process in candidates) process.Dispose();
            }

            Interlocked.Add(ref TotalMemoryFreed, result.EstimatedBytesFreed);
            Interlocked.Add(ref TotalProcessesOptimized, result.ProcessesOptimized);
            Program.Write($"Optimization complete: {result.ProcessesOptimized} idle user processes trimmed, {modCommon.LongToMbytes(result.EstimatedBytesFreed)} estimated working set released");
            return result;
        }

        private static bool IsSafeCandidate(Process process, int currentProcessId, int currentSessionId, int foregroundProcessId)
        {
            if (process.HasExited || process.Id == currentProcessId || process.Id == foregroundProcessId || process.SessionId != currentSessionId) return false;
            if (ExcludedProcesses.Contains(process.ProcessName) || SafeWorkingSet(process) < MinimumCandidateWorkingSet) return false;

            lock (ActivityLock)
            {
                ProcessActivity activity;
                // Do not act on a process that has not been observed idle for at least five seconds.
                return Activity.TryGetValue(process.Id, out activity) && activity.StartTime == process.StartTime &&
                       DateTime.UtcNow - activity.FirstSeen >= TimeSpan.FromSeconds(5) &&
                       DateTime.UtcNow - activity.LastSeen < TimeSpan.FromSeconds(5) &&
                       process.TotalProcessorTime - activity.LastCpu < TimeSpan.FromMilliseconds(100);
            }
        }

        private static long SafeWorkingSet(Process process) { try { return process.WorkingSet64; } catch { return 0; } }

        internal static long GetUsedMemory(Process process) { return SafeWorkingSet(process); }

        // The app runs with normal user-process access; no global debug privilege is required.
        internal static bool SetIncreasePrivilege(string privilegeName) { return true; }

        private static int GetForegroundProcessId()
        {
            uint id;
            GetWindowThreadProcessId(GetForegroundWindow(), out id);
            return unchecked((int)id);
        }

        private sealed class ProcessActivity { public DateTime StartTime; public DateTime FirstSeen; public TimeSpan LastCpu; public DateTime LastSeen; }

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern int EmptyWorkingSet(IntPtr handle);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    }
}
