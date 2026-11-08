using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class EditTournamentMatchesSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<ITournamentRepository> _tournamentRepository = new();
        private readonly Mock<IMatchListingCacheInvalidator> _cacheClearer = new();
        private readonly Mock<IAuthorizationPolicy<Tournament>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateTimeFormatter = new();
        private readonly Mock<IPostSaveRedirector> _postSaveRedirector = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs", UserName = "jo@example.org" };
        private readonly TeamInTournament _teamOne = new() { TournamentTeamId = Guid.NewGuid() };
        private readonly TeamInTournament _teamTwo = new() { TournamentTeamId = Guid.NewGuid() };
        private readonly Tournament _beforeUpdate;
        private readonly Tournament _updatedTournament = new() { TournamentRoute = "/tournaments/updated-tournament" };
        private readonly RedirectResult _redirect = new("/tournaments/updated-tournament/edit");

        public EditTournamentMatchesSurfaceControllerTests()
        {
            _beforeUpdate = new()
            {
                TournamentId = Guid.NewGuid(),
                TournamentName = "Example tournament",
                TournamentRoute = "/tournaments/example-tournament",
                StartTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
                Teams = [_teamOne, _teamTwo],
                Matches = [new MatchInTournament { MatchId = Guid.NewGuid(), MatchName = "Existing match" }]
            };

            Request.SetupGet(x => x.Form).Returns(new FormCollection(new Dictionary<string, StringValues> { { "UrlReferrer", "https://www.stoolball.org.uk/tournaments/example-tournament" } }));
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(_beforeUpdate);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTournament, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _tournamentRepository.Setup(x => x.UpdateMatches(It.IsAny<Tournament>(), _currentMember.Key, _currentMember.UserName!, _currentMember.Name!)).ReturnsAsync(_updatedTournament);
            _postSaveRedirector.Setup(x => x.WorkOutRedirect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>())).Returns(_redirect);
            _dateTimeFormatter.Setup(x => x.FormatDate(_beforeUpdate.StartTime, false, false, false)).Returns("1 June 2026");
        }

        private EditTournamentMatchesSurfaceController CreateController()
        {
            return new EditTournamentMatchesSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _tournamentDataSource.Object,
                _tournamentRepository.Object,
                _cacheClearer.Object,
                _authorizationPolicy.Object,
                _dateTimeFormatter.Object,
                _postSaveRedirector.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private static Tournament PostedTournament(params MatchInTournament[] matches) => new() { Matches = [.. matches] };

        private static MatchInTournament NewMatch(string name, params Guid?[] tournamentTeamIds) => new()
        {
            MatchName = name,
            Teams = [.. tournamentTeamIds.Select(id => new TeamInTournament { TournamentTeamId = id })]
        };

        private void VerifyNothingUpdated()
        {
            _tournamentRepository.Verify(x => x.UpdateMatches(It.IsAny<Tournament>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCacheForTournamentMatches(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task Null_tournament_removes_all_matches()
        {
            using var controller = CreateController();

            await controller.UpdateMatches(null!);

            _tournamentRepository.Verify(x => x.UpdateMatches(It.Is<Tournament>(t => t == _beforeUpdate && t.Matches.Count == 0), _currentMember.Key, _currentMember.UserName!, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Posted_matches_replace_matches_on_the_tournament_being_edited()
        {
            var posted = PostedTournament(new MatchInTournament { MatchId = Guid.NewGuid(), MatchName = "Kept match" }, NewMatch("New match", _teamOne.TournamentTeamId, _teamTwo.TournamentTeamId));

            using var controller = CreateController();

            await controller.UpdateMatches(posted);

            _tournamentRepository.Verify(x => x.UpdateMatches(It.Is<Tournament>(t => t == _beforeUpdate && t.Matches == posted.Matches), _currentMember.Key, _currentMember.UserName!, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Existing_model_state_errors_are_ignored()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("Tournament.Matches[0].MatchName", "Any error");

            var result = await controller.UpdateMatches(PostedTournament());

            Assert.Equal(_redirect, result);
            Assert.True(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Valid_request_updates_matches_clears_cache_and_redirects()
        {
            using var controller = CreateController();

            var result = await controller.UpdateMatches(PostedTournament(NewMatch("New match", _teamOne.TournamentTeamId, _teamTwo.TournamentTeamId)));

            _tournamentRepository.Verify(x => x.UpdateMatches(_beforeUpdate, _currentMember.Key, _currentMember.UserName!, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCacheForTournamentMatches(_beforeUpdate.TournamentId!.Value), Times.Once);
            _postSaveRedirector.Verify(x => x.WorkOutRedirect(
                _beforeUpdate.TournamentRoute!,
                _updatedTournament.TournamentRoute!,
                "/edit",
                "https://www.stoolball.org.uk/tournaments/example-tournament",
                "^(/tournaments/updated-tournament|/tournaments/updated-tournament/edit)$"), Times.Once);
            Assert.Equal(_redirect, result);
        }

        public static IEnumerable<object[]> MatchesWithoutTwoDifferentTeams()
        {
            yield return [Array.Empty<Guid?>()];
            yield return [new Guid?[] { Guid.NewGuid() }];
            yield return [new Guid?[] { null, Guid.NewGuid() }];
            yield return [new Guid?[] { Guid.NewGuid(), null }];
        }

        [Theory]
        [MemberData(nameof(MatchesWithoutTwoDifferentTeams))]
        public async Task Added_match_without_two_teams_is_invalid(Guid?[] tournamentTeamIds)
        {
            using var controller = CreateController();

            var result = await controller.UpdateMatches(PostedTournament(NewMatch("New match", tournamentTeamIds)));

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState[string.Empty]!.Errors, x => x.ErrorMessage == "Please remove 'New match'. A match must be between two different teams.");
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Added_match_between_a_team_and_itself_is_invalid()
        {
            using var controller = CreateController();

            var result = await controller.UpdateMatches(PostedTournament(NewMatch("New match", _teamOne.TournamentTeamId, _teamOne.TournamentTeamId)));

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState[string.Empty]!.Errors, x => x.ErrorMessage == "Please remove 'New match'. A match must be between two different teams.");
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Added_match_with_a_team_not_in_the_tournament_is_invalid()
        {
            using var controller = CreateController();

            var result = await controller.UpdateMatches(PostedTournament(NewMatch("New match", _teamOne.TournamentTeamId, Guid.NewGuid())));

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState[string.Empty]!.Errors, x => x.ErrorMessage == "Please remove 'New match'. We cannot add a match for a team that's not in the tournament.");
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Existing_matches_are_not_validated()
        {
            var existing = new MatchInTournament { MatchId = Guid.NewGuid(), MatchName = "Existing match" };

            using var controller = CreateController();

            var result = await controller.UpdateMatches(PostedTournament(existing));

            Assert.Equal(_redirect, result);
            Assert.True(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Not_authorised_returns_EditTournamentMatches_view_without_updating_matches()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTournament, false } });

            using var controller = CreateController();

            var result = await controller.UpdateMatches(PostedTournament());

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditTournamentMatches", view.ViewName);
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Invalid_request_returns_view_model_with_tournament_page_title_authorization_and_breadcrumbs()
        {
            var posted = PostedTournament(NewMatch("New match", _teamOne.TournamentTeamId));

            using var controller = CreateController();

            var result = await controller.UpdateMatches(posted);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<EditTournamentViewModel>(view.Model);
            Assert.Equal("EditTournamentMatches", view.ViewName);
            Assert.Equal(_beforeUpdate, model.Tournament);
            Assert.Equal(posted.Matches, model.Tournament!.Matches);
            Assert.Equal("Matches in the Example tournament, 1 June 2026", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTournament]);
            Assert.Equal([Constants.Pages.Tournaments, "Example tournament"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
            Assert.Equal(_beforeUpdate.TournamentRoute, model.Breadcrumbs.Last().Url?.ToString());
            _postSaveRedirector.Verify(x => x.WorkOutRedirect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }
    }
}
