using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Routing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Matches
{
    public class EditStartOfPlayViewModel : BaseViewModel
    {
        public EditStartOfPlayViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Match Match { get; set; }

        public List<SelectListItem> PossibleHomeTeams { get; internal set; } = new List<SelectListItem>();
        public List<SelectListItem> PossibleAwayTeams { get; internal set; } = new List<SelectListItem>();

        [Display(Name = "Home team")]
        [Required]
        public Guid? HomeTeamId { get; set; }

        [Display(Name = "Away team")]
        [Required]
        public Guid? AwayTeamId { get; set; }

        [Display(Name = "Ground or sports centre name")]
        public string MatchLocationName { get; set; }
        public Guid? MatchLocationId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TeamInMatch.MatchTeamId"/> of the team that won the toss, or the string 'Home' or 'Away' for an unknown team
        /// </summary>
        public string TossWonBy { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TeamInMatch.MatchTeamId"/> of the team that batted first, or the string 'Home' or 'Away' for an unknown team
        /// </summary>
        public string BattedFirst { get; set; }

        public IDateTimeFormatter DateFormatter { get; set; }
    }
}