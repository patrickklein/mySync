using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Net;

namespace MySync.Classes
{
    static class CheckInternetConnection
    {
        public static bool IsConnected()
        {
            Logger.WriteHeader();

            Uri Url = new Uri("https://www.google.at");

            WebRequest WebReq;
            WebResponse Resp;
            WebReq = WebRequest.Create(Url);

            try
            {
                Resp = WebReq.GetResponse();
                Resp.Close();
                WebReq = null;
                //Logger.WriteFooter(true);
                return true;
            }
            catch
            {
                WebReq = null;
                //Logger.WriteFooter(false);
                return false;
            }
        }
    }
}
