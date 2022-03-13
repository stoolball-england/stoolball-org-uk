using Stoolball.Matches;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Matches.Models
{
    public class EditCloseOfPlayViewModel : BaseViewModel
    {
        public EditCloseOfPlayViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }

        public EditCloseOfPlayFormData FormData { get; set; } = new EditCloseOfPlayFormData();

        public Match? Match { get; set; }
    }
}