using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLF.Browser
{
    class BalluffConst
    {
        //User for control Name
        public class NamePrefix
        {
            public const string NamePrefix_Webview = "WebView";
            public const string NamePrefix_TabButton = "TabButton";
            public const string NamePrefix_CloseButton = "CloseButton";
            //To show PDF
            public const string NamePrefix_ScrollViewer = "ScrollViewer";
            public const string NamePrefix_TextBlock = "TextBlock";
        }
        public class TabButtonContent
        {
            public const string TabButtonContent_Http = "Balluff";
            public const string TabButtonContent_Local = "SalesBook";
        }

        public const string Loading = " Loading...";
        public class NewTabFlg
        {
            public const string Yes = "NewDlg=1";
            public const string No = "NewDlg=0";
        }
        public const string DefaultHomePagePath = "ms-appx-web:///BalluffBrowser/home.html";
        public const string LocalHomePagePath = "ms-appdata:///local/BLF/Data/home.html";
        public const string BalluffOfficialWebsite =  "http://www.balluff.com";
        //public const string HomePagePath = "ms-appdata:///C:\\test\\__-spin.html";
        public const string LocalFolderName = "BLF";
        public const string DataFolderName = "Data";
        public const string FavoritesPopupMessage = "Remove '{0}' from favorites.";
        public const string LocalPath = "ms-appdata:///local/BLF";
        public const string FavoritesListFileName = "FavoritesList.xml";
        public const string PDFListFileName = "PDFList.xml";
        public const string SynchroProgressMsg = " Downloading:{0}";
        public const string LogFolderName = "Log";
        public const string LocalVersionFolderName = "LocalVersion";
        public const string Domain = "BALLUFF-CHINA.COM";
        public const string Subject = "BALLUFF";
        public const string BlfSpecialAccount = "BalluffParterner";
        public class PDFFilesItem
        {
            public const string PDFFilesItemRootTagName = "PDFFiles";
            public const string PDFFilesItemTagName = "PDFFile";
            public const string PDFFilesItemElementUrl = "Url";
        }
        public class FavoritesItem
        {
            public const string FavoritesItemRootTagName = "FavoriteList";
            public const string FavoritesItemTagName = "Favorite";
            public const string FavoritesItemAttributeNote = "Note";
            public const string FavoritesItemAttributeUri = "Uri";
            public const string FavoritesItemFormat = "<{0} {1}=\"{2}\" {3}=\"{4}\"/>";
        }
        public class LocalSetting
        {
            public const string Account = "Account";
            public const string Password = "Password";
            public const string Version = "Version";
            public const string Progress = "Progress";
            public const string ActivationCode = "ActivationCode";
        }

        public class FilesType
        {
            public const string MP4 = ".mp4";
            public const string PDF = ".pdf";
            public const string HTML = ".html";
        }

        public class Message
        {
            public const string Alert_InputCredential = "Please input Account and Password";
            public const string Alert_IncorrectCredential = "Incorrect Account or Password, please connect admin";
            public const string Alert_IncorrectActivationCode = "Incorrect activation code.";
            public const string Alert_CurrentUser = "Current User: {0}";
            public const string Alert_AccountInfoAccess = "Access limited to Account Info. Please turn it on in the settings of Privacy.";
            public const string ErrorOccured_Synchro = "Error occured when Synchronizing.";
            public const string Alert_InputActivationCode = "Please input activation code";
            public const string Alert_NoSearchResults = "No results. Please change keywords.";
            public const string Alert_InputKeywords = "Please input keywords";
        }
    }
}
