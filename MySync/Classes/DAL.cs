using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace My_Sync.Classes
{
    static class DAL
    {
        private static MySyncEntities dbInstance;

        /// <summary>
        /// Singleton Methof of getting a MySyncEntities database instance
        /// </summary>
        /// <returns>current instance of MySyncEntities database</returns>
        private static MySyncEntities GetDBInstance()
        {
            if (dbInstance != null) return dbInstance;

            using (new Logger())
            {
                MainWindow mainWindow = ((MainWindow)System.Windows.Application.Current.MainWindow);
                dbInstance = mainWindow.dbInstance;
                return dbInstance;
            }
        }

        /// <summary>
        /// Method for creating the SQLite database if it not exists (extract from embedded resources)
        /// </summary>
        public static void CreateDatabase()
        {
            using (new Logger())
            {
                // Get Current Assembly refrence and all imbedded resources
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                string[] arrResources = currentAssembly.GetManifestResourceNames();

                foreach (string resourceName in arrResources)
                {
                    if (resourceName.EndsWith(".db") || resourceName.EndsWith(".dll"))
                    {
                        //Name of the file saved on disk
                        MainWindow mainWindow = ((MainWindow)System.Windows.Application.Current.MainWindow);
                        string saveAsName = resourceName.Replace(mainWindow.GetType().Namespace, "").TrimStart('.');
                        saveAsName = saveAsName.Replace("Resources.", "");
                        FileInfo fileInfoOutputFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), saveAsName));

                        if (fileInfoOutputFile.Exists) continue;

                        foreach (Process proc in Process.GetProcessesByName(fileInfoOutputFile.Name.Replace(".exe", "")))
                            proc.Kill();

                        //open newly creating file for writing and get the stream to the resources
                        FileStream streamToOutputFile = fileInfoOutputFile.OpenWrite();
                        Stream streamToResourceFile = currentAssembly.GetManifestResourceStream(resourceName);

                        //save resource to folder
                        const int size = 4096;
                        byte[] bytes = new byte[4096];
                        int numBytes;
                        while ((numBytes = streamToResourceFile.Read(bytes, 0, size)) > 0)
                            streamToOutputFile.Write(bytes, 0, numBytes);

                        streamToOutputFile.Close();
                        streamToResourceFile.Close();
                    }
                }
            }
        }

        //------------------------------------------------------------------------------------------------//

        #region ToSync

        /// <summary>
        /// Adds a new history entry to the database
        /// </summary>
        /// <param name="newToSync">new entry to store in the database</param>
        public static void AddToSync(ToSync newToSync)
        {
            using (new Logger(newToSync))
            {
                newToSync.id = GetNextToSyncId();
                GetDBInstance().ToSync.Add(newToSync);
                GetDBInstance().SaveChanges();
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
                int count = GetDBInstance().ToSync.ToList().Count;
                if (count == 0) return count;
                return GetDBInstance().ToSync.OrderBy(x => x.id).ToList().Last().id + 1;
            }
        }

        /// <summary>
        /// Deletes the ToSync entry from the database with the given synchronization item id
        /// </summary>
        /// <param name="synchronizationItemId">id to delete from the database</param>
        public static void DeleteToSync(int synchronizationItemId)
        {
            using (new Logger(synchronizationItemId))
            {
                ToSync toDelete = GetDBInstance().ToSync.Single(x => x.synchronizationItemId.Equals(synchronizationItemId));
                GetDBInstance().ToSync.Remove(toDelete);
                GetDBInstance().SaveChanges();
            }
        }

        #endregion

        #region History

        /// <summary>
        /// Adds a new history entry to the database
        /// </summary>
        /// <param name="newHistory">new entry to store in the database</param>
        public static void AddHistory(History newHistory)
        {
            using (new Logger(newHistory))
            {
                newHistory.id = DAL.GetNextHistoryId();
                GetDBInstance().History.Add(newHistory);
                GetDBInstance().SaveChanges();
            }
        }

        /// <summary>
        /// Adds a new history entry to the database
        /// </summary>
        /// <param name="newHistory">new entry to store in the database</param>
        public static void AddItemToHistory(string name, string historyFlag, bool folderFlag, string serverDescription)
        {
            using (new Logger(name, historyFlag, folderFlag, serverDescription))
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
                                               (folderFlag) ? "Folder" : "File", 
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

                List<History> entries = GetDBInstance().History.OrderByDescending(x => x.timestamp).Take(showItems).ToList();
                foreach (History entry in entries)
                    history += String.Format("[{0:dd/MM/yyyy HH:mm}]: {1}\r", DateTime.ParseExact(entry.timestamp, "yyyy/MM/dd HH:mm:ss", null), entry.entry);

                return history;
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
                int count = GetDBInstance().History.ToList().Count;
                if (count == 0) return count;
                return GetDBInstance().History.OrderBy(x => x.id).ToList().Last().id + 1;
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
                History historyToDelete = GetDBInstance().History.Single(x => x.entry.Equals(entry));
                GetDBInstance().History.Remove(historyToDelete);
                GetDBInstance().SaveChanges();
            }
        }

        #endregion

        #region File Filter

        /// <summary>
        /// Adds a new File Filter entry to the database
        /// </summary>
        /// <param name="newFilter">new entry to store in the database</param>
        public static void AddFileFilter(FileFilter newFilter)
        {
            using (new Logger(newFilter))
            {
                newFilter.id = GetNextFileFilterId();
                GetDBInstance().FileFilter.Add(newFilter);
                GetDBInstance().SaveChanges();
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
                return GetDBInstance().FileFilter.ToList();
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
                List<string> terms = new List<string>();
                GetDBInstance().FileFilter.ToList().ForEach(x => terms.Add(x.term));
                return terms;
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
                int count = GetDBInstance().FileFilter.ToList().Count;
                if (count == 0) return count;
                return GetDBInstance().FileFilter.OrderBy(x => x.id).ToList().Last().id + 1;
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
                FileFilter filterToDelete = GetDBInstance().FileFilter.Single(x => x.term.Equals(filter));
                GetDBInstance().FileFilter.Remove(filterToDelete);
                GetDBInstance().SaveChanges();
            }
        }

        #endregion

        #region Server Entry Point

        /// <summary>
        /// Adds a new Server Entry Point to the database
        /// </summary>
        /// <param name="newEntryPoint">new entry to store in the database</param>
        public static void AddServerEntryPoint(SynchronizationPoint newEntryPoint)
        {
            using (new Logger(newEntryPoint))
            {
                ServerEntryPoint newPoint = new ServerEntryPoint();
                newPoint.description = newEntryPoint.Description;
                newPoint.folderpath = newEntryPoint.Folder;
                newPoint.icon = newEntryPoint.ServerType.Name;
                newPoint.serverurl = newEntryPoint.Server;
                newPoint.id = DAL.GetNextServerEntryPointId();

                GetDBInstance().ServerEntryPoint.Add(newPoint);
                GetDBInstance().SaveChanges();
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
                List<SynchronizationPoint> entryPointList = new List<SynchronizationPoint>();

                foreach (ServerEntryPoint point in GetDBInstance().ServerEntryPoint)
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

        /// <summary>
        /// Gets the next available id in the database for adding a new item
        /// </summary>
        /// <returns>next valid id value</returns>
        private static long GetNextServerEntryPointId()
        {
            using (new Logger())
            {
                int count = GetDBInstance().ServerEntryPoint.ToList().Count;
                if (count == 0) return count;
                return GetDBInstance().ServerEntryPoint.OrderBy(x => x.id).ToList().Last().id + 1;
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
                return GetDBInstance().ServerEntryPoint.Single(x => x.description.Equals(description));
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
                return GetDBInstance().ServerEntryPoint.Single(x => x.folderpath.Equals(path));
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
                ServerEntryPoint entryPointToDelete = GetDBInstance().ServerEntryPoint.Single(x => x.description.Equals(description));
                GetDBInstance().ServerEntryPoint.Remove(entryPointToDelete);
                GetDBInstance().SaveChanges();

                //delete related files and folders from the synchronization item table
                DeleteSynchronizationItem(entryPointToDelete.id);
            }
        }

        #endregion

        #region Synchronization Item

        /// <summary>
        /// Adds a new entry to the database
        /// </summary>
        /// <param name="newFile">new entry to store in the database</param>
        public static void AddSynchronizationItem(SynchronizationItem newItem)
        {
            using (new Logger(newItem))
            {
                newItem.id = DAL.GetNextSynchronizationItemId();
                GetDBInstance().SynchronizationItem.Add(newItem);
                GetDBInstance().SaveChanges();
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
                SynchronizationItem item = GetDBInstance().SynchronizationItem.ToList().Single(x => x.id == changedItem.id);
                item = changedItem;
                GetDBInstance().SaveChanges();
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
                var temp = GetDBInstance().SynchronizationItem;
                return temp.SingleOrDefault(x => x.fullname.Equals(fullname) && x.path.Equals(path));
            }
        }

        /// <summary>
        /// Returns a list of synchronization item objects based on the given path
        /// </summary>
        /// <param name="path">full path of the file/folder</param>
        /// <returns>list of found synchronization item objects</returns>
        public static List<SynchronizationItem> GetSynchronizationItems(string path)
        {
            using (new Logger(path))
            {
                return GetDBInstance().SynchronizationItem.ToList().Where(x => x.path.StartsWith(path)).ToList();
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
                return GetDBInstance().SynchronizationItem.ToList().Where(x => Convert.ToBoolean(x.folderFlag) == false).ToList();
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
                return GetDBInstance().SynchronizationItem.ToList().Where(x => Convert.ToBoolean(x.folderFlag) == true).ToList();
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
                int count = GetDBInstance().SynchronizationItem.Count();
                if (count == 0) return count;

                return GetDBInstance().SynchronizationItem.OrderBy(x => x.id).ToList().Last().id + 1;
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
                SynchronizationItem itemToDelete = GetDBInstance().SynchronizationItem.Single(x => x.name.Equals(fullFileName));
                GetDBInstance().SynchronizationItem.Remove(itemToDelete);
                GetDBInstance().SaveChanges();
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
                SynchronizationItem itemToDelete = GetDBInstance().SynchronizationItem.Single(x => x.id == item.id);
                GetDBInstance().SynchronizationItem.Remove(itemToDelete);
                GetDBInstance().SaveChanges();
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
                List<SynchronizationItem> items = GetDBInstance().SynchronizationItem.ToList().Where(x => x.serverEntryPointId == serverEntryPointId).ToList();
                foreach(SynchronizationItem itemToDelete in items)
                    GetDBInstance().SynchronizationItem.Remove(itemToDelete);

                GetDBInstance().SaveChanges();
            }
        }

        #endregion
    }
}