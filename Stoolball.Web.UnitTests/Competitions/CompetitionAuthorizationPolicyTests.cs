using System;
using Stoolball.Competitions;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class CompetitionAuthorizationPolicyTests : AuthorizationPolicyTestsBase<Competition>
    {
        protected override IAuthorizationPolicy<Competition> CreatePolicy()
        {
            return new CompetitionAuthorizationPolicy(MemberManager.Object, MemberService.Object);
        }

        protected override Competition CreateEntity(string? memberGroupName)
        {
            return new Competition { MemberGroupName = memberGroupName };
        }

        protected override string EntityParameterName => "competition";

        protected override AuthorizedAction CreateAction => AuthorizedAction.CreateCompetition;

        protected override AuthorizedAction EditAction => AuthorizedAction.EditCompetition;

        protected override AuthorizedAction DeleteAction => AuthorizedAction.DeleteCompetition;

        [Fact]
        public void Constructor_throws_ArgumentNullException_when_memberManager_is_null()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new CompetitionAuthorizationPolicy(null!, MemberService.Object));
            Assert.Equal("memberManager", ex.ParamName);
        }

        [Fact]
        public void Constructor_throws_ArgumentNullException_when_memberService_is_null()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new CompetitionAuthorizationPolicy(MemberManager.Object, null!));
            Assert.Equal("memberService", ex.ParamName);
        }
    }
}
