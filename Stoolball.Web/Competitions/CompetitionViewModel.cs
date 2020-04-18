using Stoolball.Competitions;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Competitions
{
    public class CompetitionViewModel : BaseViewModel
    {
        public CompetitionViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Competition Competition { get; set; }
    }
}