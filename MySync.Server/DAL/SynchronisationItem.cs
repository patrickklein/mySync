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
        public virtual DateTime CreationTime { get; set; }
        public virtual DateTime LastAccessTime { get; set; }
        public virtual DateTime LastWriteTime { get; set; }
        public virtual DateTime LastSyncTime { get; set; }
        public virtual long Size { get; set; }
        public virtual Boolean FolderFlag { get; set; }
        public virtual Boolean HiddenFlag { get; set; }
        public virtual Boolean SystemFlag { get; set; }
        public virtual long Files { get; set; }
        public virtual long Folders { get; set; }
        public virtual String Path { get; set; }
    }

    public class SynchronisationItemService : DBService
    {
        /// <summary>
        /// Retrievs an existing SynchronisationItem object with the given term from the database and returns the object
        /// </summary>
        /// <param name="path">search path for identifying the right entry</param>
        /// <returns>found SynchronisationItem object</returns>
        public SynchronisationItem Get(string path)
        {
            return base.GetAll<SynchronisationItem>().SingleOrDefault(x => x.Path == path);
        }

        /// <summary>
        /// Adds a given SynchronisationItem object to the database, if the value needed values are not null or empty
        /// </summary>
        /// <param name="synchronisationItem">object to save</param>
        public void Add(SynchronisationItem synchronisationItem)
        {
            //if (String.IsNullOrEmpty(synchronisationItem.Value)) return;
            base.Add(synchronisationItem);
        }

        /// <summary>
        /// Updates the database entry of the given SynchronisationItem object, if the needed values are not null or empty
        /// (checks if the object already exists and updates or creates it)
        /// </summary>
        /// <param name="synchronisationItem">object to update</param>
        public void Update(SynchronisationItem synchronisationItem)
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

        /// <summary>
        /// Deletes the database entry of the given SynchronisationItem object
        /// </summary>
        /// <param name="synchronisationItem">object to update</param>
        public void Delete(SynchronisationItem synchronisationItem)
        {
            base.Delete(synchronisationItem);
        }
    }
}