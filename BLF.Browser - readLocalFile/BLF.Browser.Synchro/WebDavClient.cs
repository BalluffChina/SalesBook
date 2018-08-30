
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;

namespace BLF.Utility.Synchro
{
    /// <summary>
    /// 下载完成委托
    /// </summary>
    /// <param name="davFile">WebDav文件实体</param>
    /// <param name="index">正在下载第几个文件</param>
    /// <param name="total">总共几个文件</param>
    public delegate void DownloadCompleted(WebDavFile davFile, int index, int total);

    /// <summary>
    /// 下载中的委托
    /// </summary>
    /// <param name="davFile">WebDav 文件实体</param>
    /// <param name="index">正在下载第几个文件</param>
    /// <param name="total">总共几个文件</param>
    public delegate void DownloadCompleting(WebDavFile davFile, int index, int total);

    public class WebDavClient
    {
        /// <summary>
        /// 文件完成的事件
        /// </summary>
        public event DownloadCompleted DownloadCompleted;

        /// <summary>
        /// 文件正在下载事件
        /// </summary>
        public event DownloadCompleting DownloadCompling;

        #region WebDAV connection parameters

        private String server;

        /// <summary>
        /// 服务器的IP地址或域名地址
        /// </summary>
        public String Server
        {
            get { return server; }
            set
            {
                value = value.TrimEnd('/');
                server = value;
            }
        }

        private String _basePath = "/";

        /// <summary>
        // 定义WebDav相对目录地址，如目录：met(默认根网站地址: /)
        /// </summary>
        public String BasePath
        {
            get { return _basePath; }
            set
            {
                value = value.Trim('/');
                _basePath = "/" + value + "/";
            }
        }

        /// <summary>
        /// 端口号(default: null = auto-detect)
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public String User { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public String Pass { get; set; }

        public WebDavClient()
        {
            Domain = null;
            Port = null;
        }

        /// <summary>
        /// 域的名称，没有则不需要设置，留空;有则设置如：consoto
        /// </summary>
        public String Domain { get; set; }


        Uri GetRootUrl()
        {
            String completePath = _basePath;

            if (completePath.EndsWith("/") == false)
            {
                completePath += '/';
            }

            if (Port.HasValue)
            {
                return new Uri(server + ":" + Port + completePath);
            }
            else
            {
                return new Uri(server + completePath);
            }
        }

        Uri GetServerUrl(String path, Boolean appendTrailingSlash)
        {
            String completePath = _basePath;
            if (path != null)
            {
                completePath += path.Trim('/');
            }

            if (appendTrailingSlash && completePath.EndsWith("/") == false)
            {
                completePath += '/';
            }

            if (Port.HasValue)
            {
                return new Uri(server + ":" + Port + completePath);
            }
            else
            {
                return new Uri(server + completePath);
            }
        }

        #endregion

        #region WebDAV operations


        /// <summary>
        ///  获取WebDav远程地址的文件列表地址
        /// </summary>
        /// <param name="remoteFilePth">远程文件目录，如http://172.23.0.1/met<</param>
        /// <param name="filters">后缀名过滤</param>
        /// <returns>返回的所有的文件集合</returns>
        public List<WebDavFile> GetFiles(string remoteFilePth, params string[] filters)
        {
            List<WebDavFile> filesList = new List<WebDavFile>();
            Uri listUri = GetServerUrl(remoteFilePth, true);
            if (remoteFilePth.ToLower().StartsWith("http://") || remoteFilePth.StartsWith("https://"))
            {
                listUri = new Uri(remoteFilePth);
            }
            StringBuilder propfind = new StringBuilder();
            propfind.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            propfind.Append("<propfind xmlns=\"DAV:\">");
            propfind.Append("  <allprop/>");
            propfind.Append("</propfind>");

            IDictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Depth", "1");
            using (
                HttpWebResponse response = HttpRequest(listUri, "PROPFIND", headers,
                    Encoding.UTF8.GetBytes(propfind.ToString())))
            {
                using (Stream stream = response.GetResponseStream())
                {
                    System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
                    xml.Load(stream);
                    XmlNamespaceManager xmlNsManager = new XmlNamespaceManager(xml.NameTable);
                    xmlNsManager.AddNamespace("d", "DAV:");
                    Windows.Data.Xml.Dom.XmlDocument xmlDom = new Windows.Data.Xml.Dom.XmlDocument();
                    xmlDom.LoadXml(xml.OuterXml);
                    Windows.Data.Xml.Dom.XmlNodeList listNodes = xmlDom.DocumentElement.ChildNodes;

                    for (int i = 0; i < listNodes.Count; i++)
                    {
                        IXmlNode node = listNodes[i];

                        IXmlNode first = node.ChildNodes.Where(x => x.NodeName.ToLower() == "d:href").FirstOrDefault();

                        if (first.InnerText == remoteFilePth) continue;

                        WebDavFile oWebDavFile = new WebDavFile { Url = first.InnerText };
                        IXmlNode secNode = node.ChildNodes.Where(x => x.NodeName.ToLower() == "d:propstat").FirstOrDefault();

                        IXmlNode propsNode = secNode.ChildNodes.Where(x => x.NodeName.ToLower() == "d:prop").FirstOrDefault();
                        //文件的显示名
                        IXmlNode attrNode = propsNode.ChildNodes.Where(x => x.NodeName.ToLower() == "d:displayname")
                            .FirstOrDefault();

                        oWebDavFile.DispalyName = attrNode.InnerText;

                        if (oWebDavFile.DispalyName == remoteFilePth) continue;
                        //文件的内容类型
                        attrNode = propsNode.ChildNodes.Where(x => x.NodeName.ToLower() == "d:getcontenttype")
                          .FirstOrDefault();

                        oWebDavFile.ContentType = attrNode.InnerText;

                        //文件的最后修改时间
                        attrNode = propsNode.ChildNodes.Where(x => x.NodeName.ToLower() == "d:getlastmodified")
                          .FirstOrDefault();

                        oWebDavFile.LastModifyDate = DateTime.Parse(attrNode.InnerText);

                        //文件的创建时间
                        attrNode = propsNode.ChildNodes.Where(x => x.NodeName.ToLower() == "d:creationdate")
                        .FirstOrDefault();

                        oWebDavFile.CreateDate = DateTime.Parse(attrNode.InnerText);

                        //文件的大小
                        attrNode = propsNode.ChildNodes.Where(x => x.NodeName.ToLower() == "d:getcontentlength")
                        .FirstOrDefault();

                        oWebDavFile.Length = long.Parse(attrNode.InnerText);

                        filesList.Add(oWebDavFile);
                    }
                }
            }

            List<WebDavFile> folderDavFiles = filesList.Where(x => x.Length == 0 && x.Url.EndsWith("/")).ToList();
            foreach (WebDavFile file in folderDavFiles)
            {
                if (file.Length == 0 && file.Url.EndsWith("/"))
                {
                    filesList.AddRange(GetFiles(file.Url));
                }
            }

            var q = filesList.Where(x => x.Length != 0);
            if (filters != null)
            {
                q = filters.Aggregate(q, (current, filter) => current.Where(x => !x.Url.ToLower().EndsWith(filter.ToLower())));
            }

            return q.ToList();
        }


        /// <summary>
        /// 身份凭证
        /// </summary>
        /// <returns></returns>
        private ICredentials GetCredentials()
        {
            return Domain != null ? new NetworkCredential(User, Pass, Domain) : new NetworkCredential(User, Pass);
        }

        private HttpWebResponse HttpRequest(Uri uri, string requestMethod, IDictionary<string, string> headers,
            byte[] content)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            /*

                System.Net.ServicePointManager.ServerCertificateValidationCallback +=
                delegate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors sslError)
                {
                        bool validationResult = true;
                        return validationResult;
                };
           */
            // The server may use authentication
            if (User != null && Pass != null)
            {
                httpWebRequest.Credentials = GetCredentials();
                //httpWebRequest.PreAuthenticate = true;
            }
            httpWebRequest.Method = requestMethod;

            if (headers != null)
            {
                foreach (string key in headers.Keys)
                {
                    httpWebRequest.Headers[key] = headers[key];
                }
            }

            using (Stream streamResponse = httpWebRequest.GetRequestStreamAsync().GetAwaiter().GetResult())
            {
                if (content != null)
                {
                    streamResponse.Write(content, 0, content.Length);
                }
            }
            HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponseAsync().GetAwaiter().GetResult();
            return response;
        }


        /// <summary>
        /// 下载所有files文件集合到指定的文件夹里
        /// </summary>
        /// <param name="files">所有需要下载的文件清单</param>
        /// <param name="defaultFolder">下载本地的文件夹</param>
        /// <param name="bDelete">true:表示比较两边的文件，删除两边不一致的文件，false：则不做处理</param>
        /// <returns>每一个下载是否成功的集合</returns>
        public List<DownloadInfo> Download(List<WebDavFile> files, string defaultFolder, bool bDelete = false)
        {

            var count = files.Count;
            var downloadInfos = files.Select((t, i) => Download(t, defaultFolder, i + 1, count)).ToList();

            if (bDelete)
            {
                DirectoryInfo rootDirectoryInfo = new DirectoryInfo(defaultFolder);

                if (rootDirectoryInfo.Exists)
                {
                    DeleteFiles(files, rootDirectoryInfo);
                }
            }
            return downloadInfos;
        }

        /// <summary>
        /// 删除本地文件夹内在远程地址不存在的文件
        /// </summary>
        /// <param name="files">WebDavaFile 集合</param>
        /// <param name="rootFolder">网站目录地址</param>
        private void DeleteFiles(List<WebDavFile> files, DirectoryInfo rootFolder)
        {

            //删除不对应的文件
            foreach (FileInfo file in rootFolder.GetFiles())
            {
                var oFile = file;
                var q = files.Where(x => x.DownloadPath.ToLower() == oFile.FullName.ToLower());
                if (!q.Any())
                {
                    Util.DeleteFileIfExists(oFile.FullName);
                }
            }

            //删除不对应的目录
            var folders = files.Where(x => x.DownloadFolderPath.ToLower().StartsWith(rootFolder.FullName.ToLower()));
            if (!folders.Any())
            {
                Directory.Delete(rootFolder.FullName, true);
            }
            else
            {
                foreach (DirectoryInfo directoryInfo in rootFolder.GetDirectories())
                {
                    DeleteFiles(files, directoryInfo);
                }
            }
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        /// <param name="path">目录的地址</param>
        private async void CreateFolder(string path)
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(path);
            });
        }
        /// <summary>
        /// 为远程文件创建对应的文件夹
        /// </summary>
        /// <param name="url"></param>
        /// <param name="defaultFolder"></param>
        /// <returns></returns>
        private string CreateFolder(string url, string defaultFolder)
        {
            DirectoryInfo rootFolder = new DirectoryInfo(defaultFolder);
            if (!rootFolder.Exists)
            {
                CreateFolder(defaultFolder);
            }
            string urldecode = WebUtility.UrlDecode(url);
            if (urldecode != null)
            {
                string[] strs = urldecode.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                string strfolder = defaultFolder;
                for (int i = 2; i < strs.Length - 1; i++)
                {
                    strfolder += "\\" + strs[i];
                }

                rootFolder = new DirectoryInfo(strfolder);
                if (!rootFolder.Exists)
                {
                    //Directory.CreateDirectory(strfolder);
                    CreateFolder(strfolder);
                }
                return strfolder;
            }
            return defaultFolder;
        }

        /// <summary>
        /// 下载一个远程文件到指定的文件路径里
        /// </summary>
        /// <param name="davFile">一个WebDavFile的实体</param>
        /// <param name="localFile">本地的文件地址</param>
        /// <returns></returns>
        private DownloadInfo DownloadFile(WebDavFile davFile, string localFile)
        {

            DownloadInfo info = new DownloadInfo { FileUrl = davFile.Url };
            try
            {
                DownloadManager manager = new DownloadManager(GetCredentials());
                string remoteFile = davFile.Url;
                bool result = false;
                if (File.Exists(localFile))
                {
                    FileInfo fm = new FileInfo(localFile);
                    //判断本地文件是否和远程的一样
                    if (davFile.LastModifyDate == fm.LastWriteTime && davFile.Length == fm.Length)
                    {
                        info.Success = true;
                        result = true;
                    }
                }
                if (!info.Success)
                {
                    string folder = localFile.Substring(0, localFile.LastIndexOf("\\") + 1);

                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    FileInfo file = new FileInfo(localFile);

                    if (file.Directory != null) Directory.CreateDirectory(file.Directory.FullName);
                    //下载文件到指定的地址
                    result = manager.DownloadFile(davFile, localFile);

                    //更改文件最后修改时间
                    if (davFile.LastModifyDate != null) File.SetLastWriteTime(localFile, davFile.LastModifyDate.Value);

                    info.Success = true;
                }
            }
            catch (Exception ex)
            {
                info.Success = false;
                info.Message = ex.Message;
            }
            return info;
        }


        /// <summary>
        /// 下载一个远程文件到指定的文件夹里，会根据远程文件的地址创建对应的目录结构
        /// </summary>
        /// <param name="file"></param>
        /// <param name="defaultFoder"></param>
        /// <param name="index">当前下载第几个</param>
        /// <param name="total">总共文件数</param>
        /// <returns></returns>
        private DownloadInfo Download(WebDavFile file, string defaultFoder, int index = 1, int total = 1)
        {
            string folderPath = CreateFolder(file.Url, defaultFoder);
            string localFile = folderPath + "\\" + file.DispalyName;
            file.DownloadPath = localFile;
            file.DownloadFolderPath = folderPath;
            //如若DownloadCompling不为空，执行相关的事件
            DownloadCompling?.Invoke(file, index, total);
            //下载文件到本地
            DownloadInfo result = DownloadFile(file, localFile);
            // //DownloadCompleted，执行相关的事件
            DownloadCompleted?.Invoke(file, index, total);

            return result;
        }

        /// <summary>
        /// 获取需要更新的文件清单
        /// </summary>
        /// <param name="remoteFilePth">远程目录地地址</param>
        /// <param name="defaultFoder">本地文件路径</param>
        /// <param name="filters">后缀名过滤</param>
        /// <returns></returns>
        public List<WebDavFile> GetUpdatedFiles(string remoteFilePth, string defaultFoder, params string[] filters)
        {
            List<WebDavFile> files = GetFiles(remoteFilePth, filters);

            for (int i = files.Count - 1; i > -1; i--)
            {
                string folderPath = CreateFolder(files[i].Url, defaultFoder);
                string localFile = folderPath + "\\" + files[i].DispalyName;
                FileInfo fm = new FileInfo(localFile);

                if (files[i].LastModifyDate == fm.LastWriteTime && files[i].Length == fm.Length)
                {
                    files.RemoveAt(i);
                }
            }

            return files;
        }

        #endregion


    }
}
