using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MySync.Server.DAL
{
    public abstract class DBService
    {
        private ISession _mSession;

        /// <summary>
        /// Sets the given session to default one
        /// </summary>
        /// <param name="session">new session</param>
        public void SetSession(ISession session)
        {
            _mSession = session;
        }

        /// <summary>
        /// Adds a given object to the database table, based on the object type (for retrieving the right table)
        /// </summary>
        /// <param name="obj">object to save</param>
        public void Add(Object obj)
        {
            using (var transaction = _mSession.BeginTransaction())
            {
                _mSession.Save(obj);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Updates the database entry of the given object, based on the object type (for retrieving the right table)
        /// </summary>
        /// <param name="obj">object to update</param>
        public void Update(Object obj)
        {
            using (var transaction = _mSession.BeginTransaction())
            {
                _mSession.Update(obj);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Deletes the database entry of the given object, based on the object type (for retrieving the right table)
        /// </summary>
        /// <param name="obj">object to delete</param>
        public void Delete(Object obj)
        {
            using (var transaction = _mSession.BeginTransaction())
            {
                _mSession.Delete(obj);
                transaction.Commit();
            }
        }

        /// <summary>
        /// Gets a list of all existing elements/rows of a table basen on the given object type (for retrieving the right table)
        /// </summary>
        /// <typeparam name="Entity">object type for retrieving the right table</typeparam>
        /// <returns>list of elements from the type related table</returns>
        public IList<Entity> GetAll<Entity>() where Entity : class
        {
            return _mSession.CreateCriteria<Entity>().List<Entity>();
        }
    }
}