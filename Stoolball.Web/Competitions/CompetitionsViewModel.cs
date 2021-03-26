using System.Collections.Generic;
using Stoolball.Competitions;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Competitions
{
    public class CompetitionsViewModel : BaseViewModel
    {
        public CompetitionsViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }

        public CompetitionFilter CompetitionFilter { get; set; } = new CompetitionFilter();
        public List<Competition> Competitions { get; internal set; } = new List<Competition>();
    }
}