using System.Collections.Generic;
using Stoolball.Competitions;
using Stoolball.Listings;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Competitions
{
    public class CompetitionsViewModel : BaseViewModel, IListingsModel<Competition, CompetitionFilter>
    {
        public CompetitionsViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }

        public CompetitionFilter Filter { get; set; } = new CompetitionFilter();
        public List<Competition> Listings { get; internal set; } = new List<Competition>();
    }
}