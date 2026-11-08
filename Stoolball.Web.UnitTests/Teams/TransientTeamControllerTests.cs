using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class TransientTeamControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateFormatter = new();
        private readonly Mock<IEmailProtector> _emailProtector = new();
        private readonly MatchListing _match = new()
        {
            MatchName = "Example match",
            MatchRoute = "/matches/example-match",
            StartTime = new DateTimeOffset(2026, 5, 1, 14, 0, 0, TimeSpan.Zero)
        };

        private TransientTeamController CreateController()
        {
            return new TransientTeamController(
                Mock.Of<ILogger<TransientTeamController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                _matchDataSource.Object,
                _authorizationPolicy.Object,
                _dateFormatter.Object,
                _emailProtector.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private Team SetupTeam()
        {
            var team = new Team
            {
                TeamId = Guid.NewGuid(),
                TeamName = "Example team",
                PlayerType = PlayerType.Mixed,
                TeamRoute = "/teams/example",
                Cost = "cost",
                Introduction = "introduction",
                PublicContactDetails = "contact",
                PrivateContactDetails = "private"
            };
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(team);
            _matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing> { _match });
            _dateFormatter.Setup(x => x.FormatDate(_match.StartTime, false, false, false)).Returns("1 May 2026");
            _emailProtector.Setup(x => x.ProtectEmailAddresses(It.IsAny<string>(), It.IsAny<bool>())).Returns((string html, bool authenticated) => $"{html}|{authenticated}");
            return team;
        }

        private async Task<TeamViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<TeamViewModel>(((ViewResult)result).Model);
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team?>(null));

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Route_matching_team_returns_TeamViewModel_with_team()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            Assert.Equal(team, model.Team);
        }

        [Fact]
        public async Task Model_has_authorization()
        {
            var team = SetupTeam();
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(team)).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
        }

        [Fact]
        public async Task Matches_are_read_for_the_team_without_match_details_and_the_next_match_is_not_highlighted()
        {
            var team = SetupTeam();

            var model = await ReadModel();

            Assert.Equal([_match], model.Matches.Matches);
            Assert.False(model.Matches.HighlightNextMatch);
            _matchDataSource.Verify(x => x.ReadMatchListings(It.Is<MatchFilter>(f => f.TeamIds.Count == 1 && f.TeamIds[0] == team.TeamId && !f.IncludeMatches), MatchSortOrder.MatchDateEarliestFirst), Times.Once);
        }

        [Fact]
        public async Task Page_title_has_team_name_player_type_and_date_of_first_match()
        {
            SetupTeam();

            var model = await ReadModel();

            Assert.Equal("Example team (Mixed), 1 May 2026", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Breadcrumb_is_the_first_match()
        {
            SetupTeam();

            var model = await ReadModel();

            var breadcrumb = model.Breadcrumbs[^1];
            Assert.Equal("Example match", breadcrumb.Name);
            Assert.Equal("/matches/example-match", breadcrumb.Url?.ToString());
        }

        [Fact]
        public async Task Email_addresses_in_public_text_are_protected_for_signed_out_users_but_private_contact_details_are_not_passed()
        {
            SetupTeam();

            var model = await ReadModel();

            Assert.Equal("cost|False", model.Team!.Cost);
            Assert.Equal("introduction|False", model.Team.Introduction);
            Assert.Equal("contact|False", model.Team.PublicContactDetails);
            Assert.Equal("private", model.Team.PrivateContactDetails);
        }
    }
}
