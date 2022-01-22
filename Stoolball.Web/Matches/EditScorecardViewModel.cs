using Stoolball.Matches;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Matches
{
    public class EditScorecardViewModel : BaseViewModel
    {
        public EditScorecardViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }

        public Match Match { get; set; } = new Match();
        public int? InningsOrderInMatch { get; set; }
        public MatchInningsViewModel CurrentInnings { get; set; } = new MatchInningsViewModel();

        public bool Autofocus { get; set; }
    }
}