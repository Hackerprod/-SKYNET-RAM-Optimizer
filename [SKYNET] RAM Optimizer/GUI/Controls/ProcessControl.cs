using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using TsudaKageyu;
using System.Timers;
using System.Threading;
using SKYNET.Properties;

namespace SKYNET.GUI.Controls
{
    public partial class ProcessControl : UserControl
    {
        public int ProcessId;
        public event EventHandler<UserControl> ProcessExited;
        private bool Exited;

        public Process Process { get; set; }

        public ProcessControl()
        {
            InitializeComponent();
        }

        public void ManageProcess(Process process)
        {
            Process = process;
            try
            {
                ProcessId = Process.Id;

                LB_Name.Text = Process.ProcessName;
                LB_Usage.Text = modCommon.LongToMbytes(Process.WorkingSet64);

                string fileName = Process.MainModule.FileName;
                PB_Icon.Image = modCommon.IconFromFile(fileName);

                Process.Exited += Process_Exited;
                WaitForExit();
            }
            catch (Exception)
            {

            }
        }

        private void WaitForExit()
        {
            Task.Run(() =>
            {
                int closeId = 0;
                string processName = "";
                while (ProcessId != closeId)
                {
                    try
                    {
                        Process processById = Process.GetProcessById(ProcessId);
                        processName = processById.ProcessName;
                        closeId = ProcessId;
                        processById.WaitForExit();
                    }
                    catch
                    {
                        Process_Exited(this, null);
                    }
                }
                Process_Exited(this, null);
            });
        }
        private void Process_Exited(object sender, EventArgs e)
        {
            Exited = true;
            ProcessExited?.Invoke(this, this);
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            BackColor = Color.FromArgb(100, 100, 100);
            PN_Container.BackColor = Color.FromArgb(58, 58, 58);

            if (sender == PB_Kill)
            {
                PB_Kill.Image = Resources.remove_48px_Selected;
            }
        }

        private void Control_MouseLeave(object sender, EventArgs e)
        {
            BackColor = Color.FromArgb(58, 58, 58);
            PN_Container.BackColor = Color.FromArgb(48, 48, 48);

            if (sender == PB_Kill)
            {
                PB_Kill.Image = Resources.remove_48px;
            }
        }

        private void PB_Kill_Click(object sender, EventArgs e)
        {
            if (Process != null)
            {
                Process.Kill();
            }
        }

        internal void SetMemoryUse(long workingSet64)
        {
            LB_Usage.Text = modCommon.LongToMbytes(workingSet64);
        }

        public void CheckMemoryUse()
        {
            if (!Exited)
            {
                Process.Refresh();
                var use = MemoryHelper.GetUsedMemory(Process);
                LB_Usage.Text = modCommon.LongToMbytes(use);
            }
        }
    }
}
