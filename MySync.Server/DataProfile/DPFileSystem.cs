using MySync.Server.Configuration;
using MySync.Server.DAL;
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
        private string savingPath = "~/App_Data";
        private string timeFormat = "yyyy/MM/dd HH:mm:ss";

        /// <summary>
        /// Method for saving the file to the file system
        /// </summary>
        public override void SaveFile()
        {
            using (new Logger())
            {
                string fullPath = null;
                string relativePath = Path.Combine(SyncRootFolder, FromRootToFolder);

                //if it is a file
                if (!IsFolder && File != null && !string.IsNullOrEmpty(File.FileName))
                {
                    SynchronisationItemService syncItemservice = new SynchronisationItemService();
                    syncItemservice.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());
                    long sizeOnDisk = syncItemservice.GetDiskSize(relativePath) + File.ContentLength;

                    if (File.ContentLength > MaxFileSize) throw new Exception("maxFile - " + MaxFileSize.ToString());
                    if (sizeOnDisk > MaxDiskSpace) throw new Exception("maxDisk - " + MaxDiskSpace.ToString());

                    string fileContentType = File.ContentType;
                    byte[] fileBytes = new byte[File.ContentLength];

                    //Create directory if not exists
                    new DirectoryInfo(Path.Combine(Server.MapPath(savingPath), SyncRootFolder, FromRootToFolder)).Create();
                    fullPath = Path.Combine(Server.MapPath(savingPath), SyncRootFolder, FromRootToFolder, Filename);
                    File.SaveAs(fullPath);

                    System.IO.File.SetLastAccessTime(fullPath, Convert.ToDateTime(LastAccessTime));
                    System.IO.File.SetLastWriteTime(fullPath, Convert.ToDateTime(LastWriteTime));
                    System.IO.File.SetCreationTime(fullPath, Convert.ToDateTime(CreationTime));
                }

                //if it is a directory
                if (IsFolder)
                {
                    //Create directory if not exists
                    fullPath = Path.Combine(Server.MapPath(savingPath), SyncRootFolder, FromRootToFolder, Directory);

                    DirectoryInfo dirInfo = new DirectoryInfo(fullPath);
                    dirInfo.Create();
                    dirInfo.Attributes = FileAttributes.Normal;

                    //System.IO.Directory.SetLastAccessTime(path, Convert.ToDateTime(LastAccessTime));
                    //System.IO.Directory.SetLastWriteTime(path, Convert.ToDateTime(LastAccessTime));
                    //System.IO.Directory.SetCreationTime(path, Convert.ToDateTime(LastAccessTime));
                }

                //Add/update file/folder values to database
                UpdateItemDB(fullPath, relativePath);
            }
        }

        /// <summary>
        /// Method for deleting all files
        /// </summary>
        public override void DeleteAll() 
        {
            using (new Logger())
            {
                var path = Path.Combine(Server.MapPath(savingPath), Directory, FullName);

                try
                {
                    if (new DirectoryInfo(path).Exists)
                    {
                        //DirectoryInfo dirInfo = new DirectoryInfo(path);
                        //dirInfo.Attributes = FileAttributes.Normal;
                        System.IO.Directory.Delete(path, true);
                    }

                    if (new FileInfo(path).Exists)
                    {
                        //FileInfo fileInfo = new FileInfo(path);
                        //fileInfo.Attributes = FileAttributes.Normal;
                        System.IO.File.Delete(path);
                    }
                }
                catch (Exception)
                {
                }

                //delete file/folder values from database
                DeleteItemDB(path);
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

        
        ///////////////////////////////////////// DB Functions /////////////////////////////////////////

        /// <summary>
        /// Add file/folder values to database
        /// </summary>
        /// <param name="path">path of the file/folder on the server</param>
        /// <param name="relativePath">relative path of the file/folder on the server (path from the mySync root)</param>
        private void AddItemDB(string path, string relativePath)
        {
            using (new Logger(path, relativePath))
            {
                LastSyncTime = DateTime.Now.ToString(timeFormat);

                DAL.SynchronisationItem item = new SynchronisationItem();
                item.Fullname = FullName;
                item.Extension = (String.IsNullOrEmpty(Extension)) ? null : Extension;
                item.Size = Length;
                item.IsFolder = IsFolder;
                item.Path = path;
                item.RelativePath = relativePath;
                item.LastWriteTime = LastWriteTime.ToString();
                item.LastSyncTime = LastSyncTime.ToString();
                item.LastAccessTime = LastAccessTime.ToString();
                item.CreationTime = CreationTime.ToString();

                if (IsFolder) item.Name = FullName;
                if (!IsFolder) item.Name = (String.IsNullOrEmpty(Extension)) ? Filename : Filename.Replace(Extension, "");

                SynchronisationItemService syncItemservice = new SynchronisationItemService();
                syncItemservice.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());
                syncItemservice.Add(item);
            }
        }

        /// <summary>
        /// Update file/folder values in the database
        /// </summary>
        /// <param name="path">path of the file/folder on the server</param>
        /// <param name="relativePath">path of the file/folder on the server (from the sync root to the file/folder directory)</param>
        private void UpdateItemDB(string path, string relativePath)
        {
            using (new Logger(path, relativePath))
            {
                SynchronisationItemService syncItemservice = new SynchronisationItemService();
                syncItemservice.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());
                SynchronisationItem item = syncItemservice.Get(path);

                if (item == null) AddItemDB(path, relativePath);
                else
                {
                    LastSyncTime = DateTime.Now.ToString(timeFormat);
                    item.Fullname = FullName;
                    item.Extension = (String.IsNullOrEmpty(Extension)) ? null : Extension;
                    item.Size = Length;
                    item.IsFolder = IsFolder;
                    item.Path = path;
                    item.RelativePath = relativePath;
                    item.LastWriteTime = LastWriteTime.ToString();
                    item.LastSyncTime = LastSyncTime.ToString();
                    item.LastAccessTime = LastAccessTime.ToString();
                    item.CreationTime = CreationTime.ToString();

                    if (IsFolder) item.Name = FullName;
                    if (!IsFolder) item.Name = (String.IsNullOrEmpty(Extension)) ? Filename : Filename.Replace(Extension, "");

                    syncItemservice.Update(item);
                }
            }
        }

        /// <summary>
        /// Delete file/folder values from database
        /// </summary>
        /// <param name="path">path of the file/folder on the server</param>
        private void DeleteItemDB(string path)
        {
            using (new Logger(path))
            {
                SynchronisationItemService syncItemservice = new SynchronisationItemService();
                syncItemservice.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());
                syncItemservice.Delete(path);
            }
        }
    }
}