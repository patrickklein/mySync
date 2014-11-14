using My_Sync.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private NotifyIcon notifyIcon;

        public MainWindow()
        {
            using (new Logger())
            {
                InitializeComponent();
                InitializeObjects();
                InitializePopup();

                //CheckInternetConnection.IsConnected();
                FolderManagement.CreateSyncFolder();
                FolderManagement.CreateShortcut("test", @"D:\Studium\MSC - Softwareentwicklung\3. Semester\Master Projekt\Projekt\Code\Log\FolderDelete");

                List<SynchronizationPoint> points = new List<SynchronizationPoint>();

                SynchronizationPoint point = new SynchronizationPoint();
                point.ServerType = Helper.GetImageOfAssembly("type1");
                point.Description = "FH Technikum Wien";
                point.Folder = @"D:\Studium\MSC - Softwareentwicklung";
                point.Server = "http://172.123.145.90/mySync";
                points.Add(point);

                point = new SynchronizationPoint();
                point.ServerType = Helper.GetImageOfAssembly("type2");
                point.Description = "Home";
                point.Folder = @"C:\Users\Frank\Home";
                point.Server = "http://home.me/mySync";
                points.Add(point);

                point = new SynchronizationPoint();
                point.ServerType = Helper.GetImageOfAssembly("type3");
                point.Description = "Work";
                point.Folder = @"D:\Work\ABC Company Inc.";
                point.Server = "http://school.me/mySync";
                points.Add(point);

                ServerDGSynchronizationPoints.ItemsSource = points;
            }
        }

        /// <summary>
        /// Initialize all needed objects for GUI and synchronisation
        /// </summary>
        private void InitializeObjects()
        {
            using (new Logger())
            {
                SetLanguageDictionary(MySync.Default.usedLanguage);

                notifyIcon = new NotifyIcon();
                notifyIcon.InitializeNotifyIcon();
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
                PopupTBXDescription.Text = "";
                PopupTBXFolder.Text = "";
                PopupTBXServerEntryPoint.Text = "";
                PopupTBXServerType.Text = "";

                List<Image> imageList = new List<Image>();
                for (int i = 1; i > 0; i++)
                {
                    Image image = Helper.GetImageOfAssembly("type" + i);

                    if (image == null) break;
                    imageList.Add(image);
                }
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
                    case "de-AT":   dict.Source = new Uri(@"..\Resources\German.xaml",  UriKind.Relative); break;
                    case "en-US":   dict.Source = new Uri(@"..\Resources\English.xaml", UriKind.Relative); break;
                    default:        dict.Source = new Uri(@"..\Resources\English.xaml", UriKind.Relative); break;
                }

                this.Resources.MergedDictionaries.Add(dict);
            }
        }

        #region Eventhandler

        #region Filter Tab

        /// <summary>
        /// Adds a new definition of a filter to the filterlist
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void FilterBTNAddTerm_Click(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                if (FilterTBTerm.Text.Trim() != "" && !FilterLVFilter.Items.Contains(FilterTBTerm.Text.Trim()))
                {
                    int index = FilterLVFilter.SelectedIndex;
                    if (index >= 0) FilterLVFilter.Items[index] = FilterTBTerm.Text.Trim();
                    else FilterLVFilter.Items.Add(FilterTBTerm.Text.Trim());
                    FilterTBTerm.Text = "";
                }
            }
        }

        /// <summary>
        /// adds the chosen filter to the textbox for editing
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void FilterBTNEditTerm_Click(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                int index = FilterLVFilter.SelectedIndex;
                if (index >= 0)
                    FilterTBTerm.Text = FilterLVFilter.Items[index].ToString();
            }
        }

        /// <summary>
        /// deletes the chosen filter from the list
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void FilterBTNDeleteTerm_Click(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                int index = FilterLVFilter.SelectedIndex;
                if (index >= 0) FilterLVFilter.Items.RemoveAt(index);
                FilterLVFilter.SelectedIndex = index;
            }
        }

        #endregion

        #region Server Tab 

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

                ServerDGSynchronizationPoints.Columns[0].Header = "";
                ServerDGSynchronizationPoints.Columns[1].MinWidth = 10;

                ServerDGSynchronizationPoints.Columns[1].Header = dict["serverDescription"].ToString();
                ServerDGSynchronizationPoints.Columns[1].MinWidth = 100;
                ServerDGSynchronizationPoints.Columns[1].Width = new DataGridLength(ServerDGSynchronizationPoints.Columns[1].ActualWidth + additionalWidth);

                ServerDGSynchronizationPoints.Columns[2].Header = dict["serverFolder"].ToString();
                ServerDGSynchronizationPoints.Columns[2].MinWidth = 200;
                ServerDGSynchronizationPoints.Columns[2].Width = new DataGridLength(ServerDGSynchronizationPoints.Columns[2].ActualWidth + additionalWidth);

                ServerDGSynchronizationPoints.Columns[3].Header = dict["serverEntryPoint"].ToString();
                ServerDGSynchronizationPoints.Columns[3].MinWidth = 200;
                ServerDGSynchronizationPoints.Columns[3].Width = new DataGridLength(ServerDGSynchronizationPoints.Columns[3].ActualWidth + additionalWidth);
            }
        }

        #endregion

        /// <summary>
        /// Exits the application
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void WindowBTNClose(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                //FolderManagement.SetFolderIcon("C:\\Test");
                //FolderManagement.ResetFolderIcon("C:\\Test");
                FolderManagement.DeleteShortcut();
                FolderManagement.DeleteSyncFolder();
            }

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

        #endregion

        private void PopupDirButton_Click(object sender, RoutedEventArgs e)
        {
            using (new Logger(sender, e))
            {
                System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
                HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
                System.Windows.Forms.IWin32Window win = new FolderBrowserWindow(source.Handle);
                folderBrowserDialog.Description = "Bitte den Ordner auswählen, in welchem die Bilder liegen.";
                folderBrowserDialog.SelectedPath = PopupTBXFolder.Text;
                folderBrowserDialog.ShowDialog(win);

                if (folderBrowserDialog.SelectedPath.ToString() != "")
                    PopupTBXFolder.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            notifyIcon.SetVisibility(false);
        }
    }
}
