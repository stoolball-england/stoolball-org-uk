using Humanizer.Configuration;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Stoolball.Tests.Matches
{
    public class TournamentTests
    {
        [Fact]
        public void Tournament_description_should_include_match_type()
        {
            var tournament = new Tournament();

            var result = tournament.Description();

            Assert.Equal("Stoolball tournament.", result);
        }

        [Fact]
        public void Tournament_description_should_include_match_type_and_location()
        {
            var tournament = new Tournament
            {
                TournamentLocation = new MatchLocation { PrimaryAddressableObjectName = "Example ground", Town = "Example town" }
            };

            var result = tournament.Description();

            Assert.Equal("Stoolball tournament at Example ground, Example town.", result);
        }

        [Fact]
        public void Tournament_description_should_include_match_type_and_single_competition()
        {
            var tournament = new Tournament
            {
                Seasons = new List<Season>{
                    new Season
                    {
                        Competition = new Competition
                        {
                            CompetitionName = "Example competition"
                        }
                    }
                }
            };

            var result = tournament.Description();

            Assert.Equal("Stoolball tournament in the Example competition.", result);
        }

        [Fact]
        public void Tournament_description_should_include_match_type_and_single_competition_handling_the()
        {
            var tournament = new Tournament
            {
                Seasons = new List<Season>{
                    new Season
                    {
                        Competition = new Competition
                        {
                            CompetitionName = "The Example"
                        }
                    }
                }
            };

            var result = tournament.Description();

            Assert.Equal("Stoolball tournament in The Example.", result);
        }

        [Fact]
        public void Tournament_description_should_include_match_type_and_multiple_competitions()
        {
            Configurator.CollectionFormatters.Register(CultureInfo.CurrentCulture.Name, new HumanizerCollectionGrammar());
            var tournament = new Tournament
            {
                Seasons = new List<Season>{
                    new Season
                    {
                        Competition = new Competition
                        {
                            CompetitionName = "Competition one"
                        }
                    },
                    new Season
                    {
                        Competition = new Competition
                        {
                            CompetitionName = "Competition two"
                        }
                    },
                    new Season
                    {
                        Competition = new Competition
                        {
                            CompetitionName = "Competition three"
                        }
                    }
                }
            };

            var result = tournament.Description();

            Assert.Equal("Stoolball tournament in Competition one, Competition two and Competition three.", result);
        }
    }
}
