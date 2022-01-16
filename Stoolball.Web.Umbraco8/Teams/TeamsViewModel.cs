using System.Collections.Generic;
using Stoolball.Listings;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Teams
{
    public class TeamsViewModel : BaseViewModel, IListingsModel<TeamListing, TeamListingFilter>
    {
        public TeamsViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public string GoogleMapsApiKey { get; set; }

        public TeamListingFilter Filter { get; set; } = new TeamListingFilter();
        public List<TeamListing> Listings { get; internal set; } = new List<TeamListing>();
    }
}