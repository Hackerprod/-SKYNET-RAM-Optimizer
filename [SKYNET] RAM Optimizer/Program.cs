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
            if (!IsAdmin())
            {
                RestartAsAdmin();
            }

            FileLogLocation = Path.Combine(modCommon.GetPath(), "[SKYNET] RAM Optimizer.log");

            Application.ThreadException += UIThreadExceptionHandler; ;
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
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
            catch { }
            Process.GetCurrentProcess().Kill();
        }

        public static void UIThreadExceptionHandler(object sender, ThreadExceptionEventArgs t)
        {
            Write(t.Exception);
        }

        public static void UnhandledExceptionHandler(object sender, System.UnhandledExceptionEventArgs t)
        {
            Write(t.ExceptionObject);
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
            catch { }
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
            }
            finally
            {
                mutexFile.ReleaseMutex();
            }
        }
    }
}
