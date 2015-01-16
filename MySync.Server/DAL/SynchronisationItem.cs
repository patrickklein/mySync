using MySync.Server.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MySync.Server.DAL
{
    public class SynchronisationItem
    {
        public virtual long Id { get; set; }
        public virtual String Name { get; set; }
        public virtual String Extension { get; set; }
        public virtual String Fullname { get; set; }
        public virtual String CreationTime { get; set; }
        public virtual String LastAccessTime { get; set; }
        public virtual String LastWriteTime { get; set; }
        public virtual String LastSyncTime { get; set; }
        public virtual long Size { get; set; }
        public virtual Boolean IsFolder { get; set; }
        public virtual String Path { get; set; }
        public virtual String RelativePath { get; set; }
    }

    public class SynchronisationItemService : DBService
    {
        /// <summary>
        /// Sums the size values from the database for the files/folders in the given folder
        /// </summary>
        /// <param name="path">search path for identifying the right entries</param>
        /// <returns>total size of all SynchronisationItem objects in the database</returns>
        public long GetDiskSize(string path)
        {
            using (new Logger(path))
            {
                return base.GetAll<SynchronisationItem>().Where(x => x.RelativePath.StartsWith(path)).Sum(x => x.Size);
            }
        }

        /// <summary>
        /// Retrievs an existing SynchronisationItem object with the given term from the database and returns the object
        /// </summary>
        /// <param name="path">search path for identifying the right entry</param>
        /// <returns>found SynchronisationItem object</returns>
        public SynchronisationItem Get(string path)
        {
            using (new Logger(path))
            {
                return base.GetAll<SynchronisationItem>().SingleOrDefault(x => x.Path == path);
            }
        }

        /// <summary>
        /// Retrievs an existing SynchronisationItem object with the given name and directory from the database and returns the object
        /// </summary>
        /// <param name="path">search path for identifying the right entry</param>
        /// <param name="fullname">search name for identifying the right entry</param>
        /// <returns>found SynchronisationItem object</returns>
        public SynchronisationItem Get(string path, string fullname)
        {
            using (new Logger(path, fullname))
            {
                return base.GetAll<SynchronisationItem>().SingleOrDefault(x => x.RelativePath == path && x.Fullname == fullname);
            }
        }

        /// <summary>
        /// Adds a given SynchronisationItem object to the database, if the value needed values are not null or empty
        /// </summary>
        /// <param name="synchronisationItem">object to save</param>
        public void Add(SynchronisationItem synchronisationItem)
        {
            using (new Logger(synchronisationItem))
            {
                base.Add(synchronisationItem);
            }
        }

        /// <summary>
        /// Updates the database entry of the given SynchronisationItem object, if the needed values are not null or empty
        /// (checks if the object already exists and updates or creates it)
        /// </summary>
        /// <param name="synchronisationItem">object to update</param>
        public void Update(SynchronisationItem synchronisationItem)
        {
            using (new Logger(synchronisationItem))
            {
                if (String.IsNullOrEmpty(synchronisationItem.Fullname)) return;

                SynchronisationItem existingValue = Get(synchronisationItem.Fullname);
                if (existingValue != null)
                {
                    existingValue.Fullname = synchronisationItem.Fullname;
                    base.Update(existingValue);
                }
                else Add(synchronisationItem);
            }
        }

        /// <summary>
        /// Deletes the database entry of the given SynchronisationItem object
        /// </summary>
        /// <param name="synchronisationItem">object to delete</param>
        public void Delete(SynchronisationItem synchronisationItem)
        {
            using (new Logger(synchronisationItem))
            {
                base.Delete(synchronisationItem);
            }
        }
        
        /// <summary>
        /// Deletes the database entry of the given path (directory and filename)
        /// </summary>
        /// <param name="fullPath">directory and filename</param>
        public void Delete(string fullPath)
        {
            using (new Logger(fullPath))
            {
                List<SynchronisationItem> existingItems = base.GetAll<SynchronisationItem>().Where(x => x.Path.StartsWith(fullPath)).ToList<SynchronisationItem>();
                foreach (SynchronisationItem item in existingItems)
                    base.Delete(item);
            }
        }
    }
}