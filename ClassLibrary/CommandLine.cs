using System;
using System.Diagnostics;
using System.IO;

namespace ClassLibrary
{
    public static class CommandLine
    {
        /// <summary>
        /// 调用命令行进行目录的读取
        /// </summary>
        /// <param name="cwd">想要读取的路径</param>
        /// <returns>路径下的文件和文件夹</returns>
        public static string Dir(string cwd)
        {
            using (Process p = new Process())
            {
                //初始化
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                //启动并读取结果
                p.Start();
                p.StandardInput.AutoFlush = true;
                p.StandardInput.WriteLine("dir \"" + cwd + "\" & exit");
                //对结果进行处理
                string[] result = p.StandardOutput.ReadToEnd().Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                string output = "";
                for (int i = 5; i < result.Length; i++)
                {
                    output += result[i] + "\n\r";
                    if (i == 5)
                        output += "\n\r";
                }
                p.WaitForExit();
                p.Close();
                return output;
            }
        }
        /// <summary>
        /// 借用IO库进行目录导航
        /// </summary>
        /// <param name="op">目的路径</param>
        public static void Cd(string op) => Directory.SetCurrentDirectory(op);
    }
}
