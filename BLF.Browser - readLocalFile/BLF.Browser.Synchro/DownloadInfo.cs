using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BLF.Utility.Synchro
{
    /// <summary>
    /// 下载信息结果类
    /// </summary>
    public class DownloadInfo
    {
        /// <summary>
        /// 是否成功，True：表示下载成功，False：表示下载失败
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 下载文件的URL地址
        /// </summary>
        public string FileUrl { get; set; }

        /// <summary>
        /// 信息，下载失败返回的错误信息
        /// </summary>
        public string Message { get; set; }
    }
}
