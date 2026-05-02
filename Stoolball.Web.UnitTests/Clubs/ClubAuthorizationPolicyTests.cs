using System;
using Stoolball.Clubs;
using Stoolball.Security;
using Stoolball.Web.Clubs;
using Xunit;

namespace Stoolball.Web.UnitTests.Clubs
{
    public class ClubAuthorizationPolicyTests : AuthorizationPolicyTestsBase<Club>
    {
        protected override IAuthorizationPolicy<Club> CreatePolicy()
        {
            return new ClubAuthorizationPolicy(MemberManager.Object, MemberService.Object);
        }

        protected override Club CreateEntity(string? memberGroupName)
        {
            return new Club { MemberGroupName = memberGroupName };
        }

        protected override string EntityParameterName => "club";

        protected override AuthorizedAction CreateAction => AuthorizedAction.CreateClub;

        protected override AuthorizedAction EditAction => AuthorizedAction.EditClub;

        protected override AuthorizedAction DeleteAction => AuthorizedAction.DeleteClub;

        [Fact]
        public void Constructor_throws_ArgumentNullException_when_memberManager_is_null()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new ClubAuthorizationPolicy(null!, MemberService.Object));
            Assert.Equal("memberManager", ex.ParamName);
        }

        [Fact]
        public void Constructor_throws_ArgumentNullException_when_memberService_is_null()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new ClubAuthorizationPolicy(MemberManager.Object, null!));
            Assert.Equal("memberService", ex.ParamName);
        }
    }
}
