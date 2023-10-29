using System;
using System.Collections.Generic;
using Stoolball.Teams;

namespace Stoolball.Web.Navigation
{
    public class TeamBreadcrumbBuilder : ITeamBreadcrumbBuilder
    {
        public void BuildBreadcrumbsForEditPlayers(List<Breadcrumb> breadcrumbs, Team team)
        {
            BuildBreadcrumbsForTeam(breadcrumbs, team, true);
            breadcrumbs.Add(new Breadcrumb { Name = "Players", Url = new Uri(team.TeamRoute + "/edit/players", UriKind.Relative) });
        }

        public void BuildBreadcrumbsForTeam(List<Breadcrumb> breadcrumbs, Team team, bool includeTeam)
        {
            if (breadcrumbs is null)
            {
                throw new ArgumentNullException(nameof(breadcrumbs));
            }

            if (team is null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
            if (team.Club != null && !string.IsNullOrEmpty(team.Club.ClubRoute))
            {
                breadcrumbs.Add(new Breadcrumb { Name = team.Club.ClubName, Url = new Uri(team.Club.ClubRoute, UriKind.Relative) });
            }
            if (includeTeam && !string.IsNullOrEmpty(team.TeamRoute))
            {
                breadcrumbs.Add(new Breadcrumb { Name = team.TeamName, Url = new Uri(team.TeamRoute, UriKind.Relative) });
            }
        }
    }
}