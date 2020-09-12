using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Teams
{
    public class TeamViewModel : BaseViewModel
    {
        public TeamViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public Team Team { get; set; }
        public MatchListingViewModel Matches { get; set; }
        public bool IsInACurrentLeague { get; set; }
        public bool IsInACurrentKnockoutCompetition { get; set; }
    }
}