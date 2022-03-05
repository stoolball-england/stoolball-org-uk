using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Teams.Models
{
    public class DeleteTeamViewModel : BaseViewModel
    {
        public DeleteTeamViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public Team? Team { get; set; }
        public int TotalMatches { get; set; }
        public MatchingTextConfirmation ConfirmDeleteRequest { get; set; } = new MatchingTextConfirmation();

        public bool Deleted { get; set; }
    }
}