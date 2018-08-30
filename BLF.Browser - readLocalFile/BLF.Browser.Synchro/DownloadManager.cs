using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BLF.Utility.Synchro
{
    
    /// <summary>
    /// 下载的管理器
    /// </summary>
    internal class DownloadManager
    {
        /// <summary>
        /// 账号于密码凭证
        /// </summary>
        private readonly ICredentials _credentials;
        /// <summary>
        /// 一次读取流最大大小
        /// </summary>
        private static readonly int BufferSize = 32768;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="credentials">账号于密码凭证</param>
        public DownloadManager(ICredentials credentials)
        {
            _credentials = credentials;
        }

        /// <summary>
        /// 下载文件到本地指定的路径
        /// </summary>
        /// <param name="file">WebDav实体</param>
        /// <param name="localfile">本地文件路径</param>
        /// <returns></returns>
        public bool DownloadFile(WebDavFile file, string localfile)
        {
            bool isDownloadSuccessfully = false;
            try
            {
                if (this.WebClientDownloadInstallerFile(file, localfile))
                {
                    isDownloadSuccessfully = true;
                }
            }
            catch
            {
                isDownloadSuccessfully = false;
            }

            return isDownloadSuccessfully;
        }



        /// <summary>
        /// 是否继续断点下载
        /// </summary>
        /// <param name="url">远程</param>
        /// <param name="localfile"></param>
        /// <returns>True：继续断点下载文件; false: 不断点下载</returns>
        private bool IsResume(string url, string localfile)
        {
            string tempFileName = localfile + ".temp";//本地文件的地址的临时地址
            string tempFileInfoName = localfile + ".temp.info";//保存着文件的ETag，用来判断远程文件是否发生改变
            bool resumeDowload = false;
            HttpWebResponse response = null;

            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Credentials = _credentials;
                response = (HttpWebResponse)request.GetResponseAsync().GetAwaiter().GetResult(); ;

                if (GetAcceptRanges(response))
                {
                    string newEtag = GetEtag(response);
                    if (File.Exists(tempFileName) && File.Exists(tempFileInfoName))
                    {
                        string oldEtag = File.ReadAllText(tempFileInfoName);
                        if (!string.IsNullOrEmpty(oldEtag) && !string.IsNullOrEmpty(newEtag) && newEtag == oldEtag)
                        {
                            resumeDowload = true;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(newEtag))
                        {
                            File.WriteAllText(tempFileInfoName, newEtag);
                        }
                    }
                }                
            }
            catch// (Exception ex)
            {
                // todo
            }
            finally
            {
                if (response != null)
                {
                    response.Dispose();
                }
            }
            return resumeDowload;
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="file">Webdav文件实例</param>
        /// <param name="localfile">下载文件地址</param>
        /// <returns>True：下载成功；false:下载不成功；</returns>
        private bool WebClientDownloadInstallerFile(WebDavFile file, string localfile)
        {
            string url = file.Url;

            bool resumeDownload = IsResume(url, localfile);
            string tempFileName = localfile + ".temp";
            bool isDownloadSuccessfully = false;
            FileMode fm = FileMode.Create;
            Stream stream = null;
            FileStream fileStream = null;
            HttpWebResponse response = null;
            try
            {
                Uri installerUrl = new Uri(url);
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.Credentials = _credentials;

                if (resumeDownload)
                {
                    //断点续传
                    FileInfo fn = new FileInfo(tempFileName);
                    httpWebRequest.Headers["Range"] = (new RangeHeaderValue(fn.Length, file.Length)).ToString();
                    fm = FileMode.Append;
                }

                response = (HttpWebResponse)httpWebRequest.GetResponseAsync().GetAwaiter().GetResult();
                stream = response.GetResponseStream();
        
                //获取响应流，写入到文件
                double contentLength = DownloadManager.GetContentLength(response);
                byte[] buffer = new byte[BufferSize];
                long downloadedLength = 0;
                int currentDataLength;
                fileStream = new FileStream(tempFileName, fm);

                while ((currentDataLength = stream.Read(buffer, 0, BufferSize)) > 0 )
                {
                    fileStream.Write(buffer, 0, currentDataLength);
                    downloadedLength += (long)currentDataLength;
                }

                if (currentDataLength >= 0)
                {
                    isDownloadSuccessfully = true;
                }
            }
            catch(Exception ex)
            {
                // todo
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Flush();
                    fileStream.Dispose();
                }
                if (stream != null)
                {
                    stream.Dispose();
                }

                if (response != null)
                {
                    response.Dispose();
                }
            }
            if (isDownloadSuccessfully)
            {
                if (File.Exists(localfile))
                {
                    Util.DeleteFileIfExists(localfile);
                }
                File.Move(tempFileName, localfile);

                string tempFileInfoName = localfile + ".temp.info";
                if (File.Exists(tempFileInfoName))
                {
                    Util.DeleteFileIfExists(tempFileInfoName);
                }
            }
            return isDownloadSuccessfully;
        }

        /// <summary>
        /// 获取请求流大小
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        private static double GetContentLength(HttpWebResponse res)
        {
            double result = 0.0;
            if (res.Headers["Content-Length"] != null)
            {
                string s = res.Headers["Content-Length"];
                if (!double.TryParse(s, out result))
                {
                    result = 0.0;
                }
            }
            return result;
        }

        /// <summary>
        /// 获取是否支持断点续传
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        private static bool GetAcceptRanges(WebResponse res)
        {
            if (res.Headers["Accept-Ranges"] != null)
            {
                string s = res.Headers["Accept-Ranges"];
                if (s == "none")
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 获取文件
        /// </summary>
        /// <param name="res"></param>
        /// <returns>True：文件</returns>
        private static string GetEtag(WebResponse res)
        {
            if (res.Headers["ETag"] != null)
            {
                return res.Headers["ETag"];
            }
            return null;
        }
    }
}
