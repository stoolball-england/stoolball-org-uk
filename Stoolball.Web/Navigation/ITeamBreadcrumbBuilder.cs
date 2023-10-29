using System.Collections.Generic;
using Stoolball.Teams;

namespace Stoolball.Web.Navigation
{
    public interface ITeamBreadcrumbBuilder
    {
        void BuildBreadcrumbsForEditPlayers(List<Breadcrumb> breadcrumbs, Team team);
        void BuildBreadcrumbsForTeam(List<Breadcrumb> breadcrumbs, Team team, bool includeTeam);
    }
}