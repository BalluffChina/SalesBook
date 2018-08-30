using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BLF.Utility.Synchro
{
    /// <summary>
    /// 通用文件类
    /// </summary>
    public class Util
    {
        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="filePath">文件地址</param>
        public static void DeleteFileIfExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException)
                {
                    File.Delete(filePath);
                }
                catch (UnauthorizedAccessException)
                {
                    FileAttributes attributes = File.GetAttributes(filePath);
                    if ((attributes & FileAttributes.ReadOnly).Equals(FileAttributes.ReadOnly))
                    {
                        File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
                        File.Delete(filePath);
                    }
                }
            }
        }
    }
}
