using System.Collections.Generic;
using Stoolball.Listings;
using Stoolball.MatchLocations;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationsViewModel : BaseViewModel, IListingsModel<MatchLocation, MatchLocationFilter>
    {
        public MatchLocationsViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }

        public MatchLocationFilter Filter { get; set; } = new MatchLocationFilter();
        public List<MatchLocation> Listings { get; internal set; } = new List<MatchLocation>();
    }
}