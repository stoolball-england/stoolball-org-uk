using System.ComponentModel.DataAnnotations;

namespace Stoolball.Web.Matches.Models
{
    public class EditMatchFormatFormData
    {
        [Display(Name = "Innings in the match")]
        [Required]
        public int MatchInnings { get; set; }

        [Display(Name = "Overs per innings")]
        [Required]
        public int Overs { get; set; }
    }
}