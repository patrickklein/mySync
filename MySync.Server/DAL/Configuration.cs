using MySync.Server.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MySync.Server.DAL
{
    public class Configuration
    {
        public virtual long Id { get; set; }
        public virtual String Field { get; set; }
        public virtual String Value { get; set; }
    }

    public class ConfigurationService : DBService
    {
        /// <summary>
        /// Retrievs an existing Configuration object with the given term from the database and returns the object
        /// </summary>
        /// <param name="term">search term for identifying the right entry</param>
        /// <returns>found Configuration object</returns>
        public Configuration Get(string term)
        {
            return base.GetAll<Configuration>().SingleOrDefault(x => x.Field == term);
        }

        /// <summary>
        /// Adds a given Configuration object to the database, if the value is not null or empty
        /// </summary>
        /// <param name="configuration">object to save</param>
        public void Add(Configuration configuration)
        {
            if (String.IsNullOrEmpty(configuration.Value)) return;
            base.Add(configuration);
        }

        /// <summary>
        /// Updates the database entry of the given Configuration object, if the value is not null or empty
        /// (checks if the object already exists and updates or creates it)
        /// </summary>
        /// <param name="configuration">object to update</param>
        public void Update(Configuration configuration)
        {
            if (String.IsNullOrEmpty(configuration.Value)) return;

            Configuration existingValue = Get(configuration.Field);
            if (existingValue != null)
            {
                existingValue.Value = configuration.Value;
                base.Update(existingValue);
            }
            else Add(configuration);
        }

        /// <summary>
        /// Deletes the database entry of the given Configuration object
        /// </summary>
        /// <param name="configuration">object to update</param>
        public void Delete(Configuration configuration)
        {
            base.Delete(configuration);
        }
    }
}