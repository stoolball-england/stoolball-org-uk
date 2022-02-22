using Stoolball.Competitions;
using Stoolball.Security;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Competitions.Models
{
    public class DeleteSeasonViewModel : BaseViewModel
    {
        public DeleteSeasonViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public Season? Season { get; set; }
        public int TotalMatches { get; set; }
        public MatchingTextConfirmation ConfirmDeleteRequest { get; set; } = new MatchingTextConfirmation();

        public bool Deleted { get; set; }
    }
}