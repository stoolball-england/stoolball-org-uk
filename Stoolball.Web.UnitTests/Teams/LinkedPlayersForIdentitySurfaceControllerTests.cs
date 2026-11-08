using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Data.Abstractions.Models;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Web.Statistics.Models;
using Stoolball.Web.Teams;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class LinkedPlayersForIdentitySurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IPlayerDataSource> _playerDataSource = new();
        private readonly Mock<IPlayerRepository> _playerRepository = new();
        private readonly Mock<IPlayerCacheInvalidator> _playerCacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Team _team = new() { TeamId = Guid.NewGuid(), TeamRoute = "/teams/example", MemberGroupName = "team/example" };
        private readonly Player _player = new() { PlayerId = Guid.NewGuid(), PlayerRoute = "/players/target" };
        private readonly PlayerIdentity _contextIdentity;

        public LinkedPlayersForIdentitySurfaceControllerTests()
        {
            _contextIdentity = new PlayerIdentity
            {
                PlayerIdentityId = Guid.NewGuid(),
                PlayerIdentityName = "John Smith",
                LinkedBy = PlayerIdentityLinkedBy.DefaultIdentity,
                Team = new Team { TeamRoute = _team.TeamRoute },
                Player = _player
            };
            _player.PlayerIdentities.Add(_contextIdentity);

            _playerDataSource.Setup(x => x.ReadPlayerIdentityByRoute(It.IsAny<string>())).ReturnsAsync(_contextIdentity);
            _playerDataSource.Setup(x => x.ReadPlayerByRoute(_player.PlayerRoute, null)).ReturnsAsync(_player);
            _teamDataSource.Setup(x => x.ReadTeamByRoute(_team.TeamRoute, false)).ReturnsAsync(_team);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_team)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _memberManager.Setup(x => x.IsMemberAuthorizedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<int>>())).ReturnsAsync(false);
        }

        private LinkedPlayersForIdentitySurfaceController CreateController()
        {
            return new LinkedPlayersForIdentitySurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext.CreatePartial(memberService: MemberService.Object, userService: Mock.Of<IUserService>(), localizationService: Mock.Of<ILocalizationService>()),
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _authorizationPolicy.Object,
                _teamDataSource.Object,
                _playerDataSource.Object,
                _playerRepository.Object,
                _playerCacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private PlayerIdentity AddLinkedIdentity(PlayerIdentityLinkedBy linkedBy)
        {
            var identity = new PlayerIdentity
            {
                PlayerIdentityId = Guid.NewGuid(),
                PlayerIdentityName = "Linked " + Guid.NewGuid(),
                LinkedBy = linkedBy,
                Team = _team,
                Player = _player
            };
            _player.PlayerIdentities.Add(identity);
            return identity;
        }

        private static PlayerIdentity SourceIdentity(Player? sourcePlayer = null)
        {
            return new PlayerIdentity
            {
                PlayerIdentityId = Guid.NewGuid(),
                Player = sourcePlayer ?? new Player { PlayerId = Guid.NewGuid(), PlayerRoute = "/players/source" }
            };
        }

        private static LinkedPlayersFormData FormData(params PlayerIdentity[] identities)
        {
            return new LinkedPlayersFormData { PlayerIdentities = identities.ToList() };
        }

        private static LinkPlayersResult LinkResult(Player source)
        {
            return new LinkPlayersResult
            {
                PlayerIdForSourcePlayer = source.PlayerId!.Value,
                PreviousMemberKeyForSourcePlayer = Guid.NewGuid(),
                PreviousRouteForSourcePlayer = source.PlayerRoute!,
                PreviousRouteForTargetPlayer = "/players/target",
                NewRouteForTargetPlayer = "/players/target"
            };
        }

        private void VerifyNothingChanged()
        {
            _playerRepository.Verify(x => x.LinkPlayers(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<PlayerIdentityLinkedBy>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _playerRepository.Verify(x => x.UnlinkPlayerIdentity(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _playerCacheClearer.Verify(x => x.InvalidateCacheForPlayer(It.IsAny<Player>()), Times.Never);
            _playerCacheClearer.Verify(x => x.InvalidateCacheForTeams(It.IsAny<Team[]>()), Times.Never);
        }

        [Fact]
        public async Task Route_not_matching_identity_returns_404()
        {
            _playerDataSource.Setup(x => x.ReadPlayerIdentityByRoute(It.IsAny<string>())).Returns(Task.FromResult<PlayerIdentity?>(null));

            using var controller = CreateController();

            var result = await controller.UpdateLinkedPlayers(FormData());

            Assert.IsType<NotFoundResult>(result);
            VerifyNothingChanged();
        }

        [Fact]
        public async Task Identity_without_team_returns_404()
        {
            _contextIdentity.Team = null;

            using var controller = CreateController();

            var result = await controller.UpdateLinkedPlayers(FormData());

            Assert.IsType<NotFoundResult>(result);
            VerifyNothingChanged();
        }

        [Fact]
        public async Task Identity_without_id_returns_404()
        {
            _contextIdentity.PlayerIdentityId = null;

            using var controller = CreateController();

            var result = await controller.UpdateLinkedPlayers(FormData());

            Assert.IsType<NotFoundResult>(result);
            VerifyNothingChanged();
        }

        [Fact]
        public async Task Identity_without_player_returns_404()
        {
            _contextIdentity.Player = null;

            using var controller = CreateController();

            var result = await controller.UpdateLinkedPlayers(FormData());

            Assert.IsType<NotFoundResult>(result);
            VerifyNothingChanged();
        }

        [Fact]
        public async Task Team_not_found_returns_404()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(_team.TeamRoute!, false)).Returns(Task.FromResult<Team?>(null));

            using var controller = CreateController();

            var result = await controller.UpdateLinkedPlayers(FormData());

            Assert.IsType<NotFoundResult>(result);
            VerifyNothingChanged();
        }

        [Fact]
        public async Task Not_authorised_to_edit_team_is_forbidden()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_team)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, false } });

            using var controller = CreateController();

            var result = await controller.UpdateLinkedPlayers(FormData(SourceIdentity()));

            Assert.IsType<ForbidResult>(result);
            VerifyNothingChanged();
        }

        [Fact]
        public async Task Player_linked_to_a_different_member_is_forbidden()
        {
            _player.MemberKey = Guid.NewGuid();

            using var controller = CreateController();

            var result = await controller.UpdateLinkedPlayers(FormData(SourceIdentity()));

            Assert.IsType<ForbidResult>(result);
            VerifyNothingChanged();
        }

        [Fact]
        public async Task Player_linked_to_the_current_member_is_allowed()
        {
            _player.MemberKey = _currentMember.Key;

            using var controller = CreateController();

            var result = await controller.UpdateLinkedPlayers(FormData());

            Assert.IsType<RedirectResult>(result);
        }

        [Fact]
        public async Task Unchanged_identities_redirect_to_edit_players_without_changes_or_cache_invalidation()
        {
            using var controller = CreateController();

            var result = await controller.UpdateLinkedPlayers(FormData(_contextIdentity));

            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/teams/example/edit/players", redirect.Url);
            VerifyNothingChanged();
        }

        [Fact]
        public async Task New_identity_is_linked_to_the_target_player_using_stoolball_england_role_for_non_owner()
        {
            var source = SourceIdentity();
            _playerRepository.Setup(x => x.LinkPlayers(_player.PlayerId!.Value, source.Player!.PlayerId!.Value, It.IsAny<PlayerIdentityLinkedBy>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(LinkResult(source.Player));

            using var controller = CreateController();

            var result = await controller.UpdateLinkedPlayers(FormData(_contextIdentity, source));

            _playerRepository.Verify(x => x.LinkPlayers(_player.PlayerId!.Value, source.Player.PlayerId!.Value, PlayerIdentityLinkedBy.StoolballEngland, _currentMember.Key, _currentMember.Name!), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/teams/example/edit/players", redirect.Url);
        }

        [Fact]
        public async Task New_identity_is_linked_using_team_role_for_team_owner()
        {
            _memberManager.Setup(x => x.IsMemberAuthorizedAsync(It.IsAny<IEnumerable<string>>(), It.Is<IEnumerable<string>>(g => g.Contains("team/example")), It.IsAny<IEnumerable<int>>())).ReturnsAsync(true);
            var source = SourceIdentity();
            _playerRepository.Setup(x => x.LinkPlayers(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<PlayerIdentityLinkedBy>(), It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(LinkResult(source.Player!));

            using var controller = CreateController();

            await controller.UpdateLinkedPlayers(FormData(_contextIdentity, source));

            _playerRepository.Verify(x => x.LinkPlayers(_player.PlayerId!.Value, source.Player!.PlayerId!.Value, PlayerIdentityLinkedBy.Team, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Several_new_identities_from_the_same_player_are_linked_once()
        {
            var sourcePlayer = new Player { PlayerId = Guid.NewGuid(), PlayerRoute = "/players/source" };
            var first = SourceIdentity(sourcePlayer);
            var second = SourceIdentity(sourcePlayer);
            _playerRepository.Setup(x => x.LinkPlayers(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<PlayerIdentityLinkedBy>(), It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(LinkResult(sourcePlayer));

            using var controller = CreateController();

            await controller.UpdateLinkedPlayers(FormData(_contextIdentity, first, second));

            _playerRepository.Verify(x => x.LinkPlayers(_player.PlayerId!.Value, sourcePlayer.PlayerId!.Value, It.IsAny<PlayerIdentityLinkedBy>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Linking_clears_cache_for_target_player_source_player_and_team_player_listings()
        {
            var source = SourceIdentity();
            var linkResult = LinkResult(source.Player!);
            _playerRepository.Setup(x => x.LinkPlayers(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<PlayerIdentityLinkedBy>(), It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(linkResult);

            using var controller = CreateController();

            await controller.UpdateLinkedPlayers(FormData(_contextIdentity, source));

            _playerCacheClearer.Verify(x => x.InvalidateCacheForPlayer(_player), Times.Once);
            _playerCacheClearer.Verify(x => x.InvalidateCacheForPlayer(It.Is<Player>(p => p.PlayerId == linkResult.PlayerIdForSourcePlayer && p.MemberKey == linkResult.PreviousMemberKeyForSourcePlayer && p.PlayerRoute == linkResult.PreviousRouteForSourcePlayer)), Times.Once);
            _playerCacheClearer.Verify(x => x.InvalidateCacheForTeams(It.Is<Team[]>(teams => teams.Length == 1 && teams[0] == _team)), Times.Once);
        }

        [Fact]
        public async Task Source_player_cache_is_cleared_once_when_several_identities_are_linked_from_it()
        {
            var sourcePlayer = new Player { PlayerId = Guid.NewGuid(), PlayerRoute = "/players/source" };
            _playerRepository.Setup(x => x.LinkPlayers(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<PlayerIdentityLinkedBy>(), It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(LinkResult(sourcePlayer));

            using var controller = CreateController();

            await controller.UpdateLinkedPlayers(FormData(_contextIdentity, SourceIdentity(sourcePlayer), SourceIdentity(sourcePlayer)));

            _playerCacheClearer.Verify(x => x.InvalidateCacheForPlayer(It.Is<Player>(p => p.PlayerId == sourcePlayer.PlayerId)), Times.Once);
        }

        [Fact]
        public async Task Identity_linked_by_team_that_is_no_longer_submitted_is_unlinked_and_cache_cleared()
        {
            var removed = AddLinkedIdentity(PlayerIdentityLinkedBy.Team);

            using var controller = CreateController();

            var result = await controller.UpdateLinkedPlayers(FormData(_contextIdentity));

            _playerRepository.Verify(x => x.UnlinkPlayerIdentity(removed.PlayerIdentityId!.Value, _currentMember.Key, _currentMember.Name!), Times.Once);
            _playerRepository.Verify(x => x.LinkPlayers(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<PlayerIdentityLinkedBy>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _playerCacheClearer.Verify(x => x.InvalidateCacheForPlayer(_player), Times.Once);
            _playerCacheClearer.Verify(x => x.InvalidateCacheForTeams(It.IsAny<Team[]>()), Times.Once);
            Assert.IsType<RedirectResult>(result);
        }

        [Fact]
        public async Task Identity_linked_by_team_that_is_still_submitted_is_kept()
        {
            var kept = AddLinkedIdentity(PlayerIdentityLinkedBy.Team);

            using var controller = CreateController();

            await controller.UpdateLinkedPlayers(FormData(_contextIdentity, kept));

            VerifyNothingChanged();
        }

        [Theory]
        [InlineData(PlayerIdentityLinkedBy.Member)]
        [InlineData(PlayerIdentityLinkedBy.StoolballEngland)]
        [InlineData(PlayerIdentityLinkedBy.DefaultIdentity)]
        public async Task Identity_not_linked_by_team_is_not_unlinked_even_if_not_submitted(PlayerIdentityLinkedBy linkedBy)
        {
            AddLinkedIdentity(linkedBy);

            using var controller = CreateController();

            await controller.UpdateLinkedPlayers(FormData(_contextIdentity));

            VerifyNothingChanged();
        }

        [Fact]
        public async Task Context_identity_is_never_unlinked_even_if_not_submitted()
        {
            _contextIdentity.LinkedBy = PlayerIdentityLinkedBy.Team;

            using var controller = CreateController();

            await controller.UpdateLinkedPlayers(FormData());

            VerifyNothingChanged();
        }
    }
}
