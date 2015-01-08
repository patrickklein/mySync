
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace My_Sync.Classes
{
    class Logger : IDisposable
    {
        private string className, methodName;
        private static StreamWriter file;

        /// <summary>
        /// Creates a Logfile in the path defined in the user settings and opens an active StreamWriter
        /// </summary>
        private void Initialize()
        {
            string path = (UserPreferences.logPath != "") ? UserPreferences.logPath : ".\\";
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("Defined log path not found. Please correct it.");
            }

            string fullPath = Path.Combine(path, "Log.txt");
            file = new StreamWriter(fullPath);
        }

        /// <summary>
        /// Creates a log entry for entering a method/function
        /// </summary>
        /// <param name="args">parameters of the called method/function</param>
        public Logger(params object [] args)
        {
            if (!UserPreferences.logState) return;
            if (file == null) Initialize();

            this.className = new StackTrace().GetFrame(1).GetMethod().DeclaringType.Name;
            this.methodName = new StackTrace().GetFrame(1).GetMethod().Name;

            string message = String.Format("--> '{0}.{1}'", this.className.Trim(), this.methodName.Trim());
            Log(message);

            int i = 0;
            foreach (var a in args)
            {
                Log(String.Format("\t\targ[{0}] = '{1}'", i, a));
                i++;
            }
        }

        /// <summary>
        /// Creates a log entry for exitting a method/function
        /// </summary>
        public void Dispose()
        {
            if (!UserPreferences.logState) return;
            string message = String.Format("<-- '{0}.{1}'", this.className.Trim(), this.methodName.Trim());
            Log(message);
        }

        /// <summary>
        /// Creates the log entry in the log file (double checks availability of the logfile and if logging is wanted)
        /// </summary>
        /// <param name="message">defines the message, which is going to be logged</param>
        public void Log(string message)
        {
            string finalMessage = String.Format("[{0:dd/MM/yyyy HH:mm:ss}]: {1}", DateTime.Now, message);

            if (file == null) Initialize();
            file.WriteLine(finalMessage);
            file.Flush();
        }
    }
}
