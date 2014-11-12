using My_Sync.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
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
            InitializeComponent();

            using (new Logger())
            {
                InitializeObjects();

                //CheckInternetConnection.IsConnected();
                FolderManagement.CreateSyncFolder();
                //FolderManagement.CreateShortcut("test", @"D:\Studium\MSC - Softwareentwicklung\3. Semester\Master Projekt\Projekt\Code\Log\FolderDelete");
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

                NotifyIcon notifyIcon = new NotifyIcon();
                notifyIcon.InitializeNotifyIcon();
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
                    case "de-AT":
                        dict.Source = new Uri("..\\Resources\\German.xaml", UriKind.Relative);
                        break;
                    case "en-US":
                        dict.Source = new Uri("..\\Resources\\English.xaml", UriKind.Relative);
                        break;
                    default:
                        dict.Source = new Uri("..\\Resources\\English.xaml", UriKind.Relative);
                        break;
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
                //FolderManagement.DeleteShortcut();
                FolderManagement.DeleteSyncFolder(false);
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
    }
}
