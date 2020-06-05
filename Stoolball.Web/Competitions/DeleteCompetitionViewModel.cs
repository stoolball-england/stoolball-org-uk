using Stoolball.Competitions;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Competitions
{
    public class DeleteCompetitionViewModel : BaseViewModel
    {
        public DeleteCompetitionViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Competition Competition { get; set; }
        public int TotalMatches { get; set; }
        public int TotalTeams { get; set; }
        public MatchingTextConfirmation ConfirmDeleteRequest { get; set; } = new MatchingTextConfirmation();

        public bool Deleted { get; set; }
    }
}