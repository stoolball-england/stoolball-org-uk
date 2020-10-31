using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Xunit;

namespace Stoolball.Tests.Matches
{
    public class MatchTests
    {
        [Fact]
        public void Match_description_should_include_match_type()
        {
            var match = new Match { MatchType = MatchType.LeagueMatch };

            var result = match.Description();

            Assert.Equal("Stoolball league match.", result);
        }

        [Fact]
        public void Match_description_should_include_match_type_and_location()
        {
            var match = new Match
            {
                MatchType = MatchType.LeagueMatch,
                MatchLocation = new MatchLocation { PrimaryAddressableObjectName = "Example ground", Town = "Example town" }
            };

            var result = match.Description();

            Assert.Equal("Stoolball league match at Example ground, Example town.", result);
        }

        [Fact]
        public void Match_description_should_include_match_type_and_single_competition()
        {
            var match = new Match
            {
                MatchType = MatchType.LeagueMatch,
                Season = new Season
                {
                    Competition = new Competition
                    {
                        CompetitionName = "Example competition"
                    }
                }
            };

            var result = match.Description();

            Assert.Equal("Stoolball league match in the Example competition.", result);
        }

        [Fact]
        public void Match_description_should_include_match_type_and_single_competition_handling_the()
        {
            var match = new Match
            {
                MatchType = MatchType.LeagueMatch,
                Season = new Season
                {
                    Competition = new Competition
                    {
                        CompetitionName = "The Example"
                    }
                }
            };

            var result = match.Description();

            Assert.Equal("Stoolball league match in The Example.", result);
        }
    }
}
