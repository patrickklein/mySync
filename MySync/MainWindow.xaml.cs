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
        public MainWindow()
        {
            Logger.WriteHeader();

            InitializeComponent();
            InitializeObjects();

            //CheckInternetConnection.IsConnected();

            Logger.WriteFooter();
        }

        /// <summary>
        /// Initialize all needed objects for GUI and synchronisation
        /// </summary>
        private void InitializeObjects()
        {
            Logger.WriteHeader();

            SetLanguageDictionary();

            NotifyIcon notifyIcon = new NotifyIcon();
            notifyIcon.InitializeNotifyIcon();

            Logger.WriteFooter();
        }

        /// <summary>
        /// Method to define the current language resource file
        /// </summary>
        private void SetLanguageDictionary(string cultureCode = "")
        {
            Logger.WriteHeader();

            ResourceDictionary dict = new ResourceDictionary();
            switch (Thread.CurrentThread.CurrentCulture.ToString())
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

            Logger.WriteFooter();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterBTNAddTerm_Click(object sender, RoutedEventArgs e)
        {
            if (FilterTBTerm.Text.Trim() != "" && !FilterLVFilter.Items.Contains(FilterTBTerm.Text.Trim()))
            {
                int index = FilterLVFilter.SelectedIndex;
                if (index >= 0) FilterLVFilter.Items[index] = FilterTBTerm.Text.Trim();
                else FilterLVFilter.Items.Add(FilterTBTerm.Text.Trim());
                FilterTBTerm.Text = "";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterBTNEditTerm_Click(object sender, RoutedEventArgs e)
        {
            int index = FilterLVFilter.SelectedIndex;
            if (index >= 0)
                FilterTBTerm.Text = FilterLVFilter.Items[index].ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterBTNDeleteTerm_Click(object sender, RoutedEventArgs e)
        {
            int index = FilterLVFilter.SelectedIndex;
            if (index >= 0) FilterLVFilter.Items.RemoveAt(index);
            FilterLVFilter.SelectedIndex = index;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowBTNClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
