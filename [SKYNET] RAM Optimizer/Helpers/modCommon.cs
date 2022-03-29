using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using TsudaKageyu;

namespace SKYNET
{
    public class modCommon
    {
        public static string LongToMbytes(long lBytes)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string str1 = "Bytes";
            if (lBytes > 1024L)
            {
                string str2;
                float num;
                if (lBytes < 1048576L)
                {
                    str2 = "KB";
                    num = Convert.ToSingle(lBytes) / 1024f;
                }
                else
                {
                    str2 = "MB";
                    num = Convert.ToSingle(lBytes) / 1048576f;
                }
                stringBuilder.AppendFormat("{0:0.0} {1}", (object)num, (object)str2);
            }
            else
            {
                float num = Convert.ToSingle(lBytes);
                stringBuilder.AppendFormat("{0:0} {1}", (object)num, (object)str1);
            }
            return stringBuilder.ToString();
        }

        internal static void Show(object v)
        {
            MessageBox.Show(v.ToString());
        }

        public static Image IconFromFile(string filePath)
        {
            Image image = null;

            try
            {
                var extractor = new IconExtractor(filePath);
                var icon = extractor.GetIcon(0);

                Icon[] splitIcons = IconUtil.Split(icon);

                Icon selectedIcon = null;

                foreach (var item in splitIcons)
                {
                    if (selectedIcon == null)
                    {
                        selectedIcon = item;
                    }
                    else
                    {
                        if (IconUtil.GetBitCount(item) > IconUtil.GetBitCount(selectedIcon))
                        {
                            selectedIcon = item;
                        }
                        else if (IconUtil.GetBitCount(item) == IconUtil.GetBitCount(selectedIcon) && item.Width > selectedIcon.Width)
                        {
                            selectedIcon = item;
                        }
                    }
                }
                return selectedIcon.ToBitmap();
            }
            catch (Exception)
            {

            }

            try
            {
                image = Icon.ExtractAssociatedIcon(filePath)?.ToBitmap();
            }
            catch
            {
                image = new Icon(SystemIcons.Application, 256, 256).ToBitmap();
            }

            return image;
        }
        public static string GetPath()
        {
            Process currentProcess;
            try
            {
                currentProcess = Process.GetCurrentProcess();
                return new FileInfo(currentProcess.MainModule.FileName).Directory?.FullName;
            }
            finally { currentProcess = null; }
        }
    }
}