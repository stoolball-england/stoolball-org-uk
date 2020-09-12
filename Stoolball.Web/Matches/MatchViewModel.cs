using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Matches
{
    public class MatchViewModel : BaseViewModel
    {
        public MatchViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public Match Match { get; set; }
        public IDateTimeFormatter DateTimeFormatter { get; set; }
    }
}