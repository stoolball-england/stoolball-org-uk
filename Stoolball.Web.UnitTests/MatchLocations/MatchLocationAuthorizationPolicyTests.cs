using System;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Web.MatchLocations;
using Xunit;

namespace Stoolball.Web.UnitTests.MatchLocations
{
    public class MatchLocationAuthorizationPolicyTests : AuthorizationPolicyTestsBase<MatchLocation>
    {
        protected override IAuthorizationPolicy<MatchLocation> CreatePolicy()
        {
            return new MatchLocationAuthorizationPolicy(MemberManager.Object, MemberService.Object);
        }

        protected override MatchLocation CreateEntity(string? memberGroupName)
        {
            return new MatchLocation { MemberGroupName = memberGroupName };
        }

        protected override string EntityParameterName => "matchLocation";

        protected override AuthorizedAction CreateAction => AuthorizedAction.CreateMatchLocation;

        protected override AuthorizedAction EditAction => AuthorizedAction.EditMatchLocation;

        protected override AuthorizedAction DeleteAction => AuthorizedAction.DeleteMatchLocation;

        [Fact]
        public void Constructor_throws_ArgumentNullException_when_memberManager_is_null()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new MatchLocationAuthorizationPolicy(null!, MemberService.Object));
            Assert.Equal("memberManager", ex.ParamName);
        }

        [Fact]
        public void Constructor_throws_ArgumentNullException_when_memberService_is_null()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new MatchLocationAuthorizationPolicy(MemberManager.Object, null!));
            Assert.Equal("memberService", ex.ParamName);
        }
    }
}
