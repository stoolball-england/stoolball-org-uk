using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using AngleSharp.Css.Dom;
using Dapper;
using Ganss.XSS;
using Moq;
using Stoolball.Awards;
using Stoolball.Data.Abstractions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerMatchRepositoryTests : IDisposable
    {
        private readonly SqlServerTestDataFixture _databaseFixture;
        private readonly TransactionScope _scope;
        private readonly Mock<IHtmlSanitizer> _sanitizer = new();
        private readonly Mock<IAuditRepository> _auditRepository = new();
        private readonly Mock<ILogger<SqlServerMatchRepository>> _logger = new();
        private readonly Mock<IRouteGenerator> _routeGenerator = new();
        private readonly Mock<IRedirectsRepository> _redirectsRepository = new();
        private readonly Mock<IMatchNameBuilder> _matchNameBuilder = new();
        private readonly Mock<IPlayerTypeSelector> _playerTypeSelector = new();
        private readonly Mock<IBowlingScorecardComparer> _bowlingScorecardComparer = new();
        private readonly Mock<IBattingScorecardComparer> _battingScorecardComparer = new();
        private readonly Mock<IPlayerRepository> _playerRepository = new();
        private readonly Mock<IDataRedactor> _dataRedactor = new();
        private readonly Mock<IStatisticsRepository> _statisticsRepository = new();
        private readonly Mock<IOversHelper> _oversHelper = new();
        private readonly Mock<IPlayerInMatchStatisticsBuilder> _statisticsBuilder = new();
        private readonly Mock<IMatchInningsFactory> _matchInningsFactory = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Guid _memberKey;
        private readonly string _memberName;

        public SqlServerMatchRepositoryTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            _sanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            _sanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            _sanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            _sanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());

            _memberKey = _databaseFixture.TestData.Members[0].memberKey;
            _memberName = _databaseFixture.TestData.Members[0].memberName;

            _battingScorecardComparer.Setup(x => x.CompareScorecards(It.IsAny<IEnumerable<PlayerInnings>>(), It.IsAny<IEnumerable<PlayerInnings>>())).Returns(new BattingScorecardComparison());

            _playerRepository.Setup(x => x.CreateOrMatchPlayerIdentity(It.IsAny<PlayerIdentity>(), _memberKey, _memberName, It.IsAny<IDbTransaction>())).Returns(new CreateOrMatchPlayerIdentityReturns((PlayerIdentity pi, Guid _, string _, IDbTransaction _) => Task.FromResult(pi)));

        }
        private SqlServerMatchRepository CreateRepository()
        {
            return new SqlServerMatchRepository(
                            _databaseFixture.ConnectionFactory,
                            new DapperWrapper(),
                            _auditRepository.Object,
                            _logger.Object,
                            _routeGenerator.Object,
                            _redirectsRepository.Object,
                            _sanitizer.Object,
                            _matchNameBuilder.Object,
                            _playerTypeSelector.Object,
                            _bowlingScorecardComparer.Object,
                            _battingScorecardComparer.Object,
                            _playerRepository.Object,
                            _dataRedactor.Object,
                            _statisticsRepository.Object,
                            _oversHelper.Object,
                            _statisticsBuilder.Object,
                            _matchInningsFactory.Object,
                            _seasonDataSource.Object,
                            new StoolballEntityCopier(_dataRedactor.Object));
        }

        // TODO: Test repository.CreateMatch();
        // TODO: Test repository.UpdateMatch();
        // TODO: Test repository.UpdateMatchFormat();
        // TODO: Test repository.UpdateStartOfPlay();

        private Stoolball.Matches.Match CloneValidMatch()
        {
            var matchToCopy = _databaseFixture.TestData.MatchInThePastWithFullDetails!;
            var inningsToCopy = matchToCopy.MatchInnings.First(x => x.PlayerInnings.Count > 1 && x.OversBowled.Count > 1);
            var match = new Stoolball.Matches.Match
            {
                MatchId = matchToCopy.MatchId,
                MatchInnings = new List<MatchInnings>
                {
                    new MatchInnings
                    {
                        MatchInningsId  = inningsToCopy.MatchInningsId,
                        BattingTeam = new TeamInMatch
                        {
                            Team = new Team
                            {
                                TeamId = inningsToCopy.BattingTeam!.Team!.TeamId
                            }
                        },
                        BowlingTeam = new TeamInMatch
                        {
                            Team = new Team
                            {
                                TeamId = inningsToCopy.BowlingTeam!.Team!.TeamId
                            }
                        },
                        OverSets = inningsToCopy.OverSets,
                        Byes = inningsToCopy.Byes,
                        Wides = inningsToCopy.Wides,
                        NoBalls = inningsToCopy.NoBalls,
                        BonusOrPenaltyRuns = inningsToCopy.BonusOrPenaltyRuns,
                        Runs = inningsToCopy.Runs,
                        Wickets = inningsToCopy.Wickets
                    }
                },
                PlayersPerTeam = matchToCopy.PlayersPerTeam
            };
            foreach (var playerInnings in inningsToCopy.PlayerInnings)
            {
                match.MatchInnings[0].PlayerInnings.Add(new PlayerInnings
                {
                    PlayerInningsId = playerInnings.PlayerInningsId,
                    BattingPosition = playerInnings.BattingPosition,
                    Batter = playerInnings.Batter,
                    DismissalType = playerInnings.DismissalType,
                    DismissedBy = playerInnings.DismissedBy,
                    Bowler = playerInnings.Bowler,
                    RunsScored = playerInnings.RunsScored,
                    BallsFaced = playerInnings.BallsFaced,
                });
            }
            foreach (var over in inningsToCopy.OversBowled)
            {
                match.MatchInnings[0].OversBowled.Add(new Over
                {
                    OverId = over.OverId,
                    OverSet = over.OverSet,
                    OverNumber = over.OverNumber,
                    Bowler = over.Bowler,
                    BallsBowled = over.BallsBowled,
                    Wides = over.Wides,
                    NoBalls = over.NoBalls,
                    RunsConceded = over.RunsConceded
                });
            }

            return match;
        }

        delegate Task<PlayerIdentity> CreateOrMatchPlayerIdentityReturns(PlayerIdentity pi, Guid memberKey, string memberName, IDbTransaction transaction);

        [Theory]
        [InlineData(false, false, false, false)]
        [InlineData(true, false, false, false)]
        [InlineData(false, true, false, false)]
        [InlineData(false, false, true, false)]
        [InlineData(false, false, false, true)]
        [InlineData(true, true, true, true)]
        public async Task UpdateBattingScorecard_inserts_new_player_innings(bool hasFielder, bool hasBowler, bool hasRuns, bool hasBallsFaced)
        {
            var repository = CreateRepository();

            var match = CloneValidMatch();
            var possibleBatters = _databaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == match.MatchInnings[0].BattingTeam!.Team!.TeamId);
            var possibleFielders = _databaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == match.MatchInnings[0].BowlingTeam!.Team!.TeamId);

            var playerInnings = AddOneNewPlayerInnings(match.MatchInnings[0].PlayerInnings, possibleBatters, possibleFielders, hasFielder, hasBowler, hasRuns, hasBallsFaced);

            var returnedInnings = await repository.UpdateBattingScorecard(match, match.MatchInnings[0].MatchInningsId!.Value, _memberKey, _memberName).ConfigureAwait(false);

            Assert.Equal(match.MatchInnings[0].PlayerInnings.Count, returnedInnings.PlayerInnings.Count);
            Assert.Equal(1, returnedInnings.PlayerInnings.Count(x =>
                x.BattingPosition == playerInnings.BattingPosition &&
                x.Batter!.PlayerIdentityId == playerInnings.Batter.PlayerIdentityId &&
                x.DismissalType == playerInnings.DismissalType &&
                x.DismissedBy?.PlayerIdentityId == playerInnings.DismissedBy?.PlayerIdentityId &&
                x.Bowler?.PlayerIdentityId == playerInnings.Bowler?.PlayerIdentityId &&
                x.RunsScored == playerInnings.RunsScored &&
                x.BallsFaced == playerInnings.BallsFaced));

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedInnings = await connection.QuerySingleOrDefaultAsync<(Guid BatterPlayerIdentityId, int? BattingPosition, DismissalType DismissalType, Guid? DismissedByPlayerIdentityId, Guid? BowledByPlayerIdentityId, int? RunsScored, int? BallsFaced)?>(
                    @$"SELECT BatterPlayerIdentityId, BattingPosition, DismissalType, DismissedByPlayerIdentityId, BowlerPlayerIdentityId, RunsScored, BallsFaced 
                       FROM {Tables.PlayerInnings} 
                       WHERE MatchInningsId = @MatchInningsId
                       AND BattingPosition = @BattingPosition",
                    new
                    {
                        match.MatchInnings[0].MatchInningsId,
                        playerInnings.BattingPosition
                    }).ConfigureAwait(false);

                Assert.NotNull(savedInnings);
                Assert.Equal(playerInnings.Batter!.PlayerIdentityId, savedInnings.Value.BatterPlayerIdentityId);
                Assert.Equal(match.MatchInnings[0].PlayerInnings.Count, savedInnings.Value.BattingPosition);
                Assert.Equal(playerInnings.DismissalType, savedInnings.Value.DismissalType);
                Assert.Equal(playerInnings.DismissedBy?.PlayerIdentityId, savedInnings.Value.DismissedByPlayerIdentityId);
                Assert.Equal(playerInnings.Bowler?.PlayerIdentityId, savedInnings.Value.BowledByPlayerIdentityId);
                Assert.Equal(playerInnings.RunsScored, savedInnings.Value.RunsScored);
                Assert.Equal(playerInnings.BallsFaced, savedInnings.Value.BallsFaced);
            }
        }

        private PlayerInnings AddOneNewPlayerInnings(List<PlayerInnings> innings, IEnumerable<PlayerIdentity> possibleBatters, IEnumerable<PlayerIdentity> possibleFielders, bool hasFielder, bool hasBowler, bool hasRuns, bool hasBallsFaced)
        {
            var playerInnings = new PlayerInnings
            {
                BattingPosition = innings.Count + 1,
                Batter = possibleBatters.First(),
                DismissalType = DismissalType.RunOut,
                DismissedBy = hasFielder ? possibleFielders.First() : null,
                Bowler = hasBowler ? possibleFielders.Last() : null,
                RunsScored = hasRuns ? 57 : null,
                BallsFaced = hasBallsFaced ? 64 : null
            };
            innings.Add(playerInnings);

            _battingScorecardComparer.Setup(x => x.CompareScorecards(It.IsAny<IEnumerable<PlayerInnings>>(), It.IsAny<IEnumerable<PlayerInnings>>())).Returns(new BattingScorecardComparison { PlayerInningsAdded = new List<PlayerInnings> { playerInnings } });
            return playerInnings;
        }

        [Theory]
        [InlineData(false, false, false, false, false, false)]
        [InlineData(true, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false)]
        [InlineData(false, false, true, false, false, false)]
        [InlineData(false, false, false, true, false, false)]
        [InlineData(false, false, false, false, true, false)]
        [InlineData(false, false, false, false, false, true)]
        public async Task UpdateBattingScorecard_updates_player_innings_previously_added(bool batterHasChanged, bool dismissalTypeHasChanged, bool fielderHasChanged, bool bowlerHasChanged, bool runsScoredHasChanged, bool ballsFacedHasChanged)
        {
            var repository = CreateRepository();
            var modifiedMatch = CloneValidMatch();
            var modifiedInnings = modifiedMatch.MatchInnings[0];
            var modifiedPlayerInnings = modifiedInnings.PlayerInnings.Last();
            var originalPlayerInnings = new PlayerInnings
            {
                PlayerInningsId = modifiedPlayerInnings.PlayerInningsId,
                BattingPosition = modifiedPlayerInnings.BattingPosition,
                Batter = modifiedPlayerInnings.Batter,
                DismissalType = modifiedPlayerInnings.DismissalType,
                DismissedBy = modifiedPlayerInnings.DismissedBy,
                Bowler = modifiedPlayerInnings.Bowler,
                RunsScored = modifiedPlayerInnings.RunsScored,
                BallsFaced = modifiedPlayerInnings.BallsFaced
            };


            if (batterHasChanged)
            {
                var possibleBatters = _databaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == modifiedMatch.MatchInnings[0].BattingTeam!.Team!.TeamId);
                modifiedPlayerInnings.Batter = possibleBatters.First(x => x.PlayerIdentityId != modifiedPlayerInnings.Batter?.PlayerIdentityId);
            }
            if (dismissalTypeHasChanged) { modifiedPlayerInnings.DismissalType = modifiedPlayerInnings.DismissalType == DismissalType.Caught ? DismissalType.CaughtAndBowled : DismissalType.Caught; }
            if (fielderHasChanged || bowlerHasChanged)
            {
                var possibleFielders = _databaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == modifiedMatch.MatchInnings[0].BowlingTeam!.Team!.TeamId);
                if (fielderHasChanged) { modifiedPlayerInnings.DismissedBy = possibleFielders.First(x => x.PlayerIdentityId != modifiedPlayerInnings.DismissedBy?.PlayerIdentityId); }
                if (bowlerHasChanged) { modifiedPlayerInnings.Bowler = possibleFielders.First(x => x.PlayerIdentityId != modifiedPlayerInnings.Bowler?.PlayerIdentityId); }
            }
            if (runsScoredHasChanged) { modifiedPlayerInnings.RunsScored = modifiedPlayerInnings.RunsScored.HasValue ? modifiedPlayerInnings.RunsScored + 1 : 60; }
            if (ballsFacedHasChanged) { modifiedPlayerInnings.BallsFaced = modifiedPlayerInnings.BallsFaced.HasValue ? modifiedPlayerInnings.BallsFaced + 1 : 70; }

            var comparison = SetupUnchangedBattingComparison(modifiedInnings);
            comparison.PlayerInningsUnchanged.Remove(modifiedPlayerInnings);
            comparison.PlayerInningsChanged.Add((originalPlayerInnings, modifiedPlayerInnings));

            var result = await repository.UpdateBattingScorecard(
                    modifiedMatch,
                    modifiedInnings.MatchInningsId!.Value,
                    _memberKey,
                    _memberName);

            Assert.Equal(modifiedInnings.PlayerInnings.Count, result.PlayerInnings.Count);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedInnings = await connection.QuerySingleOrDefaultAsync<(Guid BatterPlayerIdentityId, int? BattingPosition, DismissalType DismissalType, Guid? DismissedByPlayerIdentityId, Guid? BowledByPlayerIdentityId, int? RunsScored, int? BallsFaced)?>(
                    @$"SELECT BatterPlayerIdentityId, BattingPosition, DismissalType, DismissedByPlayerIdentityId, BowlerPlayerIdentityId, RunsScored, BallsFaced 
                       FROM {Tables.PlayerInnings} 
                       WHERE PlayerInningsId = @PlayerInningsId",
                    modifiedPlayerInnings
                    ).ConfigureAwait(false);

                Assert.NotNull(savedInnings);
                Assert.Equal(modifiedPlayerInnings.Batter!.PlayerIdentityId, savedInnings.Value.BatterPlayerIdentityId);
                Assert.Equal(modifiedPlayerInnings.BattingPosition, savedInnings.Value.BattingPosition);
                Assert.Equal(modifiedPlayerInnings.DismissalType, savedInnings.Value.DismissalType);
                Assert.Equal(modifiedPlayerInnings.DismissedBy?.PlayerIdentityId, savedInnings.Value.DismissedByPlayerIdentityId);
                Assert.Equal(modifiedPlayerInnings.Bowler?.PlayerIdentityId, savedInnings.Value.BowledByPlayerIdentityId);
                Assert.Equal(modifiedPlayerInnings.RunsScored, savedInnings.Value.RunsScored);
                Assert.Equal(modifiedPlayerInnings.BallsFaced, savedInnings.Value.BallsFaced);
            }
        }

        [Fact]
        public async Task UpdateBattingScorecard_deletes_player_innings_removed_from_scorecard()
        {
            var repository = CreateRepository();

            var modifiedMatch = CloneValidMatch();
            var modifiedInnings = modifiedMatch.MatchInnings[0];

            var playerInningsToRemove = modifiedInnings.PlayerInnings.Last();
            modifiedInnings.PlayerInnings.Remove(playerInningsToRemove);

            var comparison = SetupUnchangedBattingComparison(modifiedInnings);
            comparison.PlayerInningsRemoved.Add(playerInningsToRemove);

            var result = await repository.UpdateBattingScorecard(
                    modifiedMatch,
                    modifiedInnings.MatchInningsId!.Value,
                    _memberKey,
                    _memberName);

            Assert.Equal(modifiedInnings.PlayerInnings.Count, result.PlayerInnings.Count);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedInningsId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                    $"SELECT PlayerInningsId FROM {Tables.PlayerInnings} WHERE PlayerInningsId = @PlayerInningsId",
                    playerInningsToRemove).ConfigureAwait(false);

                Assert.Null(savedInningsId);
            }
        }

        [Fact]
        public async Task UpdateBattingScorecard_unchanged_player_innings_are_retained()
        {
            var repository = CreateRepository();

            var match = _databaseFixture.TestData.MatchInThePastWithFullDetails!;
            var innings = match.MatchInnings.First(x => x.PlayerInnings.Count > 0);

            var comparison = new BattingScorecardComparison { PlayerInningsUnchanged = innings.PlayerInnings };
            _battingScorecardComparer.Setup(x => x.CompareScorecards(It.IsAny<IEnumerable<PlayerInnings>>(), It.IsAny<IEnumerable<PlayerInnings>>())).Returns(comparison);

            var result = await repository.UpdateBattingScorecard(
                    match,
                    innings.MatchInningsId!.Value,
                    _memberKey,
                    _memberName);

            Assert.Equal(innings.PlayerInnings.Count, result.PlayerInnings.Count);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var playerInnings in innings.PlayerInnings)
                {
                    var savedInnings = await connection.QuerySingleOrDefaultAsync<(Guid BatterPlayerIdentityId, DismissalType DismissalType, Guid? DismissedByPlayerIdentityId, Guid? BowledByPlayerIdentityId, int? RunsScored, int? BallsFaced)?>(
                                        @$"SELECT BatterPlayerIdentityId, DismissalType, DismissedByPlayerIdentityId, BowlerPlayerIdentityId, RunsScored, BallsFaced 
                       FROM {Tables.PlayerInnings} 
                       WHERE PlayerInningsId = @PlayerInningsId",
                       playerInnings).ConfigureAwait(false);

                    Assert.NotNull(savedInnings);
                    Assert.Equal(playerInnings.Batter!.PlayerIdentityId, savedInnings.Value.BatterPlayerIdentityId);
                    Assert.Equal(playerInnings.DismissalType, savedInnings.Value.DismissalType);
                    Assert.Equal(playerInnings.DismissedBy?.PlayerIdentityId, savedInnings.Value.DismissedByPlayerIdentityId);
                    Assert.Equal(playerInnings.Bowler?.PlayerIdentityId, savedInnings.Value.BowledByPlayerIdentityId);
                    Assert.Equal(playerInnings.RunsScored, savedInnings.Value.RunsScored);
                    Assert.Equal(playerInnings.BallsFaced, savedInnings.Value.BallsFaced);
                }
            }
        }

        [Theory]
        [InlineData(null, null, null, null, null, null)]
        [InlineData(5, 10, 15, 20, 140, 8)]
        public async Task UpdateBattingScorecard_updates_extras_and_final_score(int? byes, int? wides, int? noBalls, int? bonus, int? runs, int? wickets)
        {
            var repository = CreateRepository();

            Func<MatchInnings, bool> inningsSelector = mi => mi.Byes != byes && mi.Wides != wides && mi.NoBalls != noBalls && mi.BonusOrPenaltyRuns != bonus && mi.Runs != runs && mi.Wickets != wickets;
            var match = _databaseFixture.TestData.Matches.First(m => m.MatchInnings.Any(inningsSelector));
            var innings = match.MatchInnings.First(inningsSelector);

            var updatedMatch = new Stoolball.Matches.Match
            {
                MatchId = match.MatchId,
                MatchInnings = new List<MatchInnings>
                {
                    new MatchInnings
                    {
                        MatchInningsId = innings.MatchInningsId,
                        Byes = byes,
                        Wides = wides,
                        NoBalls = noBalls,
                        BonusOrPenaltyRuns = bonus,
                        Runs = runs,
                        Wickets = wickets,
                        BattingTeam = innings.BattingTeam,
                        BowlingTeam = innings.BowlingTeam
                    }
                }
            };

            var returnedInnings = await repository.UpdateBattingScorecard(updatedMatch, updatedMatch.MatchInnings[0].MatchInningsId!.Value, _memberKey, _memberName).ConfigureAwait(false);

            Assert.NotNull(returnedInnings);
            Assert.Equal(innings.MatchInningsId, returnedInnings.MatchInningsId);
            Assert.Equal(byes, returnedInnings.Byes);
            Assert.Equal(wides, returnedInnings.Wides);
            Assert.Equal(noBalls, returnedInnings.NoBalls);
            Assert.Equal(bonus, returnedInnings.BonusOrPenaltyRuns);
            Assert.Equal(runs, returnedInnings.Runs);
            Assert.Equal(wickets, returnedInnings.Wickets);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedInnings = await connection.QuerySingleOrDefaultAsync<MatchInnings>(
                    @$"SELECT Byes, Wides, NoBalls, BonusOrPenaltyRuns, Runs, Wickets 
                       FROM {Tables.MatchInnings} 
                       WHERE MatchInningsId = @MatchInningsId",
                    new
                    {
                        innings.MatchInningsId
                    }).ConfigureAwait(false);

                Assert.NotNull(savedInnings);
                Assert.Equal(byes, savedInnings.Byes);
                Assert.Equal(wides, savedInnings.Wides);
                Assert.Equal(noBalls, savedInnings.NoBalls);
                Assert.Equal(bonus, savedInnings.BonusOrPenaltyRuns);
                Assert.Equal(runs, savedInnings.Runs);
                Assert.Equal(wickets, savedInnings.Wickets);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public async Task UpdateBattingScorecard_updates_players_per_team_for_match(int numberOfInningsComparedToPlayersPerTeam)
        {
            var repository = CreateRepository();

            var match = CloneValidMatch();
            var possibleBatters = _databaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == match.MatchInnings[0].BattingTeam!.Team!.TeamId);
            var possibleFielders = _databaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == match.MatchInnings[0].BowlingTeam!.Team!.TeamId);

            var expectedPlayersPerTeam = match.PlayersPerTeam;
            if (numberOfInningsComparedToPlayersPerTeam > expectedPlayersPerTeam) { expectedPlayersPerTeam += numberOfInningsComparedToPlayersPerTeam; }

            if (numberOfInningsComparedToPlayersPerTeam >= 0)
            {
                while (match.MatchInnings[0].PlayerInnings.Count < expectedPlayersPerTeam)
                {
                    _ = AddOneNewPlayerInnings(match.MatchInnings[0].PlayerInnings, possibleBatters, possibleFielders, true, true, true, true);
                }
            }
            else
            {
                while (match.MatchInnings[0].PlayerInnings.Count >= expectedPlayersPerTeam)
                {
                    match.MatchInnings[0].PlayerInnings.RemoveAt(match.MatchInnings[0].PlayerInnings.Count - 1);
                }
            }

            _ = await repository.UpdateBattingScorecard(match, match.MatchInnings[0].MatchInningsId!.Value, _memberKey, _memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedPlayersPerTeam = await connection.QuerySingleOrDefaultAsync<int>(
                    @$"SELECT PlayersPerTeam
                       FROM {Tables.Match} 
                       WHERE MatchId = @MatchId",
                    match).ConfigureAwait(false);

                Assert.Equal(expectedPlayersPerTeam, savedPlayersPerTeam);
            }
        }

        [Theory]
        [InlineData(false, false, false, false, false, false, false, false, false, false, false, false)]
        [InlineData(true, false, false, false, false, false, false, false, false, false, false, false)]
        [InlineData(false, true, false, false, false, false, false, false, false, false, false, false)]
        [InlineData(false, false, true, false, false, false, false, false, false, false, false, false)]
        [InlineData(false, false, false, true, false, false, false, false, false, false, false, false)]
        [InlineData(false, false, false, false, true, false, false, false, false, false, false, false)]
        [InlineData(false, false, false, false, false, true, false, false, false, false, false, false)]
        [InlineData(false, false, false, false, false, false, true, false, false, false, false, false)]
        [InlineData(false, false, false, false, false, false, false, true, false, false, false, false)]
        [InlineData(false, false, false, false, false, false, false, false, true, false, false, false)]
        [InlineData(false, false, false, false, false, false, false, false, false, true, false, false)]
        [InlineData(false, false, false, false, false, false, false, false, false, false, true, false)]
        [InlineData(false, false, false, false, false, false, false, false, false, false, false, true)]
        public async Task UpdateBattingScorecard_updates_bowling_figures_and_player_statistics_if_data_has_changed(
            bool batterHasChanged, bool dismissalTypeHasChanged, bool fielderHasChanged, bool bowlerHasChanged, bool runsScoredHasChanged, bool ballsFacedHasChanged,
            bool byesHasChanged, bool widesHasChanged, bool noBallsHasChanged, bool bonusHasChanged, bool teamRunsHasChanged, bool teamWicketsHasChanged
            )
        {
            var repository = CreateRepository();
            var modifiedMatch = CloneValidMatch();
            var modifiedMatchInnings = modifiedMatch.MatchInnings[0];
            var modifiedPlayerInnings = modifiedMatchInnings.PlayerInnings.Last();
            var originalPlayerInnings = new PlayerInnings
            {
                PlayerInningsId = modifiedPlayerInnings.PlayerInningsId,
                BattingPosition = modifiedPlayerInnings.BattingPosition,
                Batter = modifiedPlayerInnings.Batter,
                DismissalType = modifiedPlayerInnings.DismissalType,
                DismissedBy = modifiedPlayerInnings.DismissedBy,
                Bowler = modifiedPlayerInnings.Bowler,
                RunsScored = modifiedPlayerInnings.RunsScored,
                BallsFaced = modifiedPlayerInnings.BallsFaced
            };
            var playerInningsHasChanged = batterHasChanged || dismissalTypeHasChanged || fielderHasChanged || bowlerHasChanged || runsScoredHasChanged || ballsFacedHasChanged;
            var matchInningsHasChanged = byesHasChanged || widesHasChanged || noBallsHasChanged || bonusHasChanged || teamRunsHasChanged || teamWicketsHasChanged;
            var anythingHasChanged = playerInningsHasChanged || matchInningsHasChanged;

            if (anythingHasChanged)
            {
                if (playerInningsHasChanged)
                {
                    var possibleBatters = _databaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == modifiedMatch.MatchInnings[0].BattingTeam!.Team!.TeamId);
                    var possibleFielders = _databaseFixture.TestData.PlayerIdentities.Where(x => x.Team!.TeamId == modifiedMatch.MatchInnings[0].BowlingTeam!.Team!.TeamId);

                    if (batterHasChanged) { modifiedPlayerInnings.Batter = possibleBatters.First(x => x.PlayerIdentityId != modifiedPlayerInnings.Batter!.PlayerIdentityId); }
                    if (dismissalTypeHasChanged) { modifiedPlayerInnings.DismissalType = modifiedPlayerInnings.DismissalType == DismissalType.Caught ? DismissalType.CaughtAndBowled : DismissalType.Caught; }
                    if (fielderHasChanged) { modifiedPlayerInnings.DismissedBy = possibleFielders.First(x => x.PlayerIdentityId != modifiedPlayerInnings.DismissedBy?.PlayerIdentityId); }
                    if (bowlerHasChanged) { modifiedPlayerInnings.Bowler = possibleFielders.First(x => x.PlayerIdentityId != modifiedPlayerInnings.Bowler?.PlayerIdentityId); }
                    if (runsScoredHasChanged) { modifiedPlayerInnings.RunsScored = modifiedPlayerInnings.RunsScored.HasValue ? modifiedPlayerInnings.RunsScored + 1 : 60; }
                    if (ballsFacedHasChanged) { modifiedPlayerInnings.BallsFaced = modifiedPlayerInnings.BallsFaced.HasValue ? modifiedPlayerInnings.BallsFaced + 1 : 70; }

                    var comparison = new BattingScorecardComparison { PlayerInningsChanged = new List<(PlayerInnings, PlayerInnings)> { (originalPlayerInnings, modifiedPlayerInnings) } };
                    _battingScorecardComparer.Setup(x => x.CompareScorecards(It.IsAny<IEnumerable<PlayerInnings>>(), It.IsAny<IEnumerable<PlayerInnings>>())).Returns(comparison);
                }

                if (byesHasChanged) { modifiedMatchInnings.Byes = modifiedMatchInnings.Byes.HasValue ? modifiedMatchInnings.Byes + 1 : 10; }
                if (widesHasChanged) { modifiedMatchInnings.Wides = modifiedMatchInnings.Wides.HasValue ? modifiedMatchInnings.Wides + 1 : 9; }
                if (noBallsHasChanged) { modifiedMatchInnings.NoBalls = modifiedMatchInnings.NoBalls.HasValue ? modifiedMatchInnings.NoBalls + 1 : 7; }
                if (bonusHasChanged) { modifiedMatchInnings.BonusOrPenaltyRuns = modifiedMatchInnings.BonusOrPenaltyRuns.HasValue ? modifiedMatchInnings.BonusOrPenaltyRuns + 1 : 5; }
                if (teamRunsHasChanged) { modifiedMatchInnings.Runs = modifiedMatchInnings.Runs.HasValue ? modifiedMatchInnings.Runs + 1 : 110; }
                if (teamWicketsHasChanged) { modifiedMatchInnings.Wickets = modifiedMatchInnings.Wickets.HasValue ? modifiedMatchInnings.Wickets + 1 : 9; }
            }

            _ = await repository.UpdateBattingScorecard(modifiedMatch, modifiedMatchInnings.MatchInningsId!.Value, _memberKey, _memberName).ConfigureAwait(false);

            _statisticsRepository.Verify(x => x.UpdateBowlingFigures(It.Is<MatchInnings>(mi => mi.MatchInningsId == modifiedMatchInnings.MatchInningsId), _memberKey, _memberName, It.IsAny<IDbTransaction>()), bowlerHasChanged ? Times.Once() : Times.Never());
            _statisticsRepository.Verify(x => x.UpdatePlayerStatistics(It.IsAny<IEnumerable<PlayerInMatchStatisticsRecord>>(), It.IsAny<IDbTransaction>()), anythingHasChanged ? Times.Once() : Times.Never());
        }
        [Fact]
        public async Task UpdateBowlingScorecard_inserts_new_overs_and_extends_the_final_overset()
        {
            var repository = CreateRepository();

            var modifiedMatch = CloneValidMatch();
            var modifiedInnings = modifiedMatch.MatchInnings[0];

            var comparison = SetupUnchangedBowlingComparison(modifiedInnings);

            var totalOversInOversets = modifiedInnings.OverSets.Sum(o => o.Overs);
            var finalOverSet = modifiedInnings.OverSets.Last();
            var totalOvers = modifiedInnings.OversBowled.Count;

            do
            {
                AddOneNewBowlingOver(modifiedInnings, comparison);
                totalOvers++;
            }
            while (totalOvers <= totalOversInOversets);

            var result = await repository.UpdateBowlingScorecard(
                    modifiedMatch,
                    modifiedInnings.MatchInningsId!.Value,
                    _memberKey,
                    _memberName);

            Assert.Equal(modifiedInnings.OversBowled.Count, result.OversBowled.Count);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                Assert.True(comparison.OversAdded.Any());
                foreach (var over in comparison.OversAdded)
                {
                    var savedOver = await connection.QuerySingleOrDefaultAsync<(int? OverNumber, Guid? OverSetId, Guid? BowlerId, int? BallsBowled, int? Wides, int? NoBalls, int? RunsConceded)>(
                       $"SELECT OverNumber, OverSetId, BowlerPlayerIdentityId, BallsBowled, Wides, NoBalls, RunsConceded FROM {Tables.Over} WHERE OverId = @OverId",
                       new { over.OverId }).ConfigureAwait(false);

                    Assert.Equal(over.OverNumber, savedOver.OverNumber);
                    Assert.Equal(over.OverSet!.OverSetId, savedOver.OverSetId);
                    Assert.Equal(over.Bowler!.PlayerIdentityId, savedOver.BowlerId);
                    Assert.Equal(over.BallsBowled, savedOver.BallsBowled);
                    Assert.Equal(over.Wides, savedOver.Wides);
                    Assert.Equal(over.NoBalls, savedOver.NoBalls);
                    Assert.Equal(over.RunsConceded, savedOver.RunsConceded);

                    var oversInOverSet = await connection.QuerySingleOrDefaultAsync<int?>($"SELECT Overs FROM {Tables.OverSet} WHERE OverSetId = @OverSetId",
                        finalOverSet).ConfigureAwait(false);

                    Assert.Equal(finalOverSet.Overs + 1, oversInOverSet);
                }
            }
        }

        [Theory]
        [InlineData(false, false, false, false, false)]
        public async Task UpdateBowlingScorecard_updates_overs_previously_added(bool bowlerHasChanged, bool ballsBowledHasChanged, bool widesHasChanged, bool noBallsHasChanged, bool runsConcededHasChanged)
        {
            var repository = CreateRepository();

            var modifiedMatch = CloneValidMatch();
            var modifiedInnings = modifiedMatch.MatchInnings[0];
            var modifiedOver = modifiedInnings.OversBowled.Last();
            if (bowlerHasChanged)
            {
                modifiedOver.Bowler = _databaseFixture.TestData.PlayerIdentities.First(x => x.Team!.TeamId == modifiedInnings.BowlingTeam!.Team!.TeamId && x.PlayerIdentityId != modifiedOver.Bowler?.PlayerIdentityId);
            }
            if (ballsBowledHasChanged) { modifiedOver.BallsBowled = modifiedOver.BallsBowled.HasValue ? modifiedOver.BallsBowled + 1 : 8; }
            if (widesHasChanged) { modifiedOver.Wides = modifiedOver.Wides.HasValue ? modifiedOver.Wides + 1 : 4; }
            if (noBallsHasChanged) { modifiedOver.NoBalls = modifiedOver.NoBalls.HasValue ? modifiedOver.NoBalls + 1 : 5; }
            if (runsConcededHasChanged) { modifiedOver.RunsConceded = modifiedOver.RunsConceded.HasValue ? modifiedOver.RunsConceded + 1 : 12; }

            var comparison = SetupUnchangedBowlingComparison(modifiedInnings);
            if (bowlerHasChanged || ballsBowledHasChanged || widesHasChanged || noBallsHasChanged || runsConcededHasChanged)
            {
                comparison.OversUnchanged.Remove(modifiedOver);
                comparison.OversChanged.Add((modifiedOver, modifiedOver));
            }

            var result = await repository.UpdateBowlingScorecard(
                    modifiedMatch,
                    modifiedInnings.MatchInningsId!.Value,
                    _memberKey,
                    _memberName);

            Assert.Equal(modifiedInnings.OversBowled.Count, result.OversBowled.Count);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedOver = await connection.QuerySingleOrDefaultAsync<(int? OverNumber, Guid? OverSetId, Guid? BowlerId, int? BallsBowled, int? Wides, int? NoBalls, int? RunsConceded)>(
                   $"SELECT OverNumber, OverSetId, BowlerPlayerIdentityId, BallsBowled, Wides, NoBalls, RunsConceded FROM {Tables.Over} WHERE OverId = @OverId",
                   new { modifiedOver.OverId }).ConfigureAwait(false);

                Assert.Equal(modifiedOver.OverNumber, savedOver.OverNumber);
                Assert.Equal(modifiedOver.OverSet!.OverSetId, savedOver.OverSetId);
                Assert.Equal(modifiedOver.Bowler!.PlayerIdentityId, savedOver.BowlerId);
                Assert.Equal(modifiedOver.BallsBowled, savedOver.BallsBowled);
                Assert.Equal(modifiedOver.Wides, savedOver.Wides);
                Assert.Equal(modifiedOver.NoBalls, savedOver.NoBalls);
                Assert.Equal(modifiedOver.RunsConceded, savedOver.RunsConceded);
            }
        }

        [Fact]
        public async Task UpdateBowlingScorecard_deletes_overs_removed_from_scorecard()
        {
            var repository = CreateRepository();

            var modifiedMatch = CloneValidMatch();
            var modifiedInnings = modifiedMatch.MatchInnings[0];

            var overToRemove = modifiedInnings.OversBowled.Last();
            modifiedInnings.OversBowled.Remove(overToRemove);

            var comparison = SetupUnchangedBowlingComparison(modifiedInnings);
            comparison.OversRemoved.Add(overToRemove);

            var result = await repository.UpdateBowlingScorecard(
                    modifiedMatch,
                    modifiedInnings.MatchInningsId!.Value,
                    _memberKey,
                    _memberName);

            Assert.Equal(modifiedInnings.OversBowled.Count, result.OversBowled.Count);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedOverId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                    $"SELECT OverId FROM {Tables.Over} WHERE OverId = @OverId",
                     overToRemove).ConfigureAwait(false);

                Assert.Null(savedOverId);
            }
        }

        [Fact]
        public async Task UpdateBowlingScorecard_retains_unchanged_overs()
        {
            var repository = CreateRepository();

            var match = _databaseFixture.TestData.MatchInThePastWithFullDetails!;
            var innings = match.MatchInnings.First(x => x.OversBowled.Count > 0);

            var comparison = new BowlingScorecardComparison { OversUnchanged = innings.OversBowled };
            _bowlingScorecardComparer.Setup(x => x.CompareScorecards(It.IsAny<IEnumerable<Over>>(), It.IsAny<IEnumerable<Over>>())).Returns(comparison);

            var result = await repository.UpdateBowlingScorecard(
                    match,
                    innings.MatchInningsId!.Value,
                    _memberKey,
                    _memberName);

            Assert.Equal(innings.OversBowled.Count, result.OversBowled.Count);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                foreach (var over in innings.OversBowled)
                {
                    var savedOver = await connection.QuerySingleOrDefaultAsync<(Guid OverId, int OverNumber, Guid BowlerPlayerIdentityId, int? BallsBowled, int? NoBalls, int? Wides, int? RunsConceded)?>(
                        $"SELECT OverId, OverNumber, BowlerPlayerIdentityId, BallsBowled, NoBalls, Wides, RunsConceded FROM {Tables.Over} WHERE OverId = @OverId",
                        new
                        {
                            over.OverId,
                            over.OverNumber,
                            over.Bowler!.PlayerIdentityId,
                            over.BallsBowled,
                            over.NoBalls,
                            over.Wides,
                            over.RunsConceded
                        }).ConfigureAwait(false);

                    // Don't check OverSetId because that could legitimately change if other overs were added or removed.
                    // For example, two sets of five overs. One over removed from the start and over six becomes over five, in a different set.
                    Assert.NotNull(savedOver);
                    Assert.Equal(over.OverNumber, savedOver.Value.OverNumber);
                    Assert.Equal(over.Bowler.PlayerIdentityId, savedOver.Value.BowlerPlayerIdentityId);
                    Assert.Equal(over.BallsBowled, savedOver.Value.BallsBowled);
                    Assert.Equal(over.NoBalls, savedOver.Value.NoBalls);
                    Assert.Equal(over.Wides, savedOver.Value.Wides);
                    Assert.Equal(over.RunsConceded, savedOver.Value.RunsConceded);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateBowlingScorecard_updates_bowling_figures_and_player_statistics_if_bowling_has_changed(bool bowlingHasChanged)
        {
            var repository = CreateRepository();

            var modifiedMatch = CloneValidMatch();
            var modifiedInnings = modifiedMatch.MatchInnings[0];

            var comparison = SetupUnchangedBowlingComparison(modifiedInnings);

            if (bowlingHasChanged)
            {
                AddOneNewBowlingOver(modifiedInnings, comparison);
            }

            _ = await repository.UpdateBowlingScorecard(
                    modifiedMatch,
                    modifiedInnings.MatchInningsId!.Value,
                    _memberKey,
                    _memberName);

            _statisticsRepository.Verify(x => x.UpdateBowlingFigures(It.Is<MatchInnings>(mi => mi.MatchInningsId == modifiedInnings.MatchInningsId), _memberKey, _memberName, It.IsAny<IDbTransaction>()), bowlingHasChanged ? Times.Once() : Times.Never());
            _statisticsRepository.Verify(x => x.UpdatePlayerStatistics(It.IsAny<IEnumerable<PlayerInMatchStatisticsRecord>>(), It.IsAny<IDbTransaction>()), bowlingHasChanged ? Times.Once() : Times.Never());
        }

        private static void AddOneNewBowlingOver(MatchInnings innings, BowlingScorecardComparison comparison)
        {
            var overToAdd = new Over
            {
                OverId = Guid.NewGuid(),
                Bowler = innings.OversBowled[innings.OversBowled.Count - 2].Bowler,
                BallsBowled = 8,
                NoBalls = 2,
                Wides = 3,
                RunsConceded = 14,
                OverSet = innings.OverSets.FirstOrDefault()
            };
            innings.OversBowled.Add(overToAdd);

            comparison.OversAdded.Add(overToAdd);
        }

        private BattingScorecardComparison SetupUnchangedBattingComparison(MatchInnings innings)
        {
            var comparison = new BattingScorecardComparison
            {
                PlayerInningsUnchanged = new List<PlayerInnings>(innings.PlayerInnings)
            };
            _battingScorecardComparer.Setup(x => x.CompareScorecards(It.IsAny<IEnumerable<PlayerInnings>>(), It.IsAny<IEnumerable<PlayerInnings>>())).Returns(comparison);
            return comparison;
        }

        private BowlingScorecardComparison SetupUnchangedBowlingComparison(MatchInnings innings)
        {
            var comparison = new BowlingScorecardComparison
            {
                OversUnchanged = new List<Over>(innings.OversBowled)
            };
            _bowlingScorecardComparer.Setup(x => x.CompareScorecards(It.IsAny<IEnumerable<Over>>(), It.IsAny<IEnumerable<Over>>())).Returns(comparison);
            return comparison;
        }

        [Fact]
        public async Task UpdateCloseOfPlay_throws_MatchNotFoundException_for_match_id_that_does_not_exist()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<MatchNotFoundException>(
                async () => await repository.UpdateCloseOfPlay(new Stoolball.Matches.Match
                {
                    MatchId = Guid.NewGuid(),
                    Awards = new List<MatchAward>
                    {
                        new MatchAward {
                            Award = new Award { AwardName = StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD },
                            PlayerIdentity = new PlayerIdentity{ Team = new Team{ TeamId = Guid.NewGuid() } }
                        }
                    }
                },
                 _memberKey,
                 _memberName
                ).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateCloseOfPlay_throws_AwardNotFoundException_for_match_award_award_name_that_does_not_exist()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<AwardNotFoundException>(
                async () => await repository.UpdateCloseOfPlay(new Stoolball.Matches.Match
                {
                    MatchId = _databaseFixture.TestData.Matches[0].MatchId,
                    Awards = new List<MatchAward>
                    {
                        new MatchAward {
                            Award = new Award { AwardName = Guid.NewGuid().ToString() },
                            PlayerIdentity = new PlayerIdentity{ Team = new Team{ TeamId = Guid.NewGuid() } }
                        }
                    }
                },
                 _memberKey,
                 _memberName
                ).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UpdateCloseOfPlay_updates_or_preserves_match_name_depending_on_saved_setting(bool updateMatchName)
        {
            var repository = CreateRepository();

            var matchToUpdate = _databaseFixture.TestData.Matches.First(x => x.UpdateMatchNameAutomatically == updateMatchName);
            var nameBefore = matchToUpdate.MatchName;
            var nameAfter = "Updated match name";
            _matchNameBuilder.Setup(x => x.BuildMatchName(It.Is<Stoolball.Matches.Match>(m => m.MatchId == matchToUpdate.MatchId))).Returns(nameAfter);

            var result = await repository.UpdateCloseOfPlay(matchToUpdate, _memberKey, _memberName).ConfigureAwait(false);

            Assert.Equal(updateMatchName ? nameAfter : nameBefore, result.MatchName);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedMatchName = await connection.QuerySingleAsync<string>(
                    $"SELECT MatchName FROM {Tables.Match} WHERE MatchId = @MatchId",
                    new
                    {
                        matchToUpdate.MatchId
                    }).ConfigureAwait(false);

                Assert.Equal(updateMatchName ? nameAfter : nameBefore, savedMatchName);
            }
        }

        [Fact]
        public async Task UpdateCloseOfPlay_saves_match_result()
        {
            var repository = CreateRepository();

            var matchToUpdate = _databaseFixture.TestData.Matches.First(x => x.UpdateMatchNameAutomatically == false);
            var matchResultBefore = matchToUpdate.MatchResultType;
            var matchResultAfter = matchResultBefore == MatchResultType.HomeWin ? MatchResultType.AwayWin : MatchResultType.HomeWin;
            var modifiedMatch = new Stoolball.Matches.Match
            {
                MatchId = matchToUpdate.MatchId,
                MatchName = matchToUpdate.MatchName,
                MatchResultType = matchResultAfter
            };

            var result = await repository.UpdateCloseOfPlay(modifiedMatch, _memberKey, _memberName).ConfigureAwait(false);

            Assert.Equal(matchResultAfter, result.MatchResultType);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedMatchResult = await connection.QuerySingleAsync<string>(
                    $"SELECT MatchResultType FROM {Tables.Match} WHERE MatchId = @MatchId",
                    new
                    {
                        matchToUpdate.MatchId
                    }).ConfigureAwait(false);

                Assert.Equal(matchResultAfter, Enum.Parse(typeof(MatchResultType), savedMatchResult));
            }
        }

        [Fact]
        public async Task UpdateCloseOfPlay_updates_existing_award()
        {
            var repository = CreateRepository();

            // If you change the award or the player identity, that's probably a different award. Only changing the reason is definitely the same award.
            var matchToUpdate = _databaseFixture.TestData.Matches.First(x => x.Awards.Count > 0);
            var awardBefore = matchToUpdate.Awards[0];
            var awardAfter = new MatchAward
            {
                AwardedToId = matchToUpdate.Awards[0].AwardedToId,
                Award = matchToUpdate.Awards[0].Award,
                PlayerIdentity = matchToUpdate.Awards[0].PlayerIdentity,
                Reason = matchToUpdate.Awards[0].Reason + Guid.NewGuid().ToString()
            };
            var modifiedMatch = new Stoolball.Matches.Match
            {
                MatchId = matchToUpdate.MatchId,
                MatchName = matchToUpdate.MatchName,
                Awards = new List<MatchAward> { awardAfter }
            };

            var result = await repository.UpdateCloseOfPlay(modifiedMatch, _memberKey, _memberName).ConfigureAwait(false);

            Assert.Equal(modifiedMatch.Awards.Count, result.Awards.Count);
            Assert.Equal(modifiedMatch.Awards[0].AwardedToId, result.Awards[0].AwardedToId);
            Assert.Equal(modifiedMatch.Awards[0].Reason, result.Awards[0].Reason);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedMatchAward = await connection.QuerySingleOrDefaultAsync<Guid?>(
                    @$"SELECT AwardedToId FROM {Tables.AwardedTo} ma 
                       WHERE AwardedToId = @AwardedToId
                       AND MatchId = @MatchId
                       AND AwardId = @AwardId
                       AND PlayerIdentityId = @PlayerIdentityId
                       AND Reason = @Reason",
                    new
                    {
                        awardAfter.AwardedToId,
                        matchToUpdate.MatchId,
                        awardAfter.Award!.AwardId,
                        awardAfter.PlayerIdentity!.PlayerIdentityId,
                        awardAfter.Reason
                    }).ConfigureAwait(false);

                Assert.NotNull(savedMatchAward);
            }

        }

        [Fact]
        public async Task UpdateCloseOfPlay_adds_new_award()
        {
            var repository = CreateRepository();
            var reason = "A good reason";

            var matchToUpdate = SetupMatchWithNewAward(reason);

            var result = await repository.UpdateCloseOfPlay(matchToUpdate, _memberKey, _memberName).ConfigureAwait(false);

            Assert.Single(result.Awards.Select(aw => aw.PlayerIdentity?.PlayerIdentityId == matchToUpdate.Awards[0].PlayerIdentity!.PlayerIdentityId && aw.Reason == reason));

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedAwardId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                        $@"SELECT ma.AwardedToId
                               FROM {Tables.AwardedTo} ma
                               WHERE ma.MatchId = @MatchId AND ma.PlayerIdentityId = @PlayerIdentityId AND ma.Reason = @reason",
                        new
                        {
                            matchToUpdate.MatchId,
                            matchToUpdate.Awards[0].PlayerIdentity!.PlayerIdentityId,
                            reason
                        }).ConfigureAwait(false);

                Assert.NotNull(savedAwardId);
            }
        }

        private Stoolball.Matches.Match SetupMatchWithNewAward(string reason)
        {
            return new Stoolball.Matches.Match
            {
                MatchId = _databaseFixture.TestData.MatchInThePastWithFullDetails!.MatchId,
                Awards = new List<MatchAward> {
                    new MatchAward
                    {
                        PlayerIdentity = _databaseFixture.TestData.PlayerIdentities.First(pi => _databaseFixture.TestData.MatchInThePastWithFullDetails.Teams
                                                .Where(t => t.Team != null).Select(t => t.Team!.TeamId)
                                                .Contains( pi.Team?.TeamId) ),
                        Award = _databaseFixture.TestData.MatchInThePastWithFullDetails.Awards[0].Award,
                        Reason = reason
                    }
                }
            };
        }

        [Fact]
        public async Task UpdateCloseOfPlay_deletes_removed_award()
        {
            var repository = CreateRepository();

            var matchToUpdate = _databaseFixture.TestData.Matches.First(m => m.Awards.Count > 1);
            var copyOfMatch = new Stoolball.Matches.Match
            {
                MatchId = matchToUpdate.MatchId
            };
            var awardToRemove = matchToUpdate.Awards[0];
            for (var i = 1; i < matchToUpdate.Awards.Count; i++)
            {
                copyOfMatch.Awards.Add(matchToUpdate.Awards[i]);
            }

            var result = await repository.UpdateCloseOfPlay(copyOfMatch, _memberKey, _memberName).ConfigureAwait(false);

            Assert.Equal(copyOfMatch.Awards.Count, result.Awards.Count);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var totalAwardsForMatch = await connection.QuerySingleAsync<int>(
                        $"SELECT COUNT(ma.AwardedToId) FROM {Tables.AwardedTo} ma WHERE ma.MatchId = @MatchId",
                        new { matchToUpdate.MatchId }
                    ).ConfigureAwait(false);

                Assert.Equal(copyOfMatch.Awards.Count, totalAwardsForMatch);

                var removedAward = await connection.QuerySingleOrDefaultAsync<Guid?>(
                        $"SELECT ma.AwardedToId FROM {Tables.AwardedTo} ma WHERE ma.MatchId = @MatchId AND ma.PlayerIdentityId = @PlayerIdentityId AND ma.Reason = @Reason",
                        new
                        {
                            matchToUpdate.MatchId,
                            awardToRemove.PlayerIdentity?.PlayerIdentityId,
                            awardToRemove.Reason
                        }
                    ).ConfigureAwait(false);

                Assert.Null(removedAward);
            }
        }

        [Fact]
        public async Task UpdateCloseOfPlay_updates_player_statistics()
        {
            var repository = CreateRepository();

            var matchToUpdate = SetupMatchWithNewAward(string.Empty);

            _ = await repository.UpdateCloseOfPlay(matchToUpdate, _memberKey, _memberName).ConfigureAwait(false);

            _statisticsRepository.Verify(x => x.UpdatePlayerStatistics(It.IsAny<IEnumerable<PlayerInMatchStatisticsRecord>>(), It.IsAny<IDbTransaction>()), Times.Once());
        }

            var memberKey = Guid.NewGuid();
            var memberName = "Dee Leeter";

        [Fact]
        public async Task Delete_match_succeeds()
        {
            var repo = CreateRepository();

            await repo.DeleteMatch(_databaseFixture.TestData.MatchInThePastWithFullDetails!, _memberKey, _memberName).ConfigureAwait(false);

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var result = await connection.QuerySingleOrDefaultAsync<Guid?>($"SELECT MatchId FROM {Tables.Match} WHERE MatchId = @MatchId", new { _databaseFixture.TestData.MatchInThePastWithFullDetails!.MatchId }).ConfigureAwait(false);
                Assert.Null(result);
            }
        }

        public void Dispose() => _scope.Dispose();
    }
}
