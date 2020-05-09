using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Matches
{
    public class TournamentViewModel : BaseViewModel
    {
        public TournamentViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Tournament Tournament { get; set; }
        public IDateTimeFormatter DateTimeFormatter { get; set; }
        public MatchListingViewModel Matches { get; set; }
    }
}