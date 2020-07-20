using Stoolball.Competitions;
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
    public class CreateMatchViewModel : BaseViewModel
    {
        public CreateMatchViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }

        public Match Match { get; set; }
        public Team Team { get; set; }
        public Season Season { get; set; }

        [Display(Name = "Match date")]
        public DateTimeOffset? MatchDate { get; set; }

        [Display(Name = "Start time")]
        [Required]
        public DateTimeOffset? StartTime { get; set; }

        [Display(Name = "Home team")]
        [Required]
        public Guid? HomeTeamId { get; set; }

        [Display(Name = "Away team")]
        [Required]
        public Guid? AwayTeamId { get; set; }

        public List<SelectListItem> PossibleSeasons { get; internal set; } = new List<SelectListItem>();

        public List<SelectListItem> PossibleTeams { get; internal set; } = new List<SelectListItem>();
    }
}