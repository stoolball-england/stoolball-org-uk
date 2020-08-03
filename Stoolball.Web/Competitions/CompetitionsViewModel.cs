using Stoolball.Competitions;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Web.Routing;
using System.Collections.Generic;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Competitions
{
    public class CompetitionsViewModel : BaseViewModel
    {
        public CompetitionsViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }

        public CompetitionQuery CompetitionQuery { get; set; } = new CompetitionQuery();
        public List<Competition> Competitions { get; internal set; } = new List<Competition>();

        public int TotalCompetitions { get; set; }
    }
}