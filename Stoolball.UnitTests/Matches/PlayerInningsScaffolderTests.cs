using System.Collections.Generic;
using Stoolball.Matches;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class PlayerInningsScaffolderTests
    {
        [Fact]
        public void Player_innings_should_equal_PlayersPerTeam()
        {
            var playerInnings = new List<PlayerInnings>();
            var scaffolder = new PlayerInningsScaffolder();
            var playersPerTeam = 11;

            scaffolder.ScaffoldPlayerInnings(playerInnings, playersPerTeam);

            Assert.Equal(playersPerTeam, playerInnings.Count);
        }
    }
}
