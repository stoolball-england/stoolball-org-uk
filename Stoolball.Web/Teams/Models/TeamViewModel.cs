using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Teams.Models
{
    public class TeamViewModel : BaseViewModel
    {
        public TeamViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public Team? Team { get; set; }
        public MatchListingViewModel Matches { get; set; } = new();
        public AddMatchMenuViewModel AddMatchMenu { get; set; } = new();
        public MatchFilter DefaultMatchFilter { get; set; } = new();
        public MatchFilter AppliedMatchFilter { get; set; } = new();
        public string? FilterDescription { get; set; }
        public bool IsInACurrentLeague { get; set; }
        public bool IsInACurrentKnockoutCompetition { get; set; }
        public List<Player> Players { get; internal set; } = new();
    }
}