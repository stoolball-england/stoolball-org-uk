using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Matches
{
    public class EditLeagueMatchViewModel : BaseViewModel, IEditMatchViewModel
    {
        public EditLeagueMatchViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Match Match { get; set; }
        public Team Team { get; set; }
        public Season Season { get; set; }
        public string SeasonFullName { get; set; }

        [Display(Name = "Match date")]
        [Required]
        public DateTimeOffset? MatchDate { get; set; }

        [Display(Name = "Start time")]
        public DateTimeOffset? StartTime { get; set; }

        [Display(Name = "Ground or sports centre name")]
        public string MatchLocationName { get; set; }
        public Guid? MatchLocationId { get; set; }

        public List<SelectListItem> PossibleSeasons { get; internal set; } = new List<SelectListItem>();

        public List<SelectListItem> PossibleHomeTeams { get; internal set; } = new List<SelectListItem>();
        public List<SelectListItem> PossibleAwayTeams { get; internal set; } = new List<SelectListItem>();

        [Display(Name = "Home team")]
        [Required]
        public Guid? HomeTeamId { get; set; }
        public string HomeTeamName { get; set; }

        [Display(Name = "Away team")]
        [Required]
        public Guid? AwayTeamId { get; set; }
        public string AwayTeamName { get; set; }
        public IDateTimeFormatter DateFormatter { get; set; }
    }
}