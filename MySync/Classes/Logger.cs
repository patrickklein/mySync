
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using My_Sync.Properties;

namespace My_Sync.Classes
{
    static class Logger
    {
        private static StreamWriter file;

        private static void Initialize()
        {
            if (file == null && MySync.Default.logState)
            {
                string pfad = (MySync.Default.logPath != "") ? MySync.Default.logPath : ".\\";
                file = new StreamWriter(pfad.TrimEnd('/', '\\') + "\\Log.txt");
            }
        }

        public static void WriteHeader()
        {
            string className = new StackTrace().GetFrame(1).GetMethod().DeclaringType.Name;
            string methodName = new StackTrace().GetFrame(1).GetMethod().Name;

            string message = String.Format("--> '{0}.{1}'", className.Trim(), methodName.Trim());
            Log(message);
        }

        public static void WriteFooter()
        {
            string className = new StackTrace().GetFrame(1).GetMethod().DeclaringType.Name;
            string methodName = new StackTrace().GetFrame(1).GetMethod().Name;

            string message = String.Format("<-- '{0}.{1}'", className.Trim(), methodName.Trim());
            Log(message);
        }

        public static void Log(string message)
        {
            string finalMessage = String.Format("[{0:dd/MM/yyyy HH:mm:ss}]: {1}", DateTime.Now, message);

            if (MySync.Default.logState)
            {
                if (file == null) Initialize();
                file.WriteLine(finalMessage);
                file.Flush();
            }
        }
    }
}
