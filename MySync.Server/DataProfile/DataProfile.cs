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
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public Int64 Length { get; set; }
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
            //get params from form and for data saving
            this.File = Request.Files["uploadedFile"];
            this.CreationTime = Convert.ToDateTime(Request.Params["creationTime"]);
            this.LastWriteTime = Convert.ToDateTime(Request.Params["lastWriteTime"]);
            this.LastAccessTime = Convert.ToDateTime(Request.Params["lastAccessTime"]);
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