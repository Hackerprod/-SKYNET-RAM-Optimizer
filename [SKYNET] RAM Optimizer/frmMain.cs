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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SKYNET
{
    public partial class frmMain : frmBase
    {
        private const int DEFAULT_MEMORY_THRESHOLD_PERCENT = 80;
        private const int MEMORY_CHECK_INTERVAL_MS = 1000;
        private const int PROCESS_CHECK_INTERVAL_MS = 1000;
        private const int TOP_PROCESSES_COUNT = 5;

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
        private readonly System.Windows.Forms.Timer memoryTimer;
        private readonly System.Windows.Forms.Timer processTimer;
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private bool memoryPressure;
        private int consecutiveHighMemorySamples;
        private bool samplingActivity;

        public static frmMain frm;

        public Process CurrentProcess;
       


        private long TotalPhysicalMemorySize
        {
            get { return _TotalPhysicalMemorySize; }
            set
            {
                _TotalPhysicalMemorySize = value;
                SafeUpdateLabel(LB_TotalVisibleMemorySize, modCommon.LongToMbytes(value));
            }
        }

        private long FreePhysicalMemory
        {
            get { return _FreePhysicalMemory; }
            set
            {
                _FreePhysicalMemory = value;
                SafeUpdateLabel(LB_FreePhysicalMemory, modCommon.LongToMbytes(value));

                UsedPhysicalMemory = TotalPhysicalMemorySize - _FreePhysicalMemory;
                SafeUpdateLabel(LB_UsedPhysicalMemory, modCommon.LongToMbytes(UsedPhysicalMemory));

                var Percent = 100 * UsedPhysicalMemory / TotalPhysicalMemorySize;
                SafeUpdateLabel(LB_PercentUsedPhysicalMemory, Percent + " %");

                SafeSetProgressValue(PN_Physical_Progress, Percent);
            }
        }

        private long TotalVirtualMemorySize
        {
            get { return _TotalVirtualMemorySize; }
            set
            {
                _TotalVirtualMemorySize = value;
                SafeUpdateLabel(LB_TotalVirtualMemorySize, modCommon.LongToMbytes(value * 1024));
            }
        }

        private long FreeVirtualMemory
        {
            get { return _FreeVirtualMemory; }
            set
            {
                _FreeVirtualMemory = value;
                SafeUpdateLabel(LB_FreeVirtualMemory, modCommon.LongToMbytes(value * 1024));

                UsedVirtualMemory = TotalVirtualMemorySize - _FreeVirtualMemory;
                SafeUpdateLabel(LB_UsedVirtualMemory, modCommon.LongToMbytes(UsedVirtualMemory * 1024));

                var Percent = 100 * UsedVirtualMemory / TotalVirtualMemorySize;
                SafeUpdateLabel(LB_PercentUsedVirtualMemory, Percent + " %");

                SafeSetProgressValue(PN_Virtual_Progress, Percent);
            }
        }

        public int ReleasedTimes
        {
            get { return releasedTimes; }
            set
            {
                releasedTimes = value;
                SafeUpdateLabel(LB_ReleasedTimes, $"Released Memory {releasedTimes} times");
            }
        }

        public frmMain()
        {
            InitializeComponent();
            frm = this;
            base.SetMouseMove(panel1);

            // Load saved settings
            percentToFree = Properties.Settings.Default.PercentToFree;
            if (percentToFree <= 0 || percentToFree > 100)
            {
                percentToFree = DEFAULT_MEMORY_THRESHOLD_PERCENT;
            }
            textBox1.Text = percentToFree.ToString();

            CurrentProcess = Process.GetCurrentProcess();
            Processes = new List<Process>();
            Machine = new ComputerInfo();
            memoryTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            memoryTimer.Tick += MemoryTimer_Tick;
            processTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            processTimer.Tick += ProcessTimer_Tick;
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            MemoryTimer_Tick(this, EventArgs.Empty);
            ProcessTimer_Tick(this, EventArgs.Empty);
            memoryTimer.Start();
            processTimer.Start();
        }

        private void CloseBox1_Clicked(object sender, EventArgs e)
        {
            StopMonitoring();
            Application.Exit();
        }

        private void BT_FreeMemory_Click(object sender, EventArgs e)
        {
            ReleaseMemory(true);
        }

        private async void ReleaseMemory(bool manual = false)
        {
            if (MemoryHelper.IsBusy || shutdown.IsCancellationRequested) return;

            SafeSetButtonEnabled(skyneT_Button1, false);
            SafeUpdateButtonText(skyneT_Button1, "RELEASING...");
            try
            {
                long desiredAvailable = Math.Max(256L * 1024 * 1024, TotalPhysicalMemorySize / 10);
                long target = Math.Max(64L * 1024 * 1024, desiredAvailable - FreePhysicalMemory);
                var result = await MemoryHelper.ReleaseMemoryAsync(target, shutdown.Token);
                if (!result.WasSkipped)
                {
                    ReleasedTimes++;
                    Program.Write($"Memory release requested ({(manual ? "manual" : "automatic")}): {result.ProcessesOptimized} processes, {modCommon.LongToMbytes(result.EstimatedBytesFreed)} estimated released");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Program.WriteException("Memory release failed", ex); }
            finally
            {
                if (!IsDisposed && !shutdown.IsCancellationRequested)
                {
                    SafeSetButtonEnabled(skyneT_Button1, true);
                    SafeUpdateButtonText(skyneT_Button1, "FREE MEMORY");
                }
            }
        }

        private void MemoryTimer_Tick(object sender, EventArgs e)
        {
            if (shutdown.IsCancellationRequested) return;
            TotalPhysicalMemorySize = Convert.ToInt64(Machine.TotalPhysicalMemory);
            if (TotalPhysicalMemorySize <= 0) return;
            FreePhysicalMemory = Convert.ToInt64(Machine.AvailablePhysicalMemory);

            long percent = TotalPhysicalMemorySize == 0 ? 0 : 100 * UsedPhysicalMemory / TotalPhysicalMemorySize;
            if (percent >= percentToFree) consecutiveHighMemorySamples++;
            else consecutiveHighMemorySamples = 0;

            // Hysteresis prevents repeating trims while Windows is still settling after a cleanup.
            if (percent <= Math.Max(1, percentToFree - 10)) memoryPressure = false;
            // Four samples at two seconds ensure the activity tracker has observed candidates long enough.
            if (!memoryPressure && consecutiveHighMemorySamples >= 4)
            {
                memoryPressure = true;
                ReleaseMemory();
            }

            if (!samplingActivity)
            {
                samplingActivity = true;
                Task.Run(() => MemoryHelper.CaptureProcessActivity())
                    .ContinueWith(_ =>
                    {
                        try { if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(() => samplingActivity = false)); }
                        catch (InvalidOperationException) { }
                    });
            }
        }

        private void StopMonitoring()
        {
            if (shutdown.IsCancellationRequested) return;
            memoryTimer.Stop();
            processTimer.Stop();
            shutdown.Cancel();
            notifyIcon1.Visible = false;
            foreach (ProcessControl control in PN_ProcessContainer.Controls.OfType<ProcessControl>().ToArray()) control.ReleaseProcess();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopMonitoring();
            base.OnFormClosing(e);
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            // Just update in real-time, validation happens on Leave
        }

        private void TextBox1_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                textBox1.Text = percentToFree.ToString();
                return;
            }

            if (!int.TryParse(textBox1.Text, out int percent))
            {
                MessageBox.Show("The Percent is invalid. Please enter a number between 1 and 100.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Text = percentToFree.ToString();
                return;
            }

            if (percent <= 0 || percent > 100)
            {
                MessageBox.Show("The Percent must be between 1 and 100.", "Invalid Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox1.Text = percentToFree.ToString();
                return;
            }

            percentToFree = percent;

            // Save the setting
            Properties.Settings.Default.PercentToFree = percent;
            Properties.Settings.Default.Save();
        }

        private void TextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Only allow digits, backspace, and control characters
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
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

        // Thread-safe UI update methods
        private void SafeUpdateLabel(Label label, string text)
        {
            if (IsDisposed || label.IsDisposed || !label.IsHandleCreated) return;
            if (label.InvokeRequired)
            {
                label.BeginInvoke(new Action(() => label.Text = text));
            }
            else
            {
                label.Text = text;
            }
        }

        private void SafeSetProgressValue(SKYNET_ProgressBar progress, long percent)
        {
            if (IsDisposed || progress.IsDisposed || !progress.IsHandleCreated) return;
            if (progress.InvokeRequired)
            {
                progress.BeginInvoke(new Action(() => progress.Value = (int)percent));
            }
            else
            {
                progress.Value = (int)percent;
            }
        }

        private void SafeSetButtonEnabled(Control button, bool enabled)
        {
            if (IsDisposed || button.IsDisposed || !button.IsHandleCreated) return;
            if (button.InvokeRequired)
            {
                button.BeginInvoke(new Action(() => button.Enabled = enabled));
            }
            else
            {
                button.Enabled = enabled;
            }
        }

        private void SafeUpdateButtonText(Control button, string text)
        {
            if (IsDisposed || button.IsDisposed || !button.IsHandleCreated) return;
            if (button.InvokeRequired)
            {
                button.BeginInvoke(new Action(() => button.Text = text));
            }
            else
            {
                button.Text = text;
            }
        }

        private void ProcessTimer_Tick(object sender, EventArgs e)
        {
            if (shutdown.IsCancellationRequested) return;
            var allProcesses = new List<Process>();
            var handedToControl = new HashSet<Process>();
            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    try { if (!process.HasExited) allProcesses.Add(process); else process.Dispose(); }
                    catch { process.Dispose(); }
                }

                Processes = allProcesses.OrderByDescending(SafeWorkingSet).Take(TOP_PROCESSES_COUNT).ToList();
                var displayedIds = new HashSet<int>(Processes.Select(p => p.Id));
                foreach (var process in Processes)
                {
                    var found = PN_ProcessContainer.Controls.Find(process.Id.ToString(), false);
                    if (found.Any())
                    {
                        ((ProcessControl)found[0]).CheckMemoryUse();
                        continue;
                    }

                    var processControl = new ProcessControl();
                    processControl.ManageProcess(process);
                    processControl.Name = process.Id.ToString();
                    processControl.ProcessExited += ProcessExited;
                    processControl.Dock = DockStyle.Top;
                    PN_ProcessContainer.Controls.Add(processControl);
                    handedToControl.Add(process);
                }

                foreach (ProcessControl control in PN_ProcessContainer.Controls.OfType<ProcessControl>().ToArray())
                {
                    if (!displayedIds.Contains(control.ProcessId))
                    {
                        PN_ProcessContainer.Controls.Remove(control);
                        control.ReleaseProcess();
                        control.Dispose();
                    }
                }

                for (int i = 0; i < Processes.Count; i++)
                {
                    var found = PN_ProcessContainer.Controls.Find(Processes[i].Id.ToString(), false);
                    if (found.Any()) PN_ProcessContainer.Controls.SetChildIndex(found[0], i);
                }
            }
            catch (Exception ex) { Program.Write("Error updating process list: " + ex.Message); }
            finally
            {
                foreach (var process in allProcesses.Where(p => !handedToControl.Contains(p))) process.Dispose();
            }
        }

        private static long SafeWorkingSet(Process process)
        {
            try { return process.WorkingSet64; }
            catch { return 0; }
        }

        // Superseded by ProcessTimer_Tick. Retained temporarily only as a reference for the old implementation.
#if false
        private void InitializeProcessManager()
        {
            while (true)
            {
                try
                {
                    if (!MemoryHelper.SetIncreasePrivilege("SeDebugPrivilege"))
                    {
                        // Continue even if privilege elevation fails
                    }

                    Processes.Clear();
                    // Get accessible processes only
                    var allProcesses = new List<Process>();
                    foreach (var p in Process.GetProcesses())
                    {
                        try
                        {
                            if (p != null && !p.HasExited)
                            {
                                allProcesses.Add(p);
                            }
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            // Access denied - skip silently
                        }
                        catch (InvalidOperationException)
                        {
                            // Process exited - skip silently
                        }
                    }

                    // Sort by memory usage, handling access denied gracefully
                    Processes = allProcesses
                        .OrderByDescending(x =>
                        {
                            try { return x.WorkingSet64; }
                            catch { return 0; }
                        })
                        .Take(TOP_PROCESSES_COUNT)
                        .ToList();

                    Processes.Sort((n1, n2) =>
                    {
                        try
                        {
                            return MemoryHelper.GetUsedMemory(n1).CompareTo(MemoryHelper.GetUsedMemory(n2));
                        }
                        catch
                        {
                            return 0;
                        }
                    });

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
                                    processControl.Dock = DockStyle.Top;

                                    if (PN_ProcessContainer.InvokeRequired)
                                    {
                                        PN_ProcessContainer.BeginInvoke(new Action(() =>
                                        {
                                            try
                                            {
                                                PN_ProcessContainer.Controls.Add(processControl);
                                            }
                                            catch (Exception ex)
                                            {
                                                Program.Write($"Error adding process control: {ex.Message}");
                                            }
                                        }));
                                    }
                                    else
                                    {
                                        PN_ProcessContainer.Controls.Add(processControl);
                                    }
                                }
                            }
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            // Access denied - skip silently
                        }
                        catch (InvalidOperationException)
                        {
                            // Process exited - skip silently
                        }
                        catch (Exception ex)
                        {
                            // Only log unexpected errors
                            Program.Write("Unexpected error managing process control: " + ex.GetType().Name + ": " + ex.Message);
                        }
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
                        catch (System.ComponentModel.Win32Exception)
                        {
                            // Access denied - skip silently
                        }
                        catch (InvalidOperationException)
                        {
                            // Process exited - skip silently
                        }
                        catch (Exception ex)
                        {
                            // Only log unexpected errors
                            Program.Write("Unexpected error removing process control: " + ex.GetType().Name + ": " + ex.Message);
                        }
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
                        catch (System.ComponentModel.Win32Exception)
                        {
                            // Access denied - skip silently
                        }
                        catch (InvalidOperationException)
                        {
                            // Process exited - skip silently
                        }
                        catch (Exception ex)
                        {
                            // Only log unexpected errors
                            Program.Write("Unexpected error reordering process controls: " + ex.GetType().Name + ": " + ex.Message);
                        }
                    }

                    Thread.Sleep(PROCESS_CHECK_INTERVAL_MS);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Access denied errors are expected - don't log
                }
                catch (InvalidOperationException)
                {
                    // Process exited errors are expected - don't log
                }
                catch (Exception ex)
                {
                    // Only log unexpected errors
                    Program.Write("Unexpected error in process manager loop: " + ex.GetType().Name + ": " + ex.Message);
                }
            }
        }

#endif
        private void ProcessExited(object sender, UserControl e)
        {
            try
            {
                if (e != null && PN_ProcessContainer.Controls.Contains(e))
                {
                    if (PN_ProcessContainer.InvokeRequired)
                    {
                        PN_ProcessContainer.BeginInvoke(new Action(() =>
                        {
                            PN_ProcessContainer.Controls.Remove(e);
                            ((ProcessControl)e).ReleaseProcess();
                            e.Dispose();
                        }));
                    }
                    else
                    {
                        PN_ProcessContainer.Controls.Remove(e);
                        ((ProcessControl)e).ReleaseProcess();
                        e.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Write($"Error removing exited process control: {ex.Message}");
            }
        }

        private void PB_Icon_Click(object sender, EventArgs e)
        {

        }
    }
}
