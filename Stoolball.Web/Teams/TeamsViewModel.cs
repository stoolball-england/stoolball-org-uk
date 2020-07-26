using Stoolball.Teams;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Routing;
using System.Collections.Generic;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Teams
{
    public class TeamsViewModel : BaseViewModel
    {
        public TeamsViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }

        public TeamQuery TeamQuery { get; set; } = new TeamQuery();
        public List<TeamListing> Teams { get; internal set; } = new List<TeamListing>();
    }
}