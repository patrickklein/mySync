using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace My_Sync.Classes
{
    [Serializable]
    public class UserPreferences
    {
        public static bool logState = false;
        public static bool runAtStartup = false;
        public static bool showNotification = false;
        public static bool addToFavorites = false;
        public static bool fastSync = false;
        public static string usedLanguage = "";
        public static string mainFolder = "";
        public static string synchronizationInterval = "";
        public static string logPath = "";

        #region Getter / Setter

        public bool RunAtStartup
        {
            get { return UserPreferences.runAtStartup; }
            set { UserPreferences.runAtStartup = value; }
        }

        public bool ShowNotification
        {
            get { return UserPreferences.showNotification; }
            set { UserPreferences.showNotification = value; }
        }

        public bool AddToFavorites
        {
            get { return UserPreferences.addToFavorites; }
            set { UserPreferences.addToFavorites = value; }
        }
        
        public bool FastSync
        {
            get { return UserPreferences.fastSync; }
            set { UserPreferences.fastSync = value; }
        }

        public string UsedLanguage
        {
            get { return UserPreferences.usedLanguage; }
            set { UserPreferences.usedLanguage = value; }
        }

        public string MainFolder
        {
            get { return UserPreferences.mainFolder; }
            set { UserPreferences.mainFolder = value; }
        }

        public string SynchronizationInterval
        {
            get { return UserPreferences.synchronizationInterval; }
            set { UserPreferences.synchronizationInterval = value; }
        }

        public string LogPath
        {
            get { return UserPreferences.logPath; }
            set { UserPreferences.logPath = value; }
        }

        public bool LogState
        {
            get { return UserPreferences.logState; }
            set { UserPreferences.logState = value; }
        }
        
        #endregion

        /// <summary>
        /// Serializes the current UserPreferences object to the configuration file
        /// </summary>
        public static void Save()
        {
            using (new Logger())
            {
                string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configuration.xml");

                //Fill the parameters with the configuration values (cannot serialize static objects)
                UserPreferences preferences = new UserPreferences();
                Type type = preferences.GetType();
                MethodInfo[] methods = type.GetMethods();
                FieldInfo[] parameters = type.GetFields();
                TextInfo info = new CultureInfo("en-US", false).TextInfo;
                object instance = Activator.CreateInstance(type);

                foreach (FieldInfo parameter in parameters)
                {
                    MethodInfo method = methods.SingleOrDefault(x => x.Name.ToLower().Equals("get_" + parameter.Name.ToLower()));
                    var value = method.Invoke(instance, null);

                    string name = parameter.Name[0].ToString().ToUpper() + parameter.Name.Substring(1, parameter.Name.Length - 1);
                    preferences.GetType().GetProperty(name).SetValue(preferences, value);
                }

                XmlSerializer xs = new XmlSerializer(preferences.GetType());
                StreamWriter writer = File.CreateText(file);
                xs.Serialize(writer, preferences);
                writer.Flush();
                writer.Close();
            }
        }

        /// <summary>
        /// Deserialize a xml file to the current UserPreferences object 
        /// </summary>
        public static void Load()
        {
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configuration.xml");
            
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(UserPreferences));
                StreamReader reader = File.OpenText(file);
                xs.Deserialize(reader);
                reader.Close();
            }
            catch (Exception) { }
        }
    }
}
