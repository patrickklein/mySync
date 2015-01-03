using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace My_Sync.Classes
{
    public static class NotifyIcon
    {
        private static System.Windows.Forms.NotifyIcon notifyIcon;
        private static MainWindow mainWindow;

        /// <summary>
        /// Initializes the notification icon and adds some menu entries to the contextmenu
        /// </summary>
        public static void InitializeNotifyIcon(string iconName = "")
        {
            using (new Logger(iconName))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    mainWindow = ((MainWindow)System.Windows.Application.Current.MainWindow);
                });

                ResourceDictionary dict = mainWindow.Resources.MergedDictionaries.ToList().First();
                if (notifyIcon != null) notifyIcon.Dispose();

                MenuItem menuItem0 = new MenuItem();
                menuItem0.Index = 0;
                menuItem0.Text = dict["menuSynchronize"].ToString();
                menuItem0.Click += new System.EventHandler(MenuSync_Click);

                MenuItem menuItem1 = new MenuItem();
                menuItem1.Index = 1;
                menuItem1.Text = dict["menuSettings"].ToString();
                menuItem1.Click += new System.EventHandler(MenuOpen_Click);

                MenuItem menuItem2 = new MenuItem();
                menuItem2.Index = 2;
                menuItem2.Text = dict["menuOpen"].ToString();
                menuItem2.Click += new System.EventHandler(MenuOpenSyncFolder_Click);

                MenuItem menuItem3 = new MenuItem();
                menuItem3.Index = 3;
                menuItem3.Text = dict["menuClose"].ToString();
                menuItem3.Click += new EventHandler(MenuExit_Click);

                //Initialize context menu
                ContextMenu contextMenu = new ContextMenu();
                contextMenu.MenuItems.AddRange(new MenuItem[] { menuItem0, menuItem1, menuItem2, menuItem3 });

                notifyIcon = new System.Windows.Forms.NotifyIcon(new Container());
                notifyIcon.Text = mainWindow.applicationName;
                string uri = String.Format("/{0};component/Images/Icon/icon{1}.ico", Assembly.GetExecutingAssembly().GetName().Name, iconName);
                Stream iconStream = System.Windows.Application.GetResourceStream(new Uri(uri, UriKind.Relative)).Stream;
                notifyIcon.Icon = new Icon(iconStream);
                notifyIcon.Visible = true;
                notifyIcon.ContextMenu = contextMenu;
                notifyIcon.DoubleClick += new EventHandler(NotifyIcon_DoubleClick);
                notifyIcon.BalloonTipClicked += new EventHandler(NotifyIcon_BalloonTipClicked);
            }
        }

        /// <summary>
        /// Defines if the notification icon should be visible or not
        /// </summary>
        /// <param name="visible">true/false</param>
        public static void SetVisibility(bool visible)
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
        public static void ChangeIcon(string name)
        {
            using (new Logger(name))
            {
                InitializeNotifyIcon(name);
            }
        }

        /// <summary>
        /// Resets the notification icon to the original one
        /// </summary>
        public static void ResetIcon()
        {
            using(new Logger()) 
            {
                InitializeNotifyIcon();
            }
        }

        public static void ChangeSyncState(bool state)
        {
            using (new Logger(state))
            {
                int index = 0;
                ResourceDictionary dict = mainWindow.Resources.MergedDictionaries.ToList().First();
                foreach (MenuItem item in notifyIcon.ContextMenu.MenuItems)
                {
                    if (item.Text == dict["menuSynchronize"].ToString())
                    {
                        index = item.Index;
                        break;
                    }
                }

                notifyIcon.ContextMenu.MenuItems[index].Enabled = state;
            }
        }

        /// <summary>
        /// Defines if the window is shown or not
        /// </summary>
        /// <param name="show">yes/no</param>
        public static void ShowWindow(bool show)
        {
            using (new Logger(show))
            {
                if (show)
                {
                    mainWindow.ShowInTaskbar = false;
                    mainWindow.Show();
                    mainWindow.Visibility = Visibility.Visible;
                    mainWindow.WindowState = WindowState.Normal;
                }
                else
                {
                    mainWindow.WindowState = WindowState.Minimized;
                    mainWindow.Visibility = Visibility.Hidden;
                    mainWindow.ShowInTaskbar = false;
                }
            }
        }

        #region Eventhandler

        /// <summary>
        /// Event for open the synchronization folder
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private static void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
                ShowWindow(mainWindow.Visibility != Visibility.Visible);
            }
        }

        /// <summary>
        /// Event for open the synchronization folder
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private static void MenuOpenSyncFolder_Click(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
                string folderpath = FolderManagement.GetFavoriteFolder();
                if (Directory.Exists(folderpath))
                    Process.Start("explorer.exe", folderpath);
            }
        }

        /// <summary>
        /// Event for open the synchronization folder
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private static void MenuOpen_Click(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
                ShowWindow(true);
            }
        }

        /// <summary>
        /// Event for menu entry to start synchronizing
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private static void MenuSync_Click(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
                ChangeSyncState(false);
                Synchronization.StartSynchronization();
            }
        }

        /// <summary>
        /// Event for menu entry to close application
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private static void MenuExit_Click(object sender, EventArgs e)
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
        private static void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
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
