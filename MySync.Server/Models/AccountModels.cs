using MySync.Server.Configuration;
using MySync.Server.DAL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Data.Entity;
using System.Globalization;
using System.Web.Configuration;
using System.Web.Security;

namespace MySync.Server.Models
{
    #region Custom DataType Attributes

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FileSizeAttribute : DataTypeAttribute 
    {
        public FileSizeAttribute() : base("integer") { }

        public override string FormatErrorMessage(string name)
        {
            if(ErrorMessage == null && ErrorMessageResourceName == null)
            {
                HttpRuntimeSection section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
                int maxFileSize = section.MaxRequestLength / 1024 / 1000;

                ErrorMessage = String.Format("Please enter a valid number between {0} and {1}.", 0, maxFileSize);
            }

 	        return base.FormatErrorMessage(name);
        }

        public override bool IsValid(object value)
        {
            //if empty
            if(value == null) return true;

            //check if value is an integer value
            int retNum;
            bool parse = int.TryParse(Convert.ToString(value), out retNum);

            //check if value is lower than maximum allowed
            HttpRuntimeSection section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
            int maxFileSize = section.MaxRequestLength / 1024 / 1000;
            if ((retNum < 0 || retNum > maxFileSize) || !parse) return false;

            return true;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DiskSizeAttribute : DataTypeAttribute
    {
        public DiskSizeAttribute() : base("integer") { }

        public override string FormatErrorMessage(string name)
        {
            if (ErrorMessage == null && ErrorMessageResourceName == null)
            {
                HttpRuntimeSection section = ConfigurationManager.GetSection("system.web/httpRuntime") as HttpRuntimeSection;
                ErrorMessage = String.Format("Please enter a valid number greater than 0 and the maximum allowed file size.");
            }

            return base.FormatErrorMessage(name);
        }

        public override bool IsValid(object value)
        {
            //if empty
            if (value == null) return true;

            //check if value is an integer value
            int retNum;
            bool parse = int.TryParse(Convert.ToString(value), out retNum);

            ConfigurationService configService = new ConfigurationService();
            configService.SetSession(ApplicationCore.Instance.SessionFactory.OpenSession());
            DAL.Configuration config = configService.Get("maxFileSize");
            Int64 savedMaxFileSize = (config != null) ? Convert.ToInt64(config.Value) : 0;

            if ((retNum < 0 || retNum < savedMaxFileSize) || !parse) return false;

            return true;
        }
    }

    #endregion

    [Table("UserProfile")]
    public class UserProfile
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        public string UserName { get; set; }
    }

    public class SetupModel
    {
        [Required]
        [Display(Name = "Path for data saving")]
        public string Path { get; set; }

        [Required]
        [Display(Name = "Maximum allowed file size")]
        [FileSize]
        public int FileSize { get; set; }

        [Required]
        [Display(Name = "Maximum available disk space for synchronization")]
        [DiskSize]
        public long DiskSpace { get; set; }

        [Required]
        [Display(Name = "Type of data saving")]
        public string DataProfile { get; set; }
    }

    public class LocalPasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
}
