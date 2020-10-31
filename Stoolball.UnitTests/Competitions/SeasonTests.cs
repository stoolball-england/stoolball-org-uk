using Stoolball.Competitions;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Tests.Competitions
{
    public class SeasonTests
    {
        [Theory]
        [InlineData(2020, 2020, "2020 season")]
        [InlineData(2020, 2021, "2020/21 season")]
        public void Season_name_is_built_correctly(int fromYear, int untilYear, string expectedName)
        {
            var season = new Season { FromYear = fromYear, UntilYear = untilYear };

            var result = season.SeasonName();

            Assert.Equal(expectedName, result);
        }

        [Theory]
        [InlineData("Example competition", 2020, 2020, PlayerType.Ladies, "Example competition, 2020 season (Ladies)")]
        [InlineData("Example competition", 2020, 2021, PlayerType.Ladies, "Example competition, 2020/21 season (Ladies)")]
        [InlineData("Example ladies competition", 2020, 2020, PlayerType.Ladies, "Example ladies competition, 2020 season")]
        [InlineData("Example ladies' competition", 2020, 2021, PlayerType.Ladies, "Example ladies' competition, 2020/21 season")]
        public void Season_full_name_and_player_type_is_built_correctly(string competitionName, int fromYear, int untilYear, PlayerType playerType, string expectedName)
        {
            var season = new Season
            {
                FromYear = fromYear,
                UntilYear = untilYear,
                Competition = new Competition
                {
                    CompetitionName = competitionName,
                    PlayerType = playerType
                }
            };

            var result = season.SeasonFullNameAndPlayerType();

            Assert.Equal(result, expectedName);
        }

    }
}
