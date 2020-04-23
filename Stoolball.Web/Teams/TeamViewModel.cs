using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Teams
{
    public class TeamViewModel : BaseViewModel
    {
        public TeamViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Team Team { get; set; }
        public MatchListingViewModel Matches { get; set; }
    }
}