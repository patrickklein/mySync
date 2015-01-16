using MySync.Server.Configuration;
using MySync.Server.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace MySync.Server.DataProfile
{
    public abstract class DataProfile
    {
        public bool IsFolder { get; set; }
        public char status { get; set; }
        public string CreationTime { get; set; }
        public string LastWriteTime { get; set; }
        public string LastAccessTime { get; set; }
        public string LastSyncTime { get; set; }
        public Int64 Length { get; set; }
        public Int64 MaxFileSize { get; set; }
        public Int64 MaxDiskSpace { get; set; }
        public int Files { get; set; }
        public int Folders { get; set; }
        public int MaxRequestLength { get; set; }
        public string Directory { get; set; }
        public string RootFolder { get; set; }
        public string SyncRootFolder { get; set; }
        public string FromRootToFolder { get; set; }
        public string Filename { get; set; }
        public string Extension { get; set; }
        public string FullName { get; set; }
        public string FullPath { get; set; }
        public HttpPostedFileBase File { get; set; }
        public HttpRuntimeSection Section { get; set; }
        public HttpServerUtilityBase Server { get; set; }

        //----------------------------------------------------------------------------------------//

        /// <summary>
        /// Method for saving the file to the defined system
        /// </summary>
        public virtual void SaveFile()
        {
        }

        /// <summary>
        /// Method for deleting all files
        /// </summary>
        public virtual void DeleteAll()
        {
        }

        /// <summary>
        /// Takes the server request and reads the file/folder attributes which are needed to progress the data contents
        /// </summary>
        /// <param name="Server">HttpServerUtilityBase from asp.net page</param>
        /// <param name="Request">HttpRequestBase from asp.net page</param>
        /// <param name="section">HttpRuntimeSection from asp.net page</param>
        public virtual void SetSection(HttpServerUtilityBase Server, HttpRequestBase Request, HttpRuntimeSection section)
        {
            //get params from form (database)
            ConfigurationService configService = new ConfigurationService();
            configService.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());
            DAL.Configuration config = configService.Get("maxFileSize");
            this.MaxFileSize = (config != null) ? Convert.ToInt64(config.Value) * 1024 * 1000 : 0;

            config = configService.Get("maxDiskSpace");
            this.MaxDiskSpace = (config != null) ? Convert.ToInt64(config.Value) * 1024 * 1000 : 0;

            //get params from request for data saving
            this.File = Request.Files["uploadedFile"];
            this.CreationTime = Request.Params["creationTime"];
            this.LastWriteTime = Request.Params["lastWriteTime"];
            this.LastAccessTime = Request.Params["lastAccessTime"];
            this.Length = Convert.ToInt64(Request.Params["length"]);
            this.Files = Convert.ToInt32(Request.Params["files"]);
            this.Folders = Convert.ToInt32(Request.Params["folders"]);
            this.status = (Request.Params["status"] == null) ? ' ' : Convert.ToChar(Request.Params["status"]);
            this.Directory = Request.Params["directory"];
            this.RootFolder = Request.Params["rootFolder"];
            this.Extension = Request.Params["extension"];
            this.FullName = Request.Params["fullName"];
            this.FullPath = Request.Params["fullPath"];
            this.SyncRootFolder = Request.Params["syncRoot"];
            this.FromRootToFolder = Request.Params["fromRootToFolder"];
            this.IsFolder = Convert.ToBoolean(Request.Params["folderFlag"]);
            this.Section = section;
            this.Server = Server;
            this.MaxRequestLength = section.MaxRequestLength;
            this.Filename = (File == null) ? "" : HttpUtility.UrlDecode(File.FileName);
        }
    }
}