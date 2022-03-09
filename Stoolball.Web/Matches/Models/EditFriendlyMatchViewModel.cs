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
    public class EditFriendlyMatchViewModel : BaseViewModel, IEditMatchViewModel
    {
        public EditFriendlyMatchViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
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

        public Guid? HomeTeamId { get; set; }

        [Display(Name = "Home team")]
        public string? HomeTeamName { get; set; }

        public Guid? AwayTeamId { get; set; }

        [Display(Name = "Away team")]
        public string? AwayTeamName { get; set; }
    }
}