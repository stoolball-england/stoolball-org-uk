using System.ComponentModel.DataAnnotations;

namespace Stoolball.Web.Account
{
    public class ResetPasswordFormData
    {
        [Required(ErrorMessage = "The new password is required.")]
        // Error message matches the one in Umbraco.Web.Controllers.UmbRegisterController 
        // and the criterion set in web.config
        [RegularExpression(@"^.{10,}$", ErrorMessage = "Your password must be at least 10 characters")]
        public string? NewPassword { get; set; }
    }
}