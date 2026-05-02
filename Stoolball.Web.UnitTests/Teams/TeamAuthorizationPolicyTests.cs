using System;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Teams;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class TeamAuthorizationPolicyTests : AuthorizationPolicyTestsBase<Team>
    {
        protected override IAuthorizationPolicy<Team> CreatePolicy()
        {
            return new TeamAuthorizationPolicy(MemberManager.Object, MemberService.Object);
        }

        protected override Team CreateEntity(string? memberGroupName)
        {
            return new Team { MemberGroupName = memberGroupName };
        }

        protected override string EntityParameterName => "team";

        protected override AuthorizedAction CreateAction => AuthorizedAction.CreateTeam;

        protected override AuthorizedAction EditAction => AuthorizedAction.EditTeam;

        protected override AuthorizedAction DeleteAction => AuthorizedAction.DeleteTeam;

        [Fact]
        public void Constructor_throws_ArgumentNullException_when_memberManager_is_null()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new TeamAuthorizationPolicy(null!, MemberService.Object));
            Assert.Equal("memberManager", ex.ParamName);
        }

        [Fact]
        public void Constructor_throws_ArgumentNullException_when_memberService_is_null()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new TeamAuthorizationPolicy(MemberManager.Object, null!));
            Assert.Equal("memberService", ex.ParamName);
        }
    }
}
