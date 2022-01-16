using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Web.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationViewModel : BaseViewModel
    {
        public MatchLocationViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public MatchLocation MatchLocation { get; set; }

        public string GoogleMapsApiKey { get; set; }
        public MatchListingViewModel Matches { get; set; }
        public MatchFilter DefaultMatchFilter { get; set; }
        public MatchFilter AppliedMatchFilter { get; set; }
        public string FilterDescription { get; set; }
    }
}