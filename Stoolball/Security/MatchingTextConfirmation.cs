using System.ComponentModel.DataAnnotations;

namespace Stoolball.Security
{
    public class MatchingTextConfirmation
    {
        [Required]
        public string RequiredText { get; set; }

        [Compare(nameof(RequiredText), ErrorMessage = "The confirmation text does not match")]
        public string ConfirmationText { get; set; }
    }
}