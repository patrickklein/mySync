using MySync.Classes;
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

namespace MySync
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
    }
}
