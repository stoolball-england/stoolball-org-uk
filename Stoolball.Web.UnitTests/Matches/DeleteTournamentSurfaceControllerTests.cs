using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class DeleteTournamentSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();
        private readonly Mock<ITournamentRepository> _tournamentRepository = new();
        private readonly Mock<ICommentsDataSource<Tournament>> _commentsDataSource = new();
        private readonly Mock<IMatchListingCacheInvalidator> _cacheClearer = new();
        private readonly Mock<IAuthorizationPolicy<Tournament>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateTimeFormatter = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly List<MatchListing> _matches = [new() { MatchName = "Match one" }, new() { MatchName = "Match two" }];
        private readonly Tournament _tournament = new()
        {
            TournamentId = Guid.NewGuid(),
            TournamentName = "Example tournament",
            TournamentRoute = "/tournaments/example-tournament",
            StartTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero)
        };

        public DeleteTournamentSurfaceControllerTests()
        {
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(_tournament);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_tournament)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteTournament, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _commentsDataSource.Setup(x => x.ReadTotalComments(_tournament.TournamentId!.Value)).ReturnsAsync(4);
            _matchListingDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(_matches);
            _dateTimeFormatter.Setup(x => x.FormatDate(_tournament.StartTime, false, false, false)).Returns("1 June 2026");
        }

        private DeleteTournamentSurfaceController CreateController()
        {
            return new DeleteTournamentSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _tournamentDataSource.Object,
                _matchListingDataSource.Object,
                _tournamentRepository.Object,
                _commentsDataSource.Object,
                _cacheClearer.Object,
                _authorizationPolicy.Object,
                _dateTimeFormatter.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private void VerifyNothingDeleted()
        {
            _tournamentRepository.Verify(x => x.DeleteTournament(It.IsAny<Tournament>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCacheForTournament(It.IsAny<Tournament>()), Times.Never);
        }

        [Fact]
        public async Task Null_model_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.DeleteTournament(null!));
        }

        [Fact]
        public async Task Not_authorised_does_not_delete_and_returns_view_with_comments_matches_and_tournament_breadcrumb()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_tournament)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.DeleteTournament, false } });

            using var controller = CreateController();

            var result = await controller.DeleteTournament(new MatchingTextConfirmation());

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DeleteTournamentViewModel>(view.Model);
            Assert.Equal("DeleteTournament", view.ViewName);
            Assert.Equal(_tournament, model.Tournament);
            Assert.False(model.Deleted);
            Assert.Equal(4, model.TotalComments);
            Assert.Equal(_matches, model.Matches.Matches);
            _matchListingDataSource.Verify(x => x.ReadMatchListings(It.Is<MatchFilter>(f => f.TournamentId == _tournament.TournamentId && f.IncludeTournamentMatches && !f.IncludeTournaments), MatchSortOrder.MatchDateEarliestFirst), Times.Once);
            Assert.Equal("Delete Example tournament (Mixed), 1 June 2026", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Tournaments, "Example tournament"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
            Assert.Equal(_tournament.TournamentRoute, model.Breadcrumbs.Last().Url?.ToString());
            VerifyNothingDeleted();
        }

        [Fact]
        public async Task Invalid_ModelState_does_not_delete()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("ConfirmDeleteRequest.ConfirmationText", "Any error");

            var result = await controller.DeleteTournament(new MatchingTextConfirmation());

            var model = Assert.IsType<DeleteTournamentViewModel>(((ViewResult)result).Model);
            Assert.False(model.Deleted);
            Assert.Equal(4, model.TotalComments);
            Assert.Equal(_matches, model.Matches.Matches);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.DeleteTournament]);
            VerifyNothingDeleted();
        }

        [Fact]
        public async Task Valid_request_deletes_tournament_and_clears_cache()
        {
            using var controller = CreateController();

            await controller.DeleteTournament(new MatchingTextConfirmation());

            _tournamentRepository.Verify(x => x.DeleteTournament(_tournament, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCacheForTournament(_tournament), Times.Once);
        }

        [Fact]
        public async Task Valid_request_returns_deleted_view_model_without_tournament_breadcrumb_comments_or_matches()
        {
            using var controller = CreateController();

            var result = await controller.DeleteTournament(new MatchingTextConfirmation());

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DeleteTournamentViewModel>(view.Model);
            Assert.Equal("DeleteTournament", view.ViewName);
            Assert.True(model.Deleted);
            Assert.Equal("Delete Example tournament (Mixed), 1 June 2026", model.Metadata.PageTitle);
            Assert.Equal(Constants.Pages.Tournaments, model.Breadcrumbs.Last().Name);
            Assert.DoesNotContain(model.Breadcrumbs, x => x.Name == "Example tournament");
            _commentsDataSource.Verify(x => x.ReadTotalComments(It.IsAny<Guid>()), Times.Never);
            _matchListingDataSource.Verify(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), It.IsAny<MatchSortOrder>()), Times.Never);
        }
    }
}
