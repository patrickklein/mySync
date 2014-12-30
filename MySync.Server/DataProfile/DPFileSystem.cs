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
        /// <summary>
        /// Method for saving the file to the file system
        /// </summary>
        public override void SaveFile() 
        {
            string path = null;

            //if it is a file
            if (!IsFolder && File != null && File.ContentLength < MaxRequestLength && !string.IsNullOrEmpty(File.FileName))
            {
                string fileContentType = File.ContentType;
                byte[] fileBytes = new byte[File.ContentLength];

                //Create directory if not exists
                new DirectoryInfo(Path.Combine(Server.MapPath("~/App_Data"), SyncRootFolder, FromRootToFolder)).Create();
                path = Path.Combine(Server.MapPath("~/App_Data"), SyncRootFolder, FromRootToFolder, Filename);
                File.SaveAs(path);

                System.IO.File.SetLastAccessTime(path, LastAccessTime);
                System.IO.File.SetLastWriteTime(path, LastWriteTime);
                System.IO.File.SetCreationTime(path, CreationTime);
            }

            //if it is a directory
            if(IsFolder) 
            {
                //Create directory if not exists
                path = Path.Combine(Server.MapPath("~/App_Data"), SyncRootFolder, FromRootToFolder, Directory);

                DirectoryInfo dirInfo = new DirectoryInfo(path);
                dirInfo.Create();
                dirInfo.Attributes = FileAttributes.Normal;

                //System.IO.Directory.SetLastAccessTime(path, LastAccessTime);
                //System.IO.Directory.SetLastWriteTime(path, LastAccessTime);
                //System.IO.Directory.SetCreationTime(path, LastAccessTime);
            }

            //Add/update file/folder values to database
            UpdateItemDB(path);
        }

        /// <summary>
        /// Method for deleting all files
        /// </summary>
        public override void DeleteAll() 
        {
            var path = Path.Combine(Server.MapPath("~/App_Data"), Directory, FullName);

            try
            {
                if (!String.IsNullOrEmpty(FullName))
                {
                    //FileInfo fileInfo = new FileInfo(path);
                    //fileInfo.Attributes = FileAttributes.Normal;
                    System.IO.File.Delete(path);
                }
                else
                {
                    //DirectoryInfo dirInfo = new DirectoryInfo(path);
                    //dirInfo.Attributes = FileAttributes.Normal;
                    System.IO.Directory.Delete(path, true);
                }
            }
            catch (Exception)
            {
            }

            //delete file/folder values from database
            DeleteItemDB(path);
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
        private void AddItemDB(string path)
        {
            SynchronisationItemService syncItemservice = new SynchronisationItemService();
            syncItemservice.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());

            DAL.SynchronisationItem item = new SynchronisationItem();
            item.Name = Filename;
            item.Fullname = FullName;
            item.Extension = Extension;
            item.Size = Length;
            item.Files = Files;
            item.Folders = Folders;
            item.FolderFlag = IsFolder;
            item.Path = path;
            item.LastWriteTime = LastWriteTime.ToString();
            item.LastSyncTime = DateTime.Now.ToString();
            item.LastAccessTime = LastAccessTime.ToString();
            item.CreationTime = CreationTime.ToString();
            //item.HiddenFlag = false;
            //item.SystemFlag = false;
            syncItemservice.Add(item);
        }

        /// <summary>
        /// Update file/folder values in the database
        /// </summary>
        /// <param name="path">path of the file/folder on the server</param>
        private void UpdateItemDB(string path)
        {
            SynchronisationItemService syncItemservice = new SynchronisationItemService();
            syncItemservice.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());

            SynchronisationItem item = syncItemservice.Get(path);
            if (item == null) AddItemDB(path);
            else
            {
                item.Name = Filename;
                item.Fullname = FullName;
                item.Extension = Extension;
                item.Size = Length;
                item.Files = Files;
                item.Folders = Folders;
                item.FolderFlag = IsFolder;
                item.Path = path;
                item.LastWriteTime = LastWriteTime.ToString();
                item.LastSyncTime = DateTime.Now.ToString();
                item.LastAccessTime = LastAccessTime.ToString();
                item.CreationTime = CreationTime.ToString();
                //item.HiddenFlag = false;
                //item.SystemFlag = false;
                syncItemservice.Update(item);
            }
        }

        /// <summary>
        /// Delete file/folder values from database
        /// </summary>
        /// <param name="path">path of the file/folder on the server</param>
        private void DeleteItemDB(string path)
        {
            SynchronisationItemService syncItemservice = new SynchronisationItemService();
            syncItemservice.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());
            syncItemservice.Delete(path);
        }
    }
}