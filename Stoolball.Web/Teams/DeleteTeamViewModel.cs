using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Teams
{
    public class DeleteTeamViewModel : BaseViewModel
    {
        public DeleteTeamViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Team Team { get; set; }
        public int TotalMatches { get; set; }
        public MatchingTextConfirmation ConfirmDeleteRequest { get; set; } = new MatchingTextConfirmation();

        public bool Deleted { get; set; }
    }
}