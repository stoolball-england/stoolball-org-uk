using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class CreateSeasonControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();

        private CreateSeasonController CreateController()
        {
            return new CreateSeasonController(
                Mock.Of<ILogger<CreateSeasonController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _competitionDataSource.Object,
                _authorizationPolicy.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private Competition SetupCompetition(string name = "Example")
        {
            var competition = new Competition { CompetitionName = name, CompetitionRoute = "/competitions/example" };
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(competition);
            return competition;
        }

        private async Task<SeasonViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
        }

        [Fact]
        public async Task Route_not_matching_competition_returns_404()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition?>(null));

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Returns_SeasonViewModel()
        {
            SetupCompetition();

            var model = await ReadModel();

            Assert.NotNull(model.Season);
        }

        [Fact]
        public async Task FromYear_defaults_to_current_year()
        {
            SetupCompetition();

            var model = await ReadModel();

            Assert.Equal(DateTime.Today.Year, model.Season?.FromYear);
        }

        [Fact]
        public async Task UntilYear_defaults_to_zero()
        {
            SetupCompetition();

            var model = await ReadModel();

            Assert.Equal(0, model.Season?.UntilYear);
        }

        [Fact]
        public async Task PlayersPerTeam_defaults_to_11()
        {
            SetupCompetition();

            var model = await ReadModel();

            Assert.Equal(11, model.Season?.PlayersPerTeam);
        }

        [Fact]
        public async Task Empty_over_set_is_added_when_there_is_no_previous_season()
        {
            SetupCompetition();

            var model = await ReadModel();

            var overSet = Assert.Single(model.Season!.DefaultOverSets);
            Assert.Null(overSet.Overs);
        }

        [Fact]
        public async Task Previous_summer_season_is_used_as_template_for_the_next_summer_season()
        {
            var competition = SetupCompetition();
            var previous = new Season { FromYear = 2025, UntilYear = 2025, PlayersPerTeam = 9 };
            previous.DefaultOverSets.Add(new OverSet { Overs = 16 });
            competition.Seasons.Add(previous);

            var model = await ReadModel();

            Assert.Equal(2026, model.Season!.FromYear);
            Assert.Equal(0, model.Season.UntilYear);
            Assert.Equal(9, model.Season.PlayersPerTeam);
            Assert.Equal(16, Assert.Single(model.Season.DefaultOverSets).Overs);
        }

        [Fact]
        public async Task Previous_winter_season_is_used_as_template_for_the_next_winter_season()
        {
            var competition = SetupCompetition();
            competition.Seasons.Add(new Season { FromYear = 2025, UntilYear = 2026 });

            var model = await ReadModel();

            Assert.Equal(2026, model.Season!.FromYear);
            Assert.Equal(1, model.Season.UntilYear);
        }

        [Fact]
        public async Task Model_has_competition_and_authorization()
        {
            var competition = SetupCompetition();
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(competition)).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(competition, model.Season!.Competition);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
        }

        [Fact]
        public async Task Page_title_includes_the_before_competition_name()
        {
            SetupCompetition("Example league");

            var model = await ReadModel();

            Assert.Equal("Add a season in the Example league", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Page_title_does_not_repeat_the_when_competition_name_starts_with_it()
        {
            SetupCompetition("The Example league");

            var model = await ReadModel();

            Assert.Equal("Add a season in The Example league", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Breadcrumbs_link_to_competitions_and_competition()
        {
            SetupCompetition("Example league");

            var model = await ReadModel();

            Assert.Equal(Constants.Pages.Competitions, model.Breadcrumbs[^2].Name);
            Assert.Equal("Example league", model.Breadcrumbs[^1].Name);
            Assert.Equal("/competitions/example", model.Breadcrumbs[^1].Url?.ToString());
        }
    }
}
