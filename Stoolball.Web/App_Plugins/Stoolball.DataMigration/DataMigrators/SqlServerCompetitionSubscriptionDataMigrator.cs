using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stoolball.Logging;
using Stoolball.Notifications;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Tables = Stoolball.Data.SqlServer.Constants.Tables;
using UmbracoLogging = Umbraco.Core.Logging;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerCompetitionSubscriptionDataMigrator : ICompetitionSubscriptionDataMigrator
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IAuditRepository _auditRepository;
        private readonly UmbracoLogging.ILogger _logger;
        private readonly ServiceContext _serviceContext;

        public SqlServerCompetitionSubscriptionDataMigrator(IScopeProvider scopeProvider, IAuditRepository auditRepository, UmbracoLogging.ILogger logger, ServiceContext serviceContext)
        {
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceContext = serviceContext;
        }

        /// <summary>
        /// Clear down all the competition subscriptions data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteCompetitionSubscriptions()
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;

                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($@"DELETE FROM {Tables.NotificationSubscription} WHERE NotificationType = '{NotificationType.Matches.ToString()}'").ConfigureAwait(false);
                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerCompetitionSubscriptionDataMigrator), e);
                throw;
            }
        }

        /// <summary>
        /// Save the supplied competition subscription to the database
        /// </summary>
        public async Task<MigratedCompetitionSubscription> MigrateCompetitionSubscription(MigratedCompetitionSubscription subscription)
        {
            if (subscription is null)
            {
                throw new System.ArgumentNullException(nameof(subscription));
            }

            var migratedSubscription = new MigratedCompetitionSubscription
            {
                CompetitionSubscriptionId = Guid.NewGuid(),
                MigratedCompetitionId = subscription.MigratedCompetitionId,
                MigratedMemberEmail = subscription.MigratedMemberEmail,
                DisplayName = subscription.DisplayName,
                SubscriptionDate = subscription.SubscriptionDate,
            };

            migratedSubscription.CompetitionId = await GetCompetitionId(migratedSubscription.MigratedCompetitionId).ConfigureAwait(false);
            (migratedSubscription.MemberKey, migratedSubscription.MemberName) = GetMember(subscription.MigratedMemberEmail);
            await CreateSubscription(migratedSubscription, NotificationType.Matches).ConfigureAwait(false);

            return migratedSubscription;
        }

        private async Task<Guid?> GetCompetitionId(int migratedCompetitionId)
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;
                    var competitionId = await database.ExecuteScalarAsync<Guid?>($@"SELECT CompetitionId FROM {Tables.Competition} WHERE MigratedCompetitionId = @migratedCompetitionId", new { migratedCompetitionId }).ConfigureAwait(false);
                    scope.Complete();
                    return competitionId;
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerCompetitionSubscriptionDataMigrator), e);
                throw;
            }
        }

        private (Guid? memberKey, string memberName) GetMember(string migratedMemberEmail)
        {
            var member = _serviceContext.MemberService.GetByEmail(migratedMemberEmail);
            if (member == null)
            {
                var memberType = _serviceContext.MemberTypeService.Get("Member");
                member = _serviceContext.MemberService.CreateMemberWithIdentity(migratedMemberEmail, migratedMemberEmail, migratedMemberEmail, memberType);
                member.IsApproved = false;
                member.IsLockedOut = false;
                member.SetValue("blockLogin", false);
                _serviceContext.MemberService.Save(member);
            }
            return (member.Key, member.Name);
        }

        private async Task CreateSubscription(MigratedCompetitionSubscription migratedSubscription, NotificationType notificationType)
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;
                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($@"INSERT INTO {Tables.NotificationSubscription} 
                            (NotificationSubscriptionId, MemberKey, NotificationType, Query, DisplayName, DateSubscribed)
						    VALUES (@0, @1, @2, @3, @4, @5)",
                            migratedSubscription.CompetitionSubscriptionId,
                            migratedSubscription.MemberKey,
                            notificationType.ToString(),
                            "competition=" + migratedSubscription.CompetitionId,
                            migratedSubscription.DisplayName,
                            migratedSubscription.SubscriptionDate
                            ).ConfigureAwait(false);

                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerCompetitionSubscriptionDataMigrator), e);
                throw;
            }

            await _auditRepository.CreateAudit(new AuditRecord
            {
                Action = AuditAction.Create,
                MemberKey = migratedSubscription.MemberKey,
                ActorName = migratedSubscription.MemberName,
                AuditDate = migratedSubscription.SubscriptionDate,
                EntityUri = new Uri($"https://www.stoolball.org.uk/id/notification-subscription/{migratedSubscription.CompetitionSubscriptionId}"),
                State = JsonConvert.SerializeObject(migratedSubscription)
            }).ConfigureAwait(false);
        }
    }
}
