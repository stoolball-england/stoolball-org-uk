using System.Linq;
using Stoolball.Awards;
using Stoolball.Logging;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.MatchDataProviders
{
    public class MatchInThePastWithFullDetailsProviderTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        [Fact]
        public void The_match_without_a_tournament_has_an_updated_team_and_history()
        {
            var provider = new MatchInThePastWithFullDetailsProvider(
                _services.Get<MatchFactory>(), _services.Get<TeamFactory>(), _services.Get<CompetitionFactory>(),
                _services.Get<SeasonFactory>(), _services.Get<MatchLocationFactory>(), _services.Get<CommentFactory>(),
                _services.Get<OverFactory>(), _services.Get<TournamentFactory>(), _services.Get<Award>());

            var match = provider.CreateMatches(new TestData()).Single(x => x.Tournament is null);

            Assert.Equal(2020, match.Teams[0].Team!.UntilYear);
            Assert.Contains(match.History, x => x.Action == AuditAction.Create);
            Assert.Contains(match.History, x => x.Action == AuditAction.Update);
        }

        [Fact]
        public void The_match_with_a_tournament_is_not_updated()
        {
            var provider = new MatchInThePastWithFullDetailsProvider(
                _services.Get<MatchFactory>(), _services.Get<TeamFactory>(), _services.Get<CompetitionFactory>(),
                _services.Get<SeasonFactory>(), _services.Get<MatchLocationFactory>(), _services.Get<CommentFactory>(),
                _services.Get<OverFactory>(), _services.Get<TournamentFactory>(), _services.Get<Award>());

            var match = provider.CreateMatches(new TestData()).Single(x => x.Tournament is not null);

            Assert.Null(match.Teams[0].Team!.UntilYear);
            Assert.Empty(match.History);
        }
    }
}
