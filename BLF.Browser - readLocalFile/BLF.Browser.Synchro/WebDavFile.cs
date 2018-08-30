using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BLF.Utility.Synchro
{
    public class WebDavFile
    {
        /// <summary>
        /// 文件的显示名称
        /// </summary>
        public string DispalyName { get; set; }

        /// <summary>
        /// 文件的远程地址
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 文件的修改时间
        /// </summary>
        public DateTime? LastModifyDate { get; set; }

        /// <summary>
        /// 文件的创建时间
        /// </summary>
        public DateTime? CreateDate { get; set; }

        /// <summary>
        /// 文件的大小
        /// </summary>
        public long Length { get; set; }


        /// <summary>
        /// 文件的类型
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// 下载的本地路径
        /// </summary>
        public string DownloadPath { get; set; }


        /// <summary>
        /// 下载文件夹地址
        /// </summary>
        public string DownloadFolderPath { get; set; }

    }
}
