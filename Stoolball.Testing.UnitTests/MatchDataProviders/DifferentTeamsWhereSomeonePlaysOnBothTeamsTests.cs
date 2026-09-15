using System.Linq;
using Stoolball.Matches;
using Stoolball.Testing.Factories;
using Stoolball.Testing.MatchDataProviders;
using Xunit;

namespace Stoolball.Testing.UnitTests.MatchDataProviders
{
    public class DifferentTeamsWhereSomeonePlaysOnBothTeamsTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        [Fact]
        public void One_player_bats_for_both_teams_and_also_takes_a_wicket()
        {
            var provider = new DifferentTeamsWhereSomeonePlaysOnBothTeams(_services.Get<Randomiser>(), _services.Get<MatchFactory>(), _services.Get<TeamFactory>(), _services.Get<PlayerFactory>());

            var match = provider.CreateMatches(new TestData()).Single();

            Assert.Equal(2, match.Teams.Count);
            Assert.NotEqual(match.Teams[0].Team!.TeamId, match.Teams[1].Team!.TeamId);

            var battersByTeam = match.MatchInnings.Take(2)
                .SelectMany(innings => innings.PlayerInnings.Select(pi => new { innings.BattingTeam!.Team!.TeamId, Batter = pi.Batter }))
                .GroupBy(x => x.TeamId)
                .ToList();

            Assert.Equal(2, battersByTeam.Count);

            var playerIdsPlayingForEachTeam = battersByTeam
                .Select(g => g.Select(x => x.Batter!.Player!.PlayerId).ToHashSet())
                .ToList();
            var playerOnBothTeams = playerIdsPlayingForEachTeam[0].Intersect(playerIdsPlayingForEachTeam[1]);

            Assert.Single(playerOnBothTeams);

            var wicketTaken = match.MatchInnings[0].PlayerInnings.First();
            Assert.Equal(DismissalType.CaughtAndBowled, wicketTaken.DismissalType);
            Assert.Equal(playerOnBothTeams.Single(), wicketTaken.Bowler!.Player!.PlayerId);
        }
    }
}
