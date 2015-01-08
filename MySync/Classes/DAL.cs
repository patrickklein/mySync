using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace My_Sync.Classes
{
    static class DAL
    {
        #region ToSync

        /// <summary>
        /// Adds a new history entry to the database
        /// </summary>
        /// <param name="newToSync">new entry to store in the database</param>
        /// <returns>return the id value from the database</returns>
        public static long AddToSync(ToSync newToSync)
        {
            using (new Logger(newToSync))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    Mutex _mutex = new Mutex(false, "ToSync");
                    bool lockTaken = false;
                    bool exists = false;

                    try { lockTaken = _mutex.WaitOne(); }
                    catch (AbandonedMutexException ex) { MessageBox.Show(ex.Message.ToString()); }
                    finally
                    {
                        if (lockTaken == true)
                        {
                            //check if toSync item already exists (saves time through non redundancy for synchronization)
                            exists = CheckExistsToSync(newToSync);

                            if (!exists)
                            {
                                newToSync.id = GetNextToSyncId();
                                dbInstance.ToSync.Add(newToSync);
                                dbInstance.SaveChanges();
                            }

                            _mutex.ReleaseMutex();
                        }
                    }

                    return (exists) ? -1 : newToSync.id;
                }
            }
        }

        /// <summary>
        /// Checks if the given object already exists in the database
        /// </summary>
        /// <param name="toSync">element to check for</param>
        /// <returns>true if exists, false if not</returns>
        private static bool CheckExistsToSync(ToSync toSync)
        {
            using (new Logger(toSync))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    //check if toSync item already exists (saves time through non redundancy for synchronization)
                    List<ToSync> list = GetToSync();
                    int existingCount = list.Where(x => x.synchronizationItemId == toSync.synchronizationItemId && x.syncType == toSync.syncType).ToList().Count;
                    if (existingCount == 0) return false;
                    else return true;
                }
            }
        }

        /// <summary>
        /// Gets the next available id in the database for adding a new item
        /// </summary>
        /// <returns>next valid id value</returns>
        private static long GetNextToSyncId()
        {
            using (new Logger())
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    int count = dbInstance.ToSync.ToList().Count;
                    if (count == 0) return count;
                    return dbInstance.ToSync.OrderBy(x => x.id).ToList().Last().id + 1;
                }
            }
        }

        /// <summary>
        /// Gets the next ToSync item from the database
        /// </summary>
        /// <returns>next toSync item</returns>
        public static ToSync GetNextToSync()
        {
            using (new Logger())
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    return dbInstance.ToSync.OrderBy(x => x.id).ToList().FirstOrDefault();
                }
            }
        }

        /// <summary>
        /// Gets all existing ToSync items from the database
        /// </summary>
        /// <returns>list of toSync items</returns>
        public static List<ToSync> GetToSync()
        {
            using (new Logger())
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    return dbInstance.ToSync.ToList();
                }
            }
        }

        /// <summary>
        /// Checks if there is a ToSync item in the database with the given synchronizationItemId
        /// </summary>
        /// <param name="itemId">id of the synchronization item</param>
        /// <returns>true/false if item exists in the table or not</returns>
        public static bool ToSyncExists(long itemId)
        {
            using (new Logger())
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    return dbInstance.ToSync.ToList().Exists(x => x.synchronizationItemId == itemId);
                }
            }
        }

        /// <summary>
        /// Deletes the ToSync entry from the database with the given synchronization item id
        /// </summary>
        /// <param name="id">id to delete from the database</param>
        public static void DeleteToSync(long id)
        {
            using (new Logger(id))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    ToSync toDelete = dbInstance.ToSync.Single(x => x.id == id);
                    dbInstance.ToSync.Remove(toDelete);
                    dbInstance.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Deletes the ToSync entry from the database with the given synchronization item
        /// </summary>
        /// <param name="item">synchronization item which should be deleted from the database</param>
        public static void DeleteToSyncBySynchronizationItem(SynchronizationItem item)
        {
            using (new Logger(item))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    Mutex _mutex = new Mutex(false, "SynchronizationItem");
                    bool lockTaken = false;

                    try { lockTaken = _mutex.WaitOne(); }
                    catch (AbandonedMutexException ex) { MessageBox.Show(ex.Message.ToString()); }
                    finally
                    {
                        if (lockTaken == true)
                        {
                            List<ToSync> items = dbInstance.ToSync.ToList().Where(x => x.synchronizationItemId == item.id).ToList();
                            foreach (ToSync itemToDelete in items)
                                dbInstance.ToSync.Remove(itemToDelete);

                            dbInstance.SaveChanges();

                            _mutex.ReleaseMutex();
                        }
                    }
                }
            }
        }

        #endregion

        #region History

        /// <summary>
        /// Adds a new history entry to the database
        /// </summary>
        /// <param name="newHistory">new entry to store in the database</param>
        /// <returns>return the id value from the database</returns>
        public static long AddHistory(History newHistory)
        {
            using (new Logger(newHistory))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    Mutex _mutex = new Mutex(false, "History");
                    bool lockTaken = false;

                    try { lockTaken = _mutex.WaitOne(); }
                    finally
                    {
                        if (lockTaken == true)
                        {
                            newHistory.id = DAL.GetNextHistoryId();
                            dbInstance.History.Add(newHistory);
                            dbInstance.SaveChanges();

                            _mutex.ReleaseMutex();
                        }
                    }

                    return newHistory.id;
                }
            }
        }

        /// <summary>
        /// Adds a new history entry to the database
        /// </summary>
        /// <param name="newHistory">new entry to store in the database</param>
        public static void AddItemToHistory(string name, string historyFlag, bool isFolder, string serverDescription)
        {
            using (new Logger(name, historyFlag, isFolder, serverDescription))
            {
                History newEntry = new History();

                string historyEvent = "";
                switch(historyFlag) {
                    case "u": historyEvent = "updated on"; break;
                    case "n": historyEvent = "sent to"; break;
                    case "d": historyEvent = "deleted from"; break;
                }

                newEntry.timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"); 
                newEntry.entry = String.Format("{0} '{1}' {2} server '{3}'.", 
                                               (isFolder) ? "Folder" : "File", 
                                               name, 
                                               historyEvent,
                                               serverDescription);
                
                AddHistory(newEntry);
            }
        }

        /// <summary>
        /// Gets all existing history items from the database
        /// </summary>
        /// <param name="showItems">number of items shown in the history list on the GUI</param>
        /// <returns>concatenated history entry for the GUI textbox</returns>
        public static string GetHistory(int showItems = 100)
        {
            using (new Logger())
            {
                string history = "";
                using (MySyncEntities dbInstance = new MySyncEntities())
                {

                    List<History> entries = dbInstance.History.OrderByDescending(x => x.timestamp).Take(showItems).ToList();
                    foreach (History entry in entries)
                        history += String.Format("[{0:dd/MM/yyyy HH:mm}]: {1}\r", DateTime.ParseExact(entry.timestamp, "yyyy/MM/dd HH:mm:ss", null), entry.entry);

                    return history;
                }
            }
        }

        /// <summary>
        /// Gets the next available id in the database for adding a new item
        /// </summary>
        /// <returns>next valid id value</returns>
        private static long GetNextHistoryId()
        {
            using (new Logger())
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    int count = dbInstance.History.ToList().Count;
                    if (count == 0) return count;
                    return dbInstance.History.OrderBy(x => x.id).ToList().Last().id + 1;
                }
            }
        }

        /// <summary>
        /// Deletes the history entry from the database with the given entry value
        /// </summary>
        /// <param name="entry">entry to delete from the database</param>
        public static void DeleteHistory(string entry) 
        {
            using(new Logger(entry)) 
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    History historyToDelete = dbInstance.History.Single(x => x.entry.Equals(entry));
                    dbInstance.History.Remove(historyToDelete);
                    dbInstance.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Deletes all history entries in the database except the last count of the given value
        /// </summary>
        /// <param name="count">deletes all entries except the last given ones</param>
        public static void ShrinkHistoryEntries(int count = 100)
        {
            using (new Logger(count))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    Mutex _mutex = new Mutex(false, "History");
                    bool lockTaken = false;

                    try { lockTaken = _mutex.WaitOne(); }
                    finally
                    {
                        if (lockTaken == true)
                        {
                            List<History> entries = dbInstance.History.OrderBy(x => x.timestamp).ToList();
                            for (int i = 0; i < entries.Count - count; i++)
                                dbInstance.History.Remove(entries[i]);
                            
                            dbInstance.SaveChanges();

                            _mutex.ReleaseMutex();
                        }
                    }
                }
            }
        }

        #endregion

        #region File Filter

        /// <summary>
        /// Adds a new File Filter entry to the database
        /// </summary>
        /// <param name="newFilter">new entry to store in the database</param>
        /// <returns>return the id value from the database</returns>
        public static long AddFileFilter(FileFilter newFilter)
        {
            using (new Logger(newFilter))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    Mutex _mutex = new Mutex(false, "FileFilter");
                    bool lockTaken = false;

                    try { lockTaken = _mutex.WaitOne(); }
                    finally
                    {
                        if (lockTaken == true)
                        {
                            newFilter.id = GetNextFileFilterId();
                            dbInstance.FileFilter.Add(newFilter);
                            dbInstance.SaveChanges();

                            _mutex.ReleaseMutex();
                        }
                    }

                    return newFilter.id;
                }
            }
        }

        /// <summary>
        /// Gets all existing file filters from the database
        /// </summary>
        /// <returns>list of file filters</returns>
        public static List<FileFilter> GetFileFilters()
        {
            using (new Logger())
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    return dbInstance.FileFilter.ToList();
                }
            }
        }

        /// <summary>
        /// Gets all existing file filter terms from the database
        /// </summary>
        /// <returns>list of file filter terms</returns>
        public static List<string> GetFileFilterTerms()
        {
            using (new Logger())
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    List<string> terms = new List<string>();
                    dbInstance.FileFilter.ToList().ForEach(x => terms.Add(x.term));
                    return terms;
                }
            }
        }

        /// <summary>
        /// Gets the next available id in the database for adding a new item
        /// </summary>
        /// <returns>next valid id value</returns>
        private static long GetNextFileFilterId()
        {
            using (new Logger())
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    int count = dbInstance.FileFilter.ToList().Count;
                    if (count == 0) return count;
                    return dbInstance.FileFilter.OrderBy(x => x.id).ToList().Last().id + 1;
                }
            }
        }

        /// <summary>
        /// Deletes the file filter entry from the database with the given filter value
        /// </summary>
        /// <param name="filter">entry to delete from the database</param>
        public static void DeleteFileFilter(string filter) 
        {
            using(new Logger(filter)) 
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    FileFilter filterToDelete = dbInstance.FileFilter.Single(x => x.term.Equals(filter));
                    dbInstance.FileFilter.Remove(filterToDelete);
                    dbInstance.SaveChanges();
                }
            }
        }

        #endregion

        #region Server Entry Point

        /// <summary>
        /// Adds a new Server Entry Point to the database
        /// </summary>
        /// <param name="newEntryPoint">new entry to store in the database</param>
        /// <returns>return the id value from the database</returns>
        public static long AddServerEntryPoint(SynchronizationPoint newEntryPoint)
        {
            using (new Logger(newEntryPoint))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    ServerEntryPoint newPoint = new ServerEntryPoint();
                    Mutex _mutex = new Mutex(false, "ServerEntryPoint");
                    bool lockTaken = false;

                    try { lockTaken = _mutex.WaitOne(); }
                    finally
                    {
                        if (lockTaken == true)
                        {
                            newPoint.description = newEntryPoint.Description;
                            newPoint.folderpath = newEntryPoint.Folder;
                            newPoint.icon = newEntryPoint.ServerType.Name;
                            newPoint.serverurl = newEntryPoint.Server;
                            newPoint.id = DAL.GetNextServerEntryPointId();

                            dbInstance.ServerEntryPoint.Add(newPoint);
                            dbInstance.SaveChanges();

                            _mutex.ReleaseMutex();
                        }
                    }

                    return newPoint.id;
                }
            }
        }

        /// <summary>
        /// Gets all existing server entry points from the database
        /// </summary>
        /// <returns>list of server entry points</returns>
        public static List<SynchronizationPoint> GetServerEntryPoints()
        {
            using (new Logger())
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    List<SynchronizationPoint> entryPointList = new List<SynchronizationPoint>();

                    foreach (ServerEntryPoint point in dbInstance.ServerEntryPoint)
                    {
                        SynchronizationPoint newPoint = new SynchronizationPoint();
                        newPoint.ServerType = Helper.GetImageOfAssembly(point.icon);
                        newPoint.Description = point.description;
                        newPoint.Folder = point.folderpath;
                        newPoint.Server = point.serverurl;

                        entryPointList.Add(newPoint);
                    }

                    return entryPointList;
                }
            }
        }

        /// <summary>
        /// Gets the next available id in the database for adding a new item
        /// </summary>
        /// <returns>next valid id value</returns>
        private static long GetNextServerEntryPointId()
        {
            using (new Logger())
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    int count = dbInstance.ServerEntryPoint.ToList().Count;
                    if (count == 0) return count;
                    return dbInstance.ServerEntryPoint.OrderBy(x => x.id).ToList().Last().id + 1;
                }
            }
        }

        /// <summary>
        /// Gets the server entry point with the related description
        /// </summary>
        /// <param name="description">value to search for the wanted server entry point</param>
        /// <returns>the found server entry point</returns>
        public static ServerEntryPoint GetServerEntryPoint(string description)
        {
            using (new Logger(description))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    return dbInstance.ServerEntryPoint.SingleOrDefault(x => x.description.Equals(description));
                }
            }
        }

        /// <summary>
        /// Gets the server entry point with the given id
        /// </summary>
        /// <param name="id">value to search for the wanted server entry point</param>
        /// <returns>the found server entry point</returns>
        public static ServerEntryPoint GetServerEntryPoint(long id)
        {
            using (new Logger(id))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    return dbInstance.ServerEntryPoint.SingleOrDefault(x => x.id == id);
                }
            }
        }

        /// <summary>
        /// Gets the server entry point with the related path
        /// </summary>
        /// <param name="path">value to search for the wanted server entry point</param>
        /// <returns>the found server entry point</returns>
        public static ServerEntryPoint GetServerEntryPointByPath(string path)
        {
            using (new Logger(path))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    return dbInstance.ServerEntryPoint.SingleOrDefault(x => x.folderpath.Equals(path));
                }
            }
        }

        /// <summary>
        /// Deletes the server entry point from the database with the given description
        /// </summary>
        /// <param name="description">entry to delete from the database</param>
        public static void DeleteServerEntryPoint(string description)
        {
            using (new Logger(description))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    ServerEntryPoint entryPointToDelete = dbInstance.ServerEntryPoint.Single(x => x.description.Equals(description));
                    dbInstance.ServerEntryPoint.Remove(entryPointToDelete);
                    dbInstance.SaveChanges();

                    //delete related files and folders from the synchronization item table
                    Task.Run(() => DeleteSynchronizationItem(entryPointToDelete.id));
                }
            }
        }

        #endregion

        #region Synchronization Item

        /// <summary>
        /// Adds a new entry to the database
        /// </summary>
        /// <param name="newFile">new entry to store in the database</param>
        /// <returns>return the id value from the database</returns>
        public static long AddSynchronizationItem(SynchronizationItem newItem)
        {
            using (new Logger(newItem))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    Mutex _mutex = new Mutex(false, "SynchronizationItem");
                    bool lockTaken = false;
                    bool exists = false;

                    try { lockTaken = _mutex.WaitOne(); }
                    catch (AbandonedMutexException ex) { MessageBox.Show(ex.Message.ToString()); }
                    finally
                    {
                        if (lockTaken == true)
                        {
                            exists = CheckExistsSynchronizationItem(newItem);

                            if (!exists)
                            {
                                newItem.id = DAL.GetNextSynchronizationItemId();
                                dbInstance.SynchronizationItem.Add(newItem);
                                dbInstance.SaveChanges();
                            }

                            _mutex.ReleaseMutex();
                        }
                    }

                    return (exists) ? -1 : newItem.id;
                }
            }
        }

        /// <summary>
        /// Updates a synchronization item in the database with the passed item
        /// </summary>
        /// <param name="changedItem">item with updated values</param>
        public static void UpdateSynchronizationItem(SynchronizationItem changedItem)
        {
            using (new Logger(changedItem))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    SynchronizationItem item = dbInstance.SynchronizationItem.ToList().Single(x => x.id == changedItem.id);
                    dbInstance.SynchronizationItem.Remove(item);
                    dbInstance.SynchronizationItem.Add(changedItem);

                    dbInstance.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Returns a synchronization item object based on the stored fullname and path
        /// </summary>
        /// <param name="fullname">name of the file/folder</param>
        /// <param name="path">full path of the file/folder</param>
        /// <returns>found synchronization item object</returns>
        public static SynchronizationItem GetSynchronizationItem(string fullname, string path)
        {
            using (new Logger(fullname, path))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    return dbInstance.SynchronizationItem.SingleOrDefault(x => x.fullname.Equals(fullname) && x.path.EndsWith(path));
                }
            }
        }

        /// <summary>
        /// Returns a synchronization item object based on the stored fullname and path
        /// </summary>
        /// <param name="fullname">name of the file/folder</param>
        /// <param name="path">full path of the file/folder</param>
        /// <param name="serverPointId">related server point id of the file/folder</param>
        /// <returns>found synchronization item object</returns>
        public static SynchronizationItem GetSynchronizationItem(string fullname, string path, long serverPointId)
        {
            using (new Logger(fullname, path))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    return dbInstance.SynchronizationItem.SingleOrDefault(x => x.fullname.Equals(fullname) && x.path.Equals(path) && x.serverEntryPointId == serverPointId);
                }
            }
        }

        /// <summary>
        /// Returns a synchronization item object based on the given id
        /// </summary>
        /// <param name="id">id of the item in the database</param>
        /// <returns>found synchronization item object</returns>
        public static SynchronizationItem GetSynchronizationItem(long id)
        {
            using (new Logger(id))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    return dbInstance.SynchronizationItem.SingleOrDefault(x => x.id == id);
                }
            }
        }

        /// <summary>
        /// Returns a list of synchronization item objects based on the given path
        /// </summary>
        /// <param name="path">full path of the file/folder (starts with)</param>
        /// <returns>list of found synchronization item objects</returns>
        public static List<SynchronizationItem> GetSynchronizationItems(string path)
        {
            using (new Logger(path))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    return dbInstance.SynchronizationItem.ToList().Where(x => x.path.StartsWith(path)).ToList();
                }
            }
        }

        /// <summary>
        /// Gets all existing files from the database
        /// </summary>
        /// <returns>list of synchronization items consisting of file informations</returns>
        public static List<SynchronizationItem> GetFiles()
        {
            using (new Logger())
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    return dbInstance.SynchronizationItem.ToList().Where(x => Convert.ToBoolean(x.isFolder) == false).ToList();
                }
            }
        }

        /// <summary>
        /// Gets all existing folders from the database
        /// </summary>
        /// <returns>list of synchronization items consisting of folder informations</returns>
        public static List<SynchronizationItem> GetFolders()
        {
            using (new Logger())
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    return dbInstance.SynchronizationItem.ToList().Where(x => Convert.ToBoolean(x.isFolder) == true).ToList();
                }
            }
        }

        /// <summary>
        /// Checks if the given object already exists in the database
        /// </summary>
        /// <param name="synchronizationItem">element to check for</param>
        /// <returns>true if exists, false if not</returns>
        private static bool CheckExistsSynchronizationItem(SynchronizationItem synchronizationItem)
        {
            using (new Logger(synchronizationItem))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    //check if toSync item already exists (saves time through non redundancy for synchronization)
                    SynchronizationItem item = GetSynchronizationItem(synchronizationItem.fullname, synchronizationItem.path);
                    if (item == null) return false;
                    else return true;
                }
            }
        }

        /// <summary>
        /// Gets the next available id in the database for adding a new item
        /// </summary>
        /// <returns>next valid id value</returns>
        private static long GetNextSynchronizationItemId()
        {
            using (new Logger())
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    int count = dbInstance.SynchronizationItem.Count();
                    if (count == 0) return count;

                    var element = dbInstance.SynchronizationItem.OrderBy(x => x.id).AsEnumerable().ElementAt(count - 1);
                    return element.id + 1;
                }
            }
        }

        /// <summary>
        /// Deletes the entry from the database with the given filename
        /// </summary>
        /// <param name="fullFileName">entry to delete from the database</param>
        public static void DeleteSynchronizationItem(string fullFileName)
        {
            using (new Logger(fullFileName))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    SynchronizationItem itemToDelete = dbInstance.SynchronizationItem.Single(x => x.name.Equals(fullFileName));
                    dbInstance.SynchronizationItem.Remove(itemToDelete);
                    dbInstance.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Deletes the entry from the database with the given filename
        /// </summary>
        /// <param name="item">item which should be delete from the database</param>
        public static void DeleteSynchronizationItem(SynchronizationItem item)
        {
            using (new Logger(item))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    SynchronizationItem itemToDelete = dbInstance.SynchronizationItem.Single(x => x.id == item.id);
                    dbInstance.SynchronizationItem.Remove(itemToDelete);
                    dbInstance.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Deletes the entries from the database with the given id of the server entry point
        /// </summary>
        /// <param name="serverEntryPointId">id which entries should be deleted from the database</param>
        public static void DeleteSynchronizationItem(long serverEntryPointId)
        {
            using (new Logger(serverEntryPointId))
            {
                using (MySyncEntities dbInstance = new MySyncEntities())
                {
                    List<SynchronizationItem> items = dbInstance.SynchronizationItem.ToList().Where(x => x.serverEntryPointId == serverEntryPointId).ToList();
                    foreach (SynchronizationItem itemToDelete in items)
                    {
                        dbInstance.SynchronizationItem.Remove(itemToDelete);
                        DeleteToSyncBySynchronizationItem(itemToDelete);
                    }

                    dbInstance.SaveChanges();
                }
            }
        }

        #endregion
    }
}