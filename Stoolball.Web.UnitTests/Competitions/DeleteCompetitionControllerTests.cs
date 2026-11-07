using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class DeleteCompetitionControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();

        private DeleteCompetitionController CreateController()
        {
            return new DeleteCompetitionController(
                Mock.Of<ILogger<DeleteCompetitionController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _competitionDataSource.Object,
                _matchListingDataSource.Object,
                _teamDataSource.Object,
                _authorizationPolicy.Object
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_competition_returns_404()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_competition_returns_DeleteCompetitionViewModel()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionId = Guid.NewGuid(), CompetitionRoute = "/competitions/example" });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<DeleteCompetitionViewModel>(((ViewResult)result).Model);
            }
        }
        [Fact]
        public async Task Model_has_total_matches_and_teams_for_competition()
        {
            var competition = new Competition { CompetitionId = Guid.NewGuid(), CompetitionName = "Example", CompetitionRoute = "/competitions/example" };
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(competition);
            _matchListingDataSource.Setup(x => x.ReadTotalMatches(It.IsAny<MatchFilter>())).ReturnsAsync(12);
            _teamDataSource.Setup(x => x.ReadTotalTeams(It.IsAny<TeamFilter>())).ReturnsAsync(4);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<DeleteCompetitionViewModel>(((ViewResult)result).Model);
            Assert.Equal(competition, model.Competition);
            Assert.Equal(12, model.TotalMatches);
            Assert.Equal(4, model.TotalTeams);
            _matchListingDataSource.Verify(x => x.ReadTotalMatches(It.Is<MatchFilter>(f => f.CompetitionIds.SequenceEqual(new[] { competition.CompetitionId!.Value }) && f.IncludeTournamentMatches)), Times.Once);
            _teamDataSource.Verify(x => x.ReadTotalTeams(It.Is<TeamFilter>(f => f.CompetitionIds.SequenceEqual(new[] { competition.CompetitionId!.Value }))), Times.Once);
        }

        [Fact]
        public async Task Model_requires_competition_name_as_confirmation_text()
        {
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionId = Guid.NewGuid(), CompetitionName = "Example", CompetitionRoute = "/competitions/example" });

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<DeleteCompetitionViewModel>(((ViewResult)result).Model);
            Assert.Equal("Example", model.ConfirmDeleteRequest.RequiredText);
        }

        [Fact]
        public async Task Model_has_authorization_page_title_and_breadcrumbs()
        {
            var competition = new Competition { CompetitionId = Guid.NewGuid(), CompetitionName = "Example", CompetitionRoute = "/competitions/example" };
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteCompetition, true } };
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(competition);
            _authorizationPolicy.Setup(x => x.IsAuthorized(competition)).ReturnsAsync(authorization);

            using var controller = CreateController();

            var result = await controller.Index();

            var model = Assert.IsType<DeleteCompetitionViewModel>(((ViewResult)result).Model);
            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
            Assert.Equal("Delete Example", model.Metadata.PageTitle);
            Assert.Equal(Constants.Pages.Competitions, model.Breadcrumbs[^2].Name);
            Assert.Equal("Example", model.Breadcrumbs[^1].Name);
            Assert.Equal("/competitions/example", model.Breadcrumbs[^1].Url!.ToString());
        }
    }
}
