using System;
using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Testing.Factories;
using Xunit;

namespace Stoolball.Testing.UnitTests.Factories
{
    public class OverFactoryTests
    {
        private readonly OverFactory _overFactory = new(new OversHelper());

        [Fact]
        public void Over_exists_with_only_a_bowler_name()
        {
            var team = new TeamFactory().CreateFaker().Generate();
            var bowlingTeam = new PlayerFactory().CreatePlayerIdentityFaker(team).Generate(11);

            var overs = _overFactory.CreateOversBowledIncludingOneWithOnlyName(bowlingTeam, [new OverSet { OverSetNumber = 1, Overs = 5, BallsPerOver = 8 }]);

            Assert.Contains(overs, x => x.Bowler != null && x.BallsBowled == null && x.NoBalls == null && x.Wides == null && x.RunsConceded == null);
        }

        [Fact]
        public void Throws_ArgumentException_when_bowling_team_has_fewer_than_two_players()
        {
            var team = new TeamFactory().CreateFaker().Generate();
            var bowlingTeam = new PlayerFactory().CreatePlayerIdentityFaker(team).Generate(1);

            Assert.Throws<ArgumentException>(() => _overFactory.CreateFaker(bowlingTeam, [new OverSet { OverSetNumber = 1, Overs = 5, BallsPerOver = 8 }]));
        }

        [Fact]
        public void Overs_are_bowled_by_two_different_bowlers_from_the_bowling_team()
        {
            var team = new TeamFactory().CreateFaker().Generate();
            var bowlingTeam = new PlayerFactory().CreatePlayerIdentityFaker(team).Generate(11);

            var overs = _overFactory.CreateOversBowledIncludingOneWithOnlyName(bowlingTeam, [new OverSet { OverSetNumber = 1, Overs = 5, BallsPerOver = 8 }]);

            var bowlers = new HashSet<Guid?>();
            foreach (var over in overs)
            {
                bowlers.Add(over.Bowler?.PlayerIdentityId);
                Assert.Contains(over.Bowler, bowlingTeam);
            }
            Assert.Equal(2, bowlers.Count);
        }
    }
}
