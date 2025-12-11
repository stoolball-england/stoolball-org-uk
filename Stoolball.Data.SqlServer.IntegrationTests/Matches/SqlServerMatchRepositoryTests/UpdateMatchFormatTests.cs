using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Testing;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.SqlServerMatchRepositoryTests
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class UpdateMatchFormatTests : MatchRepositoryTestsBase, IDisposable
    {
        public UpdateMatchFormatTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task Throws_ArgumentNullException_if_match_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateMatchFormat(null!, MemberKey, MemberName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Throws_ArgumentNullException_if_memberName_is_null_or_whitespace(string? memberName)
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateMatchFormat(new Stoolball.Matches.Match
            {
                MatchId = Guid.NewGuid(),
                StartTime = DateTimeOffset.UtcNow
            }, MemberKey, memberName!));
        }

        [Fact]
        public async Task Throws_ArgumentException_if_a_match_does_not_have_a_MatchId()
        {
            var repo = CreateRepository();
            var match = new Stoolball.Matches.Match();

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UpdateMatchFormat(match, MemberKey, MemberName));
        }


        [Fact]
        public async Task Throws_MatchNotFoundException_for_match_id_that_does_not_exist()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<MatchNotFoundException>(
                async () => await repository.UpdateMatchFormat(new Stoolball.Matches.Match
                {
                    MatchId = Guid.NewGuid()
                },
                 MemberKey,
                 MemberName
                ).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Throws_InvalidOperationException_if_overs_from_first_overset_is_null()
        {
            var repository = CreateRepository();
            var matchBefore = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m =>
                m.StartTime > DateTime.UtcNow &&
                m.MatchInnings.Count > 2 &&
                m.MatchInnings[0].OverSets.Count > 0));

            matchBefore.MatchInnings[0].OverSets[0].Overs = null;

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await repository.UpdateMatchFormat(matchBefore, MemberKey, MemberName).ConfigureAwait(false)
                );

        }

        [Theory]
        [InlineData(10)]
        [InlineData(20)]
        public async Task Overs_from_first_overset_update_all_oversets_in_the_match(int expectedOvers)
        {
            var repository = CreateRepository();
            var matchBefore = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m =>
                m.StartTime > DateTime.UtcNow &&
                m.MatchInnings.Count > 2 &&
                m.MatchInnings[0].OverSets.Count > 0 &&
                m.MatchInnings[0].OverSets[0].Overs != expectedOvers));

            matchBefore.MatchInnings[0].OverSets[0].Overs = expectedOvers;

            var result = await repository.UpdateMatchFormat(matchBefore, MemberKey, MemberName).ConfigureAwait(false);

            foreach (var innings in result.MatchInnings)
            {
                foreach (var overset in innings.OverSets)
                {
                    Assert.Equal(expectedOvers, overset.Overs);
                }
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var oversFound = await connection.QueryAsync<int>($"SELECT Overs FROM {Tables.OverSet} WHERE MatchInningsId IN @MatchInningsIds",
                    new { MatchInningsIds = matchBefore.MatchInnings.Select(x => x.MatchInningsId) }).ConfigureAwait(false);
                foreach (var overs in oversFound)
                {
                    Assert.Equal(expectedOvers, overs);
                }
            }
        }

        [Fact]
        public async Task Deleting_match_innings_deletes_innings_and_oversets()
        {
            // NOTE: Controller logic prevents innings being removed after a match has happened, so other data should not exist
            var repository = CreateRepository();
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m =>
                m.StartTime > DateTime.UtcNow &&
                m.MatchInnings.Count > 2 &&
                m.MatchInnings[0].OverSets.Count > 0 &&
                m.MatchInnings[0].OverSets[0].Overs is not null));

            var lastTwoInnings = match.MatchInnings[^2..].Select(i => i.MatchInningsId!.Value);
            match.MatchInnings.RemoveAt(match.MatchInnings.Count - 1);
            match.MatchInnings.RemoveAt(match.MatchInnings.Count - 1);

            var result = await repository.UpdateMatchFormat(match, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(match.MatchInnings.Count, result.MatchInnings.Count);
            foreach (var inningsId in match.MatchInnings.Select(i => i.MatchInningsId))
            {
                Assert.Contains(result.MatchInnings, i => i.MatchInningsId == inningsId);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                // No need to check oversets as they cannot exist without the innings as a foreign key
                var inningsFound = await connection.QuerySingleAsync<int>($"SELECT COUNT(*) FROM {Tables.MatchInnings} WHERE MatchInningsId IN @MatchInningsIds",
                    new { MatchInningsIds = lastTwoInnings }).ConfigureAwait(false);
                Assert.Equal(0, inningsFound);
            }
        }

        [Fact]
        public async Task Adding_match_innings_inserts_innings_and_oversets()
        {
            var repository = CreateRepository();
            var matchBefore = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m => m.StartTime > DateTime.UtcNow && m.MatchInnings.Count > 2));

            var newInnings1 = new MatchInnings
            {
                BattingMatchTeamId = matchBefore.MatchInnings[0].BattingMatchTeamId,
                BowlingMatchTeamId = matchBefore.MatchInnings[0].BowlingMatchTeamId,
                InningsOrderInMatch = matchBefore.MatchInnings.Count + 1,
                OverSets = new List<OverSet> {
                    new OverSet { OverSetNumber = 1, Overs = 5, BallsPerOver = 7 },
                    new OverSet { OverSetNumber = 2, Overs = 6, BallsPerOver = 9 }
                }
            };
            var newInnings2 = new MatchInnings
            {
                BattingMatchTeamId = matchBefore.MatchInnings[1].BattingMatchTeamId,
                BowlingMatchTeamId = matchBefore.MatchInnings[1].BowlingMatchTeamId,
                InningsOrderInMatch = matchBefore.MatchInnings.Count + 2,
                OverSets = new List<OverSet> {
                    new OverSet { OverSetNumber = 1, Overs = 9, BallsPerOver = 6 },
                    new OverSet { OverSetNumber = 2, Overs = 8, BallsPerOver = 11 }
                }
            };
            matchBefore.MatchInnings.Add(newInnings1);
            matchBefore.MatchInnings.Add(newInnings2);

            var result = await repository.UpdateMatchFormat(matchBefore, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(matchBefore.MatchInnings.Count, result.MatchInnings.Count);
            var newInningsIds = new Collection<Guid>();
            var matchedInningsIds = new Collection<Guid>();
            foreach (var inningsId in result.MatchInnings.Select(i => i.MatchInningsId))
            {
                Assert.NotNull(inningsId);
                if (matchBefore.MatchInnings.Any(i => i.MatchInningsId == inningsId))
                {
                    if (!matchedInningsIds.Contains(inningsId!.Value))
                    {
                        matchedInningsIds.Add(inningsId!.Value);
                    }
                }
                else
                {
                    newInningsIds.Add(inningsId!.Value);

                    var newInnings = result.MatchInnings.Single(i => i.MatchInningsId == inningsId);
                    Assert.Single(matchBefore.MatchInnings.Where(i =>
                        i.MatchInningsId is null &&
                        i.BattingMatchTeamId == newInnings.BattingMatchTeamId &&
                        i.BowlingMatchTeamId == newInnings.BowlingMatchTeamId &&
                        i.InningsOrderInMatch == newInnings.InningsOrderInMatch &&
                        i.OverSets.Count == 2 &&
                        i.OverSets[0].OverSetNumber == newInnings.OverSets[0].OverSetNumber &&
                        i.OverSets[0].Overs == newInnings.OverSets[0].Overs &&
                        i.OverSets[0].BallsPerOver == newInnings.OverSets[0].BallsPerOver &&
                        i.OverSets[1].OverSetNumber == newInnings.OverSets[1].OverSetNumber &&
                        i.OverSets[1].Overs == newInnings.OverSets[1].Overs &&
                        i.OverSets[1].BallsPerOver == newInnings.OverSets[1].BallsPerOver));
                }
            }
            Assert.Equal(matchBefore.MatchInnings.Count, matchedInningsIds.Count + newInningsIds.Count);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var inningsFound = await connection.QueryAsync<(Guid MatchId, Guid MatchInningsId, Guid? BattingMatchTeamId, Guid? BowlingMatchTeamId, int InningsOrderInMatch)>(
                    @$"SELECT MatchId, MatchInningsId, BattingMatchTeamId, BowlingMatchTeamId, InningsOrderInMatch 
                       FROM {Tables.MatchInnings} WHERE MatchInningsId IN @MatchInningsIds",
                    new { MatchInningsIds = newInningsIds }).ConfigureAwait(false);

                Assert.Equal(newInningsIds.Count, inningsFound.Count());
                foreach (var innings in inningsFound)
                {
                    var matchingInnings = matchBefore.MatchInnings.Single(i =>
                        i.MatchInningsId is null &&
                        i.BattingMatchTeamId == innings.BattingMatchTeamId &&
                        i.BowlingMatchTeamId == innings.BowlingMatchTeamId &&
                        i.InningsOrderInMatch == innings.InningsOrderInMatch);
                    Assert.Equal(matchBefore.MatchId, innings.MatchId);

                    var overSetsFound = await connection.QueryAsync<(int? OverSetNumber, int? Overs, int? BallsPerOver)>(
                        @$"SELECT OverSetNumber, Overs, BallsPerOver 
                       FROM {Tables.OverSet} WHERE MatchInningsId = @MatchInningsId",
                        new { innings.MatchInningsId }).ConfigureAwait(false);

                    Assert.Equal(matchingInnings.OverSets.Count, overSetsFound.Count());
                    foreach (var overset in overSetsFound)
                    {
                        Assert.Contains(matchingInnings.OverSets, o =>
                            o.OverSetNumber == overset.OverSetNumber &&
                            o.Overs == overset.Overs &&
                            o.BallsPerOver == overset.BallsPerOver);
                    }
                }
            }
        }

        [Fact]
        public async Task Audits_and_logs()
        {
            var repository = CreateRepository();
            var matchBefore = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m =>
                m.StartTime > DateTime.UtcNow &&
                m.MatchInnings.Count > 2 &&
                m.MatchInnings[0].OverSets.Count > 0 &&
                m.MatchInnings[0].OverSets[0].Overs is not null));

            var _ = await repository.UpdateMatchFormat(matchBefore, MemberKey, MemberName).ConfigureAwait(false);

            AuditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            Logger.Verify(x => x.Info(LoggingTemplates.Updated,
                                       It.Is<Stoolball.Matches.Match>(x => x.StartTime.AccurateToTheMinute() == matchBefore.StartTime.AccurateToTheMinute()
                                                                        && x.MatchName == matchBefore.MatchName),
                                       MemberName, MemberKey, typeof(SqlServerMatchRepository), nameof(SqlServerMatchRepository.UpdateMatchFormat)));
        }
    }
}
