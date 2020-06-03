using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.MatchLocations
{
    public class DeleteMatchLocationViewModel : BaseViewModel
    {
        public DeleteMatchLocationViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }

        public MatchLocation MatchLocation { get; set; }

        public int TotalMatches { get; set; }

        public MatchingTextConfirmation ConfirmDeleteRequest { get; set; } = new MatchingTextConfirmation();

        public bool Deleted { get; set; }
    }
}