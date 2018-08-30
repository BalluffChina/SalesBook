using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BLF.Utility.Synchro
{
    /// <summary>
    /// 日志文件类
    /// </summary>
    internal class Logging
    {
        /// <summary>
        /// 文件地址
        /// </summary>
        private string _fileName = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="file">文件地址</param>
        public Logging(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }
            _fileName = file;
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="logMessage">日志信息</param>
        public void Log(string logMessage)
        {
            Log(_fileName, logMessage);
        }
        /// <summary>
        /// 写日历
        /// </summary>
        /// <param name="logMessage">日志信息</param>
        /// <param name="w">写入流</param>
        private static void Log(string logMessage, TextWriter w)
        {
            w.WriteLine("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            w.WriteLine("  :{0}", logMessage);
            w.WriteLine("-------------------------------");
        }
        /// <summary>
        /// 写入指定的信息到指定的文件下
        /// </summary>
        /// <param name="fileName">文件地址</param>
        /// <param name="logMessage">日志信息</param>
        public static void Log(string fileName, string logMessage)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("log file name is not validated!");
            }

            using (StreamWriter w = File.AppendText(fileName))
            {
                Log(logMessage, w);
            }
        }
    }
}
