using System.ComponentModel.DataAnnotations;

namespace Stoolball.Web.Teams.Models
{
    public class PlayerIdentityFormData
    {
        /// <remarks>
        /// Avoid the terms "identity" and "name" and use the term "search" to try to avoid contact/password managers adding pop-ups.
        /// </remarks>
        [Display(Name = "Name")]
        [Required]
        public string? PlayerSearch { get; set; }
    }
}
