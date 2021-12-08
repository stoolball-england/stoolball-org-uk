using System.ComponentModel.DataAnnotations;

namespace Stoolball.Web.Account
{
    public class EmailAddressFormData
    {
        [Required]
        [Display(Name = "Email address")]
        public string RequestedEmail { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
