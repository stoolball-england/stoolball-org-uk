using System.ComponentModel.DataAnnotations;

namespace Stoolball.Web.Matches
{
    public class EditMatchFormatFormData
    {
        [Display(Name = "Innings in the match")]
        public int MatchInnings { get; set; }

        [Display(Name = "Overs per innings")]
        public int? Overs { get; set; }
    }
}