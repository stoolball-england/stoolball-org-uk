using System;
using System.Collections.Generic;
using Stoolball.Clubs;
using Stoolball.Teams;
using Stoolball.Web.Navigation;
using Xunit;

namespace Stoolball.Web.UnitTests.Navigation
{
    public class TeamBreadcrumbBuilderTests
    {
#nullable disable
        [Fact]
        public void Null_breadcrumbs_throws_ArgumentNullException()
        {
            var builder = new TeamBreadcrumbBuilder();

            Assert.Throws<ArgumentNullException>(() => builder.BuildBreadcrumbs(null, new Team(), true));
        }

        [Fact]
        public void Null_team_throws_ArgumentNullException()
        {
            var builder = new TeamBreadcrumbBuilder();

            Assert.Throws<ArgumentNullException>(() => builder.BuildBreadcrumbs(new List<Breadcrumb>(), null, true));
        }
#nullable enable

        [Fact]
        public void Club_adds_club_breadcrumb()
        {
            var builder = new TeamBreadcrumbBuilder();
            var breadcrumbs = new List<Breadcrumb>();
            var team = new Team { Club = new Club { ClubName = "Example club", ClubRoute = "/clubs/example-club" } };

            builder.BuildBreadcrumbs(breadcrumbs, team, false);

            Assert.Equal(2, breadcrumbs.Count);
            Assert.Equal(Constants.Pages.Teams, breadcrumbs[0].Name);
            Assert.Equal(Constants.Pages.TeamsUrl, breadcrumbs[0].Url?.ToString());
            Assert.Equal(team.Club.ClubName, breadcrumbs[1].Name);
            Assert.Equal(team.Club.ClubRoute, breadcrumbs[1].Url?.ToString());
        }

        [Fact]
        public void IncludeTeam_adds_team_breadcrumb()
        {
            var builder = new TeamBreadcrumbBuilder();
            var breadcrumbs = new List<Breadcrumb>();
            var team = new Team { TeamName = "Example team", TeamRoute = "/teams/example-team" };

            builder.BuildBreadcrumbs(breadcrumbs, team, true);

            Assert.Equal(2, breadcrumbs.Count);
            Assert.Equal(Constants.Pages.Teams, breadcrumbs[0].Name);
            Assert.Equal(Constants.Pages.TeamsUrl, breadcrumbs[0].Url?.ToString());
            Assert.Equal(team.TeamName, breadcrumbs[1].Name);
            Assert.Equal(team.TeamRoute, breadcrumbs[1].Url?.ToString());
        }
    }
}
