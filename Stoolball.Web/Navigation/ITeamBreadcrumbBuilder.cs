using System.Collections.Generic;
using Stoolball.Teams;

namespace Stoolball.Web.Navigation
{
    public interface ITeamBreadcrumbBuilder
    {
        void BuildBreadcrumbs(List<Breadcrumb> breadcrumbs, Team team, bool includeTeam);
    }
}