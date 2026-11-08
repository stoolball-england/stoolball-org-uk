using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class EditTournamentSeasonsSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<ITournamentRepository> _tournamentRepository = new();
        private readonly Mock<IMatchListingCacheInvalidator> _cacheClearer = new();
        private readonly Mock<IAuthorizationPolicy<Tournament>> _authorizationPolicy = new();
        private readonly Mock<IPostSaveRedirector> _postSaveRedirector = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Dictionary<string, StringValues> _form = new() { { "UrlReferrer", "https://www.stoolball.org.uk/tournaments/example-tournament" } };
        private readonly Tournament _beforeUpdate = new()
        {
            TournamentId = Guid.NewGuid(),
            TournamentName = "Example tournament",
            TournamentRoute = "/tournaments/example-tournament",
            MemberKey = Guid.NewGuid()
        };
        private readonly Tournament _updatedTournament = new() { TournamentRoute = "/tournaments/example-tournament" };
        private readonly RedirectResult _redirect = new("/tournaments/example-tournament/edit");

        public EditTournamentSeasonsSurfaceControllerTests()
        {
            Request.SetupGet(x => x.Form).Returns(() => new FormCollection(_form));
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(_beforeUpdate);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTournament, true } });
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.Is<Tournament>(t => t != _beforeUpdate))).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTournament, false } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _tournamentRepository.Setup(x => x.UpdateSeasons(It.IsAny<Tournament>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_updatedTournament);
            _postSaveRedirector.Setup(x => x.WorkOutRedirect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>())).Returns(_redirect);
        }

        private EditTournamentSeasonsSurfaceController CreateController()
        {
            return new EditTournamentSeasonsSurfaceController(
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
                _postSaveRedirector.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Valid_request_updates_seasons_from_form_clears_cache_and_redirects()
        {
            var seasonIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
            _form["Tournament.Seasons"] = string.Join(",", seasonIds);

            using var controller = CreateController();

            var result = await controller.UpdateSeasons();

            _tournamentRepository.Verify(x => x.UpdateSeasons(It.Is<Tournament>(t =>
                t.TournamentId == _beforeUpdate.TournamentId &&
                t.TournamentRoute == _beforeUpdate.TournamentRoute &&
                t.Seasons.Select(s => s.SeasonId!.Value).SequenceEqual(seasonIds)
                ), _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCacheForTournament(_beforeUpdate, _updatedTournament), Times.Once);
            Assert.Equal(_redirect, result);
        }

        [Fact]
        public async Task Multiple_form_values_for_seasons_are_all_read()
        {
            var seasonIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
            _form["Tournament.Seasons"] = new StringValues([seasonIds[0].ToString(), seasonIds[1].ToString()]);

            using var controller = CreateController();

            await controller.UpdateSeasons();

            _tournamentRepository.Verify(x => x.UpdateSeasons(It.Is<Tournament>(t => t.Seasons.Select(s => s.SeasonId!.Value).SequenceEqual(seasonIds)), _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task No_seasons_in_form_removes_all_seasons()
        {
            using var controller = CreateController();

            await controller.UpdateSeasons();

            _tournamentRepository.Verify(x => x.UpdateSeasons(It.Is<Tournament>(t => t.TournamentId == _beforeUpdate.TournamentId && t.Seasons.Count == 0), _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Authorization_is_checked_against_the_tournament_being_edited()
        {
            using var controller = CreateController();

            await controller.UpdateSeasons();

            _authorizationPolicy.Verify(x => x.IsAuthorized(_beforeUpdate), Times.Once);
            _tournamentRepository.Verify(x => x.UpdateSeasons(It.IsAny<Tournament>(), _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Not_authorised_does_not_update_seasons_but_still_redirects()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTournament, false } });

            using var controller = CreateController();

            var result = await controller.UpdateSeasons();

            _tournamentRepository.Verify(x => x.UpdateSeasons(It.IsAny<Tournament>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCacheForTournament(It.IsAny<Tournament>(), It.IsAny<Tournament>()), Times.Never);
            Assert.Equal(_redirect, result);
        }

        [Fact]
        public async Task Redirects_using_referrer_from_form_back_to_edit_tournament_by_default()
        {
            using var controller = CreateController();

            await controller.UpdateSeasons();

            _postSaveRedirector.Verify(x => x.WorkOutRedirect(
                _beforeUpdate.TournamentRoute!,
                _beforeUpdate.TournamentRoute!,
                "/edit",
                "https://www.stoolball.org.uk/tournaments/example-tournament",
                null), Times.Once);
        }
    }
}
