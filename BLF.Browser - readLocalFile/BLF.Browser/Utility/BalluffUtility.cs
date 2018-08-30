using BLF.Utility.Synchro;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace BLF.Browser
{
    public class BalluffUtility
    {
        static public string GetNum(string strName, string strPrefixName)
        {
            return strName.Substring(strPrefixName.Length, strName.Length - strPrefixName.Length);
        }

        static public async void ComposeEmail(string messageBody, string filename)
        {
            var emailMessage = new Windows.ApplicationModel.Email.EmailMessage();
            emailMessage.Subject = BalluffConst.Subject + " " + filename;
            emailMessage.Body = messageBody;
            await Windows.ApplicationModel.Email.EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }

        static public async void LoadPDFToControl(PdfDocument pdfDoc, ObservableCollection<BitmapImage> PdfPages)
        {
            PdfPages.Clear();

            for (uint i = 0; i < pdfDoc.PageCount; i++)
            {
                BitmapImage image = new BitmapImage();

                var page = pdfDoc.GetPage(i);

                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    await page.RenderToStreamAsync(stream);
                    await image.SetSourceAsync(stream);
                }

                PdfPages.Add(image);
            }
        }
        static public StorageFile GetLocalFile(string path)
        {
            string localFolderPath = ApplicationData.Current.LocalFolder.Path;

            StorageFile f = StorageFile.GetFileFromPathAsync(localFolderPath + @"\" + BalluffConst.LocalFolderName + path.Replace("/", @"\")).AsTask().GetAwaiter().GetResult();
            return f;
        }

        static public IEnumerable<Favorites> RemoveFavorites(IEnumerable<Favorites> IEnumerableCol, string note)
        {
            var tempList = IEnumerableCol.ToList();
            for (int i = 0; i < tempList.Count; i++)
            {
                Favorites listItem = tempList[i];
                if (listItem.Note == note)
                {
                    tempList.Remove(listItem);
                    break;
                }

            }
            return tempList;
        }

        static public IEnumerable<Favorites> AddToFavorites(IEnumerable<Favorites> IEnumerableCol, Favorites item)
        {
            List<Favorites> tempList = new List<Favorites>();
            if (IEnumerableCol == null)
            {
                tempList.Add(item);
            }
            else
            {
                tempList = IEnumerableCol.ToList();
                tempList.Add(item);
            }
            return tempList;
        }

        static public IEnumerable<SearchResults> AddToSearchResults(IEnumerable<SearchResults> IEnumerableCol, SearchResults item)
        {
            List<SearchResults> tempList = new List<SearchResults>();
            if (IEnumerableCol == null)
            {
                tempList.Add(item);
            }
            else
            {
                tempList = IEnumerableCol.ToList();
                tempList.Add(item);
            }
            return tempList;
        }

        static public void CreateFoldersWhenNotExist()
        {
            bool hasLocalFolder = false;
            bool hasLogFolder = false;
            StorageFolder localfolder = ApplicationData.Current.LocalFolder;
            var folders = localfolder.GetFoldersAsync().AsTask().GetAwaiter().GetResult();
            foreach (var folder in folders)
            {
                if (folder.Name == BalluffConst.LocalFolderName)
                {
                    hasLocalFolder = true;
                }
                if (folder.Name == BalluffConst.LogFolderName)
                {
                    hasLogFolder = true;
                }

            }
            if (!hasLocalFolder)
            {
                localfolder.CreateFolderAsync(BalluffConst.LocalFolderName).AsTask().GetAwaiter();
            }
            if (!hasLogFolder)
            {
                localfolder.CreateFolderAsync(BalluffConst.LogFolderName).AsTask().GetAwaiter();
            }

        }

        static public string ReadLog()
        {
            string content = "";
            StorageFolder localfolder = ApplicationData.Current.LocalFolder;
            StorageFolder logFolder = localfolder.GetFolderAsync(BalluffConst.LogFolderName).AsTask().GetAwaiter().GetResult();
            string logFileName = "log_" + DateTime.Now.ToString("yyyyMMdd") + ".log";
            StorageFile logFileToday = null; ;
            var logfiles = logFolder.GetFilesAsync().AsTask().GetAwaiter().GetResult();
            foreach (var logfile in logfiles)
            {
                if (logfile.Name == logFileName)
                {
                    logFileToday = logfile;
                    break;
                }
            }
            if (logFileToday == null)
            {
                logFileToday = logFolder.CreateFileAsync(logFileName).AsTask().GetAwaiter().GetResult();
                return "";
            }
            using (Stream stearm = logFileToday.OpenStreamForReadAsync().GetAwaiter().GetResult())
            {
                using (StreamReader read = new StreamReader(stearm))
                {
                    content = read.ReadToEnd();
                }
            }
            return content;

        }

        static public void SaveCredentialAndVersion(string account, string password, string version)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[BalluffConst.LocalSetting.Account] = account;
            localSettings.Values[BalluffConst.LocalSetting.Password] = password;
            localSettings.Values[BalluffConst.LocalSetting.Version] = version;
        }

        static public async void RemoveLocalContent()
        {
            StorageFolder localfolder = ApplicationData.Current.LocalFolder;
            StorageFolder blfBrowserFolder = localfolder.GetFolderAsync(BalluffConst.LocalFolderName).AsTask().GetAwaiter().GetResult();
            await blfBrowserFolder.DeleteAsync();
            SaveCredentialAndVersion("", "", "");
        }

        /**   
       * 字符串转换成十六进制字符串  
       * @param String str 待转换的ASCII字符串  
       * @return String 每个Byte之间空格分隔，如: [61 6C 6B]  
       */
        static public String str2HexStr(String str)
        {

            char[] chars = "0123456789ABCDEF".ToCharArray();
            StringBuilder sb = new StringBuilder("");
            byte[] bs = Encoding.ASCII.GetBytes(str);
            int bit;

            for (int i = 0; i < bs.Length; i++)
            {
                bit = (bs[i] & 0x0f0) >> 4;
                sb.Append(chars[bit]);
                bit = bs[i] & 0x0f;
                sb.Append(chars[bit]);
            }
            return sb.ToString().Trim().Replace('0', 'G').Replace('1', 'H').Replace('2', 'I').Replace('3', 'J').Replace('4', 'K').Replace('5', 'L').Replace('6', 'M').Replace('7', 'N').Replace('8', 'O').Replace('9', 'P');
        }

        /**   
         * 十六进制转换字符串  
         * @param String str Byte字符串(Byte之间无分隔符 如:[616C6B])  
         * @return String 对应的字符串  
         */
        static public String hexStr2Str(String hexStr)
        {
            hexStr = hexStr.Replace('G', '0').Replace('H', '1').Replace('I', '2').Replace('J', '3').Replace('K', '4').Replace('L', '5').Replace('M', '6').Replace('N', '7').Replace('O', '8').Replace('P', '9');
            String str = "0123456789ABCDEF";
            char[] hexs = hexStr.ToCharArray();
            byte[] bytes = new byte[hexStr.Length / 2];
            int n;

            for (int i = 0; i < bytes.Length; i++)
            {
                n = str.IndexOf(hexs[2 * i]) * 16;
                n += str.IndexOf(hexs[2 * i + 1]);
                bytes[i] = (byte)(n & 0xff);
            }
            return System.Text.Encoding.ASCII.GetString(bytes);
        }
        static async public void DeleteFavoritesFile(StorageFile file)
        {
            try
            {
                if (file != null)
                {
                    await file.DeleteAsync();
                }
            }
            catch
            {

            }
        }

    }
}
