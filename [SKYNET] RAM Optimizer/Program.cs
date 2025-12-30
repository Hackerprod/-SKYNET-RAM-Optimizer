using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SKYNET
{
    static class Program
    {
        private static string FileLogLocation;
        private static Mutex mutexFile = new Mutex(false, "LogMutex");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Setup global exception handlers FIRST
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += UIThreadExceptionHandler;
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

            if (!IsAdmin())
            {
                RestartAsAdmin();
                return;
            }

            FileLogLocation = Path.Combine(modCommon.GetPath(), "[SKYNET] RAM Optimizer.log");

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new frmMain());
            }
            catch (Exception ex)
            {
                WriteException("FATAL ERROR in Main", ex);
                MessageBox.Show("A fatal error occurred. Please check the log file for details.\n\n" + ex.Message,
                    "SKYNET RAM Optimizer - Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static bool IsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal p = new WindowsPrincipal(id);
            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }
        public static void RestartAsAdmin()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Application.ExecutablePath;
            startInfo.Verb = "runas";
            try
            {
                Process p = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Write("Failed to restart as admin: " + ex.Message);
            }
            Environment.Exit(0);
        }

        public static void UIThreadExceptionHandler(object sender, ThreadExceptionEventArgs t)
        {
            try
            {
                WriteException("UI Thread Exception", t.Exception);

                // Try to show message to user
                var result = MessageBox.Show(
                    "An error occurred in the UI thread. Continue running?\n\n" + t.Exception.Message,
                    "SKYNET RAM Optimizer - Error",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error);

                if (result == DialogResult.No)
                {
                    Application.Exit();
                }
            }
            catch
            {
                // Last resort - just try to write something
                try { File.AppendAllText(FileLogLocation, $"{DateTime.Now}: CRITICAL - Exception handler failed\r\n"); } catch { }
            }
        }

        public static void UnhandledExceptionHandler(object sender, System.UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                {
                    WriteException("Unhandled Exception", ex);
                }
                else
                {
                    Write("Unhandled non-exception object: " + e.ExceptionObject?.ToString());
                }

                if (e.IsTerminating)
                {
                    Write("Application is terminating due to unhandled exception");
                }
            }
            catch
            {
                // Last resort
                try { File.AppendAllText(FileLogLocation, $"{DateTime.Now}: CRITICAL - Unhandled exception handler failed\r\n"); } catch { }
            }
        }

        private static void TaskSchedulerOnUnobservedTaskException(object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                WriteException("Unobserved Task Exception", e.Exception);
                e.SetObserved(); // Prevent process termination
            }
            catch
            {
                // Last resort
                try { File.AppendAllText(FileLogLocation, $"{DateTime.Now}: CRITICAL - Task exception handler failed\r\n"); } catch { }
            }
        }

        public static void WriteException(string context, Exception ex)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("================================================================================");
                sb.AppendLine($"EXCEPTION: {context}");
                sb.AppendLine($"Time: {DateTime.Now}");
                sb.AppendLine($"Type: {ex.GetType().FullName}");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"Stack Trace:");
                sb.AppendLine(ex.StackTrace);

                if (ex.InnerException != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("--- Inner Exception ---");
                    sb.AppendLine($"Type: {ex.InnerException.GetType().FullName}");
                    sb.AppendLine($"Message: {ex.InnerException.Message}");
                    sb.AppendLine($"Stack Trace:");
                    sb.AppendLine(ex.InnerException.StackTrace);
                }

                sb.AppendLine("================================================================================");
                sb.AppendLine();

                AppendFile(sb.ToString(), FileLogLocation);
            }
            catch (Exception writeEx)
            {
                // Last resort - try simple write
                try
                {
                    File.AppendAllText(FileLogLocation,
                        $"{DateTime.Now}: EXCEPTION WRITE FAILED: {writeEx.Message}\r\nOriginal: {ex.Message}\r\n");
                }
                catch { }
            }
        }
        public static void Write(object msg)
        {
            if (msg is Exception)
            {
                Exception ex = (Exception)msg;
                string Message = ex.Message + ": " + ex.StackTrace;
                Write(Message, FileLogLocation);
                return;
            }
            else
                Write(msg, FileLogLocation);
        }
        public static void Write(object msg, string Filename)
        {
            string returns = "";

            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(msg);
                returns = string.Format($"{(object)stringBuilder.ToString()}:");
                AppendFile(returns, Filename);
            }
            catch (Exception ex)
            {
                // Can't log errors in the logger itself, so we silently fail
                System.Diagnostics.Debug.WriteLine("Logging failed: " + ex.Message);
            }
        }
        public static void AppendFile(string s, string fname)
        {
            string path = Path.Combine(Application.StartupPath, fname);
            StreamWriter streamWriter = null;
            try
            {
                mutexFile = new Mutex(false, "LogMutex");
                mutexFile.WaitOne();
                FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write);
                streamWriter = new StreamWriter(stream);
                streamWriter.BaseStream.Seek(0L, SeekOrigin.End);
                streamWriter.WriteLine(Conversions.ToString(DateAndTime.Now) + ": " + s);
                streamWriter.Close();
            }
            catch (Exception ex)
            {
                streamWriter?.Close();
                System.Diagnostics.Debug.WriteLine("AppendFile failed: " + ex.Message);
            }
            finally
            {
                mutexFile.ReleaseMutex();
            }
        }
    }
}
