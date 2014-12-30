using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MySync.Server.Configuration
{
    public sealed class ApplicationCore
    {
        private static readonly ApplicationCore mInstance = new ApplicationCore();
        private static ISessionFactory mIsessionFactory;

        public static ApplicationCore Instance
        {
            get { return mInstance; }
        }

        /// <summary>
        /// Gets and sets the nHibernate IsessionFactory object
        /// </summary>
        public ISessionFactory SessionFactory
        {
            get { return mIsessionFactory; }
            set { mIsessionFactory = value; }
        }
    }
}