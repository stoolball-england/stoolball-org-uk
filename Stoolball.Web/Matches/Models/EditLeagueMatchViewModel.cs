using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Matches.Models
{
    public class EditLeagueMatchViewModel : BaseViewModel, IEditMatchViewModel
    {
        public EditLeagueMatchViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public Match? Match { get; set; }
        public Team? Team { get; set; }
        public Season? Season { get; set; }
        public string? SeasonFullName { get; set; }

        [Display(Name = "Match date")]
        [Required]
        public DateTimeOffset? MatchDate { get; set; }

        [Display(Name = "Start time")]
        public DateTimeOffset? StartTime { get; set; }

        [Display(Name = "Ground or sports centre name")]
        public string? MatchLocationName { get; set; }
        public Guid? MatchLocationId { get; set; }

        public List<SelectListItem> PossibleSeasons { get; internal set; } = new List<SelectListItem>();

        public List<SelectListItem> PossibleHomeTeams { get; internal set; } = new List<SelectListItem>();
        public List<SelectListItem> PossibleAwayTeams { get; internal set; } = new List<SelectListItem>();

        [Display(Name = "Home team")]
        [Required]
        public Guid? HomeTeamId { get; set; }
        public string? HomeTeamName { get; set; }

        [Display(Name = "Away team")]
        [Required]
        public Guid? AwayTeamId { get; set; }
        public string? AwayTeamName { get; set; }
    }
}