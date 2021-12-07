using System;
using System.Collections.Generic;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Statistics;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.UnitTests.Statistics
{
    public class StatisticsBreadcrumbBuilderTests
    {
        [Fact]
        public void Null_breadcrumbs_throws_ArgumentNullException()
        {
            var builder = new StatisticsBreadcrumbBuilder();

            Assert.Throws<ArgumentNullException>(() => builder.BuildBreadcrumbs(null, new StatisticsFilter()));
        }

        [Fact]
        public void Null_filter_throws_ArgumentNullException()
        {
            var builder = new StatisticsBreadcrumbBuilder();

            Assert.Throws<ArgumentNullException>(() => builder.BuildBreadcrumbs(new List<Breadcrumb>(), null));
        }

        [Fact]
        public void No_filter_adds_statistics_breadcrumb()
        {
            var builder = new StatisticsBreadcrumbBuilder();
            var breadcrumbs = new List<Breadcrumb>();

            builder.BuildBreadcrumbs(breadcrumbs, new StatisticsFilter());

            Assert.Single(breadcrumbs);
            Assert.Equal(Constants.Pages.Statistics, breadcrumbs[0].Name);
            Assert.Equal(Constants.Pages.StatisticsUrl, breadcrumbs[0].Url.ToString());
        }

        [Fact]
        public void Player_filter_adds_player_breadcrumb()
        {
            var builder = new StatisticsBreadcrumbBuilder();
            var breadcrumbs = new List<Breadcrumb>();
            var player = new Player { PlayerRoute = "/players/example", PlayerIdentities = new List<PlayerIdentity> { new PlayerIdentity { PlayerIdentityName = "Example player" } } };

            builder.BuildBreadcrumbs(breadcrumbs, new StatisticsFilter { Player = player });

            Assert.Equal(2, breadcrumbs.Count);
            Assert.Equal(Constants.Pages.Statistics, breadcrumbs[0].Name);
            Assert.Equal(Constants.Pages.StatisticsUrl, breadcrumbs[0].Url.ToString());
            Assert.Equal(player.PlayerName(), breadcrumbs[1].Name);
            Assert.Equal(player.PlayerRoute, breadcrumbs[1].Url.ToString());
        }

        [Fact]
        public void Club_filter_adds_club_breadcrumb()
        {
            var builder = new StatisticsBreadcrumbBuilder();
            var breadcrumbs = new List<Breadcrumb>();
            var club = new Club { ClubRoute = "/clubs/example", ClubName = "Example club" };

            builder.BuildBreadcrumbs(breadcrumbs, new StatisticsFilter { Club = club });

            Assert.Equal(2, breadcrumbs.Count);
            Assert.Equal(Constants.Pages.Teams, breadcrumbs[0].Name);
            Assert.Equal(Constants.Pages.TeamsUrl, breadcrumbs[0].Url.ToString());
            Assert.Equal(club.ClubName, breadcrumbs[1].Name);
            Assert.Equal(club.ClubRoute, breadcrumbs[1].Url.ToString());
        }

        [Fact]
        public void Team_filter_adds_team_breadcrumb()
        {
            var builder = new StatisticsBreadcrumbBuilder();
            var breadcrumbs = new List<Breadcrumb>();
            var team = new Team { TeamRoute = "/teams/example", TeamName = "Example team" };

            builder.BuildBreadcrumbs(breadcrumbs, new StatisticsFilter { Team = team });

            Assert.Equal(2, breadcrumbs.Count);
            Assert.Equal(Constants.Pages.Teams, breadcrumbs[0].Name);
            Assert.Equal(Constants.Pages.TeamsUrl, breadcrumbs[0].Url.ToString());
            Assert.Equal(team.TeamName, breadcrumbs[1].Name);
            Assert.Equal(team.TeamRoute, breadcrumbs[1].Url.ToString());
        }

        [Fact]
        public void Team_in_club_filter_adds_team_in_club_breadcrumb()
        {
            var builder = new StatisticsBreadcrumbBuilder();
            var breadcrumbs = new List<Breadcrumb>();
            var team = new Team { TeamRoute = "/teams/example", TeamName = "Example team", Club = new Club { ClubRoute = "/clubs/example", ClubName = "Example club" } };

            builder.BuildBreadcrumbs(breadcrumbs, new StatisticsFilter { Team = team });

            Assert.Equal(3, breadcrumbs.Count);
            Assert.Equal(Constants.Pages.Teams, breadcrumbs[0].Name);
            Assert.Equal(Constants.Pages.TeamsUrl, breadcrumbs[0].Url.ToString());
            Assert.Equal(team.Club.ClubName, breadcrumbs[1].Name);
            Assert.Equal(team.Club.ClubRoute, breadcrumbs[1].Url.ToString());
            Assert.Equal(team.TeamName, breadcrumbs[2].Name);
            Assert.Equal(team.TeamRoute, breadcrumbs[2].Url.ToString());
        }

        [Fact]
        public void MatchLocation_filter_adds_location_breadcrumb()
        {
            var builder = new StatisticsBreadcrumbBuilder();
            var breadcrumbs = new List<Breadcrumb>();
            var location = new MatchLocation { MatchLocationRoute = "/locations/example", PrimaryAddressableObjectName = "Example location", Town = "Test town" };

            builder.BuildBreadcrumbs(breadcrumbs, new StatisticsFilter { MatchLocation = location });

            Assert.Equal(2, breadcrumbs.Count);
            Assert.Equal(Constants.Pages.MatchLocations, breadcrumbs[0].Name);
            Assert.Equal(Constants.Pages.MatchLocationsUrl, breadcrumbs[0].Url.ToString());
            Assert.Equal(location.NameAndLocalityOrTownIfDifferent(), breadcrumbs[1].Name);
            Assert.Equal(location.MatchLocationRoute, breadcrumbs[1].Url.ToString());
        }

        [Fact]
        public void Competition_filter_adds_competition_breadcrumb()
        {
            var builder = new StatisticsBreadcrumbBuilder();
            var breadcrumbs = new List<Breadcrumb>();
            var competition = new Competition { CompetitionRoute = "/competitions/example", CompetitionName = "Example competition" };

            builder.BuildBreadcrumbs(breadcrumbs, new StatisticsFilter { Competition = competition });

            Assert.Equal(2, breadcrumbs.Count);
            Assert.Equal(Constants.Pages.Competitions, breadcrumbs[0].Name);
            Assert.Equal(Constants.Pages.CompetitionsUrl, breadcrumbs[0].Url.ToString());
            Assert.Equal(competition.CompetitionName, breadcrumbs[1].Name);
            Assert.Equal(competition.CompetitionRoute, breadcrumbs[1].Url.ToString());
        }

        [Fact]
        public void Season_filter_adds_season_breadcrumb()
        {
            var builder = new StatisticsBreadcrumbBuilder();
            var breadcrumbs = new List<Breadcrumb>();
            var season = new Season
            {
                SeasonRoute = "/competition/example/2021",
                FromYear = 2021,
                UntilYear = 2021,
                Competition = new Competition { CompetitionRoute = "/competitions/example", CompetitionName = "Example competition" }
            };

            builder.BuildBreadcrumbs(breadcrumbs, new StatisticsFilter { Season = season });

            Assert.Equal(3, breadcrumbs.Count);
            Assert.Equal(Constants.Pages.Competitions, breadcrumbs[0].Name);
            Assert.Equal(Constants.Pages.CompetitionsUrl, breadcrumbs[0].Url.ToString());
            Assert.Equal(season.Competition.CompetitionName, breadcrumbs[1].Name);
            Assert.Equal(season.Competition.CompetitionRoute, breadcrumbs[1].Url.ToString());
            Assert.Equal(season.SeasonName(), breadcrumbs[2].Name);
            Assert.Equal(season.SeasonRoute, breadcrumbs[2].Url.ToString());
        }
    }
}
