using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            if ((File != null) && (File.ContentLength > 0) && (File.ContentLength < MaxRequestLength) && !string.IsNullOrEmpty(File.FileName))
            {
                string fileName = File.FileName;
                string fileContentType = File.ContentType;
                byte[] fileBytes = new byte[File.ContentLength];
                var path = Path.Combine(Server.MapPath("~/App_Data"), fileName);
                File.SaveAs(path);
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