using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Configuration;

namespace MySync.Server.DataProfile
{
    public class DPFileSystem : DataProfile,IDataProfile
    {
        /// <summary>
        /// Method for saving the file to the file system
        /// </summary>
        public override void SaveFile() 
        {
            //if it is a file
            if (!IsFolder && File != null && File.ContentLength < MaxRequestLength && !string.IsNullOrEmpty(File.FileName))
            {
                string fileContentType = File.ContentType;
                byte[] fileBytes = new byte[File.ContentLength];

                //Create directory if not exists
                new DirectoryInfo(Path.Combine(Server.MapPath("~/App_Data"), SyncRootFolder, FromRootToFolder)).Create();
                var path = Path.Combine(Server.MapPath("~/App_Data"), SyncRootFolder, FromRootToFolder, Filename);
                File.SaveAs(path);

                System.IO.File.SetLastAccessTime(path, LastAccessTime);
                System.IO.File.SetLastWriteTime(path, LastWriteTime);
                System.IO.File.SetCreationTime(path, CreationTime);
            }

            //if it is a directory
            if(IsFolder) 
            {
                //Create directory if not exists
                var path = Path.Combine(Server.MapPath("~/App_Data"), SyncRootFolder, FromRootToFolder, Directory);

                DirectoryInfo dirInfo = new DirectoryInfo(path);
                dirInfo.Create();
                dirInfo.Attributes = FileAttributes.Normal;

                //System.IO.Directory.SetLastAccessTime(path, LastAccessTime);
                //System.IO.Directory.SetLastWriteTime(path, LastAccessTime);
                //System.IO.Directory.SetCreationTime(path, LastAccessTime);
            }
        }

        /// <summary>
        /// Method for deleting all files
        /// </summary>
        public override void DeleteAll() 
        {
            var path = Path.Combine(Server.MapPath("~/App_Data"), Directory, FullName);

            if (!String.IsNullOrEmpty(FullName))
            {
                FileInfo fileInfo = new FileInfo(path);
                fileInfo.Attributes = FileAttributes.Normal;
                System.IO.File.Delete(path);
            }
            else
            {
                DirectoryInfo dirInfo = new DirectoryInfo(path);
                dirInfo.Attributes = FileAttributes.Normal;
                System.IO.Directory.Delete(path, true);
            }
        }

        /// <summary>
        /// Takes the server request and reads the file/folder attributes which are needed to progress the data contents
        /// (just calling the base method)
        /// </summary>
        /// <param name="Server">HttpServerUtilityBase from asp.net page</param>
        /// <param name="Request">HttpRequestBase from asp.net page</param>
        /// <param name="section">HttpRuntimeSection from asp.net page</param>
        public override void SetSection(HttpServerUtilityBase Server, HttpRequestBase Request, HttpRuntimeSection Section) 
        {
            base.SetSection(Server, Request, Section);
        }
    }
}