using Microsoft.Win32;
using My_Sync.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace My_Sync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string applicationName = "MySync";
        private NotifyIcon notifyIcon = new NotifyIcon();
        private List<string> fileFilter = new List<string>();
        private MySyncEntities dbInstance = new MySyncEntities();

        public MainWindow()
        {
            using (new Logger())
            {
                try
                {
                    InitializeComponent();
                    InitializeObjects();
                    InitializePopup();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

                //notifyIcon.ChangeIcon("Upload");
                //notifyIcon.ChangeIcon("Download");
                //notifyIcon.ResetIcon();
                //CheckInternetConnection.IsConnected
            }
        }

        /// <summary>
        /// Initialize all needed objects for GUI and synchronisation
        /// </summary>
        /// <param name="reinitialization">defines if it is the first initialization or not</param>
        private void InitializeObjects(bool reinitialization = false)
        {
            using (new Logger(reinitialization))
            {
                //Set user language, create database and name all elements
                SetLanguageDictionary(MySync.Default.usedLanguage);
                DAL.CreateDatabase();

                //General Tab
                GeneralCBLanguage.SelectedIndex = GeneralCBLanguage.Items.Cast<ComboBoxItem>().Select(x => x.Uid == MySync.Default.usedLanguage).ToList().IndexOf(true);
                GeneralCBInterval.SelectedIndex = GeneralCBInterval.Items.Cast<ComboBoxItem>().Select(x => x.Uid == MySync.Default.synchronizationInterval).ToList().IndexOf(true);
                GeneralCBXShowNotification.IsChecked = MySync.Default.showNotification;
                GeneralCBXAddToFavorites.IsChecked = MySync.Default.addToFavorites;
                GeneralCBXStartAtStartup.IsChecked = MySync.Default.runAtStartup;
                GeneralCBXFastSync.IsChecked = MySync.Default.fastSync;

                //Notification Icon
                if (!reinitialization)
                {
                    notifyIcon.InitializeNotifyIcon();
                    notifyIcon.ShowWindow(false);
                }

                //Fill GUI for Server Entry Point, File Filter, History
                ServerDGSynchronizationPoints.Columns.Clear();
                ServerDGSynchronizationPoints.ItemsSource = DAL.GetServerEntryPoints();
                FilterLVFilter.Columns.Clear();
                FilterLVFilter.ItemsSource = DAL.GetFileFilters().Select(x => new { Value = x.term }).ToList();
                HistoryRTBHistory.Document.Blocks.Clear();
                HistoryRTBHistory.AppendText(DAL.GetHistory());      
         
                //Creates a filewatcher for every server entry point
                DAL.GetServerEntryPoints().ForEach(x => Synchronization.AddWatcher(x.Folder));
            }
        }

        /// <summary>
        /// Initialize all needed objects for the popup window
        /// </summary>
        private void InitializePopup()
        {
            using (new Logger())
            {
                PopupWindow.Visibility = Visibility.Hidden;

                //Label Color
                PopupTBLServerType.Foreground = Brushes.Black;
                PopupTBLDescription.Foreground = Brushes.Black;
                PopupTBLFolder.Foreground = Brushes.Black;
                PopupTBLServerEntryPoint.Foreground = Brushes.Black;

                //Input fields
                PopupTBXDescription.Text = "";
                PopupTBXFolder.Text = "";
                PopupTBXServerEntryPoint.Text = "";
                PopupTBXServerType.Text = "";

                List<ServerTypeImage> imageList = new List<ServerTypeImage>();
                for (int i = 1; i > 0; i++)
                {
                    BitmapImage image = Helper.GetBitmapImageOfAssembly("type" + i);
                    
                    if (image == null) break;
                    ServerTypeImage type = new ServerTypeImage();
                    type.Id = i;
                    type.Image = image;
                    type.ServerType = "type" + i;
                    imageList.Add(type);
                }

                PopupTBXServerType.ItemsSource = imageList;
            }
        }

        /// <summary>
        /// Method to define the current language resource file
        /// </summary>
        /// <param name="cultureCode">saved cultureCode coming from the user settings (if available)</param>
        private void SetLanguageDictionary(string cultureCode = "")
        {
            using (new Logger(cultureCode))
            {
                ResourceDictionary dict = new ResourceDictionary();
                cultureCode = (cultureCode == "") ? Thread.CurrentThread.CurrentCulture.ToString() : cultureCode;

                switch (cultureCode)
                {
                    case "de-AT": dict.Source = new Uri(@"..\Resources\Language\German.xaml",  UriKind.Relative); break;
                    case "en-US": dict.Source = new Uri(@"..\Resources\Language\English.xaml", UriKind.Relative); break;
                    default: dict.Source = new Uri(@"..\Resources\Language\English.xaml", UriKind.Relative); break;
                }

                this.Resources.MergedDictionaries.Clear();
                this.Resources.MergedDictionaries.Add(dict);
            }
        }

        #region Eventhandler

        #region General Tab

        public void test()
        {
            using (var client = new HttpClient())
            {
                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(@"C:\Users\Patrick\Desktop\Hive Operators and Functions.mp4"));
                    fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = "Hive Operators and Functions.mp4",
                        Name = "UploadedFile"
                    };
                    content.Add(fileContent);

                    var requestUri = "http://localhost:51992/Account/Upload";
                    var result = client.PostAsync(requestUri, content).Result;
                }
            }
        }
        /// <summary>
        /// Changes the app language to the selected one and saves the chosen value in the settings file
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void GeneralCBLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                string text = ((ContentControl)((sender as ComboBox).SelectedItem)).Content.ToString();
                string uid = ((ContentControl)((sender as ComboBox).SelectedItem)).Uid.ToString();

                SetLanguageDictionary(uid);

                //save selection to settings
                Helper.SaveSetting("usedLanguage", uid);

                InitializeObjects(true);
            }
        }

        /// <summary>
        /// Changes the synchronization interval to the selected one, restarts the timer and saves the chosen value in the settings file
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void GeneralCBInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            using (new Logger(sender, e))
            {

                if (GeneralCBInterval.SelectedIndex != -1)
                {
                    string text = ((ContentControl)(GeneralCBInterval.SelectedItem)).Content.ToString();
                    string uid = ((ContentControl)(GeneralCBInterval.SelectedItem)).Uid.ToString();

                    //save selection to settings
                    Helper.SaveSetting("synchronizationInterval", uid);
                }
            }
        }

        /// <summary>
        /// Defines if the app should synchronize immediately after a change and saves the chosen value in the settings file
        /// Disables the interval combobox if fastsync is checked
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void GeneralCBXFastSync_Check(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                bool status = (bool)GeneralCBXFastSync.IsChecked;
                if (status)
                {
                    GeneralCBInterval.SelectedIndex = -1;
                    GeneralCBInterval.IsEnabled = false;
                }
                else
                {
                    test();
                    GeneralCBInterval.SelectedIndex = GeneralCBInterval.Items.Cast<ComboBoxItem>().Select(x => x.Uid == MySync.Default.synchronizationInterval).ToList().IndexOf(true);
                    GeneralCBInterval.IsEnabled = true;
                }

                Helper.SaveSetting("fastSync", status);
            }
        }

        /// <summary>
        /// Defines if the app is started at windows boot and saves the chosen value in the settings file
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void StartAtStartup_Check(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                //The path to the key where Windows looks for startup applications
                RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                //Name of Assembly
                var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);

                //Add/remove the value in the registry so that the application runs/not runs at startup
                if ((bool)GeneralCBXStartAtStartup.IsChecked) 
                    rkApp.SetValue(nameHelper.Name, Assembly.GetExecutingAssembly().Location);
                else 
                    rkApp.DeleteValue(nameHelper.Name, false);

                //save selection to settings
                Helper.SaveSetting("runAtStartup", (bool)GeneralCBXStartAtStartup.IsChecked);
            }
        }

        /// <summary>
        /// Defines the user notification status and saves the chosen value in the settings file
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void ShowNotification_Check(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                //save selection to settings
                Helper.SaveSetting("showNotification", (bool)GeneralCBXShowNotification.IsChecked);
            }
        }

        /// <summary>
        /// Creates/Deletes the synchronisation folder in user favorites and saves the chosen value in the settings file
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void AddToFavorites_Check(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                if ((bool)GeneralCBXAddToFavorites.IsChecked)
                {
                    FolderManagement.DeleteSyncFolder();
                    FolderManagement.CreateSyncFolder();

                    foreach (SynchronizationPoint item in ServerDGSynchronizationPoints.Items)
                        FolderManagement.CreateShortcut(item.Description, item.Folder);
                }
                else
                {
                    FolderManagement.DeleteShortcut();
                    FolderManagement.DeleteSyncFolder();
                }

                //save selection to settings
                Helper.SaveSetting("addToFavorites", (bool)GeneralCBXAddToFavorites.IsChecked);
            }
        }

        #endregion

        #region Filter Tab

        /// <summary>
        /// Renames the header of the datagrid column and defines the width property
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void FilterLVFilter_AutoGeneratedColumns(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
                //Rename column
                ResourceDictionary dict = this.Resources.MergedDictionaries.ToList().First();
                FilterLVFilter.Columns[0].Header = dict["filterDescription"].ToString();
                FilterLVFilter.Columns[0].Width = TABControl.ActualWidth - 18;
            }
        }

        /// <summary>
        /// Adds a new definition of a filter to the filterlist
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void FilterBTNAddTerm_Click(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                if (FilterTBTerm.Text.Trim() != "" && !fileFilter.Contains(FilterTBTerm.Text.Trim()))
                {
                    fileFilter.Add(FilterTBTerm.Text.Trim());
                    FilterLVFilter.ItemsSource = fileFilter.Select(x => new { Value = x }).ToList();
                    FilterLVFilter.Items.Refresh();
                    FilterTBTerm.Text = "";
                }
            }
        }

        /// <summary>
        /// Deletes the chosen filter from the list
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void FilterBTNDeleteTerm_Click(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                int index = FilterLVFilter.SelectedIndex;
                if (index >= 0) fileFilter.RemoveAt(index);
                FilterLVFilter.ItemsSource = fileFilter.Select(x => new { Value = x }).ToList();
                FilterLVFilter.Items.Refresh();
            }
        }

        #endregion

        #region Server Tab 

        /// <summary>
        /// Opens a folder browser dialog for choosing the directory which should get be synchronized and adds the chosen path to the related textbox
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void PopupDirButton_Click(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
                HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
                System.Windows.Forms.IWin32Window win = new FolderBrowserWindow(source.Handle);
                ResourceDictionary dict = this.Resources.MergedDictionaries.ToList().First();
                folderBrowserDialog.Description = dict["serverPopupBrowserText"].ToString();
                folderBrowserDialog.SelectedPath = PopupTBXFolder.Text;
                folderBrowserDialog.ShowDialog(win);

                if (folderBrowserDialog.SelectedPath.ToString() != "")
                    PopupTBXFolder.Text = folderBrowserDialog.SelectedPath;
            }
        }

        /// <summary>
        /// Opens the server entry point popup for adding a new server entry point
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void ServerBTNAddTerm_Click(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                InitializePopup();
                PopupWindow.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Deletes the chosen server entry point from the list
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void ServerBTNDeleteTerm_Click(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                int index = ServerDGSynchronizationPoints.SelectedIndex;
                if (index == -1) return;

                SynchronizationPoint selectedSyncPoint = ServerDGSynchronizationPoints.Items[index] as SynchronizationPoint;
                DAL.DeleteServerEntryPoint(selectedSyncPoint.Description);

                ServerDGSynchronizationPoints.ItemsSource = DAL.GetServerEntryPoints();
                ServerDGSynchronizationPoints.Items.Refresh();

                //Check for favorites folder and deletes the related link
                AddToFavorites_Check(sender, e);

                Synchronization.RefreshWatcher();
            }
        }

        /// <summary>
        /// Closes the server popup window
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                InitializePopup();
            }
        }

        /// <summary>
        /// Checks if all fields are correctly filled out and creates a new server entry point
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void CreateNewServerEntryPoint_Click(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                //Validate inputs
                List<bool> error = new List<bool>();
                error.Add(ValidateGUIElement(PopupTBLServerType, (PopupTBXServerType.SelectedIndex != -1)));
                error.Add(ValidateGUIElement(PopupTBLDescription, !String.IsNullOrEmpty(PopupTBXDescription.Text.Trim()) && !DAL.GetServerEntryPoints().Exists(x => x.Description.Equals(PopupTBXDescription.Text.Trim()))));
                error.Add(ValidateGUIElement(PopupTBLFolder, Directory.Exists(PopupTBXFolder.Text.Trim())));
                error.Add(ValidateGUIElement(PopupTBLServerEntryPoint, !String.IsNullOrEmpty(PopupTBXServerEntryPoint.Text.Trim())));

                if (error.Contains(false)) return;

                //Add new server entry point
                SynchronizationPoint point = new SynchronizationPoint();
                point.ServerType = Helper.GetImageOfAssembly("type" + ((ServerTypeImage)PopupTBXServerType.SelectedItem).Id);
                point.Description = PopupTBXDescription.Text.Trim();
                point.Folder = PopupTBXFolder.Text.Trim();
                point.Server = PopupTBXServerEntryPoint.Text.Trim();

                DAL.AddServerEntryPoint(point);

                ServerDGSynchronizationPoints.ItemsSource = DAL.GetServerEntryPoints();
                ServerDGSynchronizationPoints.Items.Refresh();

                //Close popup window and check for favorites folder and adds a related link
                ClosePopup_Click(sender, e);
                AddToFavorites_Check(sender, e);

                //Adds all files and directories to the database
                Synchronization.AddAllFromFolder(point);
                Synchronization.RefreshWatcher();
            }
        }

        /// <summary>
        /// Generates the datagrid columns and adds the chosen servertype images
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void ServerDGSynchronizationPoints_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            using (new Logger(sender, e))
            {
                if (e.PropertyName != "ServerType") return;
                e.Cancel = true;
                
                DataGridTemplateColumn imageObject = FindResource("serverTypeTemplate") as DataGridTemplateColumn;
                if (imageObject == null) return;
                
                if(!ServerDGSynchronizationPoints.Columns.Contains(imageObject))
                    ServerDGSynchronizationPoints.Columns.Add(imageObject);
            }
        }

        /// <summary>
        /// Renames the header of the datagrid columns and defines the width property
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void ServerDGSynchronizationPoints_AutoGeneratedColumns(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
                //Rename columns
                ResourceDictionary dict = this.Resources.MergedDictionaries.ToList().First();
                int additionalWidth = 20;

                if (ServerDGSynchronizationPoints.Columns.Count == 0) return;

                ServerDGSynchronizationPoints.Columns[0].Header = "";
                ServerDGSynchronizationPoints.Columns[0].MinWidth = 20;

                ServerDGSynchronizationPoints.Columns[1].Header = dict["serverDescription"].ToString();
                ServerDGSynchronizationPoints.Columns[1].MinWidth = 110;
                ServerDGSynchronizationPoints.Columns[1].Width = new DataGridLength(ServerDGSynchronizationPoints.Columns[1].ActualWidth + additionalWidth);

                ServerDGSynchronizationPoints.Columns[2].Header = dict["serverFolder"].ToString();
                ServerDGSynchronizationPoints.Columns[2].MinWidth = 210;
                ServerDGSynchronizationPoints.Columns[2].Width = new DataGridLength(ServerDGSynchronizationPoints.Columns[2].ActualWidth + additionalWidth);

                ServerDGSynchronizationPoints.Columns[3].Header = dict["serverEntryPoint"].ToString();
                ServerDGSynchronizationPoints.Columns[3].MinWidth = 200;
                ServerDGSynchronizationPoints.Columns[3].Width = new DataGridLength(ServerDGSynchronizationPoints.Columns[3].ActualWidth + additionalWidth);
            }
        }

        #endregion

        /// <summary>
        /// Minimizes the application
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void WindowBTNMinimize(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                notifyIcon.ShowWindow(false);
            }
        }

        /// <summary>
        /// Exits the application and saves all changed values
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void WindowBTNClose(object sender, RoutedEventArgs e)
        {
            MySync.Default.Save();
            Close();
        }

        /// <summary>
        /// Sets the window position to the bottom right corner
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                this.Left = SystemParameters.WorkArea.Right - this.Width;
                this.Top = SystemParameters.WorkArea.Bottom - this.Height;
            }
        }

        /// <summary>
        /// Disposes the notification icon if application is closing (if not the icon stays in the windows statusbar)
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            using (new Logger(sender, e))
            {
                notifyIcon.SetVisibility(false);
            }
        }

        #endregion

        /// <summary>
        /// Validates the given condition and colors the label red (false) or black (true)
        /// </summary>
        /// <param name="guiLabel">label element which got validated</param>
        /// <param name="condition">defines in which color the label get painted</param>
        private bool ValidateGUIElement(TextBlock guiLabel, bool condition)
        {
            using (new Logger(guiLabel, condition))
            {
                if (condition) guiLabel.Foreground = Brushes.Black;
                else guiLabel.Foreground = Brushes.Red;

                return condition;
            }
        }
    }
}
