using System.Collections.Generic;
using Stoolball.Competitions;
using Stoolball.Umbraco.Data.Competitions;
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

        public CompetitionQuery CompetitionQuery { get; set; } = new CompetitionQuery();
        public List<Competition> Competitions { get; internal set; } = new List<Competition>();

        public int TotalCompetitions { get; set; }
    }
}