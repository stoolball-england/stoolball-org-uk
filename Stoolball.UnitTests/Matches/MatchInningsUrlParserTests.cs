using System;
using Stoolball.Matches;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class MatchInningsUrlParserTests
    {
        [Theory]
        [InlineData("https://example.com/matches/example/innings/1/page", 1)]
        [InlineData("https://example.com/matches/example/INNINGS/12/page", 12)]
        [InlineData("/matches/example/innings/3/page", 3)]
        public void Parses_valid_innings(string url, int expected)
        {
            var parser = new MatchInningsUrlParser();

            var result = parser.ParseInningsOrderInMatchFromUrl(new Uri(url, UriKind.RelativeOrAbsolute));

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("https://example.com/matches/example/")]
        [InlineData("https://example.com/matches/example/innings/invalid/page")]
        [InlineData("https://example.com/matches/example/innings/0/page")]
        [InlineData("https://example.com/matches/example/innings/-1/page")]
        public void Invalid_URL_returns_null(string url)
        {
            var parser = new MatchInningsUrlParser();

            var result = parser.ParseInningsOrderInMatchFromUrl(url != null ? new Uri(url, UriKind.RelativeOrAbsolute) : null);

            Assert.Null(result);
        }
    }
}
