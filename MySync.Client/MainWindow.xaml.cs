﻿using Microsoft.Win32;
using My_Sync.Classes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
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

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                InitializeConfiguration();
                InitializeObjects();
                InitializePopup();

                Synchronization.StartTimer();
                MemoryManagement.Reduce();

                //Start synchronisation on application startup
                Synchronization.StartSynchronization();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// Extracts the user configuration file if not exists and loads all the saved settings
        /// </summary>
        private void InitializeConfiguration()
        {
            //Kill existing instances to invoid access restrictions/errors
            int id = Process.GetCurrentProcess().Id;
            foreach (Process instance in Process.GetProcessesByName(AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "")))
            {
                if (instance.Id == id) continue;
                instance.Kill();
            }

            //Extract the user configuration file
            string file = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configuration.xml");
            if (!File.Exists(file)) Helper.ExtractEmbeddedResource(".xml");

            //load settings
            UserPreferences.Load();
        }

        /// <summary>
        /// Initialize all needed objects for GUI and synchronisation
        /// </summary>
        /// <param name="reinitialization">defines if it is the first initialization or not</param>
        private void InitializeObjects(bool reinitialization = false)
        {
            using (new Logger(reinitialization))
            {
                //Set user language from configuration
                SetLanguageDictionary(UserPreferences.usedLanguage);

                //create database and check for installed drivers
                Helper.ExtractEmbeddedResource(".db");

                try
                {
                    MySyncEntities dbInstance = new MySyncEntities();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return;
                }

                //---- name all GUI elements ----//

                //General Tab
                GeneralCBLanguage.SelectedIndex = GeneralCBLanguage.Items.Cast<ComboBoxItem>().Select(x => x.Uid == UserPreferences.usedLanguage).ToList().IndexOf(true);
                GeneralCBInterval.SelectedIndex = GeneralCBInterval.Items.Cast<ComboBoxItem>().Select(x => x.Uid == UserPreferences.synchronizationInterval).ToList().IndexOf(true);
                GeneralCBXShowNotification.IsChecked = UserPreferences.showNotification;
                GeneralCBXAddToFavorites.IsChecked = UserPreferences.addToFavorites;
                GeneralCBXStartAtStartup.IsChecked = UserPreferences.runAtStartup;
                GeneralCBXFastSync.IsChecked = UserPreferences.fastSync;
                
                //Notification Icon
                NotificationIcon.InitializeNotifyIcon();
                if (!reinitialization) NotificationIcon.ShowWindow(false);

                //Fill GUI for Server Entry Point, File Filter, History
                ServerDGSynchronizationPoints.Columns.Clear();
                ServerDGSynchronizationPoints.ItemsSource = DAL.GetServerEntryPoints();
                FilterLVFilter.Columns.Clear();
                FilterLVFilter.ItemsSource = DAL.GetFileFilters();
                HistoryRTBHistory.Document.Blocks.Clear();
                HistoryRTBHistory.AppendText(DAL.GetHistory());

                //Creates a filewatcher for every server entry point
                Synchronization.RefreshWatcher();

                //Checking the filesystem and database for new/changed/deleted files and folders
                Task.Run(() => Synchronization.CheckForDifferencies());
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
                PopupAdd.Visibility = Visibility.Collapsed;
                PopupDelete.Visibility = Visibility.Collapsed;

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
                    default:      dict.Source = new Uri(@"..\Resources\Language\English.xaml", UriKind.Relative); break;
                }

                this.Resources.MergedDictionaries.Clear();
                this.Resources.MergedDictionaries.Add(dict);
            }
        }

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

        #region Eventhandler

        #region General Tab

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
                UserPreferences.usedLanguage = uid;
                UserPreferences.Save();

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
                    UserPreferences.synchronizationInterval = uid;
                    UserPreferences.Save();

                    Synchronization.RefreshWatcher();
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
                    GeneralCBInterval.IsEnabled = false;
                else
                {
                    GeneralCBInterval.SelectedIndex = GeneralCBInterval.Items.Cast<ComboBoxItem>().Select(x => x.Uid == UserPreferences.synchronizationInterval).ToList().IndexOf(true);
                    GeneralCBInterval.IsEnabled = true;
                }

                //save selection to settings
                UserPreferences.fastSync = status;
                UserPreferences.Save();
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
                UserPreferences.runAtStartup = (bool)GeneralCBXStartAtStartup.IsChecked;
                UserPreferences.Save();
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
                UserPreferences.showNotification = (bool)GeneralCBXShowNotification.IsChecked;
                UserPreferences.Save();
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
                UserPreferences.addToFavorites = (bool)GeneralCBXAddToFavorites.IsChecked;
                UserPreferences.Save();
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
        /// Generates the datagrid columns and adds the chosen servertype images
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void FilterLVFilter_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            using (new Logger(sender, e))
            {
                if (e.PropertyName != "id") return;
                e.Cancel = true;
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
                if (FilterTBTerm.Text.Trim() != "" && !DAL.GetFileFilterTerms().Contains(FilterTBTerm.Text.Trim()))
                {
                    FileFilter newFilter = new FileFilter();
                    newFilter.term = FilterTBTerm.Text.Trim();
                    DAL.AddFileFilter(newFilter);

                    FilterLVFilter.ItemsSource = DAL.GetFileFilters();
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
                if (index == -1) return;

                FileFilter selectedFileFilter = FilterLVFilter.Items[index] as FileFilter;
                DAL.DeleteFileFilter(selectedFileFilter.term);

                FilterLVFilter.ItemsSource = DAL.GetFileFilters();
                FilterLVFilter.Items.Refresh();
            }
        }

        #endregion

        #region Server Tab 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerEntryPointConfirm_Click(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                int index = ServerDGSynchronizationPoints.SelectedIndex;
                if (index == -1) return;

                SynchronizationPoint selectedSyncPoint = ServerDGSynchronizationPoints.Items[index] as SynchronizationPoint;

                DeleteServerEntryPoint(selectedSyncPoint);
                Synchronization.DeleteFromServer(selectedSyncPoint.Server.Replace("/Upload", "/Delete"), selectedSyncPoint.Folder.Split('\\').Last());

                //Close popup window
                ClosePopup_Click(sender, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerEntryPointDeny_Click(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                int index = ServerDGSynchronizationPoints.SelectedIndex;
                if (index == -1) return;

                SynchronizationPoint selectedSyncPoint = ServerDGSynchronizationPoints.Items[index] as SynchronizationPoint;
                DeleteServerEntryPoint(selectedSyncPoint);

                //Close popup window
                ClosePopup_Click(sender, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="selectedSyncPoint"></param>
        private void DeleteServerEntryPoint(SynchronizationPoint selectedSyncPoint)
        {
            using (new Logger(selectedSyncPoint))
            {
                DAL.DeleteServerEntryPoint(selectedSyncPoint.Description);

                ServerDGSynchronizationPoints.ItemsSource = DAL.GetServerEntryPoints();
                ServerDGSynchronizationPoints.Items.Refresh();

                //Check for favorites folder and deletes the related link
                AddToFavorites_Check(null, null);

                Synchronization.RefreshWatcher();
            }
        }

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
                PopupAdd.Visibility = Visibility.Visible;
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

                InitializePopup();
                PopupDelete.Visibility = Visibility.Visible;
                PopupWindow.Visibility = Visibility.Visible;
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

                Uri uriResult = null;
                Uri.TryCreate(PopupTBXServerEntryPoint.Text.Trim(), UriKind.Absolute, out uriResult);

                //an image must be selected
                error.Add(ValidateGUIElement(PopupTBLServerType, (PopupTBXServerType.SelectedIndex != -1)));                                                         
                //description must not be empty and must not exist already
                error.Add(ValidateGUIElement(PopupTBLDescription, !String.IsNullOrEmpty(PopupTBXDescription.Text.Trim()) && !DAL.GetServerEntryPoints().Exists(x => x.Description.ToLower().Equals(PopupTBXDescription.Text.Trim().ToLower()))));
                //folder must not be empty and valid in the file system and must not be exist in another server entry point
                error.Add(ValidateGUIElement(PopupTBLFolder, Directory.Exists(PopupTBXFolder.Text.Trim()) && !DAL.GetServerEntryPoints().Exists(x => x.Folder.StartsWith(PopupTBXFolder.Text.Trim()) || PopupTBXFolder.Text.Trim().StartsWith(x.Folder))));
                //server entry point must not be empty and have a valid URI string
                error.Add(ValidateGUIElement(PopupTBLServerEntryPoint, !String.IsNullOrEmpty(PopupTBXServerEntryPoint.Text.Trim()) && uriResult != null && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)));

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
                Synchronization.RefreshWatcher();
                Task.Run(() => Synchronization.DBAddAllFromFolder(point));
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
                ServerDGSynchronizationPoints.Columns[3].MinWidth = 250;
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
                NotificationIcon.ShowWindow(false);
            }
        }

        /// <summary>
        /// Exits the application and saves all changed values
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void WindowBTNClose(object sender, RoutedEventArgs e)
        {
            UserPreferences.Save();
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
                NotificationIcon.SetVisibility(false);
            }
        }

        #endregion
    }
}
