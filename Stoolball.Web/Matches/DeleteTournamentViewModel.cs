using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Matches
{
    public class DeleteTournamentViewModel : BaseViewModel
    {
        public DeleteTournamentViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Tournament Tournament { get; set; }
        public IDateTimeFormatter DateTimeFormatter { get; set; }
        public MatchListingViewModel Matches { get; set; }
        public MatchingTextConfirmation ConfirmDeleteRequest { get; set; } = new MatchingTextConfirmation();
        public bool Deleted { get; set; }
    }
}