using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace My_Sync.Classes
{
    static class Synchronization
    {
        private static MySyncEntities dbInstance = new MySyncEntities();
        private static List<FileSystemWatcher> watcherList = new List<FileSystemWatcher>();

        /// <summary>
        /// Adds all files and folders from a given path to the database (SynchronizationItem Table)
        /// </summary>
        /// <param name="path">root path for lookup</param>
        public static void AddAllFromFolder(SynchronizationPoint entryPoint)
        {
            using (new Logger(entryPoint))
            {
                ItemInfo info = new ItemInfo();
                DirectoryInfo dirInfo = (DirectoryInfo)info.GetInfo(entryPoint.Folder);

                //All files
                FileInfo[] fileInfo = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
                foreach (FileInfo file in fileInfo)
                {
                    ItemInfo tempInfo = new ItemInfo();
                    tempInfo.GetInfo(file.FullName);
                    AddToDatabase(tempInfo, entryPoint.Description);
                }

                //All directories
                DirectoryInfo[] directoryInfo = dirInfo.GetDirectories("*", SearchOption.AllDirectories);
                foreach (DirectoryInfo directory in directoryInfo)
                {
                    ItemInfo tempInfo = new ItemInfo();
                    tempInfo.GetInfo(directory.FullName);
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
                SynchronizationItem newItem = ItemInfoToSyncItem(item, new SynchronizationItem());

                ServerEntryPoint point = DAL.GetServerEntryPoint(serverDescription);
                newItem.serverEntryPointId = point.id;

                DAL.AddSynchronizationItem(newItem);
            }
        }

        /// <summary>
        /// Converts a given ItemInfo object into a SynchronizationItem
        /// </summary>
        /// <param name="item">ItemInfo object</param>
        /// <param name="newItem">SynchronizationItem object</param>
        /// <returns>the converted SynchronizationItem object</returns>
        private static SynchronizationItem ItemInfoToSyncItem(ItemInfo item, SynchronizationItem newItem)
        {
            using (new Logger(item, newItem))
            {
                string timeFormat = "yyyy/MM/dd HH:mm:ss";

                newItem.creationTime = item.CreationTime.ToString(timeFormat);
                newItem.lastAccessTime = item.LastAccessTime.ToString(timeFormat);
                newItem.lastWriteTime = item.LastWriteTime.ToString(timeFormat);
                newItem.size = item.Size;
                newItem.extension = item.Extension;
                newItem.files = item.Files;
                newItem.folders = item.Folders;
                newItem.folderFlag = Convert.ToDecimal(item.FolderFlag);
                newItem.name = (item.FolderFlag) ? item.Directory : item.Filename;
                newItem.fullname = item.FullName;
                newItem.path = item.FullPath.TrimEnd('\\');

                return newItem;
            }
        }

        /// <summary>
        /// Updates a synchronisation item in the database
        /// </summary>
        /// <param name="oldName">old name of file/folder</param>
        /// <param name="newName">new name of file/folder (if empty it gets the value from oldName)</param>
        /// <param name="path">full path of file/folder</param>
        public static void UpdateSynchronizationItem(string path, string oldName, string newName = "")
        {
            using (new Logger(path, oldName, newName))
            {
                if (String.IsNullOrEmpty(newName)) newName = oldName;
                string parentPath = path.Replace(path.Split('\\').Last(), "").TrimEnd('\\');
                oldName = (oldName.Contains('\\')) ? oldName.Split('\\').Last() : oldName;
                newName = (newName.Contains('\\')) ? newName.Split('\\').Last() : newName;

                SynchronizationItem toRename = DAL.GetSynchronizationItem(oldName, parentPath);
                ItemInfo item = new ItemInfo();
                item.GetInfo(path);
                toRename = ItemInfoToSyncItem(item, toRename);
                DAL.UpdateSynchronizationItem(toRename);
            }
        }

        /// <summary>
        /// Deletes a synchronization item from the database chosen by the given attributes
        /// </summary>
        /// <param name="name">filename or foldername</param>
        /// <param name="path">full path</param>
        public static void DeleteSynchronizationItem(string name, string path)
        {
            using (new Logger(name, path))
            {
                SynchronizationItem toDelete = DAL.GetSynchronizationItem(name, path);

                //if it was a folder, delete all contained files/folder 
                if (Convert.ToBoolean(toDelete.folderFlag))
                {
                    string fullPath = Path.Combine(toDelete.path, toDelete.fullname);
                    List<SynchronizationItem> items = DAL.GetSynchronizationItems(fullPath);
                    foreach(SynchronizationItem item in items)
                        DAL.DeleteSynchronizationItem(item);
                }

                DAL.DeleteSynchronizationItem(toDelete);
            }
        }

        #region File Watcher

        /// <summary>
        /// Creates a new File Watcher instance for the given directory path and registers the related event handlers
        /// </summary>
        /// <param name="directory">directory which should get be watched for changes/updates</param>
        public static void AddWatcher(string directory)
        {
            using(new Logger(directory)) 
            {
                FileSystemWatcher fsw = new FileSystemWatcher(directory, "*.*");
                fsw.EnableRaisingEvents = true;
                fsw.IncludeSubdirectories = true;
                fsw.Created += fsw_Created;
                fsw.Changed += fsw_Changed;
                fsw.Renamed += fsw_Renamed;
                fsw.Deleted += fsw_Deleted;

                watcherList.Add(fsw);
            }
        }

        /// <summary>
        /// Deletes all file system watcher and creates new ones for all existing server entry points
        /// </summary>
        public static void RefreshWatcher()
        {
            using (new Logger())
            {
                //Creates a filewatcher for every server entry point
                DeleteWatcher();
                DAL.GetServerEntryPoints().ForEach(x => Synchronization.AddWatcher(x.Folder));
            }
        }

        /// <summary>
        /// Disposes all existing file system watchers and clears the list
        /// </summary>
        public static void DeleteWatcher()
        {
            using (new Logger())
            {
                watcherList.ForEach(x => x.Dispose());
                watcherList.Clear();
            }
        }

        /// <summary>
        /// Eventhandler gets fired if a file or directory has been renamed
        /// </summary>
        /// <param name="objectRenamed">event sender</param>
        /// <param name="e">event arguments</param>
        private static void fsw_Renamed(object objectRenamed, RenamedEventArgs e)
        {
            using (new Logger(objectRenamed, e))
            {
                //Delay is given to the thread for avoiding same process to be repeated
                Thread.Sleep(100);
                UpdateSynchronizationItem(e.FullPath, e.OldName, e.Name);
            }
        }

        /// <summary>
        /// Eventhandler gets fired if a file or directory has been deleted
        /// </summary>
        /// <param name="objectDeleted">event sender</param>
        /// <param name="e">event arguments</param>
        private static void fsw_Deleted(object objectDeleted, FileSystemEventArgs e)
        {
            using (new Logger(objectDeleted, e))
            {
                //Delay is given to the thread for avoiding same process to be repeated
                Thread.Sleep(100);

                string name = e.FullPath.Split('\\').Last();
                string path = e.FullPath.Replace(name, "").TrimEnd('\\');
                DeleteSynchronizationItem(name, path);
            }
        }

        /// <summary>
        /// Eventhandler gets fired if a file or directory has been changed
        /// </summary>
        /// <param name="objectChanged">event sender</param>
        /// <param name="e">event arguments</param>
        private static void fsw_Changed(object objectChanged, FileSystemEventArgs e)
        {
            using (new Logger(objectChanged, e))
            {
                //Delay is given to the thread for avoiding same process to be repeated
                Thread.Sleep(100);
                UpdateSynchronizationItem(e.FullPath, e.Name);
            }
        }

        /// <summary>
        /// Eventhandler gets fired if a file or directory has been created
        /// </summary>
        /// <param name="objectCreated">event sender</param>
        /// <param name="e">event arguments</param>
        private static void fsw_Created(object objectCreated, FileSystemEventArgs e)
        {
            using (new Logger(objectCreated, e))
            {
                //Delay is given to the thread for avoiding same process to be repeated
                Thread.Sleep(100);
                ItemInfo newItem = new ItemInfo();
                newItem.GetInfo(e.FullPath);

                string description = DAL.GetServerEntryPointByPath(((FileSystemWatcher)objectCreated).Path).description;
                AddToDatabase(newItem, description);
            }
        }

        #endregion
    }
}
