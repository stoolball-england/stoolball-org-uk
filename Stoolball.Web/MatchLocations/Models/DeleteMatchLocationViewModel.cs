using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.MatchLocations.Models
{
    public class DeleteMatchLocationViewModel : BaseViewModel
    {
        public DeleteMatchLocationViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }

        public MatchLocation? MatchLocation { get; set; }

        public int TotalMatches { get; set; }

        public MatchingTextConfirmation ConfirmDeleteRequest { get; set; } = new MatchingTextConfirmation();

        public bool Deleted { get; set; }
    }
}