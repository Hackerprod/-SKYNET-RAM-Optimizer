using Microsoft.VisualBasic.Devices;
using SKYNET.Controls;
using SKYNET.GUI;
using SKYNET.GUI.Controls;
using SKYNET.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SKYNET
{
    public partial class frmMain : frmBase
    {
        private long _TotalPhysicalMemorySize;
        private long _FreePhysicalMemory;
        private long _TotalVirtualMemorySize;
        private long _FreeVirtualMemory;
        private long UsedPhysicalMemory;
        private long UsedVirtualMemory;
        private long percentToFree;
        private int releasedTimes;
        private List<Process> Processes;
        private ComputerInfo Machine;

        public static frmMain frm;

        public Process CurrentProcess;
       


        private long TotalPhysicalMemorySize
        {
            get { return _TotalPhysicalMemorySize; }
            set
            {
                _TotalPhysicalMemorySize = value;
                LB_TotalVisibleMemorySize.Text = modCommon.LongToMbytes(value);
            }
        }

        private long FreePhysicalMemory
        {
            get { return _FreePhysicalMemory; }
            set
            {

                _FreePhysicalMemory = value;
                LB_FreePhysicalMemory.Text = modCommon.LongToMbytes(value);

                UsedPhysicalMemory = TotalPhysicalMemorySize - _FreePhysicalMemory;
                LB_UsedPhysicalMemory.Text = modCommon.LongToMbytes(UsedPhysicalMemory);

                var Percent = 100 * UsedPhysicalMemory / TotalPhysicalMemorySize;
                LB_PercentUsedPhysicalMemory.Text = Percent + " %";

                if (Percent > percentToFree)
                {
                    ReleaseMemory();
                }

                SetProgressValue(PN_Physical_Progress, Percent);
            }
        }

        private long TotalVirtualMemorySize
        {
            get { return _TotalVirtualMemorySize; }
            set
            {
                _TotalVirtualMemorySize = value;
                LB_TotalVirtualMemorySize.Text = modCommon.LongToMbytes(value * 1024);
            }
        }

        private long FreeVirtualMemory
        {
            get { return _FreeVirtualMemory; }
            set
            {
                _FreeVirtualMemory = value;
                LB_FreeVirtualMemory.Text = modCommon.LongToMbytes(value * 1024);

                UsedVirtualMemory = TotalVirtualMemorySize - _FreeVirtualMemory;
                LB_UsedVirtualMemory.Text = modCommon.LongToMbytes(UsedVirtualMemory * 1024);

                var Percent = 100 * UsedVirtualMemory / TotalVirtualMemorySize;
                LB_PercentUsedVirtualMemory.Text = Percent + " %";

                SetProgressValue(PN_Virtual_Progress, Percent);
            }
        }

        private int ReleasedTimes
        {
            get { return releasedTimes; }
            set
            {
                releasedTimes = value;
                LB_ReleasedTimes.Text = $"Released Memory {releasedTimes} times";
            }
        }

        public frmMain()
        {
            InitializeComponent();
            frm = this;
            base.SetMouseMove(panel1);
            CheckForIllegalCrossThreadCalls = false;
            percentToFree = 80;
            CurrentProcess = Process.GetCurrentProcess();
            Processes = new List<Process>();
            Machine = new ComputerInfo();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            InitializeCheckThread();

            Thread ProcessManager = new Thread(InitializeProcessManager);
            ProcessManager.IsBackground = true;
            ProcessManager.Start();
        }

        private void CloseBox1_Clicked(object sender, EventArgs e)
        {
            CurrentProcess.Kill();
        }

        private void InitializeCheckThread()
        {
            Task.Run(() => 
            {
                while (true)
                {
                    TotalPhysicalMemorySize = Convert.ToInt64(Machine.TotalPhysicalMemory);
                    FreePhysicalMemory = Convert.ToInt64(Machine.AvailablePhysicalMemory);

                    //TotalVirtualMemorySize = Convert.ToInt64(Machine.TotalVirtualMemory);
                    //FreeVirtualMemory = Convert.ToInt64(Machine.AvailableVirtualMemory);

                    ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
                    ManagementObjectCollection results = searcher.Get();

                    foreach (ManagementObject result in results)
                    {
                        if (long.TryParse(result["TotalVirtualMemorySize"].ToString(), out long totalVirtualMemorySize))
                        {
                            TotalVirtualMemorySize = totalVirtualMemorySize;
                        }

                        if (long.TryParse(result["FreeVirtualMemory"].ToString(), out long freeVirtualMemory))
                        {
                            FreeVirtualMemory = freeVirtualMemory;
                        }
                    }
                    Thread.Sleep(1000);
                }
            });
        }

        private void BT_FreeMemory_Click(object sender, EventArgs e)
        {
            ReleaseMemory();
        }

        private void ReleaseMemory()
        {
            if (!MemoryHelper.IsBusy)
            {
                MemoryHelper.ReleaseMemory();
                ReleasedTimes++;
            }
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(textBox1.Text, out int percent))
            {
                MessageBox.Show("The Percent is invalid");
                return;
            }
            if (percent > 100)
            {
                MessageBox.Show("The Percent is invalid, must be < 100");
                return;
            }
            percentToFree = percent;
        }

        private void MinimizeBox1_Clicked(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            notifyIcon1.Visible = true;
        }

        private void NotifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }

        private void Physical_MouseMove(object sender, MouseEventArgs e)
        {
            panel5.BackColor = Color.FromArgb(0, 116, 204);
            panel6.BackColor = Color.FromArgb(38, 38, 38);
            PN_Physical_Tittle.BackColor = Color.FromArgb(48, 48, 48);

            GB_Physical_L.LeftColor = Color.FromArgb(42, 42, 42);
            GB_Physical_L.RigthColor = Color.FromArgb(48, 48, 48);
            PN_Physical_L.BackColor = Color.FromArgb(42, 42, 42);

            GB_Physical_R.LeftColor = Color.FromArgb(48, 48, 48);
            GB_Physical_R.RigthColor = Color.FromArgb(42, 42, 42);
            PN_Physical_R.BackColor = Color.FromArgb(42, 42, 42);
        }

        private void Physical_MouseLeave(object sender, EventArgs e)
        {
            panel5.BackColor = Color.FromArgb(58, 58, 58);
            panel6.BackColor = Color.FromArgb(48, 48, 48);
            PN_Physical_Tittle.BackColor = Color.FromArgb(42, 42, 42);

            GB_Physical_L.LeftColor = Color.FromArgb(48, 48, 48);
            GB_Physical_L.RigthColor = Color.FromArgb(42, 42, 42);
            PN_Physical_L.BackColor = Color.FromArgb(48, 48, 48);

            GB_Physical_R.LeftColor = Color.FromArgb(42, 42, 42);
            GB_Physical_R.RigthColor = Color.FromArgb(48, 48, 48);
            PN_Physical_R.BackColor = Color.FromArgb(48, 48, 48);
        }

        private void Virtual_MouseMove(object sender, MouseEventArgs e)
        {
            panel7.BackColor = Color.FromArgb(255, 80, 80);
            panel8.BackColor = Color.FromArgb(38, 38, 38);
            panel11.BackColor = Color.FromArgb(48, 48, 48);

            GB_Virtual_L.LeftColor = Color.FromArgb(42, 42, 42);
            GB_Virtual_L.RigthColor = Color.FromArgb(48, 48, 48);
            PN_Virtual_L.BackColor = Color.FromArgb(42, 42, 42);

            GB_Virtual_R.LeftColor = Color.FromArgb(48, 48, 48);
            GB_Virtual_R.RigthColor = Color.FromArgb(42, 42, 42);
            PN_Virtual_R.BackColor = Color.FromArgb(42, 42, 42);

            panel12.BackColor = Color.FromArgb(255, 80, 80);
            panel13.BackColor = Color.FromArgb(255, 80, 80);
            panel14.BackColor = Color.FromArgb(255, 80, 80);
        }

        private void Virtual_MouseLeave(object sender, EventArgs e)
        {
            panel7.BackColor = Color.FromArgb(58, 58, 58);
            panel8.BackColor = Color.FromArgb(48, 48, 48);
            panel11.BackColor = Color.FromArgb(42, 42, 42);

            GB_Virtual_L.LeftColor = Color.FromArgb(48, 48, 48);
            GB_Virtual_L.RigthColor = Color.FromArgb(42, 42, 42);
            PN_Virtual_L.BackColor = Color.FromArgb(48, 48, 48);

            GB_Virtual_R.LeftColor = Color.FromArgb(42, 42, 42);
            GB_Virtual_R.RigthColor = Color.FromArgb(48, 48, 48);
            PN_Virtual_R.BackColor = Color.FromArgb(48, 48, 48);

            panel12.BackColor = Color.FromArgb(48, 48, 48);
            panel13.BackColor = Color.FromArgb(48, 48, 48);
            panel14.BackColor = Color.FromArgb(48, 48, 48);
        }

        private void SetProgressValue(SKYNET_ProgressBar Progress, long percent)
        {
            Progress.Value = (int)percent;
        }

        private void InitializeProcessManager()
        {
            while (true)
            {
                try
                {
                    if (!MemoryHelper.SetIncreasePrivilege("SeDebugPrivilege"))
                    {
                        //return;
                    }

                    Processes.Clear();
                    Processes = Process.GetProcesses().ToList().OrderByDescending(x => x.WorkingSet64).Take(5).ToList();
                    Processes.Sort((n1, n2) => MemoryHelper.GetUsedMemory(n1).CompareTo(MemoryHelper.GetUsedMemory(n2)));

                    foreach (var process in Processes)
                    {
                        try
                        {
                            if (process != null && !process.HasExited)
                            {
                                var Controls = PN_ProcessContainer.Controls.Find(process.Id.ToString(), false);
                                if (Controls.Any())
                                {
                                    var Control = (ProcessControl)Controls[0];
                                    Control.CheckMemoryUse();
                                }
                                else
                                {
                                    ProcessControl processControl = new ProcessControl();
                                    processControl.ManageProcess(process);
                                    processControl.Name = process.Id.ToString();
                                    processControl.ProcessExited += ProcessExited;

                                    if (PN_ProcessContainer.InvokeRequired)
                                    {
                                        PN_ProcessContainer.Invoke(new Action(() => { PN_ProcessContainer.Controls.Add(processControl); }));
                                    }
                                    else
                                    {
                                        PN_ProcessContainer.Controls.Add(processControl);
                                    }
                                    processControl.Dock = DockStyle.Top;
                                }
                            }
                        }
                        catch { }
                    }

                    for (int i = 0; i < PN_ProcessContainer.Controls.Count; i++)
                    {
                        try
                        {
                            var Control = (ProcessControl)PN_ProcessContainer.Controls[i];
                            var Process = Processes.Find(p => p != null && p.Id == Control.ProcessId);
                            if (Process == null)
                            {
                                PN_ProcessContainer.Controls.RemoveAt(i);
                            }
                        }
                        catch  { }
                    }

                    for (int i = 0; i < Processes.Count; i++)
                    {
                        try
                        {
                            var Controls = PN_ProcessContainer.Controls.Find(Processes[i].Id.ToString(), false);
                            if (Controls.Any())
                            {
                                PN_ProcessContainer.Controls.SetChildIndex(Controls[0], i);
                            }
                        }
                        catch { }
                    }
                    
                    Thread.Sleep(1000);
                }
                catch
                {
                }
            }
        }

        private void ProcessExited(object sender, UserControl e)
        {
            PN_ProcessContainer.Controls.Remove(e);
        }

        private void PB_Icon_Click(object sender, EventArgs e)
        {

        }
    }
}
