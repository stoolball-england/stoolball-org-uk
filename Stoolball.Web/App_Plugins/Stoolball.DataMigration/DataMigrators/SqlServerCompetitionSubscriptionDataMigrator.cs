using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using Stoolball.Notifications;
using Umbraco.Core.Services;
using static Stoolball.Constants;


namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerCompetitionSubscriptionDataMigrator : ICompetitionSubscriptionDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly ServiceContext _serviceContext;

        public SqlServerCompetitionSubscriptionDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, ServiceContext serviceContext)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
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
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.NotificationSubscription} WHERE NotificationType = '{NotificationType.Matches.ToString()}'", null, transaction).ConfigureAwait(false);

                    transaction.Commit();
                }
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

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    migratedSubscription.CompetitionId = await GetCompetitionId(migratedSubscription.MigratedCompetitionId, transaction).ConfigureAwait(false);
                    (migratedSubscription.MemberKey, migratedSubscription.MemberName) = GetMember(subscription.MigratedMemberEmail);
                    await CreateSubscription(migratedSubscription, NotificationType.Matches, transaction).ConfigureAwait(false);

                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Create,
                        MemberKey = migratedSubscription.MemberKey,
                        ActorName = migratedSubscription.MemberName,
                        AuditDate = migratedSubscription.SubscriptionDate,
                        EntityUri = new Uri($"https://www.stoolball.org.uk/id/notification-subscription/{migratedSubscription.CompetitionSubscriptionId}"),
                        State = JsonConvert.SerializeObject(migratedSubscription),
                        RedactedState = JsonConvert.SerializeObject(migratedSubscription)
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Migrated, migratedSubscription, GetType(), nameof(MigrateCompetitionSubscription));
                }
            }
            return migratedSubscription;
        }

        private static async Task<Guid?> GetCompetitionId(int migratedCompetitionId, IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            var competitionId = await transaction.Connection.ExecuteScalarAsync<Guid?>($@"SELECT CompetitionId FROM {Tables.Competition} WHERE MigratedCompetitionId = @migratedCompetitionId", new { migratedCompetitionId }, transaction).ConfigureAwait(false);
            return competitionId;
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

        private static async Task CreateSubscription(MigratedCompetitionSubscription migratedSubscription, NotificationType notificationType, IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.NotificationSubscription} 
                            (NotificationSubscriptionId, MemberKey, NotificationType, Query, DisplayName, DateSubscribed)
						    VALUES (@NotificationSubscriptionId, @MemberKey, @NotificationType, @Query, @DisplayName, @DateSubscribed)",
                        new
                        {
                            NotificationSubscriptionId = migratedSubscription.CompetitionSubscriptionId,
                            migratedSubscription.MemberKey,
                            NotificationType = notificationType.ToString(),
                            Query = "competition=" + migratedSubscription.CompetitionId,
                            migratedSubscription.DisplayName,
                            DateSubscribed = migratedSubscription.SubscriptionDate
                        },
                        transaction).ConfigureAwait(false);
        }
    }
}
