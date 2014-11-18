using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace My_Sync.Classes
{
    class SyncItemInfo
    {
        private string filename;

        private string directory;
        private string extension;
        private DateTime creationTime;
        private DateTime lastAccessTime;
        private DateTime lastWriteTime;
        private long size;

        private long files;
        private long folders;

        #region Getter / Setter

        public string Filename
        {
            get { return filename; }
            set { filename = value; }
        }
        
        public string Directory
        {
            get { return directory; }
            set { directory = value; }
        }

        public string Extension
        {
            get { return extension; }
            set { extension = value; }
        }

        public DateTime CreationTime
        {
            get { return creationTime; }
            set { creationTime = value; }
        }

        public DateTime LastAccessTime
        {
            get { return lastAccessTime; }
            set { lastAccessTime = value; }
        }

        public DateTime LastWriteTime
        {
            get { return lastWriteTime; }
            set { lastWriteTime = value; }
        }

        public long Size
        {
            get { return size; }
            set { size = value; }
        }

        public long Files
        {
            get { return files; }
            set { files = value; }
        }

        public long Folders
        {
            get { return folders; }
            set { folders = value; }
        }

        #endregion

        /// <summary>
        /// Gets all needed file attributes for the given file
        /// </summary>
        /// <param name="filename">file for gathering the file attributes</param>
        public void GetFileInfo(string filename)
        {
            using (new Logger(filename))
            {
                // Get Attributes for file
                FileInfo info = new FileInfo(filename);
                this.size = info.Length;
                this.lastAccessTime = info.LastAccessTime;
                this.lastWriteTime = info.LastWriteTime;
                this.creationTime = info.CreationTime;
                this.filename = info.Name;
                this.directory = info.DirectoryName;
                this.extension = info.Extension;
            }
        }

        /// <summary>
        /// Gets all needed directory attributes for the given path
        /// </summary>
        /// <param name="path">path or gathering the directory attributes</param>
        public void GetDirectoryInfo(string path)
        {
            using (new Logger(path))
            {
                // Get Attributes for directory
                DirectoryInfo info = new DirectoryInfo(path);
                this.size = info.GetFiles("*.*", SearchOption.AllDirectories).Sum(file => file.Length);
                this.lastAccessTime = info.LastAccessTime;
                this.lastWriteTime = info.LastWriteTime;
                this.creationTime = info.CreationTime;
                this.directory = info.Name;

                this.files = info.GetFiles("*.*", SearchOption.AllDirectories).Count();
                this.folders = info.GetDirectories("*", SearchOption.AllDirectories).Count();
            }
        }
    }
}
