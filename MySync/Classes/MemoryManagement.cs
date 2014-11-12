using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace My_Sync.Classes
{
    class MemoryManagement
    {
        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, int dwMinimumWorkingSetSize, int dwMaximumWorkingSetSize);

        /// <summary>
        /// Forces the call of the garbage collector and frees used memory
        /// </summary>
        public static void Reduce()
        {
            using (new Logger())
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
            }
        }
    }
}
