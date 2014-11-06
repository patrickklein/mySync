using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace My_Sync.Classes
{
    class NotifyIcon
    {
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private MainWindow mainWindow;

        /// <summary>
        /// Initializes the notification icon and adds some menu entries to the contextmenu
        /// </summary>
        public void InitializeNotifyIcon()
        {
            Logger.WriteHeader();

            mainWindow = ((MainWindow)System.Windows.Application.Current.MainWindow);

            MenuItem menuItem0 = new MenuItem();
            menuItem0.Index = 0;
            menuItem0.Text = "Synchronize folders";
            menuItem0.Enabled = false;
            menuItem0.Click += new System.EventHandler(NotifyIconSync_Click);

            MenuItem menuItem1 = new MenuItem();
            menuItem1.Index = 0;
            menuItem1.Text = "Öffnen";
            menuItem1.Click += new System.EventHandler(NotifyIconOpen_Click);

            MenuItem menuItem2 = new MenuItem();
            menuItem2.Index = 1;
            menuItem2.Text = "Beenden";
            menuItem2.Click += new EventHandler(NotifyIconExit_Click);

            //Initialize contextMenu
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.AddRange(new MenuItem[] { menuItem0, menuItem1, menuItem2 });

            notifyIcon = new System.Windows.Forms.NotifyIcon(new Container());
            notifyIcon.Text = "MySync";
            //Stream iconStream = System.Windows.Application.GetResourceStream(new Uri("/MySync;component/favicon.ico", UriKind.Relative)).Stream;
            //notifyIcon.Icon = new Icon(iconStream);
            notifyIcon.Visible = true;
            notifyIcon.ContextMenu = contextMenu;
            notifyIcon.DoubleClick += new EventHandler(NotifyIcon_DoubleClick);
            notifyIcon.BalloonTipClicked += new EventHandler(NotifyIcon_BalloonTipClicked);

            Logger.WriteFooter();
        }

        #region Eventhandler

        /// <summary>
        /// Event for double-clicking the notification icon (open/close window)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Logger.WriteHeader();

            if (mainWindow.ShowInTaskbar)
            {
                mainWindow.WindowState = WindowState.Minimized;
                mainWindow.Visibility = Visibility.Hidden;
                mainWindow.ShowInTaskbar = false;
            }
            else
            {
                mainWindow.ShowInTaskbar = true;
                mainWindow.Show();
                mainWindow.Visibility = Visibility.Visible;
                mainWindow.WindowState = WindowState.Normal;
            }

            Logger.WriteFooter();
        }

        /// <summary>
        /// Event for open the notification icon (calls NotifyIcon_DoubleClick)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyIconOpen_Click(object sender, EventArgs e)
        {
            Logger.WriteHeader();

            NotifyIcon_DoubleClick(null, null);

            Logger.WriteFooter();
        }

        /// <summary>
        /// Event for menu entry to start synchronizing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyIconSync_Click(object sender, EventArgs e)
        {
            Logger.WriteHeader();

            //btnSync_Click(sender, null);

            Logger.WriteFooter();
        }

        /// <summary>
        /// Event for menu entry to close application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyIconExit_Click(object sender, EventArgs e)
        {
            Logger.WriteHeader();

            // Close this window
            mainWindow.Close();

            Logger.WriteFooter();
        }

        /// <summary>
        /// Event for click on a shown balloon tip
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            Logger.WriteHeader();

            /*if (ballonNotifier == 1)
            {
                //Noten_tab.Focus();
                NotifyIconOpen_Click(null, null);
            }
            else if (ballonNotifier == 2 && fileText.Trim() != null) MessageBox.Show(fileText);
            */

            Logger.WriteFooter();
        }

        #endregion
    }
}
