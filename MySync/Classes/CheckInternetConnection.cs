using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Net;

namespace My_Sync.Classes
{
    static class CheckInternetConnection
    {
        /// <summary>
        /// Checks if an internet connection is available (try to get a response from google)
        /// </summary>
        /// <returns>returns true or false</returns>
        public static bool IsConnected()
        {
            using (new Logger())
            {
                Uri Url = new Uri("https://www.google.at");

                WebRequest WebReq;
                WebResponse Resp;
                WebReq = WebRequest.Create(Url);

                try
                {
                    Resp = WebReq.GetResponse();
                    Resp.Close();
                    WebReq = null;
                    return true;
                }
                catch
                {
                    WebReq = null;
                    return false;
                }
            }
        }
    }
}
