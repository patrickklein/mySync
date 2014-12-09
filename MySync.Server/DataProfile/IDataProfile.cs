using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace MySync.Server.DataProfile
{
    interface IDataProfile
    {
        /// <summary>
        /// Method for saving the file to the defined system
        /// </summary>
        void SaveFile();

        /// <summary>
        /// Takes the server request and reads the file/folder attributes which are needed to progress the data contents
        /// </summary>
        /// <param name="Server">HttpServerUtilityBase from asp.net page</param>
        /// <param name="Request">HttpRequestBase from asp.net page</param>
        /// <param name="section">HttpRuntimeSection from asp.net page</param>
        void SetSection(HttpServerUtilityBase Server, HttpRequestBase Request, HttpRuntimeSection Section);
        
    }
}
