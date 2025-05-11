using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Testing;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.SqlServerMatchRepositoryTests
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class CreateMatchTests : MatchRepositoryTestsBase, IDisposable
    {
        public CreateMatchTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task Throws_ArgumentNullException_if_match_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateMatch(null!, MemberKey, MemberName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Throws_ArgumentNullException_if_memberName_is_null_or_whitespace(string? memberName)
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateMatch(new Stoolball.Matches.Match { StartTime = DateTimeOffset.UtcNow }, MemberKey, memberName!));
        }

        [Fact]
        public async Task Throws_ArgumentNullException_if_transaction_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.CreateMatch(new Stoolball.Matches.Match { StartTime = DateTimeOffset.UtcNow }, MemberKey, MemberName, null!));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Throws_ArgumentException_if_a_team_does_not_have_a_TeamId(bool teamIsNull)
        {
            var repo = CreateRepository();
            var match = new Stoolball.Matches.Match
            {
                Teams = [new TeamInMatch {
                    Team = teamIsNull ? null : new Team()
                }]
            };

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.CreateMatch(match, MemberKey, MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentException_if_season_is_not_null_but_does_not_have_a_SeasonId()
        {
            var repo = CreateRepository();
            var match = new Stoolball.Matches.Match
            {
                Season = new Season()
            };

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.CreateMatch(match, MemberKey, MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentException_if_match_date_before_SQL_Server_minimum()
        {
            var repo = CreateRepository();
            var match = new Stoolball.Matches.Match
            {
                StartTime = new DateTimeOffset(1750, 1, 1, 0, 0, 0, TimeSpan.Zero)
            };

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.CreateMatch(match, MemberKey, MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentException_if_team_added_twice_to_training_session()
        {
            var repo = CreateRepository();

            var match = new Stoolball.Matches.Match
            {
                MatchType = MatchType.TrainingSession,
                Teams = [
                    new TeamInMatch {
                        Team = DatabaseFixture.TestData.Teams[0],
                    },
                    new TeamInMatch {
                        Team = DatabaseFixture.TestData.Teams[0],
                    }]
            };

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.CreateMatch(match, MemberKey, MemberName));
        }

        // TODO: If TeamName is missing, in PopulateTeamNames it doesn't populate PlayingAsTeamName - gets latest teamname as opposed to the one at the time of the match

        [Fact]
        public async Task Route_is_based_on_match_name_if_present()
        {
            var expectedRoute = $"/matches/expected-route-{Guid.NewGuid()}";
            var match = new Stoolball.Matches.Match
            {
                StartTime = new DateTime(2025, 06, 02),
                MatchName = "Match " + Guid.NewGuid().ToString()
            };
            RouteGenerator.Setup(x => x.GenerateRoute("/matches", match.MatchName + " 2Jun2025", NoiseWords.MatchRoute)).Returns(expectedRoute);

            var repo = CreateRepository();

            var created = await repo.CreateMatch(match, MemberKey, MemberName);

            Assert.Equal(expectedRoute, created.MatchRoute);

            await AssertMatchRouteSaved(created.MatchId, created.MatchRoute).ConfigureAwait(false);
        }

        [Fact]
        public async Task Route_based_on_home_team_name_then_away_team_name_if_teams_added()
        {
            var expectedRoute = $"/matches/expected-route-{Guid.NewGuid()}";
            var awayTeam = DatabaseFixture.TestData.Teams[0];
            var homeTeam = DatabaseFixture.TestData.Teams[1];
            var match = new Stoolball.Matches.Match
            {
                StartTime = new DateTime(2025, 06, 02),
                Teams = [
                    new TeamInMatch {
                        Team = new Team{ TeamId = awayTeam.TeamId },
                        TeamRole = TeamRole.Away
                    },
                    new TeamInMatch {
                        Team = new Team { TeamId = homeTeam.TeamId },
                        TeamRole = TeamRole.Home
                    }
                ]
            };
            MatchNameBuilder.Setup(x => x.BuildMatchName(It.IsAny<Stoolball.Matches.Match>())).Returns("Match " + Guid.NewGuid().ToString());
            RouteGenerator.Setup(x => x.GenerateRoute("/matches", homeTeam.TeamName + " " + awayTeam.TeamName + " 2Jun2025", NoiseWords.MatchRoute)).Returns(expectedRoute);

            var repo = CreateRepository();

            var created = await repo.CreateMatch(match, MemberKey, MemberName);

            Assert.Equal(expectedRoute, created.MatchRoute);

            await AssertMatchRouteSaved(created.MatchId, created.MatchRoute).ConfigureAwait(false);
        }

        [Fact]
        public async Task Route_to_be_confirmed_if_no_match_name_and_no_teams()
        {
            var expectedRoute = $"/matches/to-be-confirmed-{Guid.NewGuid()}";
            var match = new Stoolball.Matches.Match
            {
                StartTime = new DateTime(2025, 06, 02),
                MatchRoute = "provided-route-should-be-ignored"
            };
            MatchNameBuilder.Setup(x => x.BuildMatchName(It.IsAny<Stoolball.Matches.Match>())).Returns("Match " + Guid.NewGuid().ToString());
            RouteGenerator.Setup(x => x.GenerateRoute("/matches", "to-be-confirmed 2Jun2025", NoiseWords.MatchRoute)).Returns(expectedRoute);

            var repo = CreateRepository();

            var created = await repo.CreateMatch(match, MemberKey, MemberName);

            Assert.Equal(expectedRoute, created.MatchRoute);

            await AssertMatchRouteSaved(created.MatchId, created.MatchRoute).ConfigureAwait(false);
        }

        [Fact]
        public async Task Route_gets_numeric_suffix_if_route_in_use()
        {
            var routeInUse = DatabaseFixture.TestData.Matches.First(x => x.MatchRoute is not null && Regex.IsMatch(x.MatchRoute, "[a-z]$")).MatchRoute!;
            var expectedRoute = $"/matches/expected-route-{Guid.NewGuid()}-1";
            var match = new Stoolball.Matches.Match
            {
                StartTime = new DateTime(2025, 06, 02),
                MatchName = "Match " + Guid.NewGuid().ToString(),
            };
            RouteGenerator.Setup(x => x.GenerateRoute("/matches", match.MatchName + " 2Jun2025", NoiseWords.MatchRoute)).Returns(routeInUse);
            RouteGenerator.Setup(x => x.IncrementRoute(routeInUse)).Returns(expectedRoute);

            var repo = CreateRepository();

            var created = await repo.CreateMatch(match, MemberKey, MemberName);

            Assert.Equal(expectedRoute, created.MatchRoute);

            await AssertMatchRouteSaved(created.MatchId, created.MatchRoute).ConfigureAwait(false);
        }


        [Theory]
        [InlineData(true, false, true)]
        [InlineData(false, true, false)]
        public async Task Sets_basic_fields(bool startTimeIsKnown, bool inningsOrderIsKnown, bool hasSeason)
        {

            var match = new Stoolball.Matches.Match
            {
                StartTime = DateTimeOffset.Now,
                StartTimeIsKnown = startTimeIsKnown,
                MatchName = "Match " + Guid.NewGuid().ToString(),
                Tournament = DatabaseFixture.TestData.Tournaments.First(),
                MatchLocation = DatabaseFixture.TestData.MatchLocations.First(),
                MatchType = DatabaseFixture.Randomiser.RandomEnum<MatchType>(),
                InningsOrderIsKnown = inningsOrderIsKnown,
                MatchNotes = Guid.NewGuid().ToString(),
                Season = hasSeason ? DatabaseFixture.TestData.SeasonWithMinimalDetails : null,
                MemberKey = MemberKey
            };
            if (hasSeason)
            {
                SeasonDataSource.Setup(x => x.ReadSeasonById(match.Season!.SeasonId!.Value, true)).ReturnsAsync(match.Season);
            }

            var repo = CreateRepository();

            var created = await repo.CreateMatch(match, MemberKey, MemberName);

            Assert.Equal(match.StartTime, created.StartTime);
            Assert.Equal(match.StartTimeIsKnown, created.StartTimeIsKnown);
            Assert.Equal(match.MatchName, created.MatchName);
            Assert.False(created.UpdateMatchNameAutomatically);
            Assert.Equal(match.Tournament.TournamentId, created.Tournament?.TournamentId);
            Assert.Equal(match.MatchLocation.MatchLocationId, created.MatchLocation?.MatchLocationId);
            Assert.Equal(match.MatchType, created.MatchType);
            Assert.Equal(match.InningsOrderIsKnown, created.InningsOrderIsKnown);
            Assert.Equal(match.MatchNotes, created.MatchNotes);
            Assert.Equal(match.Season?.SeasonId, created.Season?.SeasonId);
            Assert.Equal(match.MemberKey, created.MemberKey);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = await connection.QuerySingleOrDefaultAsync<(
                    DateTimeOffset StartTime,
                    bool StartTimeIsKnown,
                    string MatchName,
                    Guid? TournamentId,
                    Guid? MatchLocationId,
                    string MatchType,
                    bool InningsOrderIsKnown,
                    string MatchNotes,
                    bool UpdateMatchNameAutomatically,
                    Guid? SeasonId,
                    Guid? MemberKey)>(
                    @$"SELECT StartTime, StartTimeIsKnown, MatchName, TournamentId, MatchLocationId, MatchType, InningsOrderIsKnown, MatchNotes, UpdateMatchNameAutomatically, SeasonId, MemberKey
                       FROM {Tables.Match} WHERE MatchId = @MatchId", new { created.MatchId }).ConfigureAwait(false);

                Assert.Equal(match.StartTime.AccurateToTheMinute(), saved.StartTime.AccurateToTheMinute());
                Assert.Equal(match.StartTimeIsKnown, saved.StartTimeIsKnown);
                Assert.Equal(match.MatchName, saved.MatchName);
                Assert.Equal(match.UpdateMatchNameAutomatically, saved.UpdateMatchNameAutomatically);
                Assert.Equal(match.Tournament.TournamentId, saved.TournamentId);
                Assert.Equal(match.MatchLocation.MatchLocationId, saved.MatchLocationId);
                Assert.Equal(match.MatchType.ToString(), saved.MatchType);
                Assert.Equal(match.InningsOrderIsKnown, saved.InningsOrderIsKnown);
                Assert.Equal(match.MatchNotes, saved.MatchNotes);
                Assert.Equal(match.Season?.SeasonId, saved.SeasonId);
                Assert.Equal(match.MemberKey, saved.MemberKey);
            }
        }

        [Fact]
        public async Task Sets_MatchName_from_IMatchNameBuilder_when_name_not_present()
        {
            var match = new Stoolball.Matches.Match
            {
                StartTime = DateTimeOffset.Now
            };
            var expectedMatchName = "Match " + Guid.NewGuid().ToString();
            MatchNameBuilder.Setup(x => x.BuildMatchName(It.IsAny<Stoolball.Matches.Match>())).Returns(expectedMatchName);

            var repo = CreateRepository();

            var created = await repo.CreateMatch(match, MemberKey, MemberName);

            Assert.Equal(expectedMatchName, created.MatchName);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = await connection.QuerySingleOrDefaultAsync<(string? MatchName, bool UpdateMatchNameAutomatically)>(
                    $"SELECT MatchName, UpdateMatchNameAutomatically FROM {Tables.Match} WHERE MatchId = @MatchId", new { created.MatchId }).ConfigureAwait(false);

                Assert.Equal(expectedMatchName, saved.MatchName);
                Assert.True(saved.UpdateMatchNameAutomatically);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(true, null)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(false, null)]
        public async Task Sets_EnableBonusOrPenaltyRuns_from_Season_or_defaults_to_true(bool initialValueOnMatch, bool? initialValueOnSeason)
        {
            var match = new Stoolball.Matches.Match
            {
                StartTime = DateTimeOffset.Now,
                MatchName = "Test match",
                EnableBonusOrPenaltyRuns = initialValueOnMatch
            };
            if (initialValueOnSeason.HasValue)
            {
                match.Season = DatabaseFixture.TestData.Seasons.First(s => s.EnableBonusOrPenaltyRuns == initialValueOnSeason);
                SeasonDataSource.Setup(x => x.ReadSeasonById(match.Season.SeasonId!.Value, true)).ReturnsAsync(match.Season);
            }

            var repo = CreateRepository();

            var created = await repo.CreateMatch(match, MemberKey, MemberName);

            var expected = initialValueOnSeason.HasValue ? initialValueOnSeason : true;
            Assert.Equal(expected, created.EnableBonusOrPenaltyRuns);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = await connection.QuerySingleOrDefaultAsync<bool>($"SELECT EnableBonusOrPenaltyRuns FROM {Tables.Match} WHERE MatchId = @MatchId", new { created.MatchId }).ConfigureAwait(false);
                Assert.Equal(expected, saved);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(true, null)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        [InlineData(false, null)]
        public async Task Sets_LastPlayerBatsOn_from_Season_or_defaults_to_match_value(bool initialValueOnMatch, bool? initialValueOnSeason)
        {
            var match = new Stoolball.Matches.Match
            {
                StartTime = DateTimeOffset.Now,
                MatchName = "Test match",
                LastPlayerBatsOn = initialValueOnMatch
            };
            if (initialValueOnSeason.HasValue)
            {
                match.Season = DatabaseFixture.TestData.Seasons.First(s => s.EnableLastPlayerBatsOn == initialValueOnSeason);
                SeasonDataSource.Setup(x => x.ReadSeasonById(match.Season.SeasonId!.Value, true)).ReturnsAsync(match.Season);
            }

            var repo = CreateRepository();

            var created = await repo.CreateMatch(match, MemberKey, MemberName);

            var expected = initialValueOnSeason.HasValue ? initialValueOnSeason : initialValueOnMatch;
            Assert.Equal(expected, created.LastPlayerBatsOn);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = await connection.QuerySingleOrDefaultAsync<bool>($"SELECT LastPlayerBatsOn FROM {Tables.Match} WHERE MatchId = @MatchId", new { created.MatchId }).ConfigureAwait(false);
                Assert.Equal(expected, saved);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public async Task Sets_PlayersPerTeam_from_Season_or_defaults_to_match_value(bool hasSeason, bool seasonHasPlayersPerTeam)
        {
            var match = new Stoolball.Matches.Match
            {
                StartTime = DateTimeOffset.Now,
                MatchName = "Test match",
                PlayersPerTeam = 5
            };
            if (hasSeason)
            {
                match.Season = DatabaseFixture.TestData.Seasons.First(s => s.PlayersPerTeam is null != seasonHasPlayersPerTeam && s.PlayersPerTeam != match.PlayersPerTeam);
                SeasonDataSource.Setup(x => x.ReadSeasonById(match.Season.SeasonId!.Value, true)).ReturnsAsync(match.Season);
            }

            var repo = CreateRepository();

            var created = await repo.CreateMatch(match, MemberKey, MemberName);

            var expected = match.Season is not null ? match.Season.PlayersPerTeam : match.PlayersPerTeam;
            Assert.Equal(expected, created.PlayersPerTeam);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = await connection.QuerySingleOrDefaultAsync<int?>($"SELECT PlayersPerTeam FROM {Tables.Match} WHERE MatchId = @MatchId", new { created.MatchId }).ConfigureAwait(false);
                Assert.Equal(expected, saved);
            }
        }

        [Theory]
        [InlineData(PlayerType.Men)]
        [InlineData(PlayerType.Mixed)]
        [InlineData(PlayerType.Ladies)]
        [InlineData(PlayerType.JuniorMixed)]
        [InlineData(PlayerType.JuniorGirls)]
        public async Task Sets_PlayerType_from_IPlayerTypeSelector(PlayerType playerType)
        {
            var match = new Stoolball.Matches.Match
            {
                StartTime = DateTimeOffset.Now,
                MatchName = "Test match"
            };
            PlayerTypeSelector.Setup(x => x.SelectPlayerType(It.IsAny<Stoolball.Matches.Match>())).Returns(playerType);

            var repo = CreateRepository();

            var created = await repo.CreateMatch(match, MemberKey, MemberName);

            Assert.Equal(playerType, created.PlayerType);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = await connection.QuerySingleOrDefaultAsync<string>($"SELECT PlayerType FROM {Tables.Match} WHERE MatchId = @MatchId", new { created.MatchId }).ConfigureAwait(false);
                Assert.Equal(playerType.ToString(), saved);
            }
        }

        [Theory]
        [InlineData(TeamRole.Home, TeamRole.Away)]
        [InlineData(TeamRole.Training, TeamRole.Training)]
        public async Task Adds_teams(TeamRole teamRole1, TeamRole teamRole2)
        {
            var match = new Stoolball.Matches.Match
            {
                StartTime = DateTimeOffset.Now,
                MatchName = "Test match",
                Teams = [
                    new TeamInMatch {
                        Team = DatabaseFixture.TestData.Teams[0],
                        TeamRole = teamRole1
                    },
                    new TeamInMatch {
                        Team = DatabaseFixture.TestData.Teams[1],
                        TeamRole = teamRole2
                    }
                ]
            };

            var repo = CreateRepository();

            var created = await repo.CreateMatch(match, MemberKey, MemberName);

            Assert.Equal(match.Teams.Count, created.Teams.Count);
            foreach (var team in match.Teams)
            {
                Assert.Contains(created.Teams, t =>
                    t.MatchTeamId.HasValue
                    && t.Team?.TeamId == team.Team!.TeamId
                    && t.TeamRole == team.TeamRole
                    && t.BattedFirst is null
                    && t.WonToss is null);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = (await connection.QueryAsync<(Guid TeamId, string TeamRole, bool? WonToss, Guid? WinnerOfMatchId)>(
                    $"SELECT TeamId, TeamRole, WonToss, WinnerOfMatchId FROM {Tables.MatchTeam} WHERE MatchId = @MatchId", new { created.MatchId }).ConfigureAwait(false)).ToList();
                Assert.Equal(match.Teams.Count, saved.Count);
                foreach (var team in match.Teams)
                {
                    Assert.Contains(saved, t =>
                        t.TeamId == team.Team!.TeamId
                        && t.TeamRole == team.TeamRole.ToString()
                        && t.WonToss is null
                        && t.WinnerOfMatchId is null);
                }
            }
        }

        // TODO: Adds_teams will need to test PlayingAsTeamName

        [Theory]
        [InlineData(MatchType.FriendlyMatch, true)]
        [InlineData(MatchType.LeagueMatch, true)]
        [InlineData(MatchType.KnockoutMatch, true)]
        [InlineData(MatchType.GroupMatch, true)]
        [InlineData(MatchType.TrainingSession, false)]
        public async Task Adds_innings(MatchType matchType, bool expectInnings)
        {
            var match = new Stoolball.Matches.Match
            {
                MatchType = matchType,
                StartTime = DateTimeOffset.Now,
                MatchName = "Test match",
                Teams = [
                    new TeamInMatch {
                        Team = DatabaseFixture.TestData.Teams[0],
                        TeamRole = TeamRole.Home
                    },
                    new TeamInMatch {
                        Team = DatabaseFixture.TestData.Teams[1],
                        TeamRole = TeamRole.Away
                    }
                ],
            };

            var matchInnings = new List<MatchInnings> {
                new MatchInnings
                {
                    InningsOrderInMatch = 1,
                    OverSets = [
                            new OverSet { OverSetNumber = 1, BallsPerOver = 9, Overs = 6 },
                            new OverSet { OverSetNumber = 2, BallsPerOver = 10, Overs = 2 }
                            ]
                },
                new MatchInnings
                {
                    InningsOrderInMatch = 2,
                    OverSets = [
                        new OverSet { OverSetNumber = 1, BallsPerOver = 7, Overs = 7 },
                        new OverSet { OverSetNumber = 2, BallsPerOver = 11, Overs = 1 }
                        ]
                }
            };
            MatchInningsFactory.SetupSequence(x => x.CreateMatchInnings(It.IsAny<Stoolball.Matches.Match>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(matchInnings[0])
                .Returns(matchInnings[1]);

            var repo = CreateRepository();

            var created = await repo.CreateMatch(match, MemberKey, MemberName);

            if (expectInnings)
            {
                Assert.Equal(matchInnings.Count, created.MatchInnings.Count);
                foreach (var innings in matchInnings)
                {
                    Assert.Contains(created.MatchInnings, inn =>
                        inn.MatchInningsId.HasValue
                        && inn.InningsOrderInMatch == innings.InningsOrderInMatch
                        && inn.BattingMatchTeamId is null
                        && inn.BattingTeam is null
                        && inn.BowlingMatchTeamId is null
                        && inn.BowlingTeam is null
                        && inn.Wickets is null
                        && inn.Byes is null
                        && inn.NoBalls is null
                        && inn.Wides is null
                        && inn.Runs is null
                        && inn.BonusOrPenaltyRuns is null
                        && inn.BowlingFigures.Count == 0
                        && inn.OversBowled.Count == 0
                        && inn.PlayerInnings.Count == 0
                        && inn.OverSets.Count == innings.OverSets.Count
                        && inn.OverSets[0].OverSetId.HasValue
                        && inn.OverSets[0].OverSetNumber == innings.OverSets[0].OverSetNumber
                        && inn.OverSets[0].BallsPerOver == innings.OverSets[0].BallsPerOver
                        && inn.OverSets[0].Overs == innings.OverSets[0].Overs
                        && inn.OverSets[1].OverSetId.HasValue
                        && inn.OverSets[1].OverSetNumber == innings.OverSets[1].OverSetNumber
                        && inn.OverSets[1].BallsPerOver == innings.OverSets[1].BallsPerOver
                        && inn.OverSets[1].Overs == innings.OverSets[1].Overs
                        );
                }
            }
            else
            {
                Assert.Empty(created.MatchInnings);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedInnings = (await connection.QueryAsync<(
                    Guid MatchInningsId,
                    int InningsOrderInMatch,
                    Guid? BattingMatchTeamId,
                    Guid? BowlingMatchTeamId,
                    int? Wickets,
                    int? Byes,
                    int? NoBalls,
                    int? Wides,
                    int? Runs,
                    int? BonusOrPenaltyRuns)>(
                    @$"SELECT MatchInningsId, InningsOrderInMatch, BattingMatchTeamId, BowlingMatchTeamId, Wickets, Byes, NoBalls, Wides, Runs, BonusOrPenaltyRuns 
                       FROM {Tables.MatchInnings} WHERE MatchId = @MatchId", new { created.MatchId }).ConfigureAwait(false)).ToList();

                if (expectInnings)
                {
                    Assert.Equal(matchInnings.Count, savedInnings.Count);
                    foreach (var saved in savedInnings)
                    {
                        var originalInnings = matchInnings.SingleOrDefault(inn => inn.InningsOrderInMatch == saved.InningsOrderInMatch);
                        Assert.NotNull(originalInnings);
                        Assert.Null(saved.BattingMatchTeamId);
                        Assert.Null(saved.BowlingMatchTeamId);
                        Assert.Null(saved.Wickets);
                        Assert.Null(saved.Byes);
                        Assert.Null(saved.NoBalls);
                        Assert.Null(saved.Wides);
                        Assert.Null(saved.Runs);
                        Assert.Null(saved.BonusOrPenaltyRuns);

                        var savedOverSets = (await connection.QueryAsync<(int OverSetNumber, int BallsPerOver, int Overs)>(
                            $"SELECT OverSetNumber, BallsPerOver, Overs FROM {Tables.OverSet} WHERE MatchInningsId = @MatchInningsId", new { saved.MatchInningsId }).ConfigureAwait(false)).ToList();

                        Assert.Equal(originalInnings!.OverSets.Count, savedOverSets.Count);
                        foreach (var savedOverSet in savedOverSets)
                        {
                            var originalOverSet = originalInnings.OverSets.SingleOrDefault(os => os.OverSetNumber == savedOverSet.OverSetNumber);
                            Assert.NotNull(originalOverSet);
                            Assert.Equal(originalOverSet!.BallsPerOver, savedOverSet.BallsPerOver);
                            Assert.Equal(originalOverSet.Overs, savedOverSet.Overs);
                        }
                    }
                }
                else
                {
                    Assert.Empty(savedInnings);
                }
            }
        }

        [Fact]
        public async Task Audits_and_logs()
        {
            var match = new Stoolball.Matches.Match
            {
                StartTime = DateTimeOffset.Now,
                MatchName = "Test match"
            };

            var repo = CreateRepository();

            var created = await repo.CreateMatch(match, MemberKey, MemberName);

            AuditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            Logger.Verify(x => x.Info(LoggingTemplates.Created,
                                       It.Is<Stoolball.Matches.Match>(x => x.StartTime.AccurateToTheMinute() == match.StartTime.AccurateToTheMinute()
                                                                        && x.MatchName == match.MatchName),
                                       MemberName, MemberKey, typeof(SqlServerMatchRepository), nameof(SqlServerMatchRepository.CreateMatch)));
        }
    }
}
