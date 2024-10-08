﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Humanizer;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class TournamentTests
    {
        [Fact]
        public void Tournament_full_name_and_player_type_throws_if_dateFormatter_is_null()
        {
            var tournament = new Tournament();

            Assert.Throws<ArgumentNullException>(() => tournament.TournamentFullNameAndPlayerType(null));
        }

        [Fact]
        public void Tournament_full_name_and_player_type_includes_player_type_if_not_in_the_name()
        {
            foreach (var playerType in (IEnumerable<PlayerType>)Enum.GetValues(typeof(PlayerType)))
            {
                var tournament = new Tournament { TournamentName = "Example tournament", PlayerType = playerType };

                var result = tournament.TournamentFullNameAndPlayerType(x => string.Empty);

                Assert.Contains($" ({playerType.Humanize(LetterCasing.Sentence)})", result);
            }
        }

        [Fact]
        public void Tournament_full_name_and_player_type_excludes_player_type_if_in_the_name()
        {
            foreach (var playerType in (IEnumerable<PlayerType>)Enum.GetValues(typeof(PlayerType)))
            {
                var tournament = new Tournament { TournamentName = $"Example {playerType.Humanize(LetterCasing.LowerCase)} tournament", PlayerType = playerType };

                var result = tournament.TournamentFullNameAndPlayerType(x => string.Empty);

                Assert.DoesNotMatch($" ({playerType.Humanize(LetterCasing.Sentence)})$", result);
            }
        }

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
            // Setting the culture changes the default behaviour of Humanizer(), rather than relying on the host machine.
            // At runtime the culture is set by web.config, but in tests we need to set it explicitly to work both locally and on Github Actions in Ubuntu.
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");

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

            Assert.Equal("Stoolball tournament in Competition one, Competition two, and Competition three.", result);
        }
    }
}
