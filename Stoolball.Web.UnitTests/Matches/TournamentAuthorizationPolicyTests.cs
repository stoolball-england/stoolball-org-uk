using System;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class TournamentAuthorizationPolicyTests : MatchAuthorizationPolicyTestsBase<Tournament>
    {
        protected override IAuthorizationPolicy<Tournament> CreatePolicy()
        {
            return new TournamentAuthorizationPolicy(MemberManager.Object, MemberService.Object);
        }

        protected override Tournament CreateEntity(Guid? memberKey)
        {
            return new Tournament { MemberKey = memberKey };
        }

        protected override string EntityParameterName => "tournament";

        protected override AuthorizedAction EditAction => AuthorizedAction.EditTournament;

        protected override AuthorizedAction DeleteAction => AuthorizedAction.DeleteTournament;

        [Fact]
        public void Constructor_throws_ArgumentNullException_when_memberManager_is_null()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new TournamentAuthorizationPolicy(null!, MemberService.Object));
            Assert.Equal("memberManager", ex.ParamName);
        }

        [Fact]
        public void Constructor_throws_ArgumentNullException_when_memberService_is_null()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new TournamentAuthorizationPolicy(MemberManager.Object, null!));
            Assert.Equal("memberService", ex.ParamName);
        }
    }
}
