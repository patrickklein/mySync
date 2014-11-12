using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace My_Sync.Classes
{
    static class FolderManagement
    {
        private static string folderName = "";
        private static string favoriteFolder = "";

        [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
        static extern UInt32 SHGetSetFolderCustomSettings(ref FolderSettings settings, string path, UInt32 accessRight);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct FolderSettings
        {
            public UInt32 dwSize;
            public UInt32 dwMask;
            public UInt32 dwFlags;

            public IntPtr pclsid;
            public IntPtr pvid;

            public string webViewTemplate;
            public UInt32 cchWebViewTemplate;
            public string webViewTemplateVersion;

            public string infoTip;
            public UInt32 cchInfoTip;

            public string icon;
            public UInt32 iconFile;
            public int iconIndex;
        }

        //------------------------------------------------------------------------------------------------//

        /// <summary>
        /// Creates the root directory for this application for synchronization
        /// creates the folder, changes the folder icon and adds the folder to the user favorites in Windows Explorer
        /// </summary>
        public static void CreateSyncFolder() 
        {
            using (new Logger())
            {
                folderName = MySync.Default.mainFolder;
                favoriteFolder = Environment.ExpandEnvironmentVariables(Path.Combine(@"%USERPROFILE%", folderName));
                Directory.CreateDirectory(favoriteFolder);

                SetFolderIcon(favoriteFolder);
                CreateShortcutFavorites(folderName, favoriteFolder);
            }
        }

        /// <summary>
        /// Deletes the root directory for this application for synchronization
        /// </summary>
        /// <param name="recursive">defines if the folder is deleted recursively</param>
        public static void DeleteSyncFolder(bool recursive = true)
        {
            using (new Logger())
            {
                folderName = MySync.Default.mainFolder;
                favoriteFolder = Environment.ExpandEnvironmentVariables(Path.Combine(@"%USERPROFILE%", folderName));
                Directory.Delete(favoriteFolder, recursive);
            }
        }

        /// <summary>
        /// Changes the folder icon from the given path to the application icon
        /// </summary>
        /// <param name="path">folder where the icon should be changed</param>
        public static void SetFolderIcon(string path)
        {
            using (new Logger(path))
            {
                ChangeIcon(path, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName));
            }
        }

        /// <summary>
        /// Resets the folder icon from the given path to the standard foldericon
        /// </summary>
        /// <param name="path">folder where the icon should be resetted</param>
        public static void ResetFolderIcon(string path)
        {
            using (new Logger(path))
            {
                ChangeIcon(path, @"%SystemRoot%\system32\ImageRes.dll", 3);
            }
        }

        /// <summary>
        /// Changes the folder icon from the given path to the given icon
        /// </summary>
        /// <param name="path">folder where the icon should be changed</param>
        /// <param name="newIcon">icon which is going to be set</param>
        /// <param name="index">icon index of executable files</param>
        private static void ChangeIcon(string path, string newIcon, int index = 0)
        {
            using (new Logger(path, newIcon, index))
            {
                FolderSettings FolderSettings = new FolderSettings();
                FolderSettings.dwMask = 0x10;
                FolderSettings.icon = newIcon;
                FolderSettings.iconIndex = index;

                UInt32 FCS_READ = 0x00000001;
                UInt32 FCS_FORCEWRITE = 0x00000002;
                UInt32 FCS_WRITE = FCS_READ | FCS_FORCEWRITE;

                //delete desktop.ini of the folder
                string desktopIni = Path.Combine(path, "desktop.ini");
                if (System.IO.File.Exists(desktopIni))
                    System.IO.File.Delete(desktopIni);

                UInt32 HRESULT = SHGetSetFolderCustomSettings(ref FolderSettings, path, FCS_FORCEWRITE);
            }
        }

        //------------------------------------------------------------------------------------------------//

        /// <summary>
        /// Creates a shortcut which is going to be added to the users favorites in Windows Explorer
        /// </summary>
        /// <param name="shortcutName">defines the shortcut which gets created</param>
        /// <param name="targetFileLocation">defines the location the shortcut is pointing</param>
        /// <param name="description">adds an description for the mouse over tooltip in Windows Explorer</param>
        public static void CreateShortcutFavorites(string shortcutName, string targetFileLocation, string description = "")
        {
            using (new Logger(shortcutName, targetFileLocation, description))
            {
                //define all needed paths
                string userFavouritePath = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Links");
                string iconFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppDomain.CurrentDomain.FriendlyName);

                CreateShortcut(shortcutName, targetFileLocation, userFavouritePath, iconFile, description);
            }
        }

        /// <summary>
        /// Creates a shortcut which is going to be added to the given path
        /// </summary>
        /// <param name="shortcutName">defines the shortcut which gets created</param>
        /// <param name="targetFileLocation">defines the location the shortcut is pointing</param>
        /// <param name="addToPath">defines the root path, where the shortcut should be created</param>
        /// <param name="iconFile">defines the icon which should be used for the linked folder</param>
        /// <param name="description">adds an description for the mouse over tooltip in Windows Explorer</param>
        public static void CreateShortcut(string shortcutName, string targetFileLocation, string addToPath = "", string iconFile = "", string description = "")
        {
            using (new Logger(shortcutName, targetFileLocation, addToPath, iconFile, description))
            {
                addToPath = (String.IsNullOrEmpty(addToPath)) ? favoriteFolder : addToPath;
                string shortcutLocation = Path.Combine(addToPath, shortcutName + ".lnk");

                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);
                shortcut.TargetPath = targetFileLocation;                                          // The path of the file that will launch when the shortcut is run
                if (!String.IsNullOrEmpty(description)) shortcut.Description = description;        // The description of the shortcut
                if (!String.IsNullOrEmpty(iconFile)) shortcut.IconLocation = iconFile;             // The icon of the shortcut 
                shortcut.Save();                                                                   // Save the shortcut
            }
        }

        /// <summary>
        /// Deletes an existing shortcut in the users favorites in Windows Explorer
        /// if parameter is empty, the MySync shortcut is taken
        /// </summary>
        /// <param name="shortcutName">defines the shortcut which gets deleted</param>
        public static void DeleteShortcut(string shortcutName = "")
        {
            using (new Logger(shortcutName))
            {
                folderName = MySync.Default.mainFolder;
                shortcutName = (String.IsNullOrEmpty(shortcutName)) ? folderName : shortcutName;
                string linksPath = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Links");
                string shortcutLocation = Path.Combine(linksPath, shortcutName + ".lnk");

                if (System.IO.File.Exists(shortcutLocation))
                    System.IO.File.Delete(shortcutLocation);
            }
        }
    }
}
