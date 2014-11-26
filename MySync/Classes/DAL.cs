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

namespace My_Sync.Classes
{
    static class DAL
    {
        private static MySyncEntities dbInstance = new MySyncEntities();

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

        /// <summary>
        /// Adds a new history entry to the database
        /// </summary>
        /// <param name="newHistory">new entry to store in the database</param>
        public static void AddHistory(History newHistory)
        {
            using (new Logger(newHistory))
            {
                dbInstance.History.Add(newHistory);
                dbInstance.SaveChanges();
            }
        }

        /// <summary>
        /// Gets all existing history items from the database
        /// </summary>
        /// <param name="showItems">number of items shown in the history list on the GUI</param>
        /// <returns></returns>
        public static string GetHistory(int showItems = 100)
        {
            using (new Logger())
            {
                string history = "";

                List<History> entries = dbInstance.History.OrderByDescending(x => x.timestamp).Take(showItems).ToList();
                foreach (History entry in entries)
                    history += String.Format("[{0:dd/MM/yyyy HH:mm}]: {1}\r", DateTime.ParseExact(entry.timestamp, "yyyy/MM/dd HH:mm:ss", null), entry.entry);

                return history;
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
                History historyToDelete = dbInstance.History.Single(x => x.entry.Equals(entry));
                dbInstance.History.Remove(historyToDelete);
                dbInstance.SaveChanges();
            }
        }

        /// <summary>
        /// Adds a new File Filter entry to the database
        /// </summary>
        /// <param name="newFilter">new entry to store in the database</param>
        public static void AddFileFilter(FileFilter newFilter)
        {
            using (new Logger(newFilter))
            {
                dbInstance.FileFilter.Add(newFilter);
                dbInstance.SaveChanges();
            }
        }

        /// <summary>
        /// Gets all existing file filters from the database
        /// </summary>
        /// <returns></returns>
        public static List<FileFilter> GetFileFilters()
        {
            using (new Logger())
            {
                return dbInstance.FileFilter.ToList<FileFilter>();
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
                FileFilter filterToDelete = dbInstance.FileFilter.Single(x => x.term.Equals(filter));
                dbInstance.FileFilter.Remove(filterToDelete);
                dbInstance.SaveChanges();
            }
        }

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

                dbInstance.ServerEntryPoint.Add(newPoint);
                dbInstance.SaveChanges();
            }
        }

        /// <summary>
        /// Gets all existing server entry points from the database
        /// </summary>
        /// <returns></returns>
        public static List<SynchronizationPoint> GetServerEntryPoints()
        {
            using (new Logger())
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

        /// <summary>
        /// Deletes the server entry point from the database with the given description
        /// </summary>
        /// <param name="description">entry to delete from the database</param>
        public static void DeleteServerEntryPoint(string description)
        {
            using (new Logger(description))
            {
                ServerEntryPoint entryPointToDelete = dbInstance.ServerEntryPoint.Single(x => x.description.Equals(description));
                dbInstance.ServerEntryPoint.Remove(entryPointToDelete);
                dbInstance.SaveChanges();
            }
        }
    }
}