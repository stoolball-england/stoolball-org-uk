using Stoolball.Web.Validation;
using Xunit;

namespace Stoolball.Web.UnitTests.Validation
{
    public class SentenceCaseTests
    {
        [Theory]
        [InlineData("Match date", "match date")]
        [InlineData("Name", "name")]
        [InlineData("Contact details for Stoolball England", "contact details for Stoolball England")]
        [InlineData("already lower case", "already lower case")]
        public void ToMidSentence_lower_cases_the_first_letter(string displayName, string expected)
        {
            Assert.Equal(expected, SentenceCase.ToMidSentence(displayName));
        }

        [Theory]
        [InlineData("Twitter account")]
        [InlineData("Facebook page or group")]
        [InlineData("Instagram account")]
        [InlineData("Stoolball England contact")]
        [InlineData("YouTube channel")]
        [InlineData("URL")]
        public void ToMidSentence_keeps_proper_nouns_and_brand_names(string displayName)
        {
            Assert.Equal(displayName, SentenceCase.ToMidSentence(displayName));
        }

        [Fact]
        public void ToMidSentence_does_not_treat_Stoolball_alone_as_a_proper_noun()
        {
            Assert.Equal("stoolball team", SentenceCase.ToMidSentence("Stoolball team"));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void ToMidSentence_handles_missing_names(string? displayName, string expected)
        {
            Assert.Equal(expected, SentenceCase.ToMidSentence(displayName));
        }
    }
}
