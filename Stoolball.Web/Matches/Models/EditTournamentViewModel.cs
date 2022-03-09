using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Matches.Models
{
    public class EditTournamentViewModel : BaseViewModel
    {
        public EditTournamentViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public Tournament? Tournament { get; set; }
        public Team? Team { get; set; }
        public Season? Season { get; set; }

        public List<Season> PossibleSeasons { get; internal set; } = new List<Season>();

        [Display(Name = "Tournament date")]
        [Required]
        public DateTimeOffset? TournamentDate { get; set; }

        [Display(Name = "Start time")]
        public DateTimeOffset? StartTime { get; set; }
        public Guid? TournamentLocationId { get; set; }

        [Display(Name = "Ground or sports centre name")]
        public string? TournamentLocationName { get; set; }
        public Guid? HomeTeamId { get; set; }
        public Guid? AwayTeamId { get; set; }
        public Uri? UrlReferrer { get; set; }
    }
}