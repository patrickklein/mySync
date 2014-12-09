using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace MySync.Server.DataProfile
{
    //this class is just for appreciation. if you want some functionality, you have to implement them by your own
    public class DPMicrosoftSharepoint : DataProfile, IDataProfile
    {
        /// <summary>
        /// Method for saving the file to the file system
        /// </summary>
        public override void SaveFile() { }

        /// <summary>
        /// Takes the server request and reads the file/folder attributes which are needed to progress the data contents
        /// </summary>
        /// <param name="Server">HttpServerUtilityBase from asp.net page</param>
        /// <param name="Request">HttpRequestBase from asp.net page</param>
        /// <param name="section">HttpRuntimeSection from asp.net page</param>
        public override void SetSection(HttpServerUtilityBase Server, HttpRequestBase Request, HttpRuntimeSection Section) { }
    }
}