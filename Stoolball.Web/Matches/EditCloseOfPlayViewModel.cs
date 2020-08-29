using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Matches
{
    public class EditCloseOfPlayViewModel : BaseViewModel
    {
        public EditCloseOfPlayViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Match Match { get; set; }

        public IDateTimeFormatter DateFormatter { get; set; }
    }
}