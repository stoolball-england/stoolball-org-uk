using Stoolball.Teams;
using System;
using System.Linq;
using Xunit;

namespace Stoolball.Tests.Teams
{
    public class TeamTests : ValidationBaseTest
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

        [Fact]
        public void TeamName_is_required()
        {
            var team = new Team();

            Assert.Contains(ValidateModel(team),
                v => v.MemberNames.Contains(nameof(Team.TeamName)) &&
                     v.ErrorMessage.Contains("is required", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("https://www.notfacebook.com")]
        [InlineData("https://www.facebook.com")]
        public void Invalid_Facebook_URL_fails_validation(string url)
        {
            var team = new Team
            {
                Facebook = url
            };

            Assert.Contains(ValidateModel(team),
                v => v.MemberNames.Contains(nameof(Team.Facebook)) &&
                     v.ErrorMessage.Contains("enter a valid", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("https://www.facebook.com/example")]
        [InlineData("https://facebook.com/example")]
        [InlineData("www.facebook.com/example")]
        [InlineData("facebook.com/example")]
        public void Valid_Facebook_URL_passes_validation(string url)
        {
            var team = new Team
            {
                Facebook = url
            };

            Assert.DoesNotContain(ValidateModel(team),
                v => v.MemberNames.Contains(nameof(Team.Facebook)) &&
                     v.ErrorMessage.Contains("enter a valid", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("https://www.notyoutube.com")]
        [InlineData("https://www.youtube.com")]
        public void Invalid_YouTube_URL_fails_validation(string url)
        {
            var team = new Team
            {
                YouTube = url
            };

            Assert.Contains(ValidateModel(team),
                v => v.MemberNames.Contains(nameof(Team.YouTube)) &&
                     v.ErrorMessage.Contains("enter a valid", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("https://www.youtube.com/example")]
        [InlineData("https://youtube.com/example")]
        [InlineData("www.youtube.com/example")]
        [InlineData("youtube.com/example")]
        public void Valid_YouTube_URL_passes_validation(string url)
        {
            var team = new Team
            {
                YouTube = url
            };

            Assert.DoesNotContain(ValidateModel(team),
                v => v.MemberNames.Contains(nameof(Team.YouTube)) &&
                     v.ErrorMessage.Contains("enter a valid", StringComparison.OrdinalIgnoreCase));
        }
    }
}
