using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace My_Sync.Classes
{
    static class FolderIcon
    {
        [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
        static extern UInt32 SHGetSetFolderCustomSettings(ref FolderSettings pfcs, string path, UInt32 dwReadWrite);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct FolderSettings
        {
            public UInt32 dwSize;
            public UInt32 dwMask;
            public UInt32 dwFlags;

            public IntPtr pclsid;
            public IntPtr pvid;

            public string webViewTemplate;
            public UInt32 cchWebViewTemplate;
            public string webViewTemplateVersion;

            public string infoTip;
            public UInt32 cchInfoTip;

            public string icon;
            public UInt32 iconFile;
            public int iconIndex;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public static void SetFolderIcon(string path)
        {
            ChangeIcon(path, AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public static void ResetFolderIcon(string path)
        {
            ChangeIcon(path, @"%SystemRoot%\system32\ImageRes.dll", 3);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newIcon"></param>
        /// <param name="index"></param>
        private static void ChangeIcon(string path, string newIcon, int index = 0)
        {
            FolderSettings FolderSettings = new FolderSettings();
            FolderSettings.dwMask = 0x10;
            FolderSettings.icon = newIcon;
            FolderSettings.iconIndex = index;

            UInt32 FCS_READ = 0x00000001;
            UInt32 FCS_FORCEWRITE = 0x00000002;
            UInt32 FCS_WRITE = FCS_READ | FCS_FORCEWRITE;

            //delete desktop.ini
            string desktopIni = path.TrimEnd('/') + "//desktop.ini";
            if (File.Exists(desktopIni)) File.Delete(desktopIni);

            UInt32 HRESULT = SHGetSetFolderCustomSettings(ref FolderSettings, path, FCS_FORCEWRITE);
        }
    }
}
