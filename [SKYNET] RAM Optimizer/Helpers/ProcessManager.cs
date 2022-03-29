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
        public event EventHandler<List<Process>> TopProcess;

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
            Processes.Clear();
            Processes = Process.GetProcesses().ToList().OrderByDescending(x => x.WorkingSet64).Take(5).ToList();

            TopProcess?.Invoke(this, Processes);

            timer.Interval = 1000;
            timer.Start();
        }


        public void Initialize()
        {
            timer.Interval = 1000;
            timer.Start();
        }
    }
}
