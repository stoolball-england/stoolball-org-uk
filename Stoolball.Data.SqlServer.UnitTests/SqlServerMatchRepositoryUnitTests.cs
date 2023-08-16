using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using Ganss.XSS;
using Moq;
using Stoolball.Awards;
using Stoolball.Data.Abstractions;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.UnitTests
{
    public class SqlServerMatchRepositoryUnitTests
    {
        private readonly Mock<IDatabaseConnectionFactory> _connectionFactory = new();
        private readonly Mock<IDbConnection> _databaseConnection = new();
        private readonly Mock<IDbTransaction> _transaction = new();
        private readonly Mock<IDapperWrapper> _dapperWrapper = new();
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

        public SqlServerMatchRepositoryUnitTests()
        {
            _connectionFactory.Setup(x => x.CreateDatabaseConnection()).Returns(_databaseConnection.Object);
            _databaseConnection.Setup(x => x.BeginTransaction()).Returns(_transaction.Object);

            _sanitizer.Setup(x => x.AllowedTags).Returns(new HashSet<string>());
            _sanitizer.Setup(x => x.AllowedAttributes).Returns(new HashSet<string>());
            _sanitizer.Setup(x => x.AllowedCssProperties).Returns(new HashSet<string>());
            _sanitizer.Setup(x => x.AllowedAtRules).Returns(new HashSet<CssRuleType>());
        }

        private SqlServerMatchRepository CreateRepository()
        {
            return new SqlServerMatchRepository(
                            _connectionFactory.Object,
                            _dapperWrapper.Object,
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


        private static Matches.Match CreateValidMatch()
        {
            return new Matches.Match
            {
                MatchId = Guid.NewGuid(),
                MatchInnings = new List<MatchInnings> {
                    new MatchInnings {
                        MatchInningsId = Guid.NewGuid(),
                        BattingTeam = new TeamInMatch
                        {
                            Team = new Team { TeamId = Guid.NewGuid() }
                        },
                        BowlingTeam = new TeamInMatch
                        {
                            Team = new Team { TeamId = Guid.NewGuid() }
                        },
                        OverSets = new List<OverSet>
                        {
                            new OverSet()
                        },
                        OversBowled = new List<Over>
                        {
                            new Over { Bowler = new PlayerIdentity() }
                        },
                        PlayerInnings = new List<PlayerInnings>
                        {
                            new PlayerInnings{ Batter = new PlayerIdentity() }
                        }
                    }
                }
            };
        }

#nullable disable
        [Fact]
        public async Task UpdateBattingScorecard_throws_ArgumentNullException_if_match_is_null()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await repository.UpdateBattingScorecard(
                    null,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Member name").ConfigureAwait(false)
            ).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task UpdateBattingScorecard_throws_ArgumentException_if_matchId_is_null()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateBattingScorecard(
                    new Matches.Match(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Member name").ConfigureAwait(false)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateBattingScorecard_throws_ArgumentException_if_matchInningsId_does_not_match_an_innings()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateBattingScorecard(
                    new Matches.Match { MatchId = Guid.NewGuid() },
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Member name").ConfigureAwait(false)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateBattingScorecard_throws_ArgumentException_if_MatchInnings_does_not_have_BattingTeam_TeamId()
        {
            var repository = CreateRepository();

            var match = CreateValidMatch();
            match.MatchInnings[0].BattingTeam!.Team!.TeamId = null;

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateBattingScorecard(
                    match,
                    match.MatchInnings[0].MatchInningsId!.Value,
                    Guid.NewGuid(),
                    "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateBattingScorecard_throws_ArgumentException_if_MatchInnings_does_not_have_BowlingTeam_TeamId()
        {
            var repository = CreateRepository();

            var match = CreateValidMatch();
            match.MatchInnings[0].BowlingTeam!.Team!.TeamId = null;

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateBattingScorecard(
                    match,
                    match.MatchInnings[0].MatchInningsId!.Value,
                    Guid.NewGuid(),
                    "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }

#nullable disable
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UpdateBattingScorecard_throws_ArgumentException_if_memberName_is_null_or_empty(string? memberName)
        {
            var repository = CreateRepository();

            var match = CreateValidMatch();

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await repository.UpdateBattingScorecard(
                    match,
                    match.MatchInnings[0].MatchInningsId.Value,
                    Guid.NewGuid(),
                    memberName).ConfigureAwait(false)
            ).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task UpdateBattingScorecard_audits_and_logs()
        {
            var repository = CreateRepository();
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";
            var match = CreateValidMatch();

            _dapperWrapper.Setup(x => x.QuerySingleOrDefaultAsync<Matches.Match>(It.IsAny<string>(), It.IsAny<object>(), _transaction.Object, null, null)).ReturnsAsync(match);
            var comparison = new BattingScorecardComparison();
            comparison.PlayerInningsUnchanged.AddRange(match.MatchInnings[0].PlayerInnings);
            comparison.PlayerInningsChanged.Add((comparison.PlayerInningsUnchanged.Last(), comparison.PlayerInningsUnchanged.Last()));
            comparison.PlayerInningsUnchanged.Remove(comparison.PlayerInningsUnchanged[^1]);
            _battingScorecardComparer.Setup(x => x.CompareScorecards(It.IsAny<IEnumerable<PlayerInnings>>(), It.IsAny<IEnumerable<PlayerInnings>>())).Returns(comparison);

            await repository.UpdateBattingScorecard(match, match.MatchInnings[0].MatchInningsId!.Value, memberKey, memberName).ConfigureAwait(false);

            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(a => a.Action == AuditAction.Update), _transaction.Object), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, It.IsAny<MatchInnings>(), memberName, memberKey, typeof(SqlServerMatchRepository), nameof(SqlServerMatchRepository.UpdateBattingScorecard)), Times.Once);
        }

#nullable disable
        [Fact]
        public async Task UpdateBowlingScorecard_throws_ArgumentNullException_if_match_is_null()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await repository.UpdateBowlingScorecard(
                    null,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Member name").ConfigureAwait(false)
            ).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task UpdateBowlingScorecard_throws_ArgumentException_if_matchId_is_null()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateBowlingScorecard(
                    new Matches.Match(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Member name").ConfigureAwait(false)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateBowlingScorecard_throws_ArgumentException_if_matchInningsId_does_not_match_an_innings()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateBowlingScorecard(
                    new Matches.Match { MatchId = Guid.NewGuid() },
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Member name").ConfigureAwait(false)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateBowlingScorecard_throws_ArgumentException_if_MatchInnings_does_not_have_BowlingTeam_TeamId()
        {
            var repository = CreateRepository();

            var match = CreateValidMatch();
            match.MatchInnings[0].BowlingTeam!.Team!.TeamId = null;

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateBowlingScorecard(
                    match,
                    match.MatchInnings[0].MatchInningsId!.Value,
                    Guid.NewGuid(),
                    "Member name").ConfigureAwait(false)).ConfigureAwait(false);
        }
#nullable disable
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UpdateBowlingScorecard_throws_ArgumentException_if_memberName_is_null_or_empty(string? memberName)
        {
            var repository = CreateRepository();

            var match = CreateValidMatch();

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await repository.UpdateBowlingScorecard(
                    match,
                    match.MatchInnings[0].MatchInningsId.Value,
                    Guid.NewGuid(),
                    memberName).ConfigureAwait(false)
            ).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task UpdateBowlingScorecard_audits_and_logs()
        {
            var repository = CreateRepository();
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";
            var match = CreateValidMatch();

            _dapperWrapper.Setup(x => x.QuerySingleOrDefaultAsync<Matches.Match>(It.IsAny<string>(), It.IsAny<object>(), _transaction.Object, null, null)).ReturnsAsync(match);
            _dapperWrapper.Setup(x => x.QueryAsync<OverSet>(It.IsAny<string>(), It.IsAny<object>(), _transaction.Object)).ReturnsAsync(match.MatchInnings[0].OverSets);
            var comparison = new BowlingScorecardComparison();
            comparison.OversUnchanged.AddRange(match.MatchInnings[0].OversBowled);
            comparison.OversChanged.Add((comparison.OversUnchanged.Last(), comparison.OversUnchanged.Last()));
            comparison.OversUnchanged.Remove(comparison.OversUnchanged[^1]);
            _bowlingScorecardComparer.Setup(x => x.CompareScorecards(It.IsAny<IEnumerable<Over>>(), It.IsAny<IEnumerable<Over>>())).Returns(comparison);

            await repository.UpdateBowlingScorecard(match, match.MatchInnings[0].MatchInningsId!.Value, memberKey, memberName).ConfigureAwait(false);

            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(a => a.Action == AuditAction.Update), _transaction.Object), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, It.IsAny<MatchInnings>(), memberName, memberKey, typeof(SqlServerMatchRepository), nameof(SqlServerMatchRepository.UpdateBowlingScorecard)), Times.Once);
        }

        [Fact]
        public async Task UpdateCloseOfPlay_throws_ArgumentNullException_if_match_is_null()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateCloseOfPlay(
                    new Matches.Match(),
                    Guid.NewGuid(),
                    "Member name").ConfigureAwait(false)
            ).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateCloseOfPlay_throws_ArgumentException_if_matchId_is_null()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateCloseOfPlay(
                    new Matches.Match(),
                    Guid.NewGuid(),
                    "Member name").ConfigureAwait(false)
            ).ConfigureAwait(false);
        }

#nullable disable
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UpdateCloseOfPlay_throws_ArgumentException_if_memberName_is_null_or_empty(string? memberName)
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(
             async () => await repository.UpdateCloseOfPlay(
                 new Matches.Match { MatchId = Guid.NewGuid() },
                 Guid.NewGuid(),
                 memberName).ConfigureAwait(false)
            ).ConfigureAwait(false);
        }
#nullable enable

        [Fact]
        public async Task UpdateCloseOfPlay_throws_ArgumentException_for_match_award_with_no_award()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateCloseOfPlay(new Stoolball.Matches.Match
                {
                    MatchId = Guid.NewGuid(),
                    Awards = new List<MatchAward>
                    {
                        new MatchAward{
                            PlayerIdentity = new PlayerIdentity{ Team = new Team{ TeamId = Guid.NewGuid() } }
                        }
                    }
                },
                 Guid.NewGuid(),
                 "Member name"
                ).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateCloseOfPlay_throws_ArgumentException_for_match_award_with_no_award_name()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateCloseOfPlay(new Stoolball.Matches.Match
                {
                    MatchId = Guid.NewGuid(),
                    Awards = new List<MatchAward>
                    {
                        new MatchAward {
                            Award = new Award(),
                            PlayerIdentity = new PlayerIdentity{ Team = new Team{ TeamId = Guid.NewGuid() } }
                        }
                    }
                },
                Guid.NewGuid(),
                "Member name"
                ).ConfigureAwait(false)).ConfigureAwait(false);
        }
        [Fact]
        public async Task UpdateCloseOfPlay_throws_ArgumentException_for_award_with_no_player_identity()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateCloseOfPlay(new Stoolball.Matches.Match
                {
                    MatchId = Guid.NewGuid(),
                    Awards = new List<MatchAward>
                    {
                        new MatchAward { Award = new Award { AwardName = StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD } }
                    }
                },
                 Guid.NewGuid(),
                "Member name"
                ).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateCloseOfPlay_throws_ArgumentException_for_award_with_no_team()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateCloseOfPlay(new Stoolball.Matches.Match
                {
                    MatchId = Guid.NewGuid(),
                    Awards = new List<MatchAward>
                    {
                        new MatchAward{
                            Award = new Award { AwardName = StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD },
                            PlayerIdentity = new PlayerIdentity()
                        }
                    }
                },
                Guid.NewGuid(),
                 "Member name"
                ).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateCloseOfPlay_throws_ArgumentException_for_award_with_no_team_id()
        {
            var repository = CreateRepository();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await repository.UpdateCloseOfPlay(new Matches.Match
                {
                    MatchId = Guid.NewGuid(),
                    Awards = new List<MatchAward>
                    {
                        new MatchAward{
                            Award = new Award { AwardName = StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD },
                            PlayerIdentity = new PlayerIdentity{ Team = new Team() }
                        }
                    }
                },
                 Guid.NewGuid(),
                "Member name"
                ).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task UpdateCloseOfPlay_audits_and_logs()
        {
            var repository = CreateRepository();
            var memberKey = Guid.NewGuid();
            var memberName = "Member name";
            var match = new Matches.Match { MatchId = Guid.NewGuid() };

            _dapperWrapper.Setup(x => x.QuerySingleOrDefaultAsync<Matches.Match>(It.IsAny<string>(), It.IsAny<object>(), _transaction.Object, null, null)).ReturnsAsync(match);

            await repository.UpdateCloseOfPlay(new Matches.Match { MatchId = Guid.NewGuid() }, memberKey, memberName).ConfigureAwait(false);

            _auditRepository.Verify(x => x.CreateAudit(It.Is<AuditRecord>(a => a.Action == AuditAction.Update), _transaction.Object), Times.Once);
            _logger.Verify(x => x.Info(LoggingTemplates.Updated, It.IsAny<Matches.Match>(), memberName, memberKey, typeof(SqlServerMatchRepository), nameof(SqlServerMatchRepository.UpdateCloseOfPlay)), Times.Once);
        }
    }
}
