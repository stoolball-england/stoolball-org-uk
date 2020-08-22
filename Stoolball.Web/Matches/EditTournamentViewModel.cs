using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using System;
using System.ComponentModel.DataAnnotations;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Matches
{
    public class EditTournamentViewModel : BaseViewModel
    {
        public EditTournamentViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Tournament Tournament { get; set; }
        public Team Team { get; set; }
        public Season Season { get; set; }

        [Display(Name = "Tournament date")]
        [Required]
        public DateTimeOffset? TournamentDate { get; set; }

        [Display(Name = "Start time")]
        public DateTimeOffset? StartTime { get; set; }
        public Guid? TournamentLocationId { get; set; }

        [Display(Name = "Ground or sports centre name")]
        public string TournamentLocationName { get; set; }
        public IDateTimeFormatter DateFormatter { get; set; }
    }
}