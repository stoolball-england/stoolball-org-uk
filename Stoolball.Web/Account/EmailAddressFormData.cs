using System.ComponentModel.DataAnnotations;

namespace Stoolball.Web.Account
{
    public class EmailAddressFormData
    {
        [Required]
        [Display(Name = "New email address")]
        public string RequestedEmail { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
