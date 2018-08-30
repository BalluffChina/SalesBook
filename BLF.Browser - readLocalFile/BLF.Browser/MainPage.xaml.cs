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


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BLF.Browser
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private int nTabCounter = 1;
        //private int nWebViewNum = 1;
        private int nNewPageNum = 0;
        private string strCurrentWebviewName = "WebView0";
        private string strCurrentTabButtonName = "TabButton0";
        private string strCurrentCloseButtonName = "CloseButton0";
        private string strCurrentScrollViewerName = "ScrollViewer0";
        private double dTabButtonWidth;
        private double dCloseButtonWidth;
        WebView currentView = new WebView();
        Button currentTabButton = new Button();
        Button currentCloseButton = new Button();
        ScrollViewer currentScrollViewer = new ScrollViewer();
        private bool newWebSiteFlg = false;
        private Button lockedTabButton = null;
        private Button lockedCloseButton = null;
        public IEnumerable<Favorites> FavoritesList { get; set; }
        public IEnumerable<SearchResults> SearchResultsList { get; set; }
        private string favoritesNoteForDelete = "";
        public StorageFile favoritesListConfig = null;
        public StorageFile PDFListConfig = null;
        private bool existLocalHome = false;
        public string historyContent = "";
        private double synchroProgress = 0;
        private int interval = 3;
        private string newVersion = "";
        private string synchroProgressMsg = "";
        private bool hasNotFoundPDFError = false;
        private bool hasGoneback = false;
        WebView errorWebview = null;
        WebView newWebviewFrom = null;

        public MainPage()
        {            
            this.InitializeComponent();
            try
            {
                this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
                //this.webview0.ContentLoading += Webview_ContentLoading;
                //this.webview0.DOMContentLoaded += Webview_DOMContentLoaded;
                //this.webview0.NavigationStarting += Webview_NavigationStarting;
                //this.webview0.NavigationCompleted += Webview_NavigationCompleted;
                currentView = (WebView)this.FindName(strCurrentWebviewName);
                currentView.ContentLoading += Webview_ContentLoading;
                currentView.DOMContentLoaded += Webview_DOMContentLoaded;
                currentView.NavigationStarting += Webview_NavigationStarting;
                currentView.NavigationCompleted += Webview_NavigationCompleted;
                SetExistLocalHome();
                currentTabButton = (Button)this.FindName(strCurrentTabButtonName);
                dTabButtonWidth = currentTabButton.Width;
                currentCloseButton = (Button)this.FindName(strCurrentCloseButtonName);
                dCloseButtonWidth = currentCloseButton.Width;
                this.currentScrollViewer = null;
                BalluffUtility.CreateFoldersWhenNotExist();
                //
                //Credential error,then delete local content
                CheckCredentialAndVersion();
                //CheckDomainUser();

                historyContent = BalluffUtility.ReadLog();
               
            }catch(Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);
                
            }

        }

        private void Webview_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            try
            {
                //throw new NotImplementedException();
                if (args.IsSuccess)
                {
                    statusTextBlock.Text = "Completed navigation...";

                    if(sender != null)
                    {
                        ResetCurrentTabButtonContent(sender);
                    }                   
                    HideTextBlock();

                    //when new tab,  previous site goback 2017/10/26 start
                    if (newWebSiteFlg)
                    {
                        if (sender.CanGoBack)
                        {
                            sender.GoBack();
                            newWebSiteFlg = false;
                            if (lockedTabButton != null)
                            {
                                lockedTabButton.IsEnabled = true;
                                lockedCloseButton.Visibility = Visibility.Visible;
                            }

                        }

                    }
                    //when new tab,  previous site goback 2017/10/26 end

                    //logic for args.IsSuccess=false, it must goback then goforward
                    if (hasGoneback && newWebviewFrom != null)
                    {
                        if (newWebviewFrom.CanGoForward)
                        {
                            newWebviewFrom.GoForward();
                            newWebviewFrom = null;
                            hasGoneback = false;
                        }
                    }

                }
                else
                {
                   statusTextBlock.Text = "Navigation failed with error: " + args.WebErrorStatus.ToString();
                    //logic for args.IsSuccess=false, it must goback then goforward
                    if (newWebSiteFlg)
                    {
                        newWebSiteFlg = false;
                        if (lockedTabButton != null)
                        {
                            lockedTabButton.IsEnabled = true;
                            lockedCloseButton.Visibility = Visibility.Visible;
                        }
                        if(newWebviewFrom != null)
                        {
                            if (newWebviewFrom.CanGoBack)
                            {
                                newWebviewFrom.GoBack();
                                hasGoneback = true;
                            }
                        }
                    }
                }
                //logic for PDF not found, it must goback then goforward
                if (hasNotFoundPDFError)
                {
                    if(errorWebview == sender)
                    {
                        if(hasGoneback)
                        {
                            if (sender.CanGoForward)
                            {
                                sender.GoForward();
                                hasNotFoundPDFError = false;
                                errorWebview = null;
                                hasGoneback = false;
                            }
                            
                        }
                        else
                        {
                            if (sender.CanGoBack)
                            {
                                sender.GoBack();
                                hasGoneback = true;
                            }
                        }                       
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
         }

        private void Webview_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {

            try
            {
                
                this.statusTextBlock.Visibility = Visibility.Visible;
                this.statusTextBlock.Opacity = 1;
                //throw new NotImplementedException();
                
                statusTextBlock.Text = "Starting navigation...";
                //ResetCurrentTabButtonContent(sender);
                if (args.Uri.ToString().ToLower().EndsWith(BalluffConst.FilesType.PDF))
                {
                    //New PDF
                    NewTabWith(BalluffConst.NamePrefix.NamePrefix_ScrollViewer);
                    ResetViewButtonIsEnabled(false);
                    if (nTabCounter >= 5)
                    {
                        btnNewPage.IsEnabled = false;
                    }
                    this.currentScrollViewer = (ScrollViewer)this.FindName(BalluffConst.NamePrefix.NamePrefix_ScrollViewer + nNewPageNum);
                    ItemsControl tempItemsControl = (ItemsControl)((ContentControl)this.currentScrollViewer.Content).Content;
                    ObservableCollection<BitmapImage> PdfPages = new ObservableCollection<BitmapImage>();
                    if (args.Uri.ToString().ToLower().StartsWith("http"))
                    {
                        OpenHttpPDF(args.Uri.ToString(), PdfPages, sender);
                    }
                    else
                    {
                        OpenLocalPDF(args.Uri, PdfPages, sender);
                    }
                    tempItemsControl.ItemsSource = PdfPages;

                }
                else
                {
                    //New Tab logic 20171026 start
                    if (args.Uri.ToString().Contains(BalluffConst.NewTabFlg.Yes) && nTabCounter < 5)
                    {
                        newWebSiteFlg = true;
                        string tabButtonName = BalluffConst.NamePrefix.NamePrefix_TabButton + BalluffUtility.GetNum(sender.Name, BalluffConst.NamePrefix.NamePrefix_Webview);
                        lockedTabButton = (Button)this.FindName(tabButtonName);
                        //disable previous tab and it will be enabled when complete navigation
                        lockedTabButton.IsEnabled = false;
                        string newUri = "";
                        if (args.Uri.ToString().StartsWith("ms-local-stream") && args.Uri.ToString().Contains(BalluffConst.FilesType.MP4))
                        {
                            newUri = BalluffConst.LocalPath + args.Uri.LocalPath;
                        }
                        else
                        {
                            newUri = args.Uri.ToString().Replace(BalluffConst.NewTabFlg.Yes, "");
                        }
                        NewWebTab(newUri);
                        newWebviewFrom = sender;
                    }
                    //New Tab logic 20171026 end


                }


            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(System.IO.FileNotFoundException))
                {
                    statusTextBlock.Text = "Navigation failed with error: " + ex.Message;
                    sender.Stop();
                    ResetViewButtonIsEnabled(true);
                    WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);
                }
                else
                {
                    hasNotFoundPDFError = true;
                    errorWebview = sender;
                }
                
                
            }

        }

        private void Webview_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            try
            { 
                if (args.Uri != null)
                {
                    statusTextBlock.Text = "Finished loading content...";

                }
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
            }
        }

        private void Webview_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args)
        {
            try
            {
                if (args.Uri != null)
                {
                    statusTextBlock.Text = "Loading content... ";
                }
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void webview_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args)
        {
            try {

                if (args.Uri.ToString().StartsWith("ms-local-stream") && args.Uri.ToString().Contains(BalluffConst.FilesType.PDF))
                {
                    Uri localUri = new Uri(BalluffConst.LocalPath + args.Uri.LocalPath);
                    sender.Navigate(localUri);
                }
                else if (args.Uri.ToString().StartsWith("ms-local-stream") && args.Uri.ToString().Contains(BalluffConst.FilesType.MP4))
                {
                    if (args.Uri.ToString().Contains(BalluffConst.NewTabFlg.Yes))
                    {
                        Uri localUri = new Uri(BalluffConst.LocalPath + args.Uri.LocalPath + "?" + BalluffConst.NewTabFlg.Yes);
                        sender.Navigate(localUri);
                    }
                    else
                    {
                        Uri localUri = new Uri(BalluffConst.LocalPath + args.Uri.LocalPath);
                        sender.Navigate(localUri);
                    }
                    
                }
                else if (args.Uri.ToString().StartsWith("ms-local-stream") && args.Uri.ToString().Contains(BalluffConst.FilesType.HTML))
                {
                    Uri localUri = new Uri(BalluffConst.LocalPath + args.Uri.LocalPath);
                    sender.Navigate(localUri);
                }
                else
                {
                    sender.Navigate(args.Uri);
                }
                args.Handled = true;
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void AddToFavorites_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FavoritesNewPopupGrid.Visibility = Visibility.Visible;
                GetCurrentViewTitle();               
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
                statusTextBlock.Text = "Add to Favorites failed with error: " + ex.Message;
                this.statusTextBlock.Visibility = Visibility.Visible;
                this.statusTextBlock.Opacity = 1;
            }

        }

        private async void Favorites_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FavoritesGrid.Visibility == Visibility.Collapsed)
                {
                    FavoritesGrid.Visibility = Visibility.Visible;
                    StorageFolder localfolder = ApplicationData.Current.LocalFolder;
                    var fileCol = await localfolder.GetFilesAsync();
                    bool hasFavoritesList = false;

                    foreach (StorageFile f in fileCol)
                    {
                        if (f.Name == BalluffConst.FavoritesListFileName)
                        {
                            hasFavoritesList = true;
                            favoritesListConfig = f;
                        }
                    }
                    if (hasFavoritesList)
                    {
                        XDocument favorites = XDocument.Load(favoritesListConfig.Path);
                        //var nodes = favorites.Descendants("Favorites");
                        FavoritesList = from query in favorites.Descendants(BalluffConst.FavoritesItem.FavoritesItemTagName)
                                        select new Favorites
                                        {
                                            Uri = (string)query.Attribute(BalluffConst.FavoritesItem.FavoritesItemAttributeUri),
                                            Note = (string)query.Attribute(BalluffConst.FavoritesItem.FavoritesItemAttributeNote)
                                        };
                        FavoritesDataGrid.ItemsSource = FavoritesList;
                        FavoritesDataGrid.IsItemClickEnabled = true;
                        FavoritesDataGrid.ItemClick += Favorites_ItemClick;
                    }
                    else
                    {
                        favoritesListConfig = await localfolder.CreateFileAsync(BalluffConst.FavoritesListFileName);
                    }


                }
                else
                {
                    FavoritesGrid.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "Read favorites failed with error: " + ex.Message;
                try
                {
                    BalluffUtility.DeleteFavoritesFile(favoritesListConfig);
                }
                catch
                {

                }               
                this.statusTextBlock.Visibility = Visibility.Visible;
                this.statusTextBlock.Opacity = 1;
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SearchResultsGrid.AllowDrop = true;
                if (SearchResultsGrid.Visibility == Visibility.Collapsed)
                {
                    SearchResultsGrid.Visibility = Visibility.Visible;
                    txtKeywords.Focus(FocusState.Pointer);

                }
                else
                {
                    SearchResultsGrid.Visibility = Visibility.Collapsed;
                }

            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "Search failed with error: " + ex.Message;
                this.statusTextBlock.Visibility = Visibility.Visible;
                this.statusTextBlock.Opacity = 1;
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private async void DownloadPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                folderPicker.FileTypeFilter.Add("*");
                StorageFolder downloadFolder = await folderPicker.PickSingleFolderAsync();

                if (downloadFolder is null)
                {
                    return;
                }
                string num = BalluffUtility.GetNum(this.currentScrollViewer.Name, BalluffConst.NamePrefix.NamePrefix_ScrollViewer);
                string source = ((TextBlock)(this.FindName(BalluffConst.NamePrefix.NamePrefix_TextBlock + num))).Text;
                StorageFile attachmentFile = null;
                IStorageItem item = null;
                if (source.ToLower().EndsWith(BalluffConst.FilesType.PDF))
                {
                    if (source.ToLower().StartsWith("http"))
                    {
                        string fileName = source.Split('/').Last();
                        item = await downloadFolder.TryGetItemAsync(fileName);
                        if (item == null)
                        {
                            this.statusTextBlock.Text = "Downloading...";
                            this.statusTextBlock.Visibility = Visibility.Visible;
                            this.statusTextBlock.Opacity = 1;
                            attachmentFile = await downloadFolder.CreateFileAsync(fileName);
                            HttpClient client = new HttpClient();
                            var buffer = await
                            client.GetByteArrayAsync(source);
                            await Windows.Storage.FileIO.WriteBytesAsync(attachmentFile, buffer);
                            this.statusTextBlock.Text = attachmentFile.Name + " has been saved.";
                        }
                        else
                        {
                            this.statusTextBlock.Text = item.Name + " already exists.";
                            this.statusTextBlock.Visibility = Visibility.Visible;
                            this.statusTextBlock.Opacity = 1;
                        }
                    }
                    else
                    {

                        Uri attachmentUri = new Uri(source.Replace("ms-appx-web", "ms-appx"));
                        attachmentFile = BalluffUtility.GetLocalFile(attachmentUri.LocalPath.Replace("/local/BLF", ""));
                        //attachmentFile = await StorageFile.GetFileFromPathAsync("D:\\Projects\\BLF.Browser\\BLF.Browser\\bin\\x86\\Debug\\AppX\\BalluffBrowser\\test.txt");
                        item = await downloadFolder.TryGetItemAsync(attachmentFile.Name);
                        if (item == null)
                        {
                            await attachmentFile.CopyAsync(downloadFolder);
                            this.statusTextBlock.Text = attachmentFile.Name + " has been saved.";
                            this.statusTextBlock.Visibility = Visibility.Visible;
                            this.statusTextBlock.Opacity = 1;
                        }
                        else
                        {
                            //this.popupPDFDownloadStatus.Margin = new Thickness((this.PDFGrid.ActualWidth - this.popupPDFDownloadStatus.Width) / 2, 0, 0, 0);
                            //this.popupPDFDownloadStatus.IsOpen = true;
                            //this.msgPDFDownloadStatus.Text = attachmentFile.Name + " already exists.";
                            this.statusTextBlock.Text = item.Name + " already exists.";
                            this.statusTextBlock.Visibility = Visibility.Visible;
                            this.statusTextBlock.Opacity = 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "Download failed with error: " + ex.Message;
                this.statusTextBlock.Visibility = Visibility.Visible;
                this.statusTextBlock.Opacity = 1;
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }
        private async void SendEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fileName = "";
                string messageBody = "";
                string num = BalluffUtility.GetNum(this.currentScrollViewer.Name, BalluffConst.NamePrefix.NamePrefix_ScrollViewer);
                string source = ((TextBlock)(this.FindName(BalluffConst.NamePrefix.NamePrefix_TextBlock + num))).Text;
                if (source.ToLower().EndsWith(BalluffConst.FilesType.PDF) && !source.ToLower().StartsWith("http"))
                {
                    StorageFolder localfolder = ApplicationData.Current.LocalFolder;
                    StorageFolder balluffFolder = await localfolder.GetFolderAsync(BalluffConst.LocalFolderName);
                    var fileCol = await balluffFolder.GetFilesAsync();
                    bool hasPDFList = false;

                    foreach (StorageFile f in fileCol)
                    {
                        if (f.Name == BalluffConst.PDFListFileName)
                        {
                            hasPDFList = true;
                            PDFListConfig = f;
                        }
                    }
                    if (hasPDFList)
                    {

                        fileName = source.Split('/').Last();


                        XDocument pdfFileList = XDocument.Load(PDFListConfig.Path);
                        
                        var PDFFilesList = pdfFileList.Descendants(BalluffConst.PDFFilesItem.PDFFilesItemTagName).SelectMany(y => y.Descendants()
                                                                   .Where(x => x.Value.Equals(fileName.Trim()))).ToList();
                        
                        if (PDFFilesList.Count > 0)
                        {
                            string url = WebUtility.HtmlDecode(PDFFilesList[0].NextNode.ToString().Replace("<Url>", "").Replace("</Url>", "")).Replace("http://", "");
                            url = "http://" + WebUtility.UrlEncode(url).Replace("%25", "%").Replace("%2F", "/").Replace("+","%20");
                            messageBody = url;
                        }

                    }
                    else
                    {
                        messageBody = "No PDF List at Local. Please synchronize local data first.";
                    }
                }

                BalluffUtility.ComposeEmail(messageBody, fileName);
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }
        private void Home_Click(object sender, RoutedEventArgs e)
        {
            try { 
                if (existLocalHome)
                {
                    Uri uri = new Uri(BalluffConst.LocalHomePagePath);
                    this.currentView.Navigate(uri);
                }
                else
                {
                    Uri uri = new Uri(BalluffConst.DefaultHomePagePath);
                    this.currentView.Navigate(uri);
                }
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void GlobalHome_Click(object sender, RoutedEventArgs e)
        {
            try { 
                this.currentView.Navigate(new Uri(BalluffConst.BalluffOfficialWebsite));
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void Goback_Click(object sender, RoutedEventArgs e)
        {
            try { 
                if (this.currentView.CanGoBack)
                {
                    this.currentView.GoBack();
                }
                    
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void Goforward_Click(object sender, RoutedEventArgs e)
        {
            try { 
                if (this.currentView.CanGoForward && !this.currentView.Source.ToString().ToLower().EndsWith(BalluffConst.FilesType.PDF))
                {
                    this.currentView.GoForward();
                }
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.currentView.Refresh();
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void ChangeTab_Click(object sender, RoutedEventArgs e)
        {
            try { 
                //change currentView
                if (this.currentView != null)
                {
                    this.currentView.Visibility = Visibility.Collapsed;
                }
                Button tabButton = (Button)sender;
                string url = tabButton.Content.ToString();
                string num = BalluffUtility.GetNum(tabButton.Name, BalluffConst.NamePrefix.NamePrefix_TabButton);
                strCurrentWebviewName = BalluffConst.NamePrefix.NamePrefix_Webview + num;
                object obj = this.FindName(strCurrentWebviewName);
                if (obj != null)
                {
                    this.currentView = (WebView)obj;
                    this.currentView.Visibility = Visibility.Visible;
                    //comment 20170922 by justin li
                    //if(this.currentView.Source.ToString().ToLower().EndsWith(BalluffConst.FilesType.PDF))
                    //{
                    //    if (this.currentView.CanGoBack)
                    //    {
                    //        this.currentView.GoBack();
                    //    }
                    //}
                    //this.WebViewGroupGrid.Visibility = Visibility.Visible;
                    //this.PDFGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.currentView = null;
                }



                //change currentScrollViewer
                if (this.currentScrollViewer != null)
                {
                    this.currentScrollViewer.Visibility = Visibility.Collapsed;
                }
                strCurrentScrollViewerName = BalluffConst.NamePrefix.NamePrefix_ScrollViewer + num;
                obj = this.FindName(strCurrentScrollViewerName);
                if (obj != null)
                {
                    this.currentScrollViewer = (ScrollViewer)obj;
                    this.currentScrollViewer.Visibility = Visibility.Visible;
                }
                else
                {
                    this.currentScrollViewer = null;
                }


                if (IsCurrentPDFOpened(tabButton))
                {
                    this.PDFGrid.Visibility = Visibility.Visible;
                    ResetViewButtonIsEnabled(false);
                }
                else
                {
                    this.PDFGrid.Visibility = Visibility.Collapsed;
                    if(this.currentView != null && this.currentView.Source.ToString().Contains(BalluffConst.FilesType.MP4))
                    {
                        ResetViewButtonIsEnabled(true ,true);
                    }
                    else
                    {
                        ResetViewButtonIsEnabled(true, false);
                    }
                        
                }

                //change currentTabButton
                this.currentTabButton.Style = (Style)this.Resources["NormalTabButton"];
                this.currentTabButton = (Button)sender;
                this.currentTabButton.Style = (Style)this.Resources["HighLightTabButton"];
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }

        }


        private void NewPage_Click(object sender, RoutedEventArgs e)
        {
            try { 
                if (this.currentScrollViewer != null)
                {
                    this.currentScrollViewer.Visibility = Visibility.Collapsed;
                }
                if (this.currentView != null)
                {
                    this.currentView.Visibility = Visibility.Collapsed;
                }
                NewTabWith(BalluffConst.NamePrefix.NamePrefix_Webview);
                ResetViewButtonIsEnabled(true);
                if (nTabCounter >= 5)
                {
                    ((Button)sender).IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }


        }

        private void TabClose_Click(object sender, RoutedEventArgs e)
        {
            try { 
                //remove webview
                string num = BalluffUtility.GetNum(((Button)sender).Name, BalluffConst.NamePrefix.NamePrefix_CloseButton);
                string strWebviewName = BalluffConst.NamePrefix.NamePrefix_Webview + num;
                object obj = this.FindName(strWebviewName);
                
                if (obj != null)
                {
                    //if navigation failed , do not reset source
                    if (((WebView)obj).Source.ToString().Contains(BalluffConst.FilesType.MP4))
                    {
                        //Reason;when mp4 closed, sound still exist. 
                        ((WebView)obj).Visibility = Visibility.Collapsed;
                        ((WebView)obj).Source = new Uri(BalluffConst.LocalHomePagePath);
                    }
                                     
                    this.WebViewGroupGrid.Children.Remove((WebView)obj);
                }

                //remove tabbutton and close button
                string strTabButtonName = BalluffConst.NamePrefix.NamePrefix_TabButton + num;
                double dMarginLeft = ((Button)this.FindName(strTabButtonName)).Margin.Left;
                this.TabGrid.Children.Remove((Button)this.FindName(strTabButtonName));
                this.CloseButtonGrid.Children.Remove((Button)sender);

                //remove scrollviewer
                string strScrollViewerName = BalluffConst.NamePrefix.NamePrefix_ScrollViewer + num;
                obj = this.FindName(strScrollViewerName);
                if (obj != null)
                {
                    this.PDFGrid.Children.Remove((ScrollViewer)obj);
                }
                ResetTabButtonLocation(dMarginLeft);
                ResetCloseButtonLocation();
                nTabCounter--;
                if (nTabCounter < 5)
                {
                    btnNewPage.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void ResetTabButtonLocation(double dMarginLeft)
        {
            int nTabButtonCount = 0;
            foreach (object subButton in this.TabGrid.Children)
            {
                string name = ((Button)subButton).Name;
                ((Button)subButton).Margin = new Thickness(nTabButtonCount * dTabButtonWidth, 0, 0, 0);
                nTabButtonCount++;

                if (this.FindName(this.currentTabButton.Name) == null)
                {
                    if (dMarginLeft == nTabButtonCount * dTabButtonWidth || ((Button)this.TabGrid.Children.Last()).Name == name)
                    {
                        this.currentTabButton = (Button)subButton;
                        string num = BalluffUtility.GetNum(name, BalluffConst.NamePrefix.NamePrefix_TabButton);
                        strCurrentWebviewName = BalluffConst.NamePrefix.NamePrefix_Webview + num;
                        object obj = this.FindName(strCurrentWebviewName);
                        if (obj != null)
                        {
                            this.currentView = (WebView)obj;
                            this.currentView.Visibility = Visibility.Visible;
                            this.PDFGrid.Visibility = Visibility.Collapsed;

                            if (this.currentView != null && this.currentView.Source.ToString().Contains(BalluffConst.FilesType.MP4))
                            {
                                ResetViewButtonIsEnabled(true, true);
                            }
                            else
                            {
                                ResetViewButtonIsEnabled(true, false);
                            }
                        }
                        else
                        {
                            this.currentView = null;
                        }

                        strCurrentScrollViewerName = BalluffConst.NamePrefix.NamePrefix_ScrollViewer + num;
                        obj = this.FindName(strCurrentScrollViewerName);
                        if (obj != null)
                        {
                            this.currentScrollViewer = (ScrollViewer)obj;
                            this.currentScrollViewer.Visibility = Visibility.Visible;
                            this.PDFGrid.Visibility = Visibility.Visible;
                            ResetViewButtonIsEnabled(false);
                        }
                        else
                        {
                            this.currentScrollViewer = null;
                        }
                        this.currentTabButton.Style = (Style)this.Resources["HighLightTabButton"];


                    }
                }
            }
        }

        private void ResetCloseButtonLocation()
        {
            int nCloseButtonCount = 0;
            foreach (object subButton in this.CloseButtonGrid.Children)
            {
                ((Button)subButton).Margin = new Thickness(nCloseButtonCount * dTabButtonWidth + dTabButtonWidth - dCloseButtonWidth, 0, 0, 0);
                nCloseButtonCount++;
            }
        }

        private void ResetCurrentTabButtonContent(WebView sender)
        {
            string num = BalluffUtility.GetNum(sender.Name, BalluffConst.NamePrefix.NamePrefix_Webview);
            string strTabButtonName = BalluffConst.NamePrefix.NamePrefix_TabButton + num;
            Button btnTab = (Button)this.FindName(strTabButtonName);
            if(btnTab!= null)
            {
                if (btnTab.Content.ToString().Contains(BalluffConst.Loading))
                {
                    return;
                }
                if (sender.Source.ToString().ToLower().StartsWith("http"))
                {
                    btnTab.Content = BalluffConst.TabButtonContent.TabButtonContent_Http;
                }
                else
                {
                    btnTab.Content = BalluffConst.TabButtonContent.TabButtonContent_Local;
                }
            }
            
        }

        
       
        public  void OpenLocalPDF(Uri uri, ObservableCollection<BitmapImage> PdfPages, WebView webView)
        {          
            string tabButtonName = BalluffConst.NamePrefix.NamePrefix_TabButton + nNewPageNum;
            Button tabbutton = (Button)this.FindName(tabButtonName);
            tabbutton.Content = tabbutton.Content.ToString() + BalluffConst.Loading;
            try
            {
                this.PDFGrid.Visibility = Visibility.Visible;
                StorageFolder appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                

                StorageFile f = BalluffUtility.GetLocalFile(uri.LocalPath);
                PdfDocument doc = PdfDocument.LoadFromFileAsync(f).AsTask().GetAwaiter().GetResult();
                BalluffUtility.LoadPDFToControl(doc, PdfPages);
                ResetViewButtonIsEnabled(false);
                tabbutton.Content = tabbutton.Content.ToString().Replace(BalluffConst.Loading, "");
                btnDownloadPDF.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "Navigation failed with error: " + ex.Message;
                //webView.Stop();
                if (webView.CanGoBack)
                {
                    if (ex.GetType() != typeof(System.IO.FileNotFoundException))
                    {
                        webView.GoBack();
                    }
                    //webView.GoBack();
                }
                //this.PDFGrid.Visibility = Visibility.Collapsed;
                ResetViewButtonIsEnabled(false);
                btnDownloadPDF.Visibility = Visibility.Collapsed;
                tabbutton.Content = tabbutton.Content.ToString().Replace(BalluffConst.Loading, "");
                WriteLog(DateTime.Now.ToString() + ex.Message );
                throw ex;
            }
        }

        

        public async void OpenHttpPDF(string url, ObservableCollection<BitmapImage> PdfPages, WebView webView)
        {
            //string num = GetNum(webview.Name, BalluffConst.NamePrefix.NamePrefix_Webview);
            string tabButtonName = BalluffConst.NamePrefix.NamePrefix_TabButton + nNewPageNum;
            Button tabbutton = (Button)this.FindName(tabButtonName);
            tabbutton.Content = tabbutton.Content.ToString() + BalluffConst.Loading;
            try
            {
                //this.PDFProgressBar
                this.PDFGrid.Visibility = Visibility.Visible;
                HttpClient client = new HttpClient();
                var stream = await
                client.GetStreamAsync(url);
                var memStream = new MemoryStream();
                await stream.CopyToAsync(memStream);

                memStream.Position = 0;
                PdfDocument doc = await PdfDocument.LoadFromStreamAsync(memStream.AsRandomAccessStream());
                BalluffUtility.LoadPDFToControl(doc, PdfPages);
                ResetViewButtonIsEnabled(false);
                tabbutton.Content = tabbutton.Content.ToString().Replace(BalluffConst.Loading, "");

            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "Navigation failed with error: " + ex.Message; ;
                //webView.Stop();
                if (webView.CanGoBack)
                {
                    webView.GoBack();
                }
                //this.PDFGrid.Visibility = Visibility.Collapsed;
                ResetViewButtonIsEnabled(false);
                btnDownloadPDF.Visibility = Visibility.Collapsed;
                tabbutton.Content = tabbutton.Content.ToString().Replace(BalluffConst.Loading, "");
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        public async void HideTextBlock()
        {
            await System.Threading.Tasks.Task.Delay(1000);
            for (int i = 100; i > 0; i--)
            {
                this.statusTextBlock.Opacity = ((double)i) / 100;
                await System.Threading.Tasks.Task.Delay(5);
            }

            this.statusTextBlock.Visibility = Visibility.Collapsed;
        }

        private bool IsCurrentPDFOpened(Button tabButton)
        {
            string num = BalluffUtility.GetNum(tabButton.Name, BalluffConst.NamePrefix.NamePrefix_TabButton);
            string scrollViewerName = BalluffConst.NamePrefix.NamePrefix_ScrollViewer + num;
            object obj = this.FindName(scrollViewerName);
            if (obj == null)
            {
                return false;
            }
            else
            {
                return true;
            }


        }

        private void ResetViewButtonIsEnabled(bool val, bool isMP4 = false)
        {
            btnHome.IsEnabled = val;
            btnGlobalHome.IsEnabled = val;
            btnGoBack.IsEnabled = val;
            btnGoForward.IsEnabled = val;
            btnRefresh.IsEnabled = val;
            btnAddFavorites.IsEnabled = val;
            btnFavorites.IsEnabled = val;
            if(isMP4)
            {
                btnAddFavorites.IsEnabled = false;
                btnFavorites.IsEnabled = false;
            }
            if (val)
            {
                btnDownloadPDF.Visibility = Visibility.Collapsed;
                
            }
            else
            {
                FavoritesGrid.Visibility = Visibility.Collapsed;
                btnDownloadPDF.Visibility = Visibility.Visible;
                
            }

        }

        private void NewTabWith(string type, string newTabUrl = "")
        {
            int buttonCount = this.TabGrid.Children.Count;
            this.currentTabButton.Style = (Style)this.Resources["NormalTabButton"];
            if (this.currentView != null)
            {
                this.currentView.Visibility = Visibility.Collapsed;
            }

            nNewPageNum++;
            //New TabButton
            Button newTabButton = new Button();
            newTabButton.Name = BalluffConst.NamePrefix.NamePrefix_TabButton + nNewPageNum;
            newTabButton.Content = BalluffConst.TabButtonContent.TabButtonContent_Local;
            newTabButton.Click += this.ChangeTab_Click;
            //newTabButton.Holding += this.TabButton_Holding;
            newTabButton.Margin = new Thickness(buttonCount * dTabButtonWidth, 0, 0, 0);
            newTabButton.Style = (Style)this.Resources["HighLightTabButton"];
            this.TabGrid.Children.Add(newTabButton);
            nTabCounter++;
            this.currentTabButton = newTabButton;

            //New CloseButton
            Button newCloseButton = new Button();
            newCloseButton.Name = BalluffConst.NamePrefix.NamePrefix_CloseButton + nNewPageNum;
            newCloseButton.Content = "X";
            newCloseButton.Click += this.TabClose_Click;
            //newTabButton.Holding += this.TabButton_Holding;
            newCloseButton.Margin = new Thickness(buttonCount * dTabButtonWidth + dTabButtonWidth - dCloseButtonWidth, 0, 0, 0);
            newCloseButton.Style = (Style)this.Resources["CloseButton"];
            this.CloseButtonGrid.Children.Add(newCloseButton);
            if (newTabUrl != "")
            {
                lockedCloseButton = newCloseButton;
                lockedCloseButton.Visibility = Visibility.Collapsed;
            }
            //this.currentTabButton = newTabButton;

            if (type == BalluffConst.NamePrefix.NamePrefix_ScrollViewer)
            {
                //New ScrollViewer
                this.PDFGrid.Visibility = Visibility.Visible;
                ScrollViewer newScrollViewer = new ScrollViewer();
                newScrollViewer.ZoomMode = ZoomMode.Enabled;
                newScrollViewer.Visibility = Visibility.Visible;
                newScrollViewer.SetValue(Grid.ColumnProperty, 1);
                newScrollViewer.Name = BalluffConst.NamePrefix.NamePrefix_ScrollViewer + nNewPageNum;
                ItemsControl newItemsControl = new ItemsControl();
                newItemsControl.ItemTemplate = (DataTemplate)this.Resources["ItemTemplate"];
                ContentControl newContentControl = new ContentControl();
                newContentControl.Content = newItemsControl;
                newScrollViewer.Content = newContentControl;
                PDFGrid.Children.Add(newScrollViewer);
                this.currentScrollViewer = newScrollViewer;
                //New PDFSourceTextBlock
                TextBlock newPDFSourceTextBlock = new TextBlock();
                newPDFSourceTextBlock.Name = BalluffConst.NamePrefix.NamePrefix_TextBlock + nNewPageNum;
                newPDFSourceTextBlock.Text = this.currentView.Source.ToString();
                this.PDFSourceGrid.Children.Add(newPDFSourceTextBlock);
            }

            if (type == BalluffConst.NamePrefix.NamePrefix_Webview)
            {
                //New WebView
                this.PDFGrid.Visibility = Visibility.Collapsed;
                WebView newWebView = new WebView();
                if (newTabUrl == "")
                {
                    if (existLocalHome)
                    {
                        newWebView.Source = new Uri(BalluffConst.LocalHomePagePath);
                    }
                    else
                    {
                        newWebView.Source = new Uri(BalluffConst.DefaultHomePagePath);
                    }
                }
                else
                {
                    newWebView.Source = new Uri(newTabUrl);
                }
                newWebView.Margin = new Thickness(10, 0, 0, 0);
                newWebView.NewWindowRequested += this.webview_NewWindowRequested;
                newWebView.Name = BalluffConst.NamePrefix.NamePrefix_Webview + nNewPageNum;
                this.currentView = newWebView;
                this.currentView.ContentLoading += Webview_ContentLoading;
                this.currentView.DOMContentLoaded += Webview_DOMContentLoaded;
                this.currentView.NavigationStarting += Webview_NavigationStarting;
                this.currentView.NavigationCompleted += Webview_NavigationCompleted;

                this.WebViewGroupGrid.Children.Add(newWebView);
            }
        }

        private void NewWebTab(string url)
        {
            if (this.currentScrollViewer != null)
            {
                this.currentScrollViewer.Visibility = Visibility.Collapsed;
            }
            if (this.currentView != null)
            {
                this.currentView.Visibility = Visibility.Collapsed;
            }
            NewTabWith(BalluffConst.NamePrefix.NamePrefix_Webview, url);
            if (this.currentView != null && this.currentView.Source.ToString().Contains(BalluffConst.FilesType.MP4))
            {
                ResetViewButtonIsEnabled(true, true);
            }
            else
            {
                ResetViewButtonIsEnabled(true, false);
            }
            if (nTabCounter >= 5)
            {
                btnNewPage.IsEnabled = false;
            }


        }

        //Navigate to Favorite site
        private void Favorites_ItemClick(object sender, ItemClickEventArgs e)
        {
            try { 
                string uri = ((Favorites)e.ClickedItem).Uri;
                string note = ((Favorites)e.ClickedItem).Note;
                if (uri.ToLower().StartsWith("http"))
                {
                    currentView.Navigate(new Uri(uri));
                }
                else
                {
                    currentView.Navigate(new Uri(BalluffConst.LocalPath + uri));
                }
                FavoritesGrid.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }

        }


       

        private void FavoritesDataGrid_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            try
            {
                if (e.OriginalSource.GetType() == typeof(Windows.UI.Xaml.Controls.TextBlock))
                {
                    favoritesNoteForDelete = ((TextBlock)e.OriginalSource).Text;
                    FavoritesDeletePopupGrid.Visibility = Visibility.Visible;
                    txtPopupMessage.Text = string.Format(BalluffConst.FavoritesPopupMessage, favoritesNoteForDelete);
                    btnFavoritesDeletePopupYes.Focus(FocusState.Pointer);
                }
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void FavoritesDeletePopupYes_Click(object sender, RoutedEventArgs e)
        {
            try { 
                FavoritesDeletePopupGrid.Visibility = Visibility.Collapsed;
                FavoritesList = BalluffUtility.RemoveFavorites(FavoritesList, favoritesNoteForDelete);
                FavoritesDataGrid.ItemsSource = FavoritesList;
                FavoritesDataGrid.IsItemClickEnabled = true;
                FavoritesDataGrid.ItemClick += Favorites_ItemClick;
                WriteFavoritesList();
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void FavoritesDeletePopupNo_Click(object sender, RoutedEventArgs e)
        {
            try { 
                FavoritesDeletePopupGrid.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
            }
        }

        private void FavoritesNewPopupAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Favorites NewFavorites = new Favorites();
               
                if (currentView.Source.ToString().ToLower().StartsWith("http"))
                {
                    NewFavorites.Uri = currentView.Source.ToString(); ;
                }
                else
                {
                    NewFavorites.Uri = currentView.Source.AbsolutePath;
                }
               
                
                NewFavorites.Note = txtFavoritesNote.Text;
                FavoritesList = BalluffUtility.AddToFavorites(FavoritesList, NewFavorites);
                FavoritesDataGrid.ItemsSource = FavoritesList;
                FavoritesDataGrid.IsItemClickEnabled = true;
                FavoritesDataGrid.ItemClick += Favorites_ItemClick;
                FavoritesNewPopupGrid.Visibility = Visibility.Collapsed;
                WriteFavoritesList();
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
            }


        }

        //RewriteFavoritesList
        private async void WriteFavoritesList()
        {
            await favoritesListConfig.DeleteAsync();
            StorageFolder localfolder = ApplicationData.Current.LocalFolder;
            favoritesListConfig = await localfolder.CreateFileAsync(BalluffConst.FavoritesListFileName);

            using (IRandomAccessStream writeStream = await favoritesListConfig.OpenAsync(FileAccessMode.ReadWrite))
            {

                System.IO.Stream s = writeStream.AsStreamForWrite();
                System.Xml.XmlWriterSettings settings = new System.Xml.XmlWriterSettings();
                settings.Async = true;
                using (System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create(s, settings))
                {
                    writer.WriteStartElement(BalluffConst.FavoritesItem.FavoritesItemRootTagName);
                    if (FavoritesList.ToList().Count == 0)
                    {
                        writer.WriteRaw(string.Empty);
                    }
                    else
                    {
                        foreach (Favorites item in FavoritesList)
                        {
                            string strXml = string.Format(BalluffConst.FavoritesItem.FavoritesItemFormat, BalluffConst.FavoritesItem.FavoritesItemTagName,
                                BalluffConst.FavoritesItem.FavoritesItemAttributeUri, item.Uri, BalluffConst.FavoritesItem.FavoritesItemAttributeNote, item.Note);
                            writer.WriteRaw(strXml);
                            //writer.WriteAttributeString(BalluffConst.FavoritesItem.FavoritesItemAttributeUri, item.Uri);
                            //writer.WriteAttributeString(BalluffConst.FavoritesItem.FavoritesItemAttributeNote, item.Note);
                        }
                    }
                    writer.Flush();
                    await writer.FlushAsync();
                }
            }
        }

        private void FavoritesNewPopupCancel_Click(object sender, RoutedEventArgs e)
        {
            try { 
                FavoritesNewPopupGrid.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void FavoritesNote_TextChanged(object sender, TextChangedEventArgs e)
        {
            try { 
                if (((TextBox)sender).Text.Trim() == "")
                {
                    btnFavoritesNewPopupAdd.IsEnabled = false;
                }
                else
                {
                    btnFavoritesNewPopupAdd.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private  void GetCurrentViewTitle()
        {
            string uri = this.currentView.Source.ToString();
            HtmlDocument doc = new HtmlDocument();
            if (uri.ToLower().StartsWith("http"))
            {
                WebRequest request = HttpWebRequest.Create(uri);
                WebResponse response =  request.GetResponseAsync().GetAwaiter().GetResult();
                Stream stream = response.GetResponseStream();          
                doc.Load(stream);
            }
            else
            {               
                string htmlName = uri.Split('/').Last();
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder balluffFolder = localFolder.GetFolderAsync(BalluffConst.LocalFolderName).AsTask().GetAwaiter().GetResult();
                StorageFolder DataFolder =  balluffFolder.GetFolderAsync(BalluffConst.DataFolderName).AsTask().GetAwaiter().GetResult();
                StorageFile f =  DataFolder.GetFileAsync(htmlName).AsTask().GetAwaiter().GetResult();
                var stream =  f.OpenStreamForReadAsync().GetAwaiter().GetResult();
                doc.Load(stream);
            }
            var htmlTitle = doc.DocumentNode.Descendants("title").First().InnerText;
            txtFavoritesNote.Text = htmlTitle;
        }

        private async void GetSearchResults(string keywords)
        {
            string filename = "";
            try
            {
                HtmlDocument doc = new HtmlDocument();
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder balluffFolder = await localFolder.GetFolderAsync(BalluffConst.LocalFolderName);
                StorageFolder dataFolder = await balluffFolder.GetFolderAsync(BalluffConst.DataFolderName);
                var targetFiles = await dataFolder.GetFilesAsync();
                var keywordList = keywords.Split(' ').ToArray();

                foreach (var file in targetFiles)
                {
                    if (file.Name.EndsWith(BalluffConst.FilesType.HTML))
                    {
                        filename = file.Name;
                        var stream = await file.OpenStreamForReadAsync();
                        try
                        {
                            doc.Load(stream);
                        }
                        catch
                        {
                            continue;
                        }
                        string searchResultContent = "";
                        string path = "/" + BalluffConst.DataFolderName + "/" + file.Name;

                        //search title
                        var titleRestlts = doc.DocumentNode.Descendants("title").SelectMany(y => y.Descendants()
                                                           .Where(x => x.InnerText.ToLower().Contains(keywords.ToLower().Trim()))).ToList();
                        if (titleRestlts.Count > 0)
                        {
                            foreach (var item in titleRestlts)
                            {
                                searchResultContent += (WebUtility.HtmlDecode(item.InnerText) + "...");
                            }

                        }
                        
                        //search span
                        var spanRestlts = doc.DocumentNode.Descendants("span").SelectMany(y => y.Descendants()
                                                            .Where(x => x.InnerText.ToLower().Contains(keywords.ToLower().Trim()))).ToList();
                        if (spanRestlts.Count > 0)
                        {
                            foreach (var item in spanRestlts)
                            {
                                searchResultContent += (WebUtility.HtmlDecode(item.InnerText) + "...");
                            }
                        }
   
                        if (keywordList.Length > 1)
                        {
                            foreach (var keyword in keywordList)
                            {

                                if (!string.IsNullOrEmpty(keyword.Trim()))
                                {
                                    //search title
                                    titleRestlts = doc.DocumentNode.Descendants("title").SelectMany(y => y.Descendants()
                                                                       .Where(x => x.InnerText.ToLower().Contains(keyword.ToLower().Trim()))).ToList();
                                    if (titleRestlts.Count > 0)
                                    {
                                        foreach (var item in titleRestlts)
                                        {
                                            searchResultContent += (WebUtility.HtmlDecode(item.InnerText) + "...");
                                        }

                                    }
                                   
                                    //search span
                                    spanRestlts = doc.DocumentNode.Descendants("span").SelectMany(y => y.Descendants()
                                                                        .Where(x => x.InnerText.ToLower().Contains(keyword.ToLower().Trim()))).ToList();
                                    if (spanRestlts.Count > 0)
                                    {
                                        foreach (var item in spanRestlts)
                                        {
                                            searchResultContent += (WebUtility.HtmlDecode(item.InnerText) + "...");
                                        }

                                    }
                                                                   
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(searchResultContent))
                        {

                            SearchResults searchResultsItem = new SearchResults(path, searchResultContent);
                            SearchResultsList = BalluffUtility.AddToSearchResults(SearchResultsList, searchResultsItem);
                        }


                    }
                }
                if (SearchResultsList.ToList().Count == 0)
                {
                    txtSearchMsg.Text = BalluffConst.Message.Alert_NoSearchResults;
                    txtSearchMsg.Visibility = Visibility.Visible;
                    SearchResultsDataGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    SearchResultsDataGrid.ItemsSource = SearchResultsList;
                    SearchResultsDataGrid.IsItemClickEnabled = true;
                    SearchResultsDataGrid.ItemClick += SearchResults_ItemClick;
                }

            }
            catch (Exception ex)
            {
                statusTextBlock.Text = "Search failed with error: " + ex.Message; ;
                statusTextBlock.Visibility = Visibility.Visible;
                this.statusTextBlock.Opacity = 1;
            }
        }

        private async void SetExistLocalHome()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder balluffFolder = await localFolder.GetFolderAsync(BalluffConst.LocalFolderName);
                StorageFolder dataFolder = await balluffFolder.GetFolderAsync(BalluffConst.DataFolderName);
                StorageFile f = await dataFolder.GetFileAsync("home.html");
                existLocalHome = true;
                currentView.Source = new Uri(BalluffConst.LocalHomePagePath);
                //if (navigateToHome)
                //{
                //    this.currentView.Navigate(uri);
                //}
            }
            catch
            {
                existLocalHome = false;
                currentView.Source = new Uri(BalluffConst.DefaultHomePagePath);
            }
        }

        private void SearchContent_Click(object sender, RoutedEventArgs e)
        {
            try { 
                Searching();
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void SearchResults_ItemClick(object sender, ItemClickEventArgs e)
        {
            try { 
                string uri = ((SearchResults)e.ClickedItem).Uri;
                if (uri.ToLower().StartsWith("http"))
                {
                    currentView.Navigate(new Uri(uri));
                }
                else
                {
                    currentView.Navigate(new Uri(BalluffConst.LocalPath + uri));
                }
                SearchResultsGrid.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void Keywords_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            try { 
                if (e.Key == VirtualKey.Enter)
                {
                    Searching();
                }
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void Searching()
        {
            if (string.IsNullOrEmpty(txtKeywords.Text.Trim()))
            {
                txtSearchMsg.Text = BalluffConst.Message.Alert_InputKeywords;
                txtSearchMsg.Visibility = Visibility.Visible;
                SearchResultsDataGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                SearchResultsList = new List<SearchResults>();
                SearchResultsDataGrid.ItemsSource = SearchResultsList;
                GetSearchResults(txtKeywords.Text.Trim());
                txtSearchMsg.Visibility = Visibility.Collapsed;
                SearchResultsDataGrid.Visibility = Visibility.Visible;

            }
        }

        private void SearchResultsGrid_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            try
            {
                //Grid grid = sender as Grid;
                //if (grid == null)
                //{
                //    return;
                //}
                //e.Handled = true;
                //Windows.UI.Input.PointerPoint pointerPoint = e.GetCurrentPoint(grid);
                //if (pointerPoint.Properties.IsLeftButtonPressed)
                //{
                //    Thickness margin = (Thickness)grid.GetValue(MarginProperty);
                //    double x = (double)margin.Left;
                //    double y = (double)margin.Top;
                //    double width = grid.ActualWidth;
                //    double height = grid.ActualHeight;
                //    x += pointerPoint.Position.X - grid.ActualWidth/2.0;
                //    y += pointerPoint.Position.Y - grid.ActualHeight/2.0;
                //    //x += pointerPoint.Position.X;
                //    //y += pointerPoint.Position.Y;
                //    grid.Margin = new Thickness(x, y, 0, 0);
                //    //grid.SetValue(Canvas.LeftProperty, x);
                //    //grid.SetValue(Canvas.TopProperty, y);

                //}
            }
            catch
            {

            }


        }

        public void Synchronize(string userName, string pass, string basePath = "/")
        {
            StorageFolder localfolder = ApplicationData.Current.LocalFolder;
            StorageFolder blfBrowserFolder = localfolder.GetFolderAsync(BalluffConst.LocalFolderName).AsTask().GetAwaiter().GetResult();

            //根据用户名，密码等信息初始化 WebDavClient 实例，可用于后续的获取文件清单及下载
            WebDavClient c = new WebDavClient
            {
                User = userName,//用户名
                Pass = pass,//密码
                Server = "http://116.62.218.82",//服务器地址
                BasePath = basePath //目录根地址
            };

            List<WebDavFile> updatedFiles = c.GetUpdatedFiles("/", blfBrowserFolder.Path);

            //Console.WriteLine("UpdateFiles Count:" + updatedFiles.Count);

            c.DownloadCompling += C_DownloadCompling;
            c.DownloadCompleted += C_DownloadCompleted;
            //根据上面的配置，获取http://116.62.218.82/所有的文件列表
            List<WebDavFile> filesList = c.GetFiles("/");
            Task.Run(() =>
            {
                c.Download(filesList, blfBrowserFolder.Path, true);
                BalluffUtility.SaveCredentialAndVersion(userName, pass,newVersion);
            });
                

        }

        public bool IsNewVersion(string userName, string pass,string oldVersion, string basePath = "/")
        {
            StorageFolder localfolder = ApplicationData.Current.LocalFolder;
            StorageFolder blfBrowserFolder = localfolder.GetFolderAsync(BalluffConst.LocalFolderName).AsTask().GetAwaiter().GetResult();

            //根据用户名，密码等信息初始化 WebDavClient 实例，可用于后续的获取文件清单及下载
            WebDavClient c = new WebDavClient
            {
                User = userName,//用户名
                Pass = pass,//密码
                Server = "http://116.62.218.82",//服务器地址
                BasePath = basePath //目录根地址
            };

            List<WebDavFile> updatedFiles = c.GetUpdatedFiles("/", blfBrowserFolder.Path);

            //Console.WriteLine("UpdateFiles Count:" + updatedFiles.Count);

            //c.DownloadCompling += C_DownloadCompling;
            //c.DownloadCompleted += C_DownloadCompleted;
            //根据上面的配置，获取http://116.62.218.82/所有的文件列表
            List<WebDavFile> filesList = c.GetFiles("/");
           
            c.Download(filesList, blfBrowserFolder.Path, false);
            StorageFolder versionFolder = blfBrowserFolder.GetFolderAsync("Version").AsTask().GetAwaiter().GetResult();
            var versionFile = versionFolder.GetFileAsync("Version.txt").AsTask().GetAwaiter().GetResult();
            using (Stream stearm = versionFile.OpenStreamForReadAsync().GetAwaiter().GetResult())
            {
                using (StreamReader read = new StreamReader(stearm))
                {
                    newVersion = read.ReadToEnd();
                    
                }
            }
            if (string.IsNullOrEmpty(oldVersion)){
                return true;
            }
            if(oldVersion.Equals(newVersion))
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        private async void UpdateProgress(object sender, object e)
        {
            if(txtSynchroMsg.Text != "")
            {
                (sender as DispatcherTimer).Tick -= UpdateProgress;
                (sender as DispatcherTimer).Stop();
                return;
            }
            if (pbSynchro.Value < 100)
            {
                pbSynchro.Value = synchroProgress;
                txtSynchroProgressMsg.Text = string.Format(BalluffConst.SynchroProgressMsg, synchroProgressMsg);
                if(interval <60)
                {
                    interval += 1;
                    (sender as DispatcherTimer).Interval = TimeSpan.FromSeconds(interval);
                }
               
            }
            else
            {
                SynchroPopupGrid.Visibility = Visibility.Collapsed;
                btnSynchro.Visibility = Visibility.Collapsed;
                (sender as DispatcherTimer).Tick -= UpdateProgress;
                (sender as DispatcherTimer).Stop();
                await new MessageDialog("Completed").ShowAsync();
                SetExistLocalHome();
            }
        }

        private void C_DownloadCompleted(WebDavFile davFile, int index, int total)
        {
            synchroProgressMsg = index.ToString() + "/" + total.ToString();
            synchroProgress = double.Parse(((double)index / (double)total * 100).ToString("f2"));
            //Console.WriteLine("donwload finished " + index + ",total :" + total + ", file name:" + davFile.DispalyName + " .....");
            //WriteLog(DateTime.Now.ToString() + "donwload finished " + index + ",total :" + total + ", file name:" + davFile.DispalyName + " .....");

        }
        
        private void C_DownloadCompling(WebDavFile davFile, int index, int total)
        {
            
            if (davFile.DownloadPath.Length> 200)
            {
                WriteLog(DateTime.Now.ToString() + "downloading current " + index + ",total :" + total + ", file name:" + davFile.DispalyName + " ....." + "length:" + davFile.DownloadPath.Length);

            }
            else
            {
                WriteLog(DateTime.Now.ToString() + "downloading current " + index + ",total :" + total + ", file name:" + davFile.DispalyName + " .....");
            }
            //WriteLog(DateTime.Now.ToString() + "downloading current " + index + ",total :" + total + ", file name:" + davFile.DispalyName + " .....");
            //Console.WriteLine("downloading current " + index + ",total :" + total + ", file name:" + davFile.DispalyName + " .....");
        }

        private void Synchro_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                txtSynchroAccount.Text = localSettings.Values[BalluffConst.LocalSetting.Account].ToString();
                txtPassword.Password = localSettings.Values[BalluffConst.LocalSetting.Password].ToString();
                SynchroPopupGrid.Visibility = Visibility.Visible;

            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
                if (ex.Message.Contains("(401)"))
                {
                    txtSynchroMsg.Text = BalluffConst.Message.Alert_IncorrectCredential;
                    SynchroLoginGrid.Visibility = Visibility.Visible;
                    SynchroProgressBarGrid.Visibility = Visibility.Collapsed;
                    
                }
            }

        }

        private void SynchroLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
               
                if (string.IsNullOrEmpty(txtSynchroAccount.Text.Trim()) || string.IsNullOrEmpty(txtPassword.Password.Trim()))
                {
                    //Alert
                    txtSynchroMsg.Text = BalluffConst.Message.Alert_InputCredential; ;

                }
                else
                {
                    btnSynchro.IsEnabled = false;
                    interval = 3;
                    synchroProgress = 0;
                    txtSynchroMsg.Text = "";
                    //设置进度条的模式为不重复状态
                    pbSynchro.IsIndeterminate = false;
                    //启用定时器，再每下一秒改变原来的状态
                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(interval);
                    timer.Tick += UpdateProgress;
                    timer.Start();                   
                    ShowProgressBar();
                    Synchronize(txtSynchroAccount.Text.Trim(), txtPassword.Password.Trim());


                }
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
                //incorrect Account or Password
                if (ex.Message.Contains("(401)"))
                {
                    txtSynchroMsg.Text = BalluffConst.Message.Alert_IncorrectCredential;
                    
                }
                else
                {
                    txtSynchroMsg.Text = BalluffConst.Message.ErrorOccured_Synchro;
                }
                SynchroLoginGrid.Visibility = Visibility.Visible;
                SynchroProgressBarGrid.Visibility = Visibility.Collapsed;
                btnSynchroLogin.Visibility = Visibility.Visible;
                btnSynchroClose.Visibility = Visibility.Visible;
            }

        }

        private void ShowProgressBar()
        {
            //txtSynchroErrorMsg.Text = string.Empty;
            SynchroLoginGrid.Visibility = Visibility.Collapsed;
            SynchroProgressBarGrid.Visibility = Visibility.Visible;
            btnSynchroLogin.Visibility = Visibility.Collapsed;
            btnSynchroClose.Visibility = Visibility.Collapsed;
        }

        private void SynchroClose_Click(object sender, RoutedEventArgs e)
        {
            try { 
                SynchroPopupGrid.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void Synchro_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            try
            {
                
                if (e.Key == VirtualKey.Enter)
                {
                    txtSynchroMsg.Text = "";
                    if (!string.IsNullOrEmpty(txtSynchroAccount.Text.Trim()) && !string.IsNullOrEmpty(txtPassword.Password.Trim()))
                    {
                        
                        //设置进度条的模式为不重复状态
                        pbSynchro.IsIndeterminate = false;
                        //启用定时器，再每下一秒改变原来的状态
                        DispatcherTimer timer = new DispatcherTimer();
                        timer.Interval = TimeSpan.FromSeconds(3);
                        timer.Tick += UpdateProgress;
                        timer.Start();
                        ShowProgressBar();
                        //pbSynchro.Value = 50;
                        Synchronize(txtSynchroAccount.Text.Trim(), txtPassword.Password.Trim());

                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
                //incorrect Account or Password
                if (ex.Message.Contains("(401)"))
                {
                    txtSynchroMsg.Text = BalluffConst.Message.Alert_IncorrectCredential;
                    SynchroLoginGrid.Visibility = Visibility.Visible;
                    SynchroProgressBarGrid.Visibility = Visibility.Collapsed;
                }
            }
        }

        private async void WriteLog(string content)
        {          
            try
            {
                StorageFolder localfolder = ApplicationData.Current.LocalFolder;
                StorageFolder logFolder = localfolder.GetFolderAsync(BalluffConst.LogFolderName).AsTask().GetAwaiter().GetResult();
                string logFileName = "log_" + DateTime.Now.ToString("yyyyMMdd") + ".log";
                historyContent += ("\r\n" + content);
                StorageFile logFileToday = await logFolder.GetFileAsync(logFileName);
                using (Stream stream = logFileToday.OpenStreamForWriteAsync().GetAwaiter().GetResult())
                {
                    using (StreamWriter write = new StreamWriter(stream))
                    {

                        write.Write(historyContent);
                        write.Flush();
                    }
                }

            }
            catch(Exception ex)
            {

            }
            
            //await Task.Delay(2000);

        }

        private void CheckCredentialAndVersion()
        {
            try
            {
                ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                string account = localSettings.Values[BalluffConst.LocalSetting.Account] as string;
                string password = localSettings.Values[BalluffConst.LocalSetting.Password] as string;
                if (!string.IsNullOrEmpty(account) && !string.IsNullOrEmpty(password))
                {
                    ContentFrid.Visibility = Visibility.Visible;
                    ActivationGrid.Visibility = Visibility.Collapsed;
                    //download version config
                    string oldVersioon = localSettings.Values[BalluffConst.LocalSetting.Version] as string;
                    if (IsNewVersion(account, password, oldVersioon, "Version/"))
                    {
                        btnSynchro.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        btnSynchro.Visibility = Visibility.Collapsed;
                    }
                }
             }
            catch(Exception ex)
            {
                if (ex.Message.Contains("(401)"))
                {
                    BalluffUtility.RemoveLocalContent();
                    WriteLog(DateTime.Now.ToString() + "Content Removed");
                }
            }
        }

        private async void CheckDomainUser()
        {
            try
            {
                var users = await User.FindAllAsync(UserType.LocalUser);              
                var domainWithUser = (string)await users.FirstOrDefault().GetPropertyAsync(KnownUserProperties.DomainName);
                if (string.IsNullOrEmpty(domainWithUser))
                {
                    WriteLog("");
                    Application.Current.Exit();
                    
                }
                else
                {
                    string domain = domainWithUser.Split('\\')[0];
                    string loginUser = domainWithUser.Split('\\')[1];
                    if (!domain.ToLower().Equals(BalluffConst.Domain.ToLower()))
                    {
                        //close app
                        WriteLog("");
                        Application.Current.Exit();
                    }
                }
                         
            }
            catch(Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private async void CheckActivationCode(string activationCode)
        {
            try
            {
                var users = await User.FindAllAsync(UserType.LocalUser);
                var domainWithUser = (string)await users.FirstOrDefault().GetPropertyAsync(KnownUserProperties.DomainName);
                //string activationCode = "test1 Abcd=1234";
                string strDecode = BalluffUtility.hexStr2Str(activationCode.Trim());
                //string ss = str2HexStr(activationCode);
                string user = strDecode.Split(' ')[0];
                string account = strDecode.Split(' ')[1];
                string password = strDecode.Split(' ')[2];
                if (user.ToLower().Equals(BalluffConst.BlfSpecialAccount.ToLower())){
                    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.Values[BalluffConst.LocalSetting.Account] = account;
                    localSettings.Values[BalluffConst.LocalSetting.Password] = password;
                    localSettings.Values[BalluffConst.LocalSetting.ActivationCode] = activationCode;
                    ContentFrid.Visibility = Visibility.Visible;
                    ActivationGrid.Visibility = Visibility.Collapsed;
                }
                else if (string.IsNullOrEmpty(domainWithUser))
                {
                    txtActivationMsg.Text = BalluffConst.Message.Alert_AccountInfoAccess;
                }
                else if (!domainWithUser.ToLower().Trim().Equals(user.ToLower().Trim()) && !user.ToLower().Equals(BalluffConst.BlfSpecialAccount.ToLower()))
                {
                    txtActivationMsg.Text = BalluffConst.Message.Alert_IncorrectActivationCode + "\r\n" + string.Format(BalluffConst.Message.Alert_CurrentUser, domainWithUser);
                }
                else
                {
                    //SynchronizeVersion(account, password, "Version/");
                    ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
                    localSettings.Values[BalluffConst.LocalSetting.Account] = account;
                    localSettings.Values[BalluffConst.LocalSetting.Password] = password;
                    localSettings.Values[BalluffConst.LocalSetting.ActivationCode] = activationCode;
                    ContentFrid.Visibility = Visibility.Visible;
                    ActivationGrid.Visibility = Visibility.Collapsed;
                }
                

            }
            catch(Exception ex)
            {
                txtActivationMsg.Text = BalluffConst.Message.Alert_IncorrectActivationCode;
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);
            }

        }

        private void AppActive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtActivationCode.Text.Trim()))
                {
                    txtActivationMsg.Text = BalluffConst.Message.Alert_InputActivationCode;
                }
                else
                {
                    CheckActivationCode(txtActivationCode.Text.Trim());
                }
            }catch(Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }
        }

        private void AppClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {           
                //close app
                Application.Current.Exit();
            }catch(Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace);;
                
            }

        }

        private void SearchClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SearchResultsGrid.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace); ;

            }
        }

        private void FavoritesClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FavoritesGrid.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                WriteLog(DateTime.Now.ToString() + ex.Message + "\r\n" + DateTime.Now.ToString() + ex.StackTrace); ;

            }
        }
    }

    public class SearchResults
    {
        public string Uri { get; set; }
        public string MatchedContent { get; set; }

        public SearchResults() { }
        public SearchResults(string uri,string matchedContent)
        { this.Uri = uri;this.MatchedContent = matchedContent;}

    }

    public class Favorites
    {
        public string Uri { get; set; }
        public string Note { get; set; }

        public Favorites() { }
    }
    
}
