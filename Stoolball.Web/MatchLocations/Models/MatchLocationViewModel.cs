using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.MatchLocations.Models
{
    public class MatchLocationViewModel : BaseViewModel
    {
        public MatchLocationViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public MatchLocation? MatchLocation { get; set; }

        public string? GoogleMapsApiKey { get; set; }
        public MatchListingViewModel Matches { get; set; } = new MatchListingViewModel();
        public MatchFilter DefaultMatchFilter { get; set; } = new MatchFilter();
        public MatchFilter AppliedMatchFilter { get; set; } = new MatchFilter();
        public string? FilterDescription { get; set; }
    }
}