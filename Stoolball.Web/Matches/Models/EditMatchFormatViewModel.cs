using Stoolball.Matches;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Matches.Models
{
    public class EditMatchFormatViewModel : BaseViewModel
    {
        public EditMatchFormatViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }

        public EditMatchFormatFormData FormData { get; set; } = new EditMatchFormatFormData();

        public Match? Match { get; set; }
    }
}