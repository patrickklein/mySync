using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace My_Sync.Classes
{
    class Synchronization
    {
        private MySyncEntities dbInstance = new MySyncEntities();

        /// <summary>
        /// Adds all files and folders from a given path to the database (SynchronizationItem Table)
        /// </summary>
        /// <param name="path">root path for lookup</param>
        public static void AddAllFromFolder(SynchronizationPoint entryPoint)
        {
            using (new Logger(entryPoint))
            {
                ItemInfo info = new ItemInfo();
                DirectoryInfo dirInfo = info.GetDirectoryInfo(entryPoint.Folder);

                //All files
                FileInfo[] fileInfo = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
                foreach (FileInfo file in fileInfo)
                {
                    ItemInfo tempInfo = new ItemInfo();
                    tempInfo.GetFileInfo(file.FullName);
                    AddToDatabase(tempInfo, entryPoint.Description);
                }

                //All directories
                DirectoryInfo[] directoryInfo = dirInfo.GetDirectories("*", SearchOption.AllDirectories);
                foreach (DirectoryInfo directory in directoryInfo)
                {
                    ItemInfo tempInfo = new ItemInfo();
                    tempInfo.GetDirectoryInfo(directory.FullName);
                    AddToDatabase(tempInfo, entryPoint.Description);
                }
            }
        }

        /// <summary>
        /// Method for adding a new synchronization point into the database
        /// </summary>
        /// <param name="item">iteminfo object with the containing values</param>
        /// <param name="serverDescription">needed for gathering the server entry point id</param>
        private static void AddToDatabase(ItemInfo item, string serverDescription)
        {
            using (new Logger(item, serverDescription))
            {
                string timeFormat = "yyyy/MM/dd HH:mm:ss";

                ServerEntryPoint point = DAL.GetServerEntryPoint(serverDescription);
                SynchronizationItem newItem = new SynchronizationItem();
                newItem.creationTime = item.CreationTime.ToString(timeFormat);
                newItem.lastAccessTime = item.LastAccessTime.ToString(timeFormat);
                newItem.lastWriteTime = item.LastWriteTime.ToString(timeFormat);
                newItem.size = item.Size;
                newItem.extension = item.Extension;
                newItem.files = item.Files;
                newItem.folders = item.Folders;
                newItem.folderFlag = Convert.ToDecimal(item.FolderFlag);
                newItem.name = (item.FolderFlag) ? item.Directory : item.Filename;
                newItem.path = item.FullPath;
                newItem.serverEntryPointId = point.id;

                DAL.AddSynchronizationItem(newItem);
            }
        }
    }

    class ItemInfo
    {
        private string filename;
        private string directory;
        private string extension;
        private string fullPath;
        private DateTime creationTime;
        private DateTime lastAccessTime;
        private DateTime lastWriteTime;
        private long size;
        private long files;
        private long folders;
        private bool folderFlag = false;

        #region Getter / Setter

        public string Filename
        {
            get { return filename; }
            set { filename = value; }
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

        public long Files
        {
            get { return files; }
            set { files = value; }
        }

        public long Folders
        {
            get { return folders; }
            set { folders = value; }
        }

        public bool FolderFlag
        {
            get { return folderFlag; }
            set { folderFlag = value; }
        }

        #endregion

        /// <summary>
        /// Gets all needed file attributes for the given file
        /// </summary>
        /// <param name="filename">file for gathering the file attributes</param>
        public void GetFileInfo(string filename)
        {
            using (new Logger(filename))
            {
                // Get Attributes for file
                FileInfo info = new FileInfo(filename);
                this.Size = info.Length;
                this.LastAccessTime = info.LastAccessTime;
                this.LastWriteTime = info.LastWriteTime;
                this.CreationTime = info.CreationTime;
                this.Filename = info.Name.Replace(info.Extension, "");
                this.Directory = info.DirectoryName;
                this.Extension = info.Extension;
                this.FolderFlag = false;
                this.FullPath = info.DirectoryName;
            }
        }

        /// <summary>
        /// Gets all needed directory attributes for the given path
        /// </summary>
        /// <param name="path">path or gathering the directory attributes</param>
        /// <returns>directory info object with the wanted directory</returns>
        public DirectoryInfo GetDirectoryInfo(string path)
        {
            using (new Logger(path))
            {
                // Get Attributes for directory
                DirectoryInfo info = new DirectoryInfo(path);

                try
                {
                    this.Size = info.GetFiles("*.*", SearchOption.AllDirectories).Sum(file => file.Length);
                    this.LastAccessTime = info.LastAccessTime;
                    this.LastWriteTime = info.LastWriteTime;
                    this.CreationTime = info.CreationTime;
                    this.Directory = info.Name;
                    this.FullPath = info.FullName.Replace(info.Name, "");

                    this.Files = info.GetFiles("*.*", SearchOption.AllDirectories).Count();
                    this.Folders = info.GetDirectories("*", SearchOption.AllDirectories).Count();
                    this.FolderFlag = true;
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show(ex.ToString());
                }

                return info;
            }
        }
    }
}
