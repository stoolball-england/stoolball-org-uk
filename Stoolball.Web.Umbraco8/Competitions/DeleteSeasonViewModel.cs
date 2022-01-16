using Stoolball.Competitions;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Competitions
{
    public class DeleteSeasonViewModel : BaseViewModel
    {
        public DeleteSeasonViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public Season Season { get; set; }
        public int TotalMatches { get; set; }
        public MatchingTextConfirmation ConfirmDeleteRequest { get; set; } = new MatchingTextConfirmation();

        public bool Deleted { get; set; }
    }
}