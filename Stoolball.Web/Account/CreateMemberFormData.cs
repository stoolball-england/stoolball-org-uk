using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Web.Common.Models;

namespace Stoolball.Web.Account
{
    public class CreateMemberFormData : PostRedirectModel
    {
        public CreateMemberFormData()
        {
            MemberTypeAlias = "Member";
            UsernameIsEmail = true;
            MemberProperties = new List<MemberPropertyModel>();
            AutomaticLogIn = true;
        }

        /// <summary>
        /// The member's real name.
        /// </summary>
        [MemberName]
        public string? Name { get; set; }

        [Required]
        [EmailAddress]
        [RegularExpression(@".*(?<!\.ru)$", ErrorMessage = "You cannot create an account with that email address")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        /// <summary>
        /// Returns the member properties
        /// </summary>
        public List<MemberPropertyModel> MemberProperties { get; set; }

        /// <summary>
        /// The member type alias to use to register the member
        /// </summary>
        [Editable(false)]
        public string MemberTypeAlias { get; set; }

        /// <summary>
        /// The members password
        /// </summary>
        [Required]
        [StringLength(256)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        /// <summary>
        /// The username of the model, if UsernameIsEmail is true then this is ignored.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Flag to determine if the username should be the email address, if true then the
        /// Username property is ignored
        /// </summary>
        public bool UsernameIsEmail { get; set; }

        /// <summary>
        /// Flag to determine if the member should be logged in automatically after successful
        //  registration
        /// </summary>
        public bool AutomaticLogIn { get; set; }
    }
}
