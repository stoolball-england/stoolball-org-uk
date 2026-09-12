using System;
using System.Collections.Generic;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class PlayerTypeSelectorTests
    {
        private readonly PlayerTypeSelector _selector = new PlayerTypeSelector();

        [Fact]
        public void Null_match_throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _selector.SelectPlayerType(null!));
        }

        [Theory]
        [InlineData(PlayerType.Mixed)]
        [InlineData(PlayerType.Ladies)]
        [InlineData(PlayerType.Men)]
        [InlineData(PlayerType.JuniorMixed)]
        [InlineData(PlayerType.JuniorGirls)]
        [InlineData(PlayerType.JuniorBoys)]
        public void Match_with_one_team_returns_that_teams_player_type(PlayerType playerType)
        {
            var match = new Match
            {
                Teams = new List<TeamInMatch>
                {
                    new TeamInMatch { Team = new Team { PlayerType = playerType } }
                }
            };

            var result = _selector.SelectPlayerType(match);

            Assert.Equal(playerType, result);
        }

        [Theory]
        [InlineData(PlayerType.Mixed, PlayerType.Men, PlayerType.Mixed)]
        [InlineData(PlayerType.Mixed, PlayerType.Ladies, PlayerType.Mixed)]
        [InlineData(PlayerType.Mixed, PlayerType.JuniorBoys, PlayerType.Mixed)]
        [InlineData(PlayerType.Ladies, PlayerType.Men, PlayerType.Mixed)]
        [InlineData(PlayerType.Ladies, PlayerType.JuniorGirls, PlayerType.Ladies)]
        [InlineData(PlayerType.Men, PlayerType.JuniorBoys, PlayerType.Men)]
        [InlineData(PlayerType.Ladies, PlayerType.JuniorBoys, PlayerType.Mixed)]
        [InlineData(PlayerType.Men, PlayerType.JuniorGirls, PlayerType.Mixed)]
        [InlineData(PlayerType.JuniorGirls, PlayerType.JuniorBoys, PlayerType.JuniorMixed)]
        public void Match_with_two_teams_returns_expected_player_type(PlayerType homePlayerType, PlayerType awayPlayerType, PlayerType expected)
        {
            var match = new Match
            {
                Teams = new List<TeamInMatch>
                {
                    new TeamInMatch { Team = new Team { PlayerType = homePlayerType } },
                    new TeamInMatch { Team = new Team { PlayerType = awayPlayerType } }
                }
            };

            var result = _selector.SelectPlayerType(match);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Match_with_duplicate_team_player_types_returns_that_player_type()
        {
            var match = new Match
            {
                Teams = new List<TeamInMatch>
                {
                    new TeamInMatch { Team = new Team { PlayerType = PlayerType.Men } },
                    new TeamInMatch { Team = new Team { PlayerType = PlayerType.Men } }
                }
            };

            var result = _selector.SelectPlayerType(match);

            Assert.Equal(PlayerType.Men, result);
        }

        [Fact]
        public void Match_with_teams_present_but_no_team_data_falls_back_to_season_player_type()
        {
            // Teams with no Team data loaded are treated the same as having no teams at all,
            // so the season's player type is used instead.
            var match = new Match
            {
                Teams = new List<TeamInMatch>
                {
                    new TeamInMatch { Team = null },
                    new TeamInMatch { Team = null }
                },
                Season = new Season
                {
                    Competition = new Competition { PlayerType = PlayerType.Ladies }
                }
            };

            var result = _selector.SelectPlayerType(match);

            Assert.Equal(PlayerType.Ladies, result);
        }

        [Fact]
        public void Match_with_some_teams_missing_team_data_uses_the_teams_that_have_data()
        {
            var match = new Match
            {
                Teams = new List<TeamInMatch>
                {
                    new TeamInMatch { Team = new Team { PlayerType = PlayerType.Men } },
                    new TeamInMatch { Team = null }
                }
            };

            var result = _selector.SelectPlayerType(match);

            Assert.Equal(PlayerType.Men, result);
        }

        [Fact]
        public void Match_with_junior_girls_and_junior_boys_teams_defaults_to_JuniorMixed()
        {
            // No Mixed/Ladies/Men teams and no single-sex adult+junior combination is present,
            // so SelectPlayerTypeHelper falls through to its JuniorMixed default.
            var match = new Match
            {
                Teams = new List<TeamInMatch>
                {
                    new TeamInMatch { Team = new Team { PlayerType = PlayerType.JuniorGirls } },
                    new TeamInMatch { Team = new Team { PlayerType = PlayerType.JuniorBoys } }
                }
            };

            var result = _selector.SelectPlayerType(match);

            Assert.Equal(PlayerType.JuniorMixed, result);
        }

        [Fact]
        public void Tournament_with_only_junior_teams_of_both_sexes_defaults_to_JuniorMixed()
        {
            var match = new Match
            {
                Teams = new List<TeamInMatch>
                {
                    new TeamInMatch { Team = new Team { PlayerType = PlayerType.JuniorGirls } },
                    new TeamInMatch { Team = new Team { PlayerType = PlayerType.JuniorBoys } },
                    new TeamInMatch { Team = new Team { PlayerType = PlayerType.JuniorGirls } },
                    new TeamInMatch { Team = new Team { PlayerType = PlayerType.JuniorBoys } }
                }
            };

            var result = _selector.SelectPlayerType(match);

            Assert.Equal(PlayerType.JuniorMixed, result);
        }

        [Fact]
        public void Match_with_a_JuniorMixed_team_and_a_single_sex_junior_team_defaults_to_JuniorMixed()
        {
            // A team already typed JuniorMixed isn't treated as PlayerType.Mixed by the helper,
            // so this combination also falls through to the JuniorMixed default rather than
            // being caught by the explicit "mixed" check.
            var match = new Match
            {
                Teams = new List<TeamInMatch>
                {
                    new TeamInMatch { Team = new Team { PlayerType = PlayerType.JuniorMixed } },
                    new TeamInMatch { Team = new Team { PlayerType = PlayerType.JuniorGirls } }
                }
            };

            var result = _selector.SelectPlayerType(match);

            Assert.Equal(PlayerType.JuniorMixed, result);
        }

        [Fact]
        public void Match_with_no_teams_returns_season_competition_player_type()
        {
            var match = new Match
            {
                Teams = new List<TeamInMatch>(),
                Season = new Season
                {
                    Competition = new Competition { PlayerType = PlayerType.JuniorBoys }
                }
            };

            var result = _selector.SelectPlayerType(match);

            Assert.Equal(PlayerType.JuniorBoys, result);
        }

        [Fact]
        public void Match_with_no_teams_and_season_with_no_competition_returns_Mixed()
        {
            var match = new Match
            {
                Teams = new List<TeamInMatch>(),
                Season = new Season
                {
                    Competition = null
                }
            };

            var result = _selector.SelectPlayerType(match);

            Assert.Equal(PlayerType.Mixed, result);
        }

        [Fact]
        public void Match_with_no_teams_and_no_season_returns_Mixed()
        {
            var match = new Match
            {
                Teams = new List<TeamInMatch>(),
                Season = null
            };

            var result = _selector.SelectPlayerType(match);

            Assert.Equal(PlayerType.Mixed, result);
        }
    }
}
