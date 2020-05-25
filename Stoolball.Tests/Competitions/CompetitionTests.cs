using Stoolball.Competitions;
using System;
using System.Linq;
using Xunit;

namespace Stoolball.Tests.Competitions
{
    public class CompetitionTests : ValidationBaseTest
    {
        [Fact]
        public void CompetitionName_is_required()
        {
            var competition = new Competition();

            Assert.Contains(ValidateModel(competition),
                v => v.MemberNames.Contains(nameof(Competition.CompetitionName)) &&
                     v.ErrorMessage.Contains("is required", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("https://www.notfacebook.com")]
        [InlineData("https://www.facebook.com")]
        public void Invalid_Facebook_URL_fails_validation(string url)
        {
            var competition = new Competition
            {
                Facebook = url
            };

            Assert.Contains(ValidateModel(competition),
                v => v.MemberNames.Contains(nameof(Competition.Facebook)) &&
                     v.ErrorMessage.Contains("enter a valid", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("https://www.facebook.com/example")]
        [InlineData("https://facebook.com/example")]
        [InlineData("www.facebook.com/example")]
        [InlineData("facebook.com/example")]
        public void Valid_Facebook_URL_passes_validation(string url)
        {
            var competition = new Competition
            {
                Facebook = url
            };

            Assert.DoesNotContain(ValidateModel(competition),
                v => v.MemberNames.Contains(nameof(Competition.Facebook)) &&
                     v.ErrorMessage.Contains("enter a valid", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("https://www.notyoutube.com")]
        [InlineData("https://www.youtube.com")]
        public void Invalid_YouTube_URL_fails_validation(string url)
        {
            var competition = new Competition
            {
                YouTube = url
            };

            Assert.Contains(ValidateModel(competition),
                v => v.MemberNames.Contains(nameof(Competition.YouTube)) &&
                     v.ErrorMessage.Contains("enter a valid", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("https://www.youtube.com/example")]
        [InlineData("https://youtube.com/example")]
        [InlineData("www.youtube.com/example")]
        [InlineData("youtube.com/example")]
        public void Valid_YouTube_URL_passes_validation(string url)
        {
            var competition = new Competition
            {
                YouTube = url
            };

            Assert.DoesNotContain(ValidateModel(competition),
                            v => v.MemberNames.Contains(nameof(Competition.YouTube)) &&
                                 v.ErrorMessage.Contains("enter a valid", StringComparison.OrdinalIgnoreCase));
        }
    }
}
