using System.ComponentModel.DataAnnotations;

namespace Stoolball.Web.Account
{
    public class ResetPasswordRequestFormData
    {
        [Required]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }
    }
}