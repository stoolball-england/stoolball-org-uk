using System.Collections.Generic;
using Stoolball.Listings;
using Stoolball.MatchLocations;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.MatchLocations.Models
{
    public class MatchLocationsViewModel : BaseViewModel, IListingsModel<MatchLocation, MatchLocationFilter>
    {
        public MatchLocationsViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }

        public MatchLocationFilter Filter { get; set; } = new MatchLocationFilter();
        public List<MatchLocation> Listings { get; internal set; } = new List<MatchLocation>();
    }
}