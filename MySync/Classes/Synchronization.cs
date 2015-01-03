using CSharpTest.Net.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace My_Sync.Classes
{
    static class Synchronization
    {
        //Task.Run(() => Synchronization.DownloadFile(null, null, null, null)).Wait();

                    
        private static List<FileSystemWatcher> watcherList = new List<FileSystemWatcher>();
        private static System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private static List<Tuple<char, SynchronizationItem>> syncItemList = new List<Tuple<char, SynchronizationItem>>();
        private static bool sync = false;
        private static string timeFormat = "yyyy/MM/dd HH:mm:ss";

        /// <summary>
        /// Gets all ToSync items from the database and start to transfer the data/changes to the related server
        /// </summary>
        private static void StartSynchronize()
        {
            using (new Logger())
            {
                NotifyIcon.ChangeSyncState(false);
                //NotifyIcon.ChangeIcon("Upload");

                while (true)
                {
                    ToSync toSyncItem = DAL.GetNextToSync();
                    if (toSyncItem == null) break;

                    SynchronizationItem item = DAL.GetSynchronizationItem((long)toSyncItem.synchronizationItemId);
                    ServerEntryPoint entryPoint = DAL.GetServerEntryPoint((long)item.serverEntryPointId);
                    FileInfo file = new FileInfo(Path.Combine(item.path, item.fullname));

                    try
                    {
                        if (toSyncItem.syncType == "d")
                        {
                            string folder = Path.Combine(entryPoint.folderpath.Split('\\').Last(), item.path.Replace(entryPoint.folderpath, "").Trim('\\'));
                            DeleteFromServer(entryPoint.serverurl.Replace("/Upload", "/Delete"), folder, item.fullname);
                        }
                        else SendFileToServer(toSyncItem.syncType, file, entryPoint.serverurl, entryPoint.folderpath);

                        DAL.AddItemToHistory(item.fullname, toSyncItem.syncType, Convert.ToBoolean(item.folderFlag), entryPoint.description);
                        DAL.DeleteToSync(toSyncItem.id);

                        if (toSyncItem.syncType == "d")
                            DBDeleteSynchronizationItem(item.fullname, item.path);

                        RefreshHistoryEntries();
                    }
                    catch (Exception ex)
                    {
                        new Logger().Log(String.Format("{0}: {1}", file.FullName, ex.Message.ToString()));
                        break;
                    }
                }

                sync = false;
                //NotifyIcon.ResetIcon();
                NotifyIcon.ChangeSyncState(true);
                MemoryManagement.Reduce();
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
        /// Refreshes the history entries on the GUI
        /// </summary>
        private static void RefreshHistoryEntries()
        {
            using (new Logger())
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    MainWindow mainWindow = ((MainWindow)System.Windows.Application.Current.MainWindow);
                    mainWindow.HistoryRTBHistory.Document.Blocks.Clear();
                    mainWindow.HistoryRTBHistory.AppendText(DAL.GetHistory());
                });
            }
        }

        /// <summary>
        /// Method for checking the filesystem and database for new/changed/deleted files and folders
        /// Updates the database entries and preparesfor synchronization
        /// </summary>
        public static void CheckForDifferencies()
        {
            using (new Logger())
            {
                MainWindow mainWindow = null;
                System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    mainWindow = ((MainWindow)System.Windows.Application.Current.MainWindow);
                });

                foreach (SynchronizationPoint syncPoint in mainWindow.ServerDGSynchronizationPoints.Items)
                {
                    ServerEntryPoint point = DAL.GetServerEntryPoint(syncPoint.Description);
                    FindFile fileCounter = new FindFile(point.folderpath, "*", true, true, true);
                    fileCounter.RaiseOnAccessDenied = false;

                    //Get all files and folders from the watching directories
                    List<SynchronizationItem> items = new List<SynchronizationItem>();
                    fileCounter.FileFound +=
                        (o, x) =>
                        {
                            SynchronizationItem info = new SynchronizationItem();
                            info.extension = x.Extension;
                            info.folderFlag = Convert.ToDecimal(x.IsDirectory);
                            info.fullname = x.Name;
                            info.name = x.Name;
                            info.lastAccessTime = x.LastAccessTime.ToString(timeFormat);
                            info.lastWriteTime = x.LastWriteTime.ToString(timeFormat);
                            info.creationTime = x.CreationTime.ToString(timeFormat);
                            info.size = x.Length;
                            info.path = x.ParentPath.TrimEnd('\\');
                            info.serverEntryPointId = point.id;

                            //Fix bug for getting the correct file extension
                            if(!x.IsDirectory) 
                            {
                                if (!String.IsNullOrEmpty(info.extension))
                                    info.name = x.Name.Replace(x.Extension, "");
                                else
                                {
                                    string[] split = x.Name.Split('.');
                                    info.name = (split.Length > 1) ? x.Name.Replace("." + split.Last(), "") : x.Name;
                                    info.extension = (split.Length > 1) ? "." + split.Last() : x.Extension;
                                }
                            }

                            items.Add(info);
                        };

                    fileCounter.Find();

                    //Get all files and folders from the database for the watching directory
                    List<SynchronizationItem> dbItems = DAL.GetSynchronizationItems(point.folderpath);

                    //Compare found files/folders attributes with the database
                    foreach (SynchronizationItem item in items)
                    {
                        if (GetFiltered(item.fullname)) continue;
                        SynchronizationItem dbItem = dbItems.SingleOrDefault(x => x.serverEntryPointId == item.serverEntryPointId && x.fullname == item.fullname && x.path == item.path);

                        //Create item if not exists in the database
                        if (dbItem == null)
                        {
                            ItemInfo newItem = new ItemInfo();
                            newItem.GetInfo(Path.Combine(item.path, item.fullname));

                            AddToDatabase(newItem, point.description);
                        }
                        else
                        {
                            dbItems.Remove(dbItem);

                            //Update if attributes have changed
                            bool isEqual = CheckSynchronizationItemIsEqual(item, dbItem);
                            if (!isEqual)
                            {
                                dbItem.creationTime = item.creationTime;
                                dbItem.lastAccessTime = item.lastAccessTime;
                                dbItem.lastWriteTime = item.lastWriteTime;
                                dbItem.size = item.size;

                                DBUpdateSynchronizationItem(dbItem);
                            }
                        }
                    }

                    //if dbItems still have some files/folders, delete them from the database (they don't exist anymore)
                    foreach (SynchronizationItem item in dbItems)
                        DBAddDeletion(item.fullname, item.path);

                    MemoryManagement.Reduce();
                }
            }
        }

        /// <summary>
        /// Checks if the given synchronization items are equal for the defined attributes
        /// </summary>
        /// <param name="item1">first item</param>
        /// <param name="item2">second item</param>
        /// <returns>value if equal or not</returns>
        private static bool CheckSynchronizationItemIsEqual(SynchronizationItem item1, SynchronizationItem item2)
        {
            using (new Logger(item1, item2))
            {
                List<bool> equalCount = new List<bool>();
                equalCount.Add(item1.creationTime == item2.creationTime);
                equalCount.Add(item1.extension == item2.extension);
                equalCount.Add(item1.folderFlag == item2.folderFlag);
                equalCount.Add(item1.fullname == item2.fullname);
                equalCount.Add(item1.lastAccessTime == item2.lastAccessTime);
                equalCount.Add(item1.lastWriteTime == item2.lastWriteTime);
                equalCount.Add(item1.path == item2.path);
                equalCount.Add(item1.serverEntryPointId == item2.serverEntryPointId);
                equalCount.Add(item1.size == item2.size);

                return !equalCount.Contains(false);
            }
        }

        /// <summary>
        /// Checks if the given name matches the defined file filter expressions
        /// </summary>
        /// <param name="name">file/folder name to check</param>
        /// <returns>true/false if the name matches the filter or not</returns>
        private static bool GetFiltered(string name)
        {
            using (new Logger(name))
            {
                List<bool> matchesCount = new List<bool>();
                foreach (FileFilter filter in DAL.GetFileFilters())
                {
                    var pattern = Regex.Escape(filter.term).Replace(@"\*", ".+?").Replace(@"\?", ".");
                    matchesCount.Add(Regex.IsMatch(name, pattern, RegexOptions.IgnoreCase));
                }

                return matchesCount.Contains(true);
            }
        }

        #region Server Methods

        /// <summary>
        /// Sends the given file/folder to the server address, provided from the related server entry point
        /// </summary>
        /// <param name="status">defines if the file was added (n), changed (u), or deleted (d)</param>
        /// <param name="file2Sync">file/folder which should be send to the server</param>
        /// <param name="requestUri">server address from the related server entry point</param>
        /// <param name="syncFolderPath">path from the related server entry point</param>
        public static void SendFileToServer(string status, FileInfo file2Sync, string requestUri, string syncFolderPath)
        {
            using (new Logger(status, file2Sync, requestUri))
            {
                using (var client = new HttpClient())
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        ItemInfo info = new ItemInfo();
                        info.GetInfo(file2Sync.FullName);

                        //if the item is a file, load content to server
                        if (info.FolderFlag == false)
                        {
                            string path = Path.Combine(info.Directory, info.FullName);
                            var fileContent = new ByteArrayContent(File.ReadAllBytes(path));
                            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                            {
                                FileName = HttpUtility.UrlEncode(file2Sync.Name),
                                Name = "uploadedFile"
                            };

                            content.Add(fileContent);
                        }

                        content.Add(new StringContent(info.FullPath.Replace(syncFolderPath, "").Trim('\\')), "fromRootToFolder");
                        content.Add(new StringContent(syncFolderPath.Split('\\').Last()), "syncRoot");
                        content.Add(new StringContent(info.FullPath), "fullPath");
                        content.Add(new StringContent(info.RootFolder), "rootFolder");
                        content.Add(new StringContent(info.FolderFlag.ToString()), "folderFlag");
                        content.Add(new StringContent(status.ToLower()), "status");
                        content.Add(new StringContent(info.Size.ToString()), "length");
                        content.Add(new StringContent(info.CreationTime.ToString()), "creationTime");
                        content.Add(new StringContent(info.LastWriteTime.ToString()), "lastWriteTime");
                        content.Add(new StringContent(info.LastAccessTime.ToString()), "lastAccessTime");
                        content.Add(new StringContent(info.Directory), "directory");
                        content.Add(new StringContent(info.Extension), "extension");
                        content.Add(new StringContent(info.Files.ToString()), "files");
                        content.Add(new StringContent(info.Folders.ToString()), "folders");
                        content.Add(new StringContent(info.FullName), "fullName");

                        try
                        {
                            var result = client.PostAsync(requestUri, content).Result;
                        }
                        catch (Exception ex) { throw ex; }
                    }
                }
            }
        }

        /// <summary>
        /// Deletes the given folder/file on the server
        /// </summary>
        /// <param name="requestUri">server address from the related server entry point</param>
        /// <param name="rootFolder">folder to delete (if file is empty) or root path of file to delete</param>
        /// <param name="file">file to delete if given</param>
        public static void DeleteFromServer(string requestUri, string rootFolder, string file = "")
        {
            using (new Logger(requestUri, rootFolder, file))
            {
                using (var client = new HttpClient())
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        content.Add(new StringContent(rootFolder), "directory");
                        content.Add(new StringContent(file), "fullName");

                        try
                        {
                            var result = client.PostAsync(requestUri, content).Result;
                        }
                        catch (Exception ex) { throw ex; }
                    }
                }
            }
        }

        public static async void DownloadFile(string requestUri, string rootFolder, string file, string savingPath)
        {
            requestUri = "http://localhost:51992/Account/Download";
            rootFolder = "FolderDelete";
            file = "Neues Textdokument.txt";
            savingPath = @"C:\temp\";

            using (new Logger(requestUri, rootFolder, file))
            {
                using (var client = new HttpClient())
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        content.Add(new StringContent(rootFolder), "directory");
                        content.Add(new StringContent(file), "fullName");

                        try
                        {
                            HttpResponseMessage responseMessage = client.PostAsync(requestUri, content).Result;
                            string fileName = ((string[])responseMessage.Headers.GetValues("Filename"))[0];
                            var temp = ((string[])responseMessage.Headers.GetValues("Path"))[0];

                            if (!Directory.Exists(savingPath)) Directory.CreateDirectory(savingPath);
                            using (var fileStream = File.Create(Path.Combine(savingPath + fileName)))
                            {
                                using (var httpStream = await responseMessage.Content.ReadAsStreamAsync())
                                {
                                    httpStream.CopyTo(fileStream);
                                    fileStream.Flush();
                                }
                            }
                        }
                        catch (Exception ex) { throw ex; }
                    }
                }
            }
        }

        #endregion

        #region Timer

        /// <summary>
        /// Method for starting a countdown to synchronize new/deleted or changed files and folders
        /// </summary>
        public static void StartTimer()
        {
            using (new Logger())
            {
                if (timer.Enabled) timer.Stop();
                MainWindow mainWindow = ((MainWindow)System.Windows.Application.Current.MainWindow);
                string selectedTime = ((ContentControl)(mainWindow.GeneralCBInterval.SelectedItem)).Uid.ToString();
                timer.Interval = Convert.ToInt32(selectedTime) * 60 * 1000;
                timer.Interval = 5000;
                timer.Tick += new EventHandler(Timer_Tick);
                timer.Start();
            }
        }

        /// <summary>
        /// Starts instantly the file synchronization
        /// </summary>
        public static void StartSynchronization()
        {
            using (new Logger())
            {
                Timer_Tick(null, null);
            }
        }

        /// <summary>
        /// Timer Tick event method - counts down the given interval and starts synchronization
        /// </summary>
        /// <param name="sender">event sender</param>
        /// <param name="e">event arguments</param>
        private static void Timer_Tick(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
                try
                {
                    if (!sync)
                    {
                        sync = true;
                        Task.Run(() => StartSynchronize());
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                }
            }
        }

        #endregion

        #region Database Functions

        /// <summary>
        /// Adds all files and folders from a given path to the database (SynchronizationItem Table)
        /// </summary>
        /// <param name="path">root path for lookup</param>
        public static void DBAddAllFromFolder(SynchronizationPoint entryPoint)
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
        /// <returns>item which was added to the database</returns>
        private static SynchronizationItem AddToDatabase(ItemInfo item, string serverDescription)
        {
            using (new Logger(item, serverDescription))
            {
                SynchronizationItem newItem = ItemInfoToSyncItem(item, new SynchronizationItem());

                ServerEntryPoint point = DAL.GetServerEntryPoint(serverDescription);
                newItem.serverEntryPointId = point.id;

                ToSync sync = new ToSync();
                sync.syncType = "n";

                //make changes in the database
                sync.synchronizationItemId = DAL.AddSynchronizationItem(newItem);
                if (sync.synchronizationItemId != -1) DAL.AddToSync(sync);

                return newItem;
            }
        }

        /// <summary>
        /// Updates a synchronisation item in the database
        /// </summary>
        /// <param name="path">full path of file/folder</param>
        /// <param name="oldName">old name of file/folder</param>
        /// <param name="newName">new name of file/folder (if empty it gets the value from oldName)</param>
        /// <returns>item which was updated in the database</returns>
        private static SynchronizationItem DBUpdateSynchronizationItem(string path, string oldName, string newName = "")
        {
            using (new Logger(path, oldName, newName))
            {
                if (String.IsNullOrEmpty(newName)) newName = oldName;
                string parentPath = path.TrimEnd(path.Split('\\').Last().ToCharArray()).TrimEnd('\\');
                oldName = (oldName.Contains('\\')) ? oldName.Split('\\').Last() : oldName;
                newName = (newName.Contains('\\')) ? newName.Split('\\').Last() : newName;

                SynchronizationItem toRename = DAL.GetSynchronizationItem(oldName, parentPath);
                ItemInfo item = new ItemInfo();
                item.GetInfo(path);
                toRename = ItemInfoToSyncItem(item, toRename);

                ToSync sync = new ToSync();
                sync.syncType = "u";
                sync.synchronizationItemId = toRename.id;

                //make changes in the database
                DAL.UpdateSynchronizationItem(toRename);
                DAL.AddToSync(sync);

                return toRename;
            }
        }

        /// <summary>
        /// Updates a synchronisation item in the database
        /// </summary>
        /// <param name="toUpdate">item which should get updated</param>
        /// <returns>item which was updated in the database</returns>
        private static SynchronizationItem DBUpdateSynchronizationItem(SynchronizationItem toUpdate)
        {
            using (new Logger(toUpdate))
            {
                ToSync sync = new ToSync();
                sync.syncType = "u";
                sync.synchronizationItemId = toUpdate.id;

                //make changes in the database
                DAL.UpdateSynchronizationItem(toUpdate);
                DAL.AddToSync(sync);

                return toUpdate;
            }
        }

        /// <summary>
        /// Deletes a synchronization item from the database chosen by the given attributes
        /// </summary>
        /// <param name="name">filename or foldername</param>
        /// <param name="path">full path</param>
        /// <returns>item which was deleted from the database</returns>
        private static SynchronizationItem DBDeleteSynchronizationItem(string name, string path)
        {
            using (new Logger(name, path))
            {
                SynchronizationItem toDelete = DAL.GetSynchronizationItem(name, path);

                //if it was a folder, delete all contained files/folder 
                if (toDelete != null)
                {
                    if (Convert.ToBoolean(toDelete.folderFlag))
                    {
                        string fullPath = Path.Combine(toDelete.path, toDelete.fullname);
                        List<SynchronizationItem> items = DAL.GetSynchronizationItems(fullPath);
                        foreach (SynchronizationItem item in items)
                            DAL.DeleteSynchronizationItem(item);
                    }

                    DAL.DeleteSynchronizationItem(toDelete);
                }

                return toDelete;
            }
        }

        /// <summary>
        /// Deletes a synchronization item from the database chosen by the given attributes
        /// </summary>
        /// <param name="name">filename or foldername</param>
        /// <param name="path">full path</param>
        /// <returns>item which was deleted from the database</returns>
        private static SynchronizationItem DBAddDeletion(string name, string path)
        {
            using (new Logger(name, path))
            {
                SynchronizationItem toDelete = DAL.GetSynchronizationItem(name, path);

                //if it was a folder, delete all contained files/folder 
                if (toDelete != null)
                {
                    ToSync sync = new ToSync();
                    sync.syncType = "d";
                    sync.synchronizationItemId = toDelete.id;
                    DAL.AddToSync(sync);
                }

                return toDelete;
            }
        }

        #endregion

        #region File Watcher

        /// <summary>
        /// Creates a new File Watcher instance for the given directory path and registers the related event handlers
        /// </summary>
        /// <param name="directory">directory which should get be watched for changes/updates</param>
        public static void AddWatcher(string directory)
        {
            using (new Logger(directory))
            {
                if (Directory.Exists(directory))
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
                StartTimer();
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
                Thread.Sleep(250);

                if (GetFiltered(e.Name)) return;
                DBUpdateSynchronizationItem(e.FullPath, e.OldName, e.Name);

                //if fastsync is active
                if (MySync.Default.fastSync) StartSynchronization();

                MemoryManagement.Reduce();
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
                Thread.Sleep(250);
                
                string name = e.FullPath.Split('\\').Last();
                string path = e.FullPath.Replace(name, "").TrimEnd('\\');
                DBAddDeletion(name, path);

                //if fastsync is active
                if (MySync.Default.fastSync) StartSynchronization();

                MemoryManagement.Reduce();
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
                Thread.Sleep(250);

                if (GetFiltered(e.Name)) return;
                DBUpdateSynchronizationItem(e.FullPath, e.Name);

                //if fastsync is active
                if (MySync.Default.fastSync) StartSynchronization();

                MemoryManagement.Reduce();
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
                Thread.Sleep(250);

                ItemInfo newItem = new ItemInfo();
                newItem.GetInfo(e.FullPath);

                if (GetFiltered(newItem.FullName)) return;
                string description = DAL.GetServerEntryPointByPath(((FileSystemWatcher)objectCreated).Path).description;
                AddToDatabase(newItem, description);

                //if fastsync is active
                if (MySync.Default.fastSync) StartSynchronization();

                MemoryManagement.Reduce();
            }
        }

        #endregion
    }
}
