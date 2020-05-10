using Stoolball.MatchLocations;
using Stoolball.Umbraco.Data.MatchLocations;
using Stoolball.Web.Routing;
using System.Collections.Generic;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationsViewModel : BaseViewModel
    {
        public MatchLocationsViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }

        public MatchLocationQuery MatchLocationQuery { get; set; } = new MatchLocationQuery();
        public List<MatchLocation> MatchLocations { get; internal set; } = new List<MatchLocation>();
    }
}