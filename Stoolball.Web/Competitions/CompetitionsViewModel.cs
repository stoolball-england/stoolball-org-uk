using System.Collections.Generic;
using Stoolball.Competitions;
using Stoolball.Listings;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Competitions
{
    public class CompetitionsViewModel : BaseViewModel, IListingsModel<Competition, CompetitionFilter>
    {
        public CompetitionsViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }

        public CompetitionFilter Filter { get; set; } = new CompetitionFilter();
        public List<Competition> Listings { get; internal set; } = new List<Competition>();
    }
}