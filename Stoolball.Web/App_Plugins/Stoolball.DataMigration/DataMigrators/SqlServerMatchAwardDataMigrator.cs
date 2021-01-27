using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Stoolball.Awards;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using static Stoolball.Constants;
using Tables = Stoolball.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerMatchAwardDataMigrator : IMatchAwardDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;

        public SqlServerMatchAwardDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Clear down all the match awards data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteMatchAwards()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.AwardedTo}", null, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.Award}", null, transaction).ConfigureAwait(false);

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Save the supplied match award to the database
        /// </summary>
        public async Task<MigratedMatchAward> MigrateMatchAward(MigratedMatchAward award)
        {
            if (award is null)
            {
                throw new System.ArgumentNullException(nameof(award));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var migratedMatchAward = new MigratedMatchAward
                    {
                        MigratedMatchId = award.MigratedMatchId,
                        PlayerOfTheMatchId = award.PlayerOfTheMatchId,
                        PlayerOfTheMatchHomeId = award.PlayerOfTheMatchHomeId,
                        PlayerOfTheMatchAwayId = award.PlayerOfTheMatchAwayId
                    };

                    if (migratedMatchAward.PlayerOfTheMatchId != null)
                    {
                        migratedMatchAward.AwardedToId = Guid.NewGuid();
                        migratedMatchAward.AwardId = await CreateOrGetMatchAwardTypeId("Player of the match", transaction).ConfigureAwait(false);
                        migratedMatchAward.MatchId = await GetMatchId(migratedMatchAward.MigratedMatchId, transaction).ConfigureAwait(false);
                        migratedMatchAward.PlayerIdentityId = await GetPlayerIdentityId(migratedMatchAward.PlayerOfTheMatchId.Value, transaction).ConfigureAwait(false);
                        await CreateMatchAward(migratedMatchAward, transaction).ConfigureAwait(false);
                    }
                    if (migratedMatchAward.PlayerOfTheMatchHomeId != null)
                    {
                        migratedMatchAward.AwardedToId = Guid.NewGuid();
                        migratedMatchAward.AwardId = await CreateOrGetMatchAwardTypeId("Player of the match", transaction).ConfigureAwait(false);
                        migratedMatchAward.MatchId = await GetMatchId(migratedMatchAward.MigratedMatchId, transaction).ConfigureAwait(false);
                        migratedMatchAward.PlayerIdentityId = await GetPlayerIdentityId(migratedMatchAward.PlayerOfTheMatchHomeId.Value, transaction).ConfigureAwait(false);
                        await CreateMatchAward(migratedMatchAward, transaction).ConfigureAwait(false);
                    }
                    if (migratedMatchAward.PlayerOfTheMatchAwayId != null)
                    {
                        migratedMatchAward.AwardedToId = Guid.NewGuid();
                        migratedMatchAward.AwardId = await CreateOrGetMatchAwardTypeId("Player of the match", transaction).ConfigureAwait(false);
                        migratedMatchAward.MatchId = await GetMatchId(migratedMatchAward.MigratedMatchId, transaction).ConfigureAwait(false);
                        migratedMatchAward.PlayerIdentityId = await GetPlayerIdentityId(migratedMatchAward.PlayerOfTheMatchAwayId.Value, transaction).ConfigureAwait(false);
                        await CreateMatchAward(migratedMatchAward, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Migrated, migratedMatchAward, GetType(), nameof(MigrateMatchAward));

                    return migratedMatchAward;
                }
            }

        }

        private static async Task<Guid> GetPlayerIdentityId(int playerOfTheMatchId, IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            return await transaction.Connection.ExecuteScalarAsync<Guid>($@"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @playerOfTheMatchId", new { playerOfTheMatchId }, transaction).ConfigureAwait(false);
        }

        private static async Task<Guid> GetMatchId(int migratedMatchId, IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            return await transaction.Connection.ExecuteScalarAsync<Guid>($@"SELECT MatchId FROM {Tables.Match} WHERE MigratedMatchId = @migratedMatchId", new { migratedMatchId }, transaction).ConfigureAwait(false);
        }

        private async Task CreateMatchAward(MigratedMatchAward migratedMatchAward, IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.AwardedTo} 
                            (AwardedToId, MatchId, AwardId, PlayerIdentityId)
						    VALUES (@AwardedToId, @MatchId, @AwardId, @PlayerIdentityId)",
                        new
                        {
                            migratedMatchAward.AwardedToId,
                            migratedMatchAward.MatchId,
                            migratedMatchAward.AwardId,
                            migratedMatchAward.PlayerIdentityId
                        },
                        transaction).ConfigureAwait(false);

            await _auditRepository.CreateAudit(new AuditRecord
            {
                Action = AuditAction.Create,
                ActorName = nameof(SqlServerMatchAwardDataMigrator),
                AuditDate = DateTime.Now,
                EntityUri = migratedMatchAward.EntityUri,
                State = JsonConvert.SerializeObject(migratedMatchAward),
                RedactedState = JsonConvert.SerializeObject(migratedMatchAward),
            }, transaction).ConfigureAwait(false);
        }

        private static async Task<Guid> CreateOrGetMatchAwardTypeId(string awardName, IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            var awardId = await transaction.Connection.ExecuteScalarAsync<Guid?>($"SELECT AwardId FROM {Tables.Award} WHERE AwardName = @awardName", new { awardName }, transaction).ConfigureAwait(false);
            if (awardId == null)
            {
                awardId = Guid.NewGuid();
                await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.Award} 
                            (AwardId, AwardName, AwardScope)
						    VALUES (@AwardId, @AwardName, @AwardScope)",
                            new
                            {
                                AwardId = awardId,
                                AwardName = awardName,
                                AwardScope = AwardScope.Match.ToString()
                            },
                            transaction).ConfigureAwait(false);
            }
            return awardId.Value;
        }
    }
}
