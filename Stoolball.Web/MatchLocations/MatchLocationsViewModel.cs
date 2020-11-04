using System.Collections.Generic;
using Stoolball.MatchLocations;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationsViewModel : BaseViewModel
    {
        public MatchLocationsViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }

        public MatchLocationQuery MatchLocationQuery { get; set; } = new MatchLocationQuery();
        public List<MatchLocation> MatchLocations { get; internal set; } = new List<MatchLocation>();
        public int TotalMatchLocations { get; set; }
    }
}