using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public void InitializeNotifyIcon(string iconName = "")
        {
            using (new Logger(iconName))
            {
                mainWindow = ((MainWindow)System.Windows.Application.Current.MainWindow);
                ResourceDictionary dict = mainWindow.Resources.MergedDictionaries.ToList().First();

                MenuItem menuItem0 = new MenuItem();
                menuItem0.Index = 0;
                menuItem0.Text = dict["menuSynchronize"].ToString();
                menuItem0.Enabled = false;
                menuItem0.Click += new System.EventHandler(NotifyIconSync_Click);

                MenuItem menuItem1 = new MenuItem();
                menuItem1.Index = 1;
                menuItem1.Text = dict["menuSettings"].ToString();
                //menuItem1.Click += new System.EventHandler(NotifyIconOpen_Click);

                MenuItem menuItem2 = new MenuItem();
                menuItem2.Index = 2;
                menuItem2.Text = dict["menuOpen"].ToString();
                menuItem2.Click += new System.EventHandler(NotifyIconOpen_Click);

                MenuItem menuItem3 = new MenuItem();
                menuItem3.Index = 3;
                menuItem3.Text = dict["menuClose"].ToString();
                menuItem3.Click += new EventHandler(NotifyIconExit_Click);

                //Initialize contextMenu
                ContextMenu contextMenu = new ContextMenu();
                contextMenu.MenuItems.AddRange(new MenuItem[] { menuItem0, menuItem1, menuItem2, menuItem3 });

                if (notifyIcon != null) notifyIcon.Dispose();
                notifyIcon = new System.Windows.Forms.NotifyIcon(new Container());
                notifyIcon.Text = mainWindow.applicationName;
                string uri = String.Format("/{0};component/Images/Icon/icon{1}.ico", Assembly.GetExecutingAssembly().GetName().Name, iconName);
                Stream iconStream = System.Windows.Application.GetResourceStream(new Uri(uri, UriKind.Relative)).Stream;
                notifyIcon.Icon = new Icon(iconStream);
                notifyIcon.Visible = true;
                notifyIcon.ContextMenu = contextMenu;
                notifyIcon.Click += new EventHandler(NotifyIcon_Click);
                notifyIcon.DoubleClick += new EventHandler(NotifyIcon_DoubleClick);
                notifyIcon.BalloonTipClicked += new EventHandler(NotifyIcon_BalloonTipClicked);
            }
        }

        /// <summary>
        /// Defines if the notification icon should be visible or not
        /// </summary>
        /// <param name="visible">true/false</param>
        public void SetVisibility(bool visible)
        {
            using (new Logger(visible))
            {
                notifyIcon.Visible = visible;
            }
        }

        /// <summary>
        /// Changes the notification icon of the application to a new one
        /// </summary>
        /// <param name="name">name of the icon without "icon" in front of it. Possible values are "", "Download" and "Upload"</param>
        public void ChangeIcon(string name)
        {
            using (new Logger(name))
            {
                InitializeNotifyIcon(name);
            }
        }

        /// <summary>
        /// Resets the notification icon to the original one
        /// </summary>
        public void ResetIcon()
        {
            using(new Logger()) 
            {
                InitializeNotifyIcon();
            }
        }

        #region Eventhandler

        /// <summary>
        /// Event for double-clicking the notification icon (open/close window)
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
                string folderpath = FolderManagement.GetFavoriteFolder();
                if (Directory.Exists(folderpath))
                    Process.Start("explorer.exe", folderpath);
            }
        }

        /// <summary>
        /// Event for single-clicking the notification icon (opens the synchronization folder)
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
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
            }
        }

        /// <summary>
        /// Event for open the notification icon (calls NotifyIcon_DoubleClick)
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void NotifyIconOpen_Click(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
                NotifyIcon_Click(sender, e);
            }
        }

        /// <summary>
        /// Event for menu entry to start synchronizing
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void NotifyIconSync_Click(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
                //btnSync_Click(sender, null);
            }
        }

        /// <summary>
        /// Event for menu entry to close application
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void NotifyIconExit_Click(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
                // Close this window
                mainWindow.Close();
            }
        }

        /// <summary>
        /// Event for click on a shown balloon tip
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
                /*if (ballonNotifier == 1)
                {
                    //Noten_tab.Focus();
                    NotifyIconOpen_Click(null, null);
                }
                else if (ballonNotifier == 2 && fileText.Trim() != null) MessageBox.Show(fileText);
                */
            }
        }

        #endregion
    }
}
