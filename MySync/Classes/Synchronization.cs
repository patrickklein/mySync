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
        private static DateTime lastRaised;

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
                newItem.path = item.FullPath.TrimEnd('\\');
                newItem.serverEntryPointId = point.id;

                DAL.AddSynchronizationItem(newItem);
            }
        }

        /// <summary>
        /// Deletes a synchronization item from the database chosen by the given attributes
        /// </summary>
        /// <param name="name">filename or foldername</param>
        /// <param name="path">full path</param>
        public static void DeleteFromSynchronizationItem(string name, string path)
        {
            using (new Logger(name, path))
            {
                string fullPath = Path.Combine(path, name);

                SynchronizationItem toDelete = dbInstance.SynchronizationItem.ToList().Single(x => x.name.Equals(name) && x.path.Equals(path));
                dbInstance.SynchronizationItem.Remove(toDelete);
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
                if (DateTime.Now.Subtract(lastRaised).TotalMilliseconds > 1000)
                {
                    lastRaised = DateTime.Now;

                    //Delay is given to the thread for avoiding same process to be repeated
                    Thread.Sleep(100);
                    MessageBox.Show("renamed: " + e.Name + " " + e.OldName);
                }
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
                if (DateTime.Now.Subtract(lastRaised).TotalMilliseconds > 1000)
                {
                    lastRaised = DateTime.Now;

                    //Delay is given to the thread for avoiding same process to be repeated
                    Thread.Sleep(100);

                    string name = e.FullPath.Split('\\').Last();
                    string path = e.FullPath.Replace(name, "").TrimEnd('\\');
                    name = (name.Contains(".")) ? name.Split('.').First() : name;
                    
                    DeleteFromSynchronizationItem(name, path);
                }
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
                if (DateTime.Now.Subtract(lastRaised).TotalMilliseconds > 1000)
                {
                    lastRaised = DateTime.Now;

                    //Delay is given to the thread for avoiding same process to be repeated
                    Thread.Sleep(100);
                    MessageBox.Show("changed: " + e.Name);
                }
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
                if (DateTime.Now.Subtract(lastRaised).TotalMilliseconds > 1000)
                {
                    lastRaised = DateTime.Now;

                    //Delay is given to the thread for avoiding same process to be repeated
                    Thread.Sleep(100);

                    ItemInfo newItem = new ItemInfo();
                    if (Directory.Exists(e.FullPath)) newItem.GetDirectoryInfo(e.FullPath);
                    if (File.Exists(e.FullPath)) newItem.GetFileInfo(e.FullPath);

                    string description = DAL.GetServerEntryPointByPath(((FileSystemWatcher)objectCreated).Path).description;
                    AddToDatabase(newItem, description);
                }
            }
        }

        #endregion
    }
}
