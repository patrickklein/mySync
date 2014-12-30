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

namespace MySync.Server.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class AccountController : Controller
    {
        //
        // GET: /Account/Setup

        [AllowAnonymous]
        public ActionResult Setup(string returnUrl)
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

            return View(model);
        }

        //
        // POST: /Account/Setup

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Setup(SetupModel model, string returnUrl)
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

            return View(model);
        }

        //
        // POST: /Account/Upload

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Upload(FormCollection formCollection)
        {
            if (Request != null)
            {                
                HttpRuntimeSection section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;

                ConfigurationService configService = new ConfigurationService();
                configService.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());

                DAL.Configuration config = configService.Get("dataSavingPoint");
                string className = (config != null) ? config.Value : "";
                DataProfile.DataProfile newContent = (DataProfile.DataProfile)Activator.CreateInstance(Assembly.GetExecutingAssembly().GetType("MySync.Server.DataProfile." + className, true, true));
                newContent.SetSection(Server, Request, section);
                newContent.SaveFile();

                //Add file values to database
                SynchronisationItemService syncItemservice = new SynchronisationItemService();
                syncItemservice.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());

                DAL.SynchronisationItem item = new SynchronisationItem();
                item.Name = newContent.Filename;
                item.Fullname = newContent.FullName;
                item.Extension = newContent.Extension;
                item.Size = newContent.Length;
                item.Files = newContent.Files;
                item.Folders = newContent.Folders;
                item.FolderFlag = newContent.IsFolder;
                item.Path = newContent.FullPath;
                item.LastWriteTime = newContent.LastWriteTime;
                item.LastSyncTime = DateTime.Now;
                item.LastAccessTime = newContent.LastAccessTime;
                item.CreationTime = newContent.CreationTime;
                //item.HiddenFlag = false;
                //item.SystemFlag = false;
                syncItemservice.Add(item);
            }

            return RedirectToAction("Setup", "Account");
        }

        //
        // POST: /Account/Delete

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Delete(FormCollection formCollection)
        {
            if (Request != null)
            {
                HttpRuntimeSection section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;

                string className = "DPFileSystem";
                DataProfile.DataProfile newContent = (DataProfile.DataProfile)Activator.CreateInstance(Assembly.GetExecutingAssembly().GetType("MySync.Server.DataProfile." + className, true, true));
                newContent.SetSection(Server, Request, section);
                newContent.DeleteAll();
            }

            return RedirectToAction("Setup", "Account");
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
