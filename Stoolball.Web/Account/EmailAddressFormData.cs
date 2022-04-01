using System.ComponentModel.DataAnnotations;
using Stoolball.Email;

namespace Stoolball.Web.Account
{
    public class EmailAddressFormData
    {
        /// <remarks>
        /// Deliberately not called "Email" so that contact managers don't fill in the member's current email address
        /// </remarks>
        [Required]
        [Email]
        [Display(Name = "New email address")]
        public string? Requested { get; set; }

        [Required]
        public string? Password { get; set; }
    }
}
