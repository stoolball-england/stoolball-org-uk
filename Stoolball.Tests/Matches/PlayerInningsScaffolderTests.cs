using System.Collections.Generic;
using System.Linq;
using Stoolball.Matches;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Tests.Matches
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

            Assert.Equal(playersPerTeam, playerInnings.Where(x => x.PlayerIdentity.PlayerRole == PlayerRole.Player).Count());
        }

        [Fact]
        public void Byes_innings_should_be_added_at_Count_minus_4()
        {
            var playerInnings = new List<PlayerInnings>();
            var scaffolder = new PlayerInningsScaffolder();
            var playersPerTeam = 0;

            scaffolder.ScaffoldPlayerInnings(playerInnings, playersPerTeam);

            Assert.Equal(playerInnings.Count - 4, playerInnings.IndexOf(playerInnings.First(x => x.PlayerIdentity.PlayerRole == PlayerRole.Byes)));
        }

        [Fact]
        public void Wides_innings_should_be_added_at_Count_minus_3()
        {
            var playerInnings = new List<PlayerInnings>();
            var scaffolder = new PlayerInningsScaffolder();
            var playersPerTeam = 0;

            scaffolder.ScaffoldPlayerInnings(playerInnings, playersPerTeam);

            Assert.Equal(playerInnings.Count - 3, playerInnings.IndexOf(playerInnings.First(x => x.PlayerIdentity.PlayerRole == PlayerRole.Wides)));
        }

        [Fact]
        public void No_balls_innings_should_be_added_at_Count_minus_2()
        {
            var playerInnings = new List<PlayerInnings>();
            var scaffolder = new PlayerInningsScaffolder();
            var playersPerTeam = 0;

            scaffolder.ScaffoldPlayerInnings(playerInnings, playersPerTeam);

            Assert.Equal(playerInnings.Count - 2, playerInnings.IndexOf(playerInnings.First(x => x.PlayerIdentity.PlayerRole == PlayerRole.NoBalls)));
        }

        [Fact]
        public void Bonus_runs_innings_should_be_added_at_Count_minus_1()
        {
            var playerInnings = new List<PlayerInnings>();
            var scaffolder = new PlayerInningsScaffolder();
            var playersPerTeam = 0;

            scaffolder.ScaffoldPlayerInnings(playerInnings, playersPerTeam);

            Assert.Equal(playerInnings.Count - 1, playerInnings.IndexOf(playerInnings.First(x => x.PlayerIdentity.PlayerRole == PlayerRole.BonusRuns)));
        }

        [Fact]
        public void Player_innings_should_be_scaffolded_before_extras_from_blank()
        {
            var playerInnings = new List<PlayerInnings>();
            var scaffolder = new PlayerInningsScaffolder();
            var playersPerTeam = 11;

            scaffolder.ScaffoldPlayerInnings(playerInnings, playersPerTeam);

            Assert.Equal(playerInnings.IndexOf(playerInnings.Last(x => x.PlayerIdentity.PlayerRole == PlayerRole.Player)) + 1, playerInnings.IndexOf(playerInnings.First(x => x.PlayerIdentity.PlayerRole != PlayerRole.Player)));
        }


        [Fact]
        public void Player_innings_should_be_inserted_before_extras()
        {
            var playerInnings = new List<PlayerInnings>() {
                new PlayerInnings { PlayerIdentity = new PlayerIdentity { PlayerRole = PlayerRole.Player } },
                new PlayerInnings { PlayerIdentity = new PlayerIdentity { PlayerRole = PlayerRole.Player } },
                new PlayerInnings { PlayerIdentity = new PlayerIdentity { PlayerRole = PlayerRole.Player } },
                new PlayerInnings { PlayerIdentity = new PlayerIdentity { PlayerRole = PlayerRole.Player } },
                new PlayerInnings { PlayerIdentity = new PlayerIdentity { PlayerRole = PlayerRole.Byes } },
                new PlayerInnings { PlayerIdentity = new PlayerIdentity { PlayerRole = PlayerRole.Wides } },
                new PlayerInnings { PlayerIdentity = new PlayerIdentity { PlayerRole = PlayerRole.NoBalls } },
                new PlayerInnings { PlayerIdentity = new PlayerIdentity { PlayerRole = PlayerRole.BonusRuns } }
            };
            var scaffolder = new PlayerInningsScaffolder();
            var playersPerTeam = 11;

            scaffolder.ScaffoldPlayerInnings(playerInnings, playersPerTeam);

            Assert.Equal(playerInnings.IndexOf(playerInnings.Last(x => x.PlayerIdentity.PlayerRole == PlayerRole.Player)) + 1, playerInnings.IndexOf(playerInnings.First(x => x.PlayerIdentity.PlayerRole != PlayerRole.Player)));
        }
    }
}
