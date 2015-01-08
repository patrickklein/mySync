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
        private static List<FileSystemWatcher> watcherList = new List<FileSystemWatcher>();
        private static List<string> ignoreFromWatching = new List<string>();
        private static System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private static bool sync = false;
        private static string timeFormat = "yyyy/MM/dd HH:mm:ss";

        #region Synchronization

        /// <summary>
        /// Downloads/Updates/Deletes files/folders on the filesystem and the server
        /// </summary>
        private static void StartSynchronize()
        {
            using (new Logger())
            {
                //NotifyIcon.ChangeSyncState(false);
                //NotifyIcon.ChangeIcon("Upload");
                MainWindow mainWindow = null;
                System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    mainWindow = ((MainWindow)System.Windows.Application.Current.MainWindow);
                });

                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                //Check if items on the filesystem and the databases matches together
                CheckForDifferencies();

                foreach (SynchronizationPoint syncPoint in mainWindow.ServerDGSynchronizationPoints.Items)
                {
                    ServerEntryPoint point = DAL.GetServerEntryPoint(syncPoint.Description);
                    List<SynchronizationItem> serverList = Synchronization.GetListFromServer(point.serverurl.Replace("/Upload", "/GetList"), point.id);

                    //Download files from server, if something changed
                    DownloadItemsFromServer(serverList, point);

                    //Delete files/folders if they don't exist on the server anymore
                    DeleteItemsOnClient(serverList, point);
                }

                //Send files/folders to the server, or deletes them
                UploadItemsToServer();                              

                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                ignoreFromWatching.Clear();
                DAL.ShrinkHistoryEntries();
                //NotifyIcon.ResetIcon();
                //NotifyIcon.ChangeSyncState(true);
                MemoryManagement.Reduce();

                sync = false;
            }
        }

        /// <summary>
        /// Calls an item list from the server and compares it with the filesystem/database and downloads new/changed elements
        /// </summary>
        /// <param name="serverList">list of synchronization items on the server</param>
        /// <param name="point">current server entry point</param>
        /// <returns>list of conflicting synchronization items</returns>
        private static List<SynchronizationItem> DownloadItemsFromServer(List<SynchronizationItem> serverList, ServerEntryPoint point)
        {
            using (new Logger(serverList, point))
            {
                List<SynchronizationItem> conflicted = new List<SynchronizationItem>();

                foreach (SynchronizationItem serverItem in serverList)
                {
                    SynchronizationItem item = DAL.GetSynchronizationItem(serverItem.fullname, serverItem.path);
                    if (item == null)
                    {
                        string itemPath = "";

                        //Create new file or folder
                        if (Convert.ToBoolean(serverItem.isFolder))
                        {
                            string path = Path.Combine(point.folderpath.Replace(point.folderpath.Split('\\').Last(), ""), serverItem.path, serverItem.fullname); ;

                            ignoreFromWatching.Add(path);
                            Directory.CreateDirectory(path);

                            Directory.SetLastAccessTime(path, Convert.ToDateTime(serverItem.lastAccessTime));
                            Directory.SetLastWriteTime(path, Convert.ToDateTime(serverItem.lastWriteTime));
                            Directory.SetCreationTime(path, Convert.ToDateTime(serverItem.creationTime));

                            itemPath = path;
                        }
                        else
                        {
                            string savingPath = Path.Combine(point.folderpath.Replace(point.folderpath.Split('\\').Last(), ""), serverItem.path);
                            DownloadFile(point.serverurl.Replace("/Upload", "/Download"), false, serverItem.path, savingPath, serverItem.fullname);
                            itemPath = Path.Combine(savingPath, serverItem.fullname);
                        }

                        //Add new file/folder to database
                        ItemInfo newItem = new ItemInfo();
                        newItem.GetInfo(itemPath);
                        if (!newItem.IsFolder && GetFiltered(newItem.FullName)) continue;
                        AddToDatabase(newItem, point.description);
                    }
                    else
                    {
                        bool isEqual = CheckSynchronizationItemIsEqual(item, serverItem);
                        if (!isEqual && !Convert.ToBoolean(item.isFolder))
                        {
                            string conflictedFilename = "";

                            //check if it is in ToSync table. if so there is a file conflict
                            bool toSyncExists = DAL.ToSyncExists(item.id);
                            if (toSyncExists)
                            {
                                conflictedFilename = string.Format("{0}.conflictedServer{1}", item.name, item.extension);
                                conflicted.Add(item);
                            }

                            string syncRootToFolder = Path.Combine(point.folderpath.Split('\\').Last(), item.path.Replace(point.folderpath, "").Trim('\\'));
                            DownloadFile(point.serverurl.Replace("/Upload", "/Download"), true, syncRootToFolder, item.path, item.fullname, conflictedFilename);
                        }
                    }
                }

                return conflicted;
            }
        }

        /// <summary>
        /// Gets all ToSync items from the database and start to transfer the data/changes to the related server
        /// </summary>
        private static void UploadItemsToServer()
        {
            using (new Logger())
            {
                while (true)
                {
                    ToSync toSyncItem = DAL.GetNextToSync();
                    if (toSyncItem == null) break;

                    SynchronizationItem item = DAL.GetSynchronizationItem((long)toSyncItem.synchronizationItemId);
                    if (item == null)
                    {
                        DAL.DeleteToSync(toSyncItem.id);
                        continue;
                    }

                    ServerEntryPoint entryPoint = DAL.GetServerEntryPoint((long)item.serverEntryPointId);

                    try
                    {
                        if (toSyncItem.syncType == "d")
                        {
                            string folder = Path.Combine(entryPoint.folderpath.Split('\\').Last(), item.path.Replace(entryPoint.folderpath, "").Trim('\\'));
                            DeleteFromServer(entryPoint.serverurl.Replace("/Upload", "/Delete"), folder, item.fullname);
                        }
                        else SendFileToServer(toSyncItem.syncType, item, entryPoint.serverurl, entryPoint.folderpath);

                        DAL.AddItemToHistory(item.fullname, toSyncItem.syncType, Convert.ToBoolean(item.isFolder), entryPoint.description);
                        DAL.DeleteToSync(toSyncItem.id);

                        if (toSyncItem.syncType == "d")
                            DBDeleteSynchronizationItem(item.fullname, item.path);

                        RefreshHistoryEntries();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                        new Logger().Log(String.Format("{0}: {1}", item.fullname, ex.Message.ToString()));
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Delete files/folders if they don't exist on the server anymore
        /// </summary>
        /// <param name="serverList">list of synchronization items on the server</param>
        /// <param name="point">current server entry point</param>
        private static void DeleteItemsOnClient(List<SynchronizationItem> serverList, ServerEntryPoint point)
        {
            using (new Logger(serverList, point))
            {
                List<SynchronizationItem> localItems = GetItemsFromFilesystem(point.folderpath, point.id);
                
                //Compare found files/folders attributes with the database
                foreach (SynchronizationItem item in localItems)
                {
                    int count = serverList.Where(x => x.fullname == item.fullname && item.path.EndsWith(x.path)).ToList().Count;
                    if (count == 0)
                    {
                        //Check if action was on client or server
                        SynchronizationItem dbItem = DAL.GetSynchronizationItem(item.fullname, item.path);
                        if (DAL.ToSyncExists(dbItem.id)) continue;

                        string pathWithName = Path.Combine(item.path, item.fullname);
                        ignoreFromWatching.Add(pathWithName);

                        if (Convert.ToBoolean(item.isFolder))
                        {
                            if(Directory.Exists(pathWithName)) Directory.Delete(pathWithName, true);
                        }
                        else
                        {
                            if(File.Exists(pathWithName)) File.Delete(pathWithName);
                        }
                    }
                }
            }
        }

        #endregion

        #region Diff/Compare Synchronization Items

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

                    //Get all files and folders from the watching directories
                    List<SynchronizationItem> localItems = GetItemsFromFilesystem(point.folderpath, point.id);

                    //Get all files and folders from the database for the watching directory
                    List<SynchronizationItem> dbItems = DAL.GetSynchronizationItems(point.folderpath);

                    //Compare found files/folders attributes with the database
                    foreach (SynchronizationItem item in localItems)
                    {
                        if (!Convert.ToBoolean(item.isFolder) && GetFiltered(item.fullname)) continue;
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
        /// <param name="filesystemItem">first item from the file system</param>
        /// <param name="compareItem">second item from database or server</param>
        /// <returns>value if equal or not</returns>
        private static bool CheckSynchronizationItemIsEqual(SynchronizationItem filesystemItem, SynchronizationItem compareItem)
        {
            using (new Logger(filesystemItem, compareItem))
            {
                List<bool> equalCount = new List<bool>();
                equalCount.Add(filesystemItem.creationTime == compareItem.creationTime);
                equalCount.Add(filesystemItem.extension == compareItem.extension);
                equalCount.Add(filesystemItem.isFolder == compareItem.isFolder);
                equalCount.Add(filesystemItem.fullname == compareItem.fullname);
                equalCount.Add(filesystemItem.lastAccessTime == compareItem.lastAccessTime);
                equalCount.Add(filesystemItem.lastWriteTime == compareItem.lastWriteTime);
                equalCount.Add(filesystemItem.path.EndsWith(compareItem.path));
                equalCount.Add(filesystemItem.serverEntryPointId == compareItem.serverEntryPointId);
                equalCount.Add(filesystemItem.size == compareItem.size);

                return !equalCount.Contains(false);
            }
        }

        #endregion

        #region Server Methods

        /// <summary>
        /// Sends the given file/folder to the server address, provided from the related server entry point
        /// </summary>
        /// <param name="status">defines if the file was added (n), changed (u), or deleted (d)</param>
        /// <param name="item">file/folder which should be send to the server</param>
        /// <param name="requestUri">server address from the related server entry point</param>
        /// <param name="syncFolderPath">path from the related server entry point</param>
        private static void SendFileToServer(string status, SynchronizationItem item, string requestUri, string syncFolderPath)
        {
            using (new Logger(status, item, requestUri))
            {
                using (var client = new HttpClient())
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        FileInfo file2Sync = new FileInfo(Path.Combine(item.path, item.fullname));

                        ItemInfo info = new ItemInfo();
                        info.GetInfo(file2Sync.FullName);

                        //if the item is a file, load content to server
                        if (info.IsFolder == false)
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
                        content.Add(new StringContent(info.IsFolder.ToString()), "folderFlag");
                        content.Add(new StringContent(status.ToLower()), "status");
                        content.Add(new StringContent(info.Size.ToString()), "length");
                        content.Add(new StringContent(info.CreationTime.ToString(timeFormat)), "creationTime");
                        content.Add(new StringContent(info.LastWriteTime.ToString(timeFormat)), "lastWriteTime");
                        content.Add(new StringContent(info.LastAccessTime.ToString(timeFormat)), "lastAccessTime");
                        content.Add(new StringContent(info.Directory), "directory");
                        content.Add(new StringContent(info.Extension), "extension");
                        content.Add(new StringContent(info.FullName), "fullName");

                        try
                        {
                            HttpResponseMessage responseMessage = client.PostAsync(requestUri, content).Result;
                            
                            string fileName = ((string[])responseMessage.Headers.GetValues("Filename"))[0];
                            string path = ((string[])responseMessage.Headers.GetValues("Path"))[0];
                            string lastSyncTime = ((string[])responseMessage.Headers.GetValues("LastSyncTime"))[0];
                            string error = ((string[])responseMessage.Headers.GetValues("Error"))[0];

                            if (String.IsNullOrEmpty(error))
                            {
                                item.lastSyncTime = lastSyncTime;
                                DAL.UpdateSynchronizationItem(item);
                            }
                            else
                            {
                                MessageBox.Show("Send: " + fileName + " - " + error);
                                throw new Exception(error);
                            }
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
                            HttpResponseMessage responseMessage = client.PostAsync(requestUri, content).Result;

                            string fileName = ((string[])responseMessage.Headers.GetValues("Filename"))[0];
                            string path = ((string[])responseMessage.Headers.GetValues("Path"))[0];
                            string error = ((string[])responseMessage.Headers.GetValues("Error"))[0];

                            if (!String.IsNullOrEmpty(error))
                            {
                                MessageBox.Show("Delete: " + fileName + " - " + error);
                                throw new Exception(error);
                            }
                        }
                        catch (Exception ex) { throw ex; }
                    }
                }
            }
        }

        /// <summary>
        /// Gets a list of files/folders existing on the server
        /// </summary>
        /// <param name="requestUri">server address from the related server entry point</param>
        /// <param name="serverEntryPointId">current id of the related server entry point</param>
        /// <returns>list of retrieved files/folders from the server</returns>
        private static List<SynchronizationItem> GetListFromServer(string requestUri, decimal serverEntryPointId)
        {
            using (new Logger(requestUri, serverEntryPointId))
            {
                using (var client = new HttpClient())
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        List<SynchronizationItem> list = new List<SynchronizationItem>();

                        try
                        {
                            HttpResponseMessage responseMessage = client.PostAsync(requestUri, content).Result;

                            for (int i = 0; i >= 0; i++)
                            {
                                if (!responseMessage.Headers.Contains("Fullname" + i.ToString())) break;

                                SynchronizationItem item = new SynchronizationItem();
                                item.serverEntryPointId = serverEntryPointId;
                                item.fullname = ((string[])responseMessage.Headers.GetValues("Fullname" + i.ToString()))[0];
                                item.name = ((string[])responseMessage.Headers.GetValues("Name" + i.ToString()))[0];
                                item.extension = ((string[])responseMessage.Headers.GetValues("Extension" + i.ToString()))[0];
                                item.creationTime = ((string[])responseMessage.Headers.GetValues("CreationTime" + i.ToString()))[0];
                                item.lastAccessTime = ((string[])responseMessage.Headers.GetValues("LastAccessTime" + i.ToString()))[0];
                                item.lastWriteTime = ((string[])responseMessage.Headers.GetValues("LastWriteTime" + i.ToString()))[0];
                                item.lastSyncTime = ((string[])responseMessage.Headers.GetValues("LastSyncTime" + i.ToString()))[0];
                                item.path = ((string[])responseMessage.Headers.GetValues("Path" + i.ToString()))[0];
                                item.size = Convert.ToDecimal(((string[])responseMessage.Headers.GetValues("Size" + i.ToString()))[0]);
                                item.isFolder = Convert.ToDecimal(((string[])responseMessage.Headers.GetValues("IsFolder" + i.ToString()))[0]);
                                list.Add(item);
                            }
                        }
                        catch (Exception ex) { throw ex; }

                        return list;
                    }
                }
            }
        }

        /// <summary>
        /// Downloads the given file from the server (address provided from the related server entry point)
        /// </summary>
        /// <param name="requestUri">server address from the related server entry point</param>
        /// <param name="ignore">defines if the file should be ignored by the filewatchers</param>
        /// <param name="rootFolder">folder from the server entry point to the file/folder</param>
        /// <param name="savingPath">saving path for the new file</param>
        /// <param name="fullName">filename to download from server</param>
        /// <param name="conflictedFilename">filename to rename the file is it conflicts</param>
        private static async void DownloadFile(string requestUri, bool ignore, string rootFolder, string savingPath, string fullName, string conflictedFilename = "")
        {
            using (new Logger(requestUri, ignore, rootFolder, savingPath, fullName, conflictedFilename))
            {
                using (var client = new HttpClient())
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        content.Add(new StringContent(rootFolder), "directory");
                        content.Add(new StringContent(fullName), "fullName");

                        try
                        {
                            HttpResponseMessage responseMessage = client.PostAsync(requestUri, content).Result;
                            string fileName = (String.IsNullOrEmpty(conflictedFilename)) ? ((string[])responseMessage.Headers.GetValues("Fullname"))[0] : conflictedFilename;
                            string fileNameTmp = fileName + ".tmp";
                            string fileWithPath = Path.Combine(savingPath, fileName);
                            bool isFolder = Convert.ToBoolean(((string[])responseMessage.Headers.GetValues("IsFolder"))[0]);

                            ignoreFromWatching.Add(Path.Combine(savingPath, fileName));
                            ignoreFromWatching.Add(Path.Combine(savingPath, fileNameTmp));

                            if (isFolder)
                            {
                                if (!Directory.Exists(savingPath)) Directory.CreateDirectory(fileWithPath);
                            }
                            else
                            {

                                if (!Directory.Exists(savingPath)) Directory.CreateDirectory(savingPath);
                                using (var fileStream = File.Create(Path.Combine(savingPath, fileNameTmp)))
                                {
                                    using (var httpStream = await responseMessage.Content.ReadAsStreamAsync())
                                    {
                                        httpStream.CopyTo(fileStream);
                                        fileStream.Flush();
                                    }
                                }

                                //delete old file and rename the temp file to his original name
                                File.Delete(fileWithPath);
                                File.Move(Path.Combine(savingPath, fileNameTmp), Path.Combine(savingPath, fileName));
                            }

                            Directory.SetLastAccessTime(fileWithPath, Convert.ToDateTime(((string[])responseMessage.Headers.GetValues("LastAccessTime"))[0]));
                            Directory.SetLastWriteTime(fileWithPath, Convert.ToDateTime(((string[])responseMessage.Headers.GetValues("LastWriteTime"))[0]));
                            Directory.SetCreationTime(fileWithPath, Convert.ToDateTime(((string[])responseMessage.Headers.GetValues("CreationTime"))[0]));
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
        /// <param name="toUpdate">Synchronization item to update</param>
        /// <param name="path">full path of file/folder</param>
        /// <returns>item which was updated in the database</returns>
        private static SynchronizationItem DBUpdateSynchronizationItem(SynchronizationItem toUpdate, string path)
        {
            using (new Logger(toUpdate, path))
            {
                ItemInfo item = new ItemInfo();
                item.GetInfo(path);
                toUpdate = ItemInfoToSyncItem(item, toUpdate);

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
                    if (Convert.ToBoolean(toDelete.isFolder))
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
                    //delete all entries with the same synchronizationItemId
                    DAL.DeleteToSyncBySynchronizationItem(toDelete);

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

                //Write it into database
                if (ignoreFromWatching.Contains(Path.Combine(path, name))) return;
                DBAddDeletion(name, path);

                //if fastsync is active
                if (UserPreferences.fastSync) StartSynchronization();

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

                string parentPath = e.FullPath.TrimEnd(e.FullPath.Split('\\').Last().ToCharArray()).TrimEnd('\\');
                string name = (e.Name.Contains('\\')) ? e.Name.Split('\\').Last() : e.Name;

                if (ignoreFromWatching.Contains(Path.Combine(parentPath, name))) return;

                //Write it into database
                SynchronizationItem toUpdate = DAL.GetSynchronizationItem(name, parentPath);
                if (toUpdate != null)
                {
                    if (!Convert.ToBoolean(toUpdate.isFolder) && GetFiltered(e.Name)) return;
                    DBUpdateSynchronizationItem(toUpdate, e.FullPath);
                }                

                //if fastsync is active
                if (UserPreferences.fastSync) StartSynchronization();

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

                if (ignoreFromWatching.Contains(e.FullPath)) return;

                ItemInfo newItem = new ItemInfo();
                newItem.GetInfo(e.FullPath);

                if (!newItem.IsFolder && GetFiltered(newItem.FullName)) return;

                //Write it into database
                string description = DAL.GetServerEntryPointByPath(((FileSystemWatcher)objectCreated).Path).description;
                AddToDatabase(newItem, description);

                //if fastsync is active
                if (UserPreferences.fastSync) StartSynchronization();

                MemoryManagement.Reduce();
            }
        }

        #endregion

        /// <summary>
        /// Searches all existing files and folders on the filesystem for the given path
        /// </summary>
        /// <param name="path">lookup path</param>
        /// <param name="serverEntryPointId">id of the related server entry point</param>
        /// <returns></returns>
        private static List<SynchronizationItem> GetItemsFromFilesystem(string path, decimal serverEntryPointId)
        {
            using (new Logger(path, serverEntryPointId))
            {
                List<SynchronizationItem> items = new List<SynchronizationItem>();
                FindFile fileCounter = new FindFile(path, "*", true, true, true);
                fileCounter.RaiseOnAccessDenied = false;

                fileCounter.FileFound +=
                    (o, x) =>
                    {
                        SynchronizationItem info = new SynchronizationItem();
                        info.extension = x.Extension;
                        info.isFolder = Convert.ToDecimal(x.IsDirectory);
                        info.fullname = x.Name;
                        info.name = x.Name;
                        info.size = x.Length;
                        info.lastAccessTime = x.LastAccessTime.ToString(timeFormat);
                        info.lastWriteTime = x.LastWriteTime.ToString(timeFormat);
                        info.creationTime = x.CreationTime.ToString(timeFormat);
                        info.path = x.ParentPath.TrimEnd('\\');
                        info.serverEntryPointId = serverEntryPointId;

                        //Fixing bug for getting the correct file extension
                        if (!x.IsDirectory)
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

                        //Fixing bug for getting the correct size of an folder
                        if (x.IsDirectory)
                        {
                            long size = new DirectoryInfo(x.FullPath).GetFiles("*.*", SearchOption.AllDirectories).Sum(file => file.Length);
                            info.size = size;
                        }

                        items.Add(info);
                    };

                fileCounter.Find();

                return items;
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
                newItem.isFolder = Convert.ToDecimal(item.IsFolder);
                newItem.name = (item.IsFolder) ? item.Directory : item.Filename;
                newItem.fullname = item.FullName;
                newItem.lastFullname = item.LastFullName;
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
    }
}
