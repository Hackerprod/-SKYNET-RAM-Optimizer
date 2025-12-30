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

                // Try to get process icon, but handle access denied gracefully
                try
                {
                    string fileName = Process.MainModule.FileName;
                    PB_Icon.Image = modCommon.IconFromFile(fileName);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // 64-bit process accessed from 32-bit or access denied - use default icon
                    PB_Icon.Image = new Icon(SystemIcons.Application, 32, 32).ToBitmap();
                }
                catch (InvalidOperationException)
                {
                    // Process has exited
                    PB_Icon.Image = new Icon(SystemIcons.Application, 32, 32).ToBitmap();
                }

                Process.Exited += Process_Exited;
                WaitForExit();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Access denied or 64-bit process - already handled above
            }
            catch (InvalidOperationException)
            {
                // Process exited - skip silently
            }
            catch (Exception ex)
            {
                // Only log unexpected errors
                Program.Write("Unexpected error initializing process control: " + ex.GetType().Name + ": " + ex.Message);
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
                    catch (System.ComponentModel.Win32Exception)
                    {
                        // Access denied - process exited
                        Process_Exited(this, null);
                    }
                    catch (InvalidOperationException)
                    {
                        // Process exited
                        Process_Exited(this, null);
                    }
                    catch (Exception ex)
                    {
                        // Only log unexpected errors
                        Program.Write("Unexpected error waiting for process exit: " + ex.GetType().Name + ": " + ex.Message);
                        Process_Exited(this, null);
                    }
                }
                Process_Exited(this, null);
            });
        }
        private void Process_Exited(object sender, EventArgs e)
        {
            try
            {
                Exited = true;
                ProcessExited?.Invoke(this, this);
            }
            catch (Exception ex)
            {
                Program.Write($"Error in Process_Exited handler: {ex.Message}");
            }
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
                try
                {
                    if (!Process.HasExited)
                    {
                        Process.Kill();
                        Process.WaitForExit(1000); // Wait up to 1 second for process to exit
                        Process_Exited(this, null); // Manually trigger exit handler
                    }
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    // Access denied or process already exited
                    Program.Write($"Could not kill process {Process.ProcessName}: {ex.Message}");
                    MessageBox.Show($"Could not terminate process: {ex.Message}", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (InvalidOperationException)
                {
                    // Process already exited
                    Process_Exited(this, null);
                }
                catch (Exception ex)
                {
                    Program.WriteException("Error killing process from UI", ex);
                    MessageBox.Show($"Error terminating process: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                try
                {
                    Process.Refresh();
                    var use = MemoryHelper.GetUsedMemory(Process);
                    LB_Usage.Text = modCommon.LongToMbytes(use);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Access denied - mark as exited
                    Process_Exited(this, null);
                }
                catch (InvalidOperationException)
                {
                    // Process exited
                    Process_Exited(this, null);
                }
                catch (Exception ex)
                {
                    // Only log unexpected errors
                    Program.Write("Unexpected error updating process memory: " + ex.GetType().Name + ": " + ex.Message);
                    Process_Exited(this, null);
                }
            }
        }
    }
}
