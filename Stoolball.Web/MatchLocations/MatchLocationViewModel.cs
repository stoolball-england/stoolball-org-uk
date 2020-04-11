using Stoolball.MatchLocations;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.MatchLocations
{
    public class MatchLocationViewModel : BaseViewModel
    {
        public MatchLocationViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public MatchLocation MatchLocation { get; set; }

        public string GoogleMapsApiKey { get; set; }
    }
}