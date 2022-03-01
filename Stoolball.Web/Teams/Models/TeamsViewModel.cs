using System.Collections.Generic;
using Stoolball.Listings;
using Stoolball.Teams;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Teams.Models
{
    public class TeamsViewModel : BaseViewModel, IListingsModel<TeamListing, TeamListingFilter>
    {
        public TeamsViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public string? GoogleMapsApiKey { get; set; }

        public TeamListingFilter Filter { get; set; } = new TeamListingFilter();
        public List<TeamListing> Listings { get; internal set; } = new List<TeamListing>();
    }
}