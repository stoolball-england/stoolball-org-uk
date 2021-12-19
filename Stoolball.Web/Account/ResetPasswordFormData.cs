using System.ComponentModel.DataAnnotations;

namespace Stoolball.Web.Account
{
    public class ResetPasswordFormData
    {
        [Required]
        public string PasswordResetToken { get; set; }

        [Required(ErrorMessage = "The new password is required.")]
        // Error message matches the one in Umbraco.Web.Controllers.UmbRegisterController 
        // and the criterion set in web.config
        [RegularExpression(@"^.{10,}$", ErrorMessage = "The password is not strong enough")]
        public string NewPassword { get; set; }
    }
}