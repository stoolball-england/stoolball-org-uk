using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Comments;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Html;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Configuration;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class MatchControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<ICommentsDataSource<Stoolball.Matches.Match>> _commentsDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Stoolball.Matches.Match>> _authorizationPolicy = new();
        private readonly Mock<IEmailProtector> _emailProtector = new();
        private readonly Mock<IBadLanguageFilter> _badLanguageFilter = new();

        private MatchController CreateController()
        {
            return new MatchController(
                Mock.Of<ILogger<MatchController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchDataSource.Object,
                _commentsDataSource.Object,
                _authorizationPolicy.Object,
                Mock.Of<IDateTimeFormatter>(),
                Mock.Of<IApiKeyProvider>(),
                _emailProtector.Object,
                _badLanguageFilter.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private void SetupMatch(Stoolball.Matches.Match match)
        {
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(match);
            _commentsDataSource.Setup(x => x.ReadComments(match.MatchId!.Value)).Returns(Task.FromResult(new List<HtmlComment>()));
        }

        private async Task<MatchViewModel> GetModel()
        {
            using var controller = CreateController();
            var result = await controller.Index();
            return (MatchViewModel)((ViewResult)result).Model!;
        }

        [Fact]
        public async Task Route_not_matching_match_returns_404()
        {
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).Returns(Task.FromResult<Stoolball.Matches.Match?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_not_matching_match_does_not_read_comments()
        {
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).Returns(Task.FromResult<Stoolball.Matches.Match?>(null));

            using var controller = CreateController();
            await controller.Index();

            _commentsDataSource.Verify(x => x.ReadComments(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Route_matching_match_returns_MatchViewModel()
        {
            var matchId = Guid.NewGuid();
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { MatchId = matchId });

            _commentsDataSource.Setup(x => x.ReadComments(matchId)).Returns(Task.FromResult(new List<HtmlComment>()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<MatchViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Route_matching_match_reads_comments()
        {
            var matchId = Guid.NewGuid();
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { MatchId = matchId });

            _commentsDataSource.Setup(x => x.ReadComments(matchId)).Returns(Task.FromResult(new List<HtmlComment>()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                _commentsDataSource.Verify(x => x.ReadComments(matchId), Times.Once);
            }
        }

        [Fact]
        public async Task Route_matching_match_assigns_match_to_view_model()
        {
            var match = new Stoolball.Matches.Match { MatchId = Guid.NewGuid() };
            SetupMatch(match);

            var model = await GetModel();

            Assert.Same(match, model.Match);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Authorization_is_set_from_authorization_policy(bool authorized)
        {
            var match = new Stoolball.Matches.Match { MatchId = Guid.NewGuid() };
            SetupMatch(match);
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditMatch, authorized } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(match)).ReturnsAsync(authorization);

            var model = await GetModel();

            Assert.Equal(authorized, model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatch]);
        }

        [Fact]
        public async Task Comments_are_protected_and_filtered()
        {
            var match = new Stoolball.Matches.Match { MatchId = Guid.NewGuid() };
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(match);
            _commentsDataSource.Setup(x => x.ReadComments(match.MatchId!.Value)).Returns(Task.FromResult(new List<HtmlComment> { new HtmlComment { Comment = "original" } }));
            _emailProtector.Setup(x => x.ProtectEmailAddresses("original", false)).Returns("protected");
            _badLanguageFilter.Setup(x => x.Filter("protected")).Returns("filtered");

            var model = await GetModel();

            Assert.Equal("filtered", model.Match!.Comments.Single().Comment);
        }

        [Fact]
        public async Task Match_notes_are_protected()
        {
            var match = new Stoolball.Matches.Match { MatchId = Guid.NewGuid(), MatchNotes = "notes" };
            SetupMatch(match);
            _emailProtector.Setup(x => x.ProtectEmailAddresses("notes", false)).Returns("protected notes");

            var model = await GetModel();

            Assert.Equal("protected notes", model.Match!.MatchNotes);
        }

        [Fact]
        public async Task Page_title_and_description_are_set()
        {
            var match = new Stoolball.Matches.Match { MatchId = Guid.NewGuid(), MatchName = "Team A v Team B" };
            SetupMatch(match);

            var model = await GetModel();

            Assert.StartsWith("Team A v Team B", model.Metadata.PageTitle);
            Assert.EndsWith(" - stoolball match", model.Metadata.PageTitle);
            Assert.False(string.IsNullOrEmpty(model.Metadata.Description));
        }

        [Theory]
        [InlineData(11, false, 10, -1)]
        [InlineData(11, false, 9, 9)]
        [InlineData(11, true, 11, -1)]
        [InlineData(11, true, 10, 10)]
        public async Task All_out_innings_have_wickets_converted_to_minus_one(int playersPerTeam, bool lastPlayerBatsOn, int wickets, int expectedWickets)
        {
            var match = new Stoolball.Matches.Match { MatchId = Guid.NewGuid(), PlayersPerTeam = playersPerTeam, LastPlayerBatsOn = lastPlayerBatsOn };
            match.MatchInnings.Add(new MatchInnings { Wickets = wickets });
            SetupMatch(match);

            var model = await GetModel();

            Assert.Equal(expectedWickets, model.Match!.MatchInnings.Single().Wickets);
        }

        [Fact]
        public async Task Innings_without_wickets_are_left_alone()
        {
            var match = new Stoolball.Matches.Match { MatchId = Guid.NewGuid(), PlayersPerTeam = 11 };
            match.MatchInnings.Add(new MatchInnings { Wickets = null });
            SetupMatch(match);

            var model = await GetModel();

            Assert.Null(model.Match!.MatchInnings.Single().Wickets);
        }

        [Fact]
        public async Task Match_in_season_has_competition_and_season_breadcrumbs()
        {
            var match = new Stoolball.Matches.Match
            {
                MatchId = Guid.NewGuid(),
                Season = new Season
                {
                    FromYear = 2020,
                    UntilYear = 2020,
                    SeasonRoute = "/competitions/example/2020",
                    Competition = new Competition { CompetitionName = "Example league", CompetitionRoute = "/competitions/example" }
                }
            };
            SetupMatch(match);

            var model = await GetModel();

            Assert.Equal(new[] { Constants.Pages.Competitions, "Example league", match.Season.SeasonName() }, model.Breadcrumbs.Skip(model.Breadcrumbs.Count - 3).Select(x => x.Name));
            Assert.Equal("/competitions/example/2020", model.Breadcrumbs.Last().Url!.ToString());
        }

        [Fact]
        public async Task Match_in_tournament_has_tournament_breadcrumbs()
        {
            var match = new Stoolball.Matches.Match
            {
                MatchId = Guid.NewGuid(),
                Tournament = new Tournament { TournamentName = "Example tournament", TournamentRoute = "/tournaments/example" }
            };
            SetupMatch(match);

            var model = await GetModel();

            Assert.Equal(new[] { Constants.Pages.Tournaments, "Example tournament" }, model.Breadcrumbs.Skip(model.Breadcrumbs.Count - 2).Select(x => x.Name));
            Assert.Equal("/tournaments/example", model.Breadcrumbs.Last().Url!.ToString());
        }

        [Fact]
        public async Task Match_not_in_season_or_tournament_has_matches_breadcrumb()
        {
            SetupMatch(new Stoolball.Matches.Match { MatchId = Guid.NewGuid() });

            var model = await GetModel();

            Assert.Equal(Constants.Pages.Matches, model.Breadcrumbs.Last().Name);
        }
    }
}
