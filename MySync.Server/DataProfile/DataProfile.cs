using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace MySync.Server.DataProfile
{
    public abstract class DataProfile
    {
        public string CreationTime { get; set; }
        public string LastWriteTime { get; set; }
        public string LastAccessTime { get; set; }
        public Int64 Length { get; set; }
        public int MaxRequestLength { get; set; }
        public string Directory { get; set; }
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
        /// Takes the server request and reads the file/folder attributes which are needed to progress the data contents
        /// </summary>
        /// <param name="Server">HttpServerUtilityBase from asp.net page</param>
        /// <param name="Request">HttpRequestBase from asp.net page</param>
        /// <param name="section">HttpRuntimeSection from asp.net page</param>
        public virtual void SetSection(HttpServerUtilityBase Server, HttpRequestBase Request, HttpRuntimeSection section)
        {
            //get params from form and for data saving
            this.File = Request.Files["uploadedFile"];
            this.CreationTime = Request.Params["creationTime"];
            this.LastWriteTime = Request.Params["lastWriteTime"];
            this.LastAccessTime = Request.Params["lastAccessTime"];
            this.Length = Convert.ToInt64(Request.Params["length"]);
            this.Directory = Request.Params["directory"];
            this.Section = section;
            this.Server = Server;
            this.MaxRequestLength = section.MaxRequestLength;
        }
    }
}