using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SKYNET.Helpers
{
    public class ProcessManager
    {
        private const int PROCESS_CHECK_INTERVAL_MS = 1000;
        private const int TOP_PROCESSES_COUNT = 5;

        public event EventHandler<List<Process>> TopProcessesChanged;

        private System.Timers.Timer timer;
        private List<Process> Processes;

        public ProcessManager()
        {
            timer = new System.Timers.Timer();
            timer.AutoReset = false;
            timer.Elapsed += Timer_Elapsed;

            Processes = new List<Process>();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                Processes.Clear();
                Processes = Process.GetProcesses()
                    .Where(p => p != null)
                    .OrderByDescending(x => x.WorkingSet64)
                    .Take(TOP_PROCESSES_COUNT)
                    .ToList();

                // Sort by actual memory usage
                Processes.Sort((n1, n2) => MemoryHelper.GetUsedMemory(n1).CompareTo(MemoryHelper.GetUsedMemory(n2)));

                TopProcessesChanged?.Invoke(this, Processes);

                timer.Interval = PROCESS_CHECK_INTERVAL_MS;
                timer.Start();
            }
            catch (Exception ex)
            {
                Program.Write("Error in ProcessManager timer: " + ex.Message);
                timer.Interval = PROCESS_CHECK_INTERVAL_MS;
                timer.Start();
            }
        }


        public void Initialize()
        {
            timer.Interval = PROCESS_CHECK_INTERVAL_MS;
            timer.Start();
        }
    }
}
