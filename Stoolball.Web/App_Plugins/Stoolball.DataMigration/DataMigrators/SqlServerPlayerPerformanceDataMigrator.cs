using System;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerPlayerPerformanceDataMigrator : IPlayerPerformanceDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;

        public SqlServerPlayerPerformanceDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory, IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository, ILogger logger)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Clear down all the player innings data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeletePlayerInnings()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInnings}", null, transaction).ConfigureAwait(false);
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Save the supplied player innings to the database
        /// </summary>
        public async Task<PlayerInnings> MigratePlayerInnings(MigratedPlayerInnings innings)
        {
            if (innings is null)
            {
                throw new ArgumentNullException(nameof(innings));
            }

            var migratedInnings = new MigratedPlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                MigratedMatchId = innings.MigratedMatchId,
                MigratedPlayerIdentityId = innings.MigratedPlayerIdentityId,
                MigratedTeamId = innings.MigratedTeamId,
                MigratedMatchTeamId = innings.MigratedMatchTeamId,
                MigratedDismissedById = innings.MigratedDismissedById,
                MigratedBowlerId = innings.MigratedBowlerId,
                BattingPosition = innings.BattingPosition,
                DismissalType = innings.DismissalType,
                DismissedBy = innings.DismissedBy,
                Bowler = innings.Bowler,
                RunsScored = innings.RunsScored,
                BallsFaced = innings.BallsFaced
            };

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    _auditHistoryBuilder.BuildInitialAuditHistory(innings, migratedInnings, nameof(SqlServerPlayerPerformanceDataMigrator), x => x);

                    migratedInnings.Match = new Match
                    {
                        MatchId = (await connection.ExecuteScalarAsync<Guid>($"SELECT MatchId FROM {Tables.Match} WHERE MigratedMatchId = @MigratedMatchId", new { migratedInnings.MigratedMatchId }, transaction).ConfigureAwait(false))
                    };
                    migratedInnings.PlayerIdentity = new PlayerIdentity
                    {
                        PlayerIdentityId = (await connection.ExecuteScalarAsync<Guid>($"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @MigratedPlayerIdentityId", new { migratedInnings.MigratedPlayerIdentityId }, transaction).ConfigureAwait(false)),
                        Team = new Team
                        {
                            TeamId = (await connection.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @MigratedTeamId", new { migratedInnings.MigratedTeamId }, transaction).ConfigureAwait(false))
                        }
                    };
                    if (migratedInnings.MigratedDismissedById.HasValue)
                    {
                        migratedInnings.DismissedBy = new PlayerIdentity
                        {
                            PlayerIdentityId = (await connection.ExecuteScalarAsync<Guid>($"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @MigratedDismissedById", new { migratedInnings.MigratedDismissedById }, transaction).ConfigureAwait(false)),
                        };
                    }
                    if (migratedInnings.MigratedBowlerId.HasValue)
                    {
                        migratedInnings.Bowler = new PlayerIdentity
                        {
                            PlayerIdentityId = (await connection.ExecuteScalarAsync<Guid>($"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @MigratedBowlerId", new { migratedInnings.MigratedBowlerId }, transaction).ConfigureAwait(false)),
                        };
                    }
                    var inningsId = (await connection.ExecuteScalarAsync<Guid>(
                        $@"SELECT MatchInningsId FROM {Tables.MatchInnings} 
									WHERE MatchId = @MatchId AND BattingMatchTeamId = (
										SELECT MatchTeamId FROM {Tables.MatchTeam} WHERE MigratedMatchTeamId = @MigratedMatchTeamId
									)",
                        new
                        {
                            migratedInnings.Match.MatchId,
                            migratedInnings.MigratedMatchTeamId
                        },
                        transaction).ConfigureAwait(false));

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.PlayerInnings}
						(PlayerInningsId, MatchInningsId, PlayerIdentityId, BattingPosition, DismissalType, DismissedById, BowlerId, RunsScored, BallsFaced)
						VALUES 
                        (@PlayerInningsId, @MatchInningsId, @PlayerIdentityId, @BattingPosition, @DismissalType, @DismissedById, @BowlerId, @RunsScored, @BallsFaced)",
                    new
                    {
                        migratedInnings.PlayerInningsId,
                        MatchInningsId = inningsId,
                        migratedInnings.PlayerIdentity.PlayerIdentityId,
                        migratedInnings.BattingPosition,
                        DismissalType = migratedInnings.DismissalType?.ToString(),
                        DismissedById = migratedInnings.DismissedBy?.PlayerIdentityId,
                        BowlerId = migratedInnings.Bowler?.PlayerIdentityId,
                        migratedInnings.RunsScored,
                        migratedInnings.BallsFaced
                    },
                    transaction).ConfigureAwait(false);

                    foreach (var audit in migratedInnings.History)
                    {
                        await _auditRepository.CreateAudit(audit, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    migratedInnings.History.Clear();
                    _logger.Info(GetType(), LoggingTemplates.Migrated, migratedInnings, GetType(), nameof(MigratePlayerInnings));
                }

            }

            return migratedInnings;
        }


        /// <summary>
        /// Clear down all the bowling data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteOvers()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($"TRUNCATE TABLE {Tables.BowlingFigures}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"TRUNCATE TABLE {Tables.Over}", null, transaction).ConfigureAwait(false);
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Save the supplied bowling over to the database
        /// </summary>
        public async Task<Over> MigrateOver(MigratedOver over)
        {
            if (over is null)
            {
                throw new ArgumentNullException(nameof(over));
            }

            var migratedOver = new MigratedOver
            {
                OverId = Guid.NewGuid(),
                MigratedMatchId = over.MigratedMatchId,
                MigratedPlayerIdentityId = over.MigratedPlayerIdentityId,
                MigratedTeamId = over.MigratedTeamId,
                MigratedMatchTeamId = over.MigratedMatchTeamId,
                OverNumber = over.OverNumber,
                BallsBowled = over.BallsBowled,
                NoBalls = over.NoBalls,
                Wides = over.Wides,
                RunsConceded = over.RunsConceded
            };

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    _auditHistoryBuilder.BuildInitialAuditHistory(over, migratedOver, nameof(SqlServerPlayerPerformanceDataMigrator), x => x);

                    migratedOver.Match = new Match
                    {
                        MatchId = (await connection.ExecuteScalarAsync<Guid>($"SELECT MatchId FROM {Tables.Match} WHERE MigratedMatchId = @MigratedMatchId", new { migratedOver.MigratedMatchId }, transaction).ConfigureAwait(false))
                    };
                    migratedOver.PlayerIdentity = new PlayerIdentity
                    {
                        PlayerIdentityId = (await connection.ExecuteScalarAsync<Guid>($"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @MigratedPlayerIdentityId", new { migratedOver.MigratedPlayerIdentityId }, transaction).ConfigureAwait(false)),
                        Team = new Team
                        {
                            TeamId = (await connection.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @MigratedTeamId", new { migratedOver.MigratedTeamId }, transaction).ConfigureAwait(false))
                        }
                    };
                    var inningsId = (await connection.ExecuteScalarAsync<Guid>(
                        $@"SELECT MatchInningsId FROM {Tables.MatchInnings} 
									WHERE MatchId = @MatchId AND BowlingMatchTeamId = (
										SELECT MatchTeamId FROM {Tables.MatchTeam} WHERE MigratedMatchTeamId = @MigratedMatchTeamId
									)",
                        new
                        {
                            migratedOver.Match.MatchId,
                            migratedOver.MigratedMatchTeamId
                        },
                        transaction).ConfigureAwait(false));

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.Over}
						(OverId, MatchInningsId, PlayerIdentityId, OverNumber, BallsBowled, NoBalls, Wides, RunsConceded)
						VALUES (@OverId, @MatchInningsId, @PlayerIdentityId, @OverNumber, @BallsBowled, @NoBalls, @Wides, @RunsConceded)",
                    new
                    {
                        migratedOver.OverId,
                        MatchInningsId = inningsId,
                        migratedOver.PlayerIdentity.PlayerIdentityId,
                        migratedOver.OverNumber,
                        migratedOver.BallsBowled,
                        migratedOver.NoBalls,
                        migratedOver.Wides,
                        migratedOver.RunsConceded
                    },
                    transaction).ConfigureAwait(false);

                    foreach (var audit in migratedOver.History)
                    {
                        await _auditRepository.CreateAudit(audit, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    migratedOver.History.Clear();
                    _logger.Info(GetType(), LoggingTemplates.Migrated, migratedOver, GetType(), nameof(MigrateOver));
                }
            }
            return migratedOver;
        }
    }
}
