using System.Collections.Generic;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Teams
{
    public class TeamsViewModel : BaseViewModel
    {
        public TeamsViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }

        public TeamQuery TeamQuery { get; set; } = new TeamQuery();
        public List<TeamListing> Teams { get; internal set; } = new List<TeamListing>();
        public int TotalTeams { get; set; }
    }
}