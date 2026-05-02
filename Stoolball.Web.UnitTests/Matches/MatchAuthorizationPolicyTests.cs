using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Umbraco.Cms.Core.Security;
using Xunit;
using Match = Stoolball.Matches.Match;

namespace Stoolball.Web.UnitTests.Matches
{
    public class MatchAuthorizationPolicyTests : MatchAuthorizationPolicyTestsBase<Match>
    {
        protected override IAuthorizationPolicy<Match> CreatePolicy()
        {
            return new MatchAuthorizationPolicy(MemberManager.Object, MemberService.Object);
        }

        protected override Match CreateEntity(Guid? memberKey)
        {
            return new Match { MemberKey = memberKey };
        }

        protected override string EntityParameterName => "match";

        protected override AuthorizedAction EditAction => AuthorizedAction.EditMatch;

        protected override AuthorizedAction DeleteAction => AuthorizedAction.DeleteMatch;

        [Fact]
        public void Constructor_throws_ArgumentNullException_when_memberManager_is_null()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new MatchAuthorizationPolicy(null!, MemberService.Object));
            Assert.Equal("memberManager", ex.ParamName);
        }

        [Fact]
        public void Constructor_throws_ArgumentNullException_when_memberService_is_null()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new MatchAuthorizationPolicy(MemberManager.Object, null!));
            Assert.Equal("memberService", ex.ParamName);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsAuthorized_EditMatchResult_equals_IsLoggedIn(bool isLoggedIn)
        {
            var policy = CreatePolicy();
            var match = CreateEntity(null);

            MemberManager.Setup(x => x.IsLoggedIn()).Returns(isLoggedIn);
            MemberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult<MemberIdentityUser?>(null));

            var result = await policy.IsAuthorized(match);

            Assert.Equal(isLoggedIn, result[AuthorizedAction.EditMatchResult]);
        }

        [Fact]
        public void AuthorizedGroupNames_returns_competition_member_group_names()
        {
            var policy = CreatePolicy();
            var match = new Match();
            var season = new Season { Competition = new Competition { MemberGroupName = "CompetitionManagers" } };
            match.Season = season;

            var result = policy.AuthorizedGroupNames(match);

            Assert.Contains("CompetitionManagers", result);
        }

        [Fact]
        public void AuthorizedGroupNames_returns_team_group_names()
        {
            var policy = CreatePolicy();
            var match = new Match();
            var team = new Team { MemberGroupName = "TeamManagers" };
            var teamInMatch = new TeamInMatch { Team = team };
            match.Teams.Add(teamInMatch);

            var result = policy.AuthorizedGroupNames(match);

            Assert.Contains("TeamManagers", result);
        }

        [Fact]
        public void AuthorizedGroupNames_returns_empty_list_when_no_groups()
        {
            var policy = CreatePolicy();
            var match = new Match();

            var result = policy.AuthorizedGroupNames(match);

            Assert.Empty(result);
        }

        [Theory]
        [InlineData(AuthorizedAction.EditMatch, true)]
        [InlineData(AuthorizedAction.EditMatch, false)]
        [InlineData(AuthorizedAction.DeleteMatch, true)]
        [InlineData(AuthorizedAction.DeleteMatch, false)]
        public async Task IsAuthorized_returns_specified_permission_when_member_in_group_from_MemberGroupNames(AuthorizedAction action, bool isMemberInGroup)
        {
            var policy = CreatePolicy();
            var match = new Match();
            var teamGroupName = "TeamManagers";
            var competitionGroupName = "CompetitionManagers";
            var team = new Team { MemberGroupName = teamGroupName };
            var teamInMatch = new TeamInMatch { Team = team };
            match.Teams.Add(teamInMatch);
            var season = new Season { Competition = new Competition { MemberGroupName = competitionGroupName } };
            match.Season = season;
            var currentMember = new MemberIdentityUser { Key = Guid.NewGuid() };

            MemberManager.Setup(x => x.IsLoggedIn()).Returns(true);
            MemberManager.Setup(x => x.GetCurrentMemberAsync()).Returns(Task.FromResult<MemberIdentityUser?>(currentMember));
            MemberManager.Setup(x => x.IsMemberAuthorizedAsync(null, It.Is<IEnumerable<string>>(groups =>
                groups.ToList().Contains(teamGroupName) && groups.ToList().Contains(competitionGroupName)), null))
                .Returns(Task.FromResult(isMemberInGroup));

            var result = await policy.IsAuthorized(match);

            Assert.Equal(isMemberInGroup, result[action]);
        }
    }
}
