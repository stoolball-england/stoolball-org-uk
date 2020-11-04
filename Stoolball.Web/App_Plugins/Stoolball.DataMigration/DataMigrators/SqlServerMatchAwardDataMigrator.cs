using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stoolball.Logging;
using Umbraco.Core.Scoping;
using Tables = Stoolball.Data.SqlServer.Constants.Tables;
using UmbracoLogging = Umbraco.Core.Logging;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerMatchAwardDataMigrator : IMatchAwardDataMigrator
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IAuditRepository _auditRepository;
        private readonly UmbracoLogging.ILogger _logger;

        public SqlServerMatchAwardDataMigrator(IScopeProvider scopeProvider, IAuditRepository auditRepository, UmbracoLogging.ILogger logger)
        {
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Clear down all the match awards data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteMatchAwards()
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;

                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($@"DELETE FROM {Tables.MatchAward}").ConfigureAwait(false);
                        await database.ExecuteAsync($@"DELETE FROM {Tables.Award}").ConfigureAwait(false);
                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerMatchAwardDataMigrator), e);
                throw;
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

            var migratedMatchAward = new MigratedMatchAward
            {
                MigratedMatchId = award.MigratedMatchId,
                PlayerOfTheMatchId = award.PlayerOfTheMatchId,
                PlayerOfTheMatchHomeId = award.PlayerOfTheMatchHomeId,
                PlayerOfTheMatchAwayId = award.PlayerOfTheMatchAwayId
            };

            if (migratedMatchAward.PlayerOfTheMatchId != null)
            {
                migratedMatchAward.MatchAwardId = Guid.NewGuid();
                migratedMatchAward.AwardId = await CreateOrGetMatchAwardTypeId("Player of the match").ConfigureAwait(false);
                migratedMatchAward.MatchId = await GetMatchId(migratedMatchAward.MigratedMatchId).ConfigureAwait(false);
                migratedMatchAward.PlayerIdentityId = await GetPlayerIdentityId(migratedMatchAward.PlayerOfTheMatchId.Value).ConfigureAwait(false);
                await CreateMatchAward(migratedMatchAward).ConfigureAwait(false);
            }
            if (migratedMatchAward.PlayerOfTheMatchHomeId != null)
            {
                migratedMatchAward.MatchAwardId = Guid.NewGuid();
                migratedMatchAward.AwardId = await CreateOrGetMatchAwardTypeId("Player of the match (home)").ConfigureAwait(false);
                migratedMatchAward.MatchId = await GetMatchId(migratedMatchAward.MigratedMatchId).ConfigureAwait(false);
                migratedMatchAward.PlayerIdentityId = await GetPlayerIdentityId(migratedMatchAward.PlayerOfTheMatchHomeId.Value).ConfigureAwait(false);
                await CreateMatchAward(migratedMatchAward).ConfigureAwait(false);
            }
            if (migratedMatchAward.PlayerOfTheMatchAwayId != null)
            {
                migratedMatchAward.MatchAwardId = Guid.NewGuid();
                migratedMatchAward.AwardId = await CreateOrGetMatchAwardTypeId("Player of the match (away)").ConfigureAwait(false);
                migratedMatchAward.MatchId = await GetMatchId(migratedMatchAward.MigratedMatchId).ConfigureAwait(false);
                migratedMatchAward.PlayerIdentityId = await GetPlayerIdentityId(migratedMatchAward.PlayerOfTheMatchAwayId.Value).ConfigureAwait(false);
                await CreateMatchAward(migratedMatchAward).ConfigureAwait(false);
            }


            return migratedMatchAward;
        }

        private async Task<Guid> GetPlayerIdentityId(int playerOfTheMatchId)
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;
                    var playerIdentityId = await database.ExecuteScalarAsync<Guid>($@"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @playerOfTheMatchId", new { playerOfTheMatchId }).ConfigureAwait(false);
                    scope.Complete();
                    return playerIdentityId;
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerMatchAwardDataMigrator), e);
                throw;
            }
        }

        private async Task<Guid> GetMatchId(int migratedMatchId)
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;
                    var matchId = await database.ExecuteScalarAsync<Guid>($@"SELECT MatchId FROM {Tables.Match} WHERE MigratedMatchId = @migratedMatchId", new { migratedMatchId }).ConfigureAwait(false);
                    scope.Complete();
                    return matchId;
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerMatchAwardDataMigrator), e);
                throw;
            }
        }

        private async Task CreateMatchAward(MigratedMatchAward migratedMatchAward)
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;
                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($@"INSERT INTO {Tables.MatchAward} 
                            (MatchAwardId, MatchId, AwardId, PlayerIdentityId)
						    VALUES (@0, @1, @2, @3)",
                            migratedMatchAward.MatchAwardId,
                            migratedMatchAward.MatchId,
                            migratedMatchAward.AwardId,
                            migratedMatchAward.PlayerIdentityId).ConfigureAwait(false);

                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerMatchAwardDataMigrator), e);
                throw;
            }

            await _auditRepository.CreateAudit(new AuditRecord
            {
                Action = AuditAction.Create,
                ActorName = nameof(SqlServerMatchAwardDataMigrator),
                AuditDate = DateTime.Now,
                EntityUri = migratedMatchAward.EntityUri,
                State = JsonConvert.SerializeObject(migratedMatchAward)
            }).ConfigureAwait(false);
        }

        private async Task<Guid> CreateOrGetMatchAwardTypeId(string awardName)
        {
            using (var scope = _scopeProvider.CreateScope())
            {
                var awardId = await scope.Database.ExecuteScalarAsync<Guid?>($"SELECT AwardId FROM {Tables.Award} WHERE AwardName = @awardName", new { awardName }).ConfigureAwait(false);
                if (awardId == null)
                {
                    var equivalentAwardSet = await scope.Database.ExecuteScalarAsync<Guid?>($"SELECT TOP 1 EquivalentAwardSet FROM {Tables.Award}").ConfigureAwait(false);
                    if (!equivalentAwardSet.HasValue)
                    {
                        equivalentAwardSet = Guid.NewGuid();
                    }

                    Guid? awardSet = null;
                    if (awardName.Contains("(home)") || awardName.Contains("away"))
                    {
                        awardSet = await scope.Database.ExecuteScalarAsync<Guid?>($"SELECT TOP 1 AwardSet FROM {Tables.Award} WHERE AwardSet IS NOT NULL").ConfigureAwait(false);
                        if (!awardSet.HasValue)
                        {
                            awardSet = Guid.NewGuid();
                        }
                    }

                    awardId = Guid.NewGuid();
                    await scope.Database.ExecuteAsync($@"INSERT INTO {Tables.Award} 
                            (AwardId, AwardName, AwardSet, EquivalentAwardSet)
						    VALUES (@0, @1, @2, @3)",
                            awardId,
                            awardName,
                            awardSet,
                            equivalentAwardSet).ConfigureAwait(false);
                }
                scope.Complete();
                return awardId.Value;
            }
        }
    }
}
