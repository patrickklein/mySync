using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DotNetOpenAuth.AspNet;
using Microsoft.Web.WebPages.OAuth;
using WebMatrix.WebData;
using MySync.Server.Filters;
using MySync.Server.Models;
using System.IO;
using System.Web.Configuration;
using System.Configuration;
using MySync.Server.DataProfile;
using System.Collections.Specialized;
using System.Reflection;
using MySync.Server.DAL;
using MySync.Server.Configuration;
using System.Net;

namespace MySync.Server.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class AccountController : Controller
    {
        ///
        /// GET: /Account/Setup
        /// 
        /// <summary>
        /// Setup page for configuring the server settings
        /// </summary>
        /// <param name="returnUrl">URI to return to</param>
        /// <returns>view to show</returns>
        [AllowAnonymous]
        public ActionResult Setup(string returnUrl)
        {
            using (new Logger(returnUrl))
            {
                // existing DataProfile Classes from web.config file
                NameValueCollection appSettings = ConfigurationManager.AppSettings;
                Dictionary<string, string> dpClasses = new Dictionary<string, string>();
                dpClasses.Add("", "");
                List<string> dpKeys = appSettings.AllKeys.Where(x => x.StartsWith("DP")).ToList();
                foreach (string key in dpKeys) dpClasses.Add(key, appSettings.Get(key));
                ViewBag.DPClasses = dpClasses;

                HttpRuntimeSection section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
                ViewBag.MaxFileSize = section.MaxRequestLength / 1024 / 1000;
                ViewBag.ReturnUrl = returnUrl;

                //Get values from database
                SetupModel model = new SetupModel();
                ConfigurationService configService = new ConfigurationService();
                configService.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());

                DAL.Configuration config = configService.Get("dataSavingPoint");
                model.DataProfile = (config != null) ? config.Value : "";

                config = configService.Get("maxFileSize");
                model.FileSize = (config != null) ? Convert.ToInt32(config.Value) : 0;

                config = configService.Get("maxDiskSpace");
                model.DiskSpace = (config != null) ? Convert.ToInt32(config.Value) : 0;

                //get synchronization url for setup page
                string ipHost = Dns.GetHostName();
                string ip = Dns.GetHostByName(ipHost).AddressList[0].ToString();
                int port = Request.Url.Port;
                ViewBag.SyncURL = String.Format("http://{0}:{1}{2}", ip, port, Request.Url.LocalPath.Replace("Setup", "Upload"));

                return View(model);
            }
        }

        ///
        /// POST: /Account/Setup
        /// <summary>
        /// Setup page for checking and saving the server settings
        /// </summary>
        /// <param name="model">current data model</param>
        /// <param name="returnUrl">URI to return to</param>
        /// <returns>current view to show</returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Setup(SetupModel model, string returnUrl)
        {
            using (new Logger(model, returnUrl))
            {
                //Get values from database
                ConfigurationService configService = new ConfigurationService();
                configService.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());

                DAL.Configuration config = configService.Get("dataSavingPoint");
                ViewBag.SavedDataSavingPoint = (config != null) ? config.Value : "";

                config = configService.Get("maxFileSize");
                ViewBag.SavedMaxFileSize = (config != null) ? config.Value : "";

                config = configService.Get("maxDiskSpace");
                ViewBag.SavedMaxDiskSpace = (config != null) ? config.Value : "";

                HttpRuntimeSection section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
                int maxFilesize = section.MaxRequestLength / 1024 / 1000;
                ViewBag.MaxFileSize = maxFilesize;

                // existing DataProfile Classes from web.config file
                NameValueCollection appSettings = ConfigurationManager.AppSettings;
                Dictionary<string, string> dpClasses = new Dictionary<string, string>();
                dpClasses.Add("", "");
                List<string> dpKeys = appSettings.AllKeys.Where(x => x.StartsWith("DP")).ToList();
                foreach (string key in dpKeys) dpClasses.Add(key, appSettings.Get(key));
                ViewBag.DPClasses = dpClasses;

                //check all given values from the view
                if (model.FileSize >= 0 && model.FileSize <= maxFilesize && model.DiskSpace >= 0 && model.DataProfile != "")
                {
                    //Save data to database
                    configService.Update(new DAL.Configuration() { Field = "dataSavingPoint", Value = model.DataProfile });
                    configService.Update(new DAL.Configuration() { Field = "maxFileSize", Value = model.FileSize.ToString() });
                    configService.Update(new DAL.Configuration() { Field = "maxDiskSpace", Value = model.DiskSpace.ToString() });
                }

                //get synchronization url for setup page
                string ipHost = Dns.GetHostName();
                string ip = Dns.GetHostByName(ipHost).AddressList[0].ToString();
                int port = Request.Url.Port;
                ViewBag.SyncURL = String.Format("http://{0}:{1}/Account/Upload", ip, port);

                return View(model);
            }
        }

        ///
        /// POST: /Account/Upload
        /// 
        /// <summary>
        /// Page for uploading a file/folder from the client to the server
        /// </summary>
        /// <param name="formCollection">current collection of the form</param>
        /// <returns>current view to show</returns>
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Upload(FormCollection formCollection)
        {
            using (new Logger(formCollection))
            {
                if (Request != null)
                {
                    try {
                        HttpRuntimeSection section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;

                        ConfigurationService configService = new ConfigurationService();
                        configService.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());

                        DAL.Configuration config = configService.Get("dataSavingPoint");
                        string className = (config != null) ? config.Value : "";
                        DataProfile.DataProfile newContent = (DataProfile.DataProfile)Activator.CreateInstance(Assembly.GetExecutingAssembly().GetType("MySync.Server.DataProfile." + className, true, true));
                        newContent.SetSection(Server, Request, section);

                        string error = "";
                        try { newContent.SaveFile(); }
                        catch (Exception ex) 
                        { 
                            error = ex.Message;
                            string message = String.Format("Filename: {0}, Path: {1}, Error: {2}", newContent.FullName, newContent.FromRootToFolder, error);
                            new Logger().Log(message);
                        }

                        //Response back to client
                        Response.Clear();
                        Response.AppendHeader("Filename", newContent.FullName);
                        Response.AppendHeader("Path", newContent.FromRootToFolder);
                        Response.AppendHeader("LastSyncTime", newContent.LastSyncTime);
                        Response.AppendHeader("Error", error);
                        Response.Flush();
                        Response.End();
                    }
                    catch (Exception ex)
                    {
                        new Logger().Log("Error: " + ex.Message.ToString());
                    }
                }

                return View();
            }
        }

        ///
        /// POST: /Account/Delete
        /// 
        /// <summary>
        /// Page for deleting a file/folder on the server
        /// </summary>
        /// <param name="formCollection">current collection of the form</param>
        /// <returns>current view to show</returns>
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Delete(FormCollection formCollection)
        {
            using (new Logger(formCollection))
            {
                if (Request != null)
                {
                    try {
                        HttpRuntimeSection section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;

                        ConfigurationService configService = new ConfigurationService();
                        configService.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());

                        DAL.Configuration config = configService.Get("dataSavingPoint");
                        string className = (config != null) ? config.Value : "";
                        DataProfile.DataProfile newContent = (DataProfile.DataProfile)Activator.CreateInstance(Assembly.GetExecutingAssembly().GetType("MySync.Server.DataProfile." + className, true, true));
                        newContent.SetSection(Server, Request, section);

                        string error = "";
                        try { newContent.DeleteAll(); }
                        catch (Exception ex)
                        {
                            error = ex.Message;
                            string message = String.Format("Filename: {0}, Path: {1}, Error: {2}", newContent.FullName, newContent.FromRootToFolder, error);
                            new Logger().Log(message);
                        }

                        //Response back to client
                        string path = (newContent.FromRootToFolder == null) ? "" : newContent.FromRootToFolder;
                        Response.Clear();
                        Response.AppendHeader("Filename", HttpUtility.UrlEncode(newContent.FullName));
                        Response.AppendHeader("Path", HttpUtility.UrlEncode(path));
                        Response.AppendHeader("Error", error);
                        Response.Flush();
                        Response.End();
                    }
                    catch (Exception ex)
                    {
                        new Logger().Log("Error: " + ex.Message.ToString());
                    }
                }

                return View();
            }
        }

        ///
        /// POST: /Account/GetList
        /// 
        /// <summary>
        /// Page for gathering a list of files and folders existing in the server database
        /// </summary>
        /// <param name="formCollection">current collection of the form</param>
        /// <returns>current view to show</returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult GetList(FormCollection formCollection)
        {
            using (new Logger(formCollection))
            {
                try
                {
                    int i = 0;
                    SynchronisationItemService syncItemservice = new SynchronisationItemService();
                    syncItemservice.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());

                    List<SynchronisationItem> list = syncItemservice.GetAll<SynchronisationItem>().ToList();
                    int bulkSize = Convert.ToInt32(Request.Params["bulkSize"]);
                    int startAt = Convert.ToInt32(Request.Params["startAt"]);

                    Response.Clear();

                    for (i = startAt; i < list.Count; i++)
                    {
                        Response.AppendHeader("Fullname" + i.ToString(), HttpUtility.UrlEncode(list[i].Fullname));
                        Response.AppendHeader("Name" + i.ToString(), HttpUtility.UrlEncode(list[i].Name));
                        Response.AppendHeader("CreationTime" + i.ToString(), list[i].CreationTime);
                        Response.AppendHeader("LastAccessTime" + i.ToString(), list[i].LastAccessTime);
                        Response.AppendHeader("LastWriteTime" + i.ToString(), list[i].LastWriteTime);
                        Response.AppendHeader("LastSyncTime" + i.ToString(), list[i].LastSyncTime);
                        Response.AppendHeader("Path" + i.ToString(), HttpUtility.UrlEncode(list[i].RelativePath));
                        Response.AppendHeader("Extension" + i.ToString(), (String.IsNullOrEmpty(list[i].Extension)) ? "" : list[i].Extension);
                        Response.AppendHeader("Size" + i.ToString(), list[i].Size.ToString());
                        Response.AppendHeader("IsFolder" + i.ToString(), Convert.ToDecimal(list[i].IsFolder).ToString());

                        if (i == startAt + bulkSize) break;
                    }
                    Response.Flush();
                    Response.End();
                }
                catch (Exception ex)
                {
                    new Logger().Log("Error: " + ex.Message.ToString());
                }

                return View();
            }
        }

        ///
        /// POST: /Account/Download
        /// 
        /// <summary>
        /// Page for downloadinf a file from the server to the client
        /// </summary>
        /// <param name="formCollection">current collection of the form</param>
        /// <returns>current view to show</returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Download(FormCollection formCollection)
        {
            using (new Logger(formCollection))
            {
                SynchronisationItemService syncItemservice = new SynchronisationItemService();
                syncItemservice.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());

                SynchronisationItem item = syncItemservice.Get(Request.Params["directory"], Request.Params["fullName"]);

                Response.Clear();

                if (item != null)
                {
                    if (!item.IsFolder && new FileInfo(item.Path).Exists)
                    {
                        Response.AddHeader("Content-Disposition", "attachment; filename=" + item.Fullname);
                        Response.ContentType = "application/octet-stream";
                        Response.WriteFile(item.Path);
                    }
                    Response.AppendHeader("Fullname", HttpUtility.UrlEncode(item.Fullname));
                    Response.AppendHeader("CreationTime", item.CreationTime);
                    Response.AppendHeader("LastAccessTime", item.LastAccessTime);
                    Response.AppendHeader("LastWriteTime", item.LastWriteTime);
                    Response.AppendHeader("LastSyncTime", item.LastSyncTime);
                    Response.AppendHeader("IsFolder", item.IsFolder.ToString());
                    Response.AppendHeader("Path", HttpUtility.UrlEncode(item.RelativePath));
                }

                Response.Flush();
                Response.End();

                return View();
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //
        // GET: /Account/Login

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (ModelState.IsValid && WebSecurity.Login(model.UserName, model.Password))
            {
                return RedirectToLocal(returnUrl);
            }

            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "The user name or password provided is incorrect.");
            return View(model);
        }

        //
        // POST: /Account/LogOff

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            WebSecurity.Logout();
            return RedirectToAction("Index", "Home");
        }

        #region Helpers
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
        }

        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
        #endregion
    }
}
