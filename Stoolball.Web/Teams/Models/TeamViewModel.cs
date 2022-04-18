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
        public MatchListingViewModel Matches { get; set; } = new MatchListingViewModel();
        public AddMatchMenuViewModel AddMatchMenu { get; set; } = new AddMatchMenuViewModel();
        public MatchFilter DefaultMatchFilter { get; set; } = new MatchFilter();
        public MatchFilter AppliedMatchFilter { get; set; } = new MatchFilter();
        public string? FilterDescription { get; set; }
        public bool IsInACurrentLeague { get; set; }
        public bool IsInACurrentKnockoutCompetition { get; set; }
        public IList<Player> Players { get; internal set; } = new List<Player>();
    }
}