using Stoolball.Teams;
using Xunit;

namespace Stoolball.Tests
{
    public class TeamTests
    {
        [Theory]
        [InlineData("Example", PlayerType.Ladies, "Example (Ladies)")]
        [InlineData("Example Ladies", PlayerType.Ladies, "Example Ladies")]
        [InlineData("Example Ladies' Stoolball Club", PlayerType.Ladies, "Example Ladies' Stoolball Club")]
        [InlineData("Example mixed", PlayerType.Mixed, "Example mixed")]
        [InlineData("Example mixed stoolball club", PlayerType.Mixed, "Example mixed stoolball club")]
        public void TeamNameAndPlayerType_should_not_repeat_type(string teamName, PlayerType playerType, string expected)
        {
            var team = new Team
            {
                TeamName = teamName,
                PlayerType = playerType
            };

            var result = team.TeamNameAndPlayerType();

            Assert.Equal(expected, result);
        }
    }
}
