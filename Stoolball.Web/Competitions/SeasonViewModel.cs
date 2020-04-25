using Stoolball.Competitions;
using Stoolball.Web.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Competitions
{
    public class SeasonViewModel : BaseViewModel
    {
        public SeasonViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Season Season { get; set; }
        public MatchListingViewModel Matches { get; set; }
    }
}