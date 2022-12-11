using Stoolball.Statistics;
using Stoolball.Web.Statistics;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Teams.Models
{
    public class PlayerIdentityViewModel : BasePlayerViewModel
    {
        public PlayerIdentityViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public PlayerIdentity? PlayerIdentity { get; set; }
        public PlayerIdentityFormData FormData { get; set; } = new();
    }
}