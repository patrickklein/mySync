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
    public static class NotificationIcon
    {
        private static System.Windows.Forms.NotifyIcon notifyIcon;
        private static MainWindow mainWindow;
        private static BallonNotifier notifier;

        enum Notifier { FILESIZE, DISKSPACE, SYNC, CONFLICT };

        struct BallonNotifier
        {
            public Notifier notifier;
            public string message;
            public string title;
            public string filename;
            public string path;
            public ToolTipIcon icon;
        }

        /// <summary>
        /// Initializes the notification icon and adds some menu entries to the contextmenu
        /// </summary>
        public static void InitializeNotifyIcon()
        {
            using (new Logger())
            {
                System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    mainWindow = ((MainWindow)System.Windows.Application.Current.MainWindow);
                });

                ResourceDictionary dict = mainWindow.Resources.MergedDictionaries.ToList().First();

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

                if (notifyIcon == null)
                {
                    notifyIcon = new System.Windows.Forms.NotifyIcon(new Container());
                    notifyIcon.ContextMenu = contextMenu;
                    notifyIcon.DoubleClick += new EventHandler(NotifyIcon_DoubleClick);
                    notifyIcon.BalloonTipClicked += new EventHandler(NotifyIcon_BalloonTipClicked);
                }

                notifyIcon.Text = mainWindow.applicationName;
                string uri = String.Format("/{0};component/Images/Icon/icon.ico", Assembly.GetExecutingAssembly().GetName().Name);
                Stream iconStream = System.Windows.Application.GetResourceStream(new Uri(uri, UriKind.Relative)).Stream;
                notifyIcon.Icon = new Icon(iconStream);
                notifyIcon.Visible = true;
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
                if (notifyIcon == null) return;
                notifyIcon.Visible = visible;
            }
        }

        /// <summary>
        /// Changes the notification icon of the application to a new one
        /// </summary>
        /// <param name="iconName">name of the icon without "icon" in front of it. Possible values are "", "Download" and "Upload"</param>
        public static void ChangeIcon(string iconName)
        {
            using (new Logger(iconName))
            {
                string uri = String.Format("/{0};component/Images/Icon/icon{1}.ico", Assembly.GetExecutingAssembly().GetName().Name, iconName);
                Stream iconStream = System.Windows.Application.GetResourceStream(new Uri(uri, UriKind.Relative)).Stream;
                notifyIcon.Icon = new Icon(iconStream);
            }
        }

        /// <summary>
        /// Resets the notification icon to the original one
        /// </summary>
        public static void ResetIcon()
        {
            using(new Logger()) 
            {
                string uri = String.Format("/{0};component/Images/Icon/icon.ico", Assembly.GetExecutingAssembly().GetName().Name);
                Stream iconStream = System.Windows.Application.GetResourceStream(new Uri(uri, UriKind.Relative)).Stream;
                notifyIcon.Icon = new Icon(iconStream);
            }
        }

        /// <summary>
        /// Enables/Disables the start synchronization menuentry of the notification icon
        /// </summary>
        /// <param name="state">true/false if is should get enabled or not</param>
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

        #region Balloon Tips

        /// <summary>
        /// Creates a balloon tip for a successfull synchronisation
        /// </summary>
        /// <param name="count">count of affected fils/folders</param>
        public static void ItemsSynced(long count)
        {
            using (new Logger(count))
            {
                ResourceDictionary dict = mainWindow.Resources.MergedDictionaries.ToList().First();

                BallonNotifier item = new BallonNotifier();
                item.notifier = Notifier.SYNC;
                item.message = String.Format(dict["iconSyncFinish"].ToString(), count);
                item.icon = ToolTipIcon.Info;
                item.title = dict["iconTitle"].ToString();

                ShowNotification(item);
            }
        }

        /// <summary>
        /// Shows a balloon tip for a conflicted synchronisation
        /// </summary>
        /// <param name="count">count of affected fils/folders</param>
        public static void ErrorConflict(long count)
        {
            using (new Logger(count))
            {
                ResourceDictionary dict = mainWindow.Resources.MergedDictionaries.ToList().First();

                BallonNotifier item = new BallonNotifier();
                item.notifier = Notifier.CONFLICT;
                item.message = String.Format(dict["iconConflict"].ToString(), count);
                item.icon = ToolTipIcon.Error;
                item.title = dict["iconTitle"].ToString();

                ShowNotification(item);
            }
        }

        /// <summary>
        /// Shows a balloon tip for the defined disk space reached error of the server
        /// </summary>
        /// <param name="path">path, where the error occurs</param>
        /// <param name="filename">error causing file</param>
        /// <param name="errorMessage">related error message from the server</param>
        public static void ErrorDiskSpace(string path, string filename, string errorMessage)
        {
            using (new Logger(path, filename, errorMessage))
            {
                ResourceDictionary dict = mainWindow.Resources.MergedDictionaries.ToList().First();
                long size = Convert.ToInt64(errorMessage.Split('-').Last().Trim());

                BallonNotifier item = new BallonNotifier();
                item.notifier = Notifier.DISKSPACE;
                item.message = String.Format(dict["iconDiskSpace"].ToString(), size / 1000 / 1024);
                item.icon = ToolTipIcon.Warning;
                item.path = path;
                item.filename = filename;
                item.title = dict["iconTitle"].ToString();

                ShowNotification(item);
            }
        }

        /// <summary>
        /// Shows a balloon tip for the file size is too big error of the server
        /// </summary>
        /// <param name="path">path, where the error occurs</param>
        /// <param name="filename">error causing file</param>
        /// <param name="errorMessage">related error message from the server</param>
        public static void ErrorFileSize(string path, string filename, string errorMessage)
        {
            using (new Logger(path, filename, errorMessage))
            {
                ResourceDictionary dict = mainWindow.Resources.MergedDictionaries.ToList().First();
                long size = Convert.ToInt64(errorMessage.Split('-').Last().Trim());
                
                BallonNotifier item = new BallonNotifier();
                item.notifier = Notifier.FILESIZE;
                item.message = String.Format(dict["iconFileSize"].ToString(), filename, size / 1000 / 1024);
                item.icon = ToolTipIcon.Warning;
                item.path = path;
                item.filename = filename;
                item.title = dict["iconTitle"].ToString();

                ShowNotification(item);
            }
        }

        /// <summary>
        /// Method for showing the defined balloon tip
        /// </summary>
        /// <param name="notifyItem">item for designing the balloon tip</param>
        private static void ShowNotification(BallonNotifier notifyItem)
        {
            using (new Logger(notifyItem))
            {
                if (UserPreferences.showNotification)
                {
                    notifier = notifyItem;
                    notifyIcon.ShowBalloonTip(0, notifyItem.title, notifyItem.message, notifyItem.icon);
                }
            }
        }

        #endregion

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
                string args = (!String.IsNullOrEmpty(notifier.path) && !String.IsNullOrEmpty(notifier.filename)) 
                                ? String.Format("/select,\"{0}\"", Path.Combine(notifier.path, notifier.filename)) 
                                : "";

                switch (notifier.notifier)
                {
                    case Notifier.FILESIZE: 
                        if (Directory.Exists(notifier.path))
                            Process.Start("explorer.exe", args);
                        break;
                    case Notifier.DISKSPACE: 
                        if (Directory.Exists(notifier.path))
                            Process.Start("explorer.exe", args);
                        break;
                    case Notifier.CONFLICT: 
                        if (Directory.Exists(notifier.path))
                            Process.Start("explorer.exe", notifier.path);
                        break;
                    case Notifier.SYNC: break;
                }
            }
        }

        #endregion
    }
}
