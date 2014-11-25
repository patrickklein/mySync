using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace My_Sync.Classes
{
    class Database
    {
        static string connectionString = "Data Source=mySync.db; Pooling=false; FailIfMissing=false;";
        private static SQLiteConnection dbConnection;
        private static string dbFileFilter = "FileFilter";
        private static string dbServerEntryPoint = "ServerEntryPoint";
        private static string dbHistory = "History";

        public static void CreateDatabase()
        {
            using (new Logger())
            {
                ConnectDB();

                SQLiteCommand cmd = dbConnection.CreateCommand();

                //Create ServerEntryPoint
                cmd.CommandText = String.Format(@"CREATE TABLE IF NOT EXISTS {0} (id integer primary key, description text, serverurl text, folderpath text, icon text);", dbServerEntryPoint);
                cmd.ExecuteNonQuery();

                //Create Filefilter
                cmd.CommandText = String.Format(@"CREATE TABLE IF NOT EXISTS {0} (id integer primary key, term text);", dbFileFilter);
                cmd.ExecuteNonQuery();

                //Create History
                cmd.CommandText = String.Format(@"CREATE TABLE IF NOT EXISTS {0} (id integer primary key, entry text, timestamp text);", dbHistory);
                cmd.ExecuteNonQuery();

                dbConnection.Close();
                dbConnection.Dispose();
            }
        }

        private static void ConnectDB()
        {
            using (new Logger())
            {
                if (dbConnection == null || dbConnection.State != ConnectionState.Open)
                {
                    dbConnection = new SQLiteConnection(connectionString);
                    dbConnection.Open();
                    dbConnection.Disposed += dbConnection_Disposed;
                }
            }
        }

        private static void dbConnection_Disposed(object sender, EventArgs e)
        {
            using (new Logger(sender, e))
            {
                dbConnection = null;
            }
        }

        #region File Filter

        public static void AddFileFilter(string filter)
        {
            using (new Logger(filter))
            {
                ConnectDB();

                SQLiteCommand cmd = dbConnection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = String.Format("INSERT INTO {0} (term) VALUES (@param);", dbFileFilter);
                cmd.Parameters.Add(new SQLiteParameter("@param", filter.Trim()));
                cmd.ExecuteNonQuery();

                dbConnection.Close();
                dbConnection.Dispose();
            }
        }

        public static void DeleteFileFilter(string filter)
        {
            using (new Logger(filter))
            {
                ConnectDB();

                SQLiteCommand cmd = dbConnection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = String.Format("DELETE FROM {0} WHERE term = @param;", dbFileFilter);
                cmd.Parameters.Add(new SQLiteParameter("@param", filter.Trim()));
                cmd.ExecuteNonQuery();

                dbConnection.Close();
                dbConnection.Dispose();
            }
        }

        public static List<string> GetFileFilters()
        {
            using (new Logger())
            {
                ConnectDB();
                List<string> filters = new List<string>();

                SQLiteCommand cmd = dbConnection.CreateCommand();
                cmd.CommandText = String.Format("SELECT term FROM {0} ORDER BY id ASC", dbFileFilter);

                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    filters.Add(reader.GetString(0));
                }

                dbConnection.Close();
                dbConnection.Dispose();

                return filters;
            }
        }

        #endregion

        #region History

        public static void AddHistory(string entry)
        {
            using (new Logger(entry))
            {
                ConnectDB();

                SQLiteCommand cmd = dbConnection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = String.Format("INSERT INTO {0} (entry, timestamp) VALUES (@param1, @param2);", dbHistory);
                cmd.Parameters.Add(new SQLiteParameter("@param1", entry.Trim()));
                cmd.Parameters.Add(new SQLiteParameter("@param2", String.Format("{0:yyyy/MM/dd HH:mm:ss}", DateTime.Now)));
                cmd.ExecuteNonQuery();

                dbConnection.Close();
                dbConnection.Dispose();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemsInList">defines how many items are shown in the history log</param>
        /// <returns></returns>
        public static List<string> GetHistory(int itemsInList = 100)
        {
            using (new Logger(itemsInList))
            {
                ConnectDB();
                List<string> entries = new List<string>();

                SQLiteCommand cmd = dbConnection.CreateCommand();
                cmd.CommandText = String.Format("SELECT entry, timestamp FROM {0} ORDER BY timestamp DESC", dbHistory);

                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string entry = String.Format("[{0:dd/MM/yyyy HH:mm}]: {1}", DateTime.ParseExact(reader.GetString(1), "yyyy/MM/dd HH:mm:ss", null), reader.GetString(0));
                    entries.Add(entry);
                }

                dbConnection.Close();
                dbConnection.Dispose();

                return entries;
            }
        }

        #endregion

        #region Server Entry Point

        public static void AddServerEntryPoint(SynchronizationPoint point)
        {
            using (new Logger(point))
            {
                ConnectDB();

                SQLiteCommand cmd = dbConnection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = String.Format("INSERT INTO {0} (description, serverurl, folderpath, icon) VALUES (@param1, @param2, @param3, @param4);", dbServerEntryPoint);
                cmd.Parameters.Add(new SQLiteParameter("@param1", point.Description.Trim()));
                cmd.Parameters.Add(new SQLiteParameter("@param2", point.Server.Trim()));
                cmd.Parameters.Add(new SQLiteParameter("@param3", point.Folder.Trim()));
                cmd.Parameters.Add(new SQLiteParameter("@param4", point.ServerType.Name));
                cmd.ExecuteNonQuery();

                dbConnection.Close();
                dbConnection.Dispose();
            }
        }

        public static void DeleteServerEntryPoint(SynchronizationPoint point)
        {
            using (new Logger(point))
            {
                ConnectDB();

                SQLiteCommand cmd = dbConnection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = String.Format("DELETE FROM {0} WHERE description = @param1 AND serverurl = @param2 AND folderpath = @param3 AND icon = @param4;", dbServerEntryPoint);
                cmd.Parameters.Add(new SQLiteParameter("@param1", point.Description.Trim()));
                cmd.Parameters.Add(new SQLiteParameter("@param2", point.Server.Trim()));
                cmd.Parameters.Add(new SQLiteParameter("@param3", point.Folder.Trim()));
                cmd.Parameters.Add(new SQLiteParameter("@param4", point.ServerType.Name));
                cmd.ExecuteNonQuery();

                dbConnection.Close();
                dbConnection.Dispose();
            }
        }

        public static List<SynchronizationPoint> GetServerEntryPoints()
        {
            using (new Logger())
            {
                ConnectDB();
                List<SynchronizationPoint> serverPoints = new List<SynchronizationPoint>();

                SQLiteCommand cmd = dbConnection.CreateCommand();
                cmd.CommandText = String.Format("SELECT description, serverurl, folderpath, icon FROM {0} ORDER BY id ASC", dbServerEntryPoint);

                SQLiteDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    SynchronizationPoint point = new SynchronizationPoint();
                    point.Description = reader.GetString(0);
                    point.Server = reader.GetString(1);
                    point.Folder = reader.GetString(2);
                    point.ServerType = Helper.GetImageOfAssembly(reader.GetString(3));
                    serverPoints.Add(point);
                }

                dbConnection.Close();
                dbConnection.Dispose();

                return serverPoints;
            }
        }

        #endregion
    }
}
