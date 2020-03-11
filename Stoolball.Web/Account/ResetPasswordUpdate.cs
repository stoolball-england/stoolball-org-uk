using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Stoolball.Web.Account
{
    public class ResetPasswordUpdate
    {
        [Required]
        public string PasswordResetToken { get; set; }

        [Required(ErrorMessage ="The new password is required.")]
        // Error message matches the one in Umbraco.Web.Controllers.UmbRegisterController 
        // and the criterion set in web.config
        [RegularExpression(@"^.{10,}$", ErrorMessage = "The password is not strong enough")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirming the new password is required.")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmNewPassword { get; set; }
    }
}