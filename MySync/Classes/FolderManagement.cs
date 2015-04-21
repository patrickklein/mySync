using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
        /// Retrieves the favorite folder of the user
        /// </summary>
        /// <returns>favorite folder path</returns>
        public static string GetFavoriteFolder()
        {
            folderName = UserPreferences.mainFolder;
            favoriteFolder = Environment.ExpandEnvironmentVariables(Path.Combine(@"%USERPROFILE%", folderName));
            return favoriteFolder;
        }

        /// <summary>
        /// Creates the root directory for this application for synchronization
        /// creates the folder, changes the folder icon and adds the folder to the user favorites in Windows Explorer
        /// </summary>
        public static void CreateSyncFolder()
        {
            using (new Logger())
            {
                folderName = UserPreferences.mainFolder;
                if (String.IsNullOrEmpty(favoriteFolder)) GetFavoriteFolder();

                Directory.CreateDirectory(favoriteFolder);

                SetFolderIcon(favoriteFolder);
                CreateShortcutFavorites(folderName, favoriteFolder);

                //Create shortcut to folders entered in the server section
                DAL.GetServerEntryPoints().ForEach(x => CreateShortcut(x.Description, x.Folder));
            }
        }

        /// <summary>
        /// Deletes the root directory for this application for synchronization
        /// </summary>
        /// <param name="recursive">defines if the folder is deleted recursively</param>
        public static void DeleteSyncFolder(bool recursive = true)
        {
            using (new Logger(recursive))
            {
                folderName = UserPreferences.mainFolder;
                favoriteFolder = Environment.ExpandEnvironmentVariables(Path.Combine(@"%USERPROFILE%", folderName));

                //Set directory attributes to normal to grant deletion rights
                if (Directory.Exists(favoriteFolder))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(favoriteFolder);
                    dirInfo.Attributes = FileAttributes.Normal;
                    Directory.Delete(favoriteFolder, recursive);
                }
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
                folderName = UserPreferences.mainFolder;
                shortcutName = (String.IsNullOrEmpty(shortcutName)) ? folderName : shortcutName;
                string linksPath = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Links");
                string shortcutLocation = Path.Combine(linksPath, shortcutName + ".lnk");

                if (System.IO.File.Exists(shortcutLocation))
                    System.IO.File.Delete(shortcutLocation);
            }
        }
    }

    class ItemInfo
    {
        private string filename;
        private string fullName;
        private string lastFullName;
        private string directory;
        private string extension;
        private string fullPath;
        private string rootFolder;
        private DateTime creationTime;
        private DateTime lastAccessTime;
        private DateTime lastWriteTime;
        private long size;
        private bool isFolder = false;

        #region Getter / Setter

        public string Filename
        {
            get { return filename; }
            set { filename = value; }
        }

        public string FullName
        {
            get { return fullName; }
            set { fullName = value; }
        }

        public string LastFullName
        {
            get { return lastFullName; }
            set { lastFullName = value; }
        }

        public string Directory
        {
            get { return directory; }
            set { directory = value; }
        }

        public string Extension
        {
            get { return extension; }
            set { extension = value; }
        }

        public string FullPath
        {
            get { return fullPath; }
            set { fullPath = value; }
        }

        public string RootFolder
        {
            get { return rootFolder; }
            set { rootFolder = value; }
        }

        public DateTime CreationTime
        {
            get { return creationTime; }
            set { creationTime = value; }
        }

        public DateTime LastAccessTime
        {
            get { return lastAccessTime; }
            set { lastAccessTime = value; }
        }

        public DateTime LastWriteTime
        {
            get { return lastWriteTime; }
            set { lastWriteTime = value; }
        }

        public long Size
        {
            get { return size; }
            set { size = value; }
        }

        public bool IsFolder
        {
            get { return isFolder; }
            set { isFolder = value; }
        }

        #endregion

        /// <summary>
        /// Gets all needed file or directory attributes for the given path
        /// </summary>
        /// <param name="fullName">path for gathering the attributes</param>
        /// <returns>file system info object with the needed directory/file attributes</returns>
        public FileSystemInfo GetInfo(string fullName)
        {
            using (new Logger(fullName))
            {
                // Get Attributes for directory
                FileSystemInfo info = null;

                try
                {
                    //get attributes for file
                    if (new FileInfo(fullName).Exists)
                    {
                        info = new FileInfo(fullName);
                        this.IsFolder = false;
                        this.Size = ((FileInfo)info).Length;
                        this.Directory = ((FileInfo)info).DirectoryName;
                        this.Extension = info.Extension;
                        this.FullPath = ((FileInfo)info).DirectoryName.TrimEnd('\\');
                        this.Filename = (info.Extension == "") ? info.Name : info.Name.Replace(info.Extension, "");
                        this.RootFolder = ((FileInfo)(info)).Directory.Name;
                    }

                    //get attributes for directory
                    if (new DirectoryInfo(fullName).Exists)
                    {

                        info = new DirectoryInfo(fullName);
                        int lastindex = info.FullName.TrimEnd('\\').LastIndexOf('\\');
                        this.FullPath = info.FullName.Substring(0, lastindex);
                        this.IsFolder = true;
                        this.Size = ((DirectoryInfo)info).GetFiles("*.*", SearchOption.AllDirectories).Sum(file => file.Length);
                        this.Directory = info.Name;
                        this.Extension = "";
                        this.RootFolder = ((DirectoryInfo)(info)).Parent.Name;
                    }

                    this.LastAccessTime = info.LastAccessTime;
                    this.LastWriteTime = info.LastWriteTime;
                    this.CreationTime = info.CreationTime;
                    this.FullName = info.Name;
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                catch (NullReferenceException ex)
                {
                    //MessageBox.Show(ex.ToString());
                }

                return info;
            }
        }
    }
}
