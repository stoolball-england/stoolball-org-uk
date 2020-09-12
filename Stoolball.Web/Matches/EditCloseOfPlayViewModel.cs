using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Matches
{
    public class EditCloseOfPlayViewModel : BaseViewModel
    {
        public EditCloseOfPlayViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public Match Match { get; set; }

        public IDateTimeFormatter DateFormatter { get; set; }
    }
}