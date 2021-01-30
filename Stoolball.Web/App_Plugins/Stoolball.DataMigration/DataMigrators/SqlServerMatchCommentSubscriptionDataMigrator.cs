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
    public class SqlServerMatchCommentSubscriptionDataMigrator : IMatchCommentSubscriptionDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly ServiceContext _serviceContext;

        public SqlServerMatchCommentSubscriptionDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, ServiceContext serviceContext)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceContext = serviceContext;
        }

        /// <summary>
        /// Clear down all the match comments subscriptions data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteMatchCommentSubscriptions()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await transaction.Connection.ExecuteAsync($@"DELETE FROM {Tables.NotificationSubscription} WHERE NotificationType = '{NotificationType.MatchComments.ToString()}'", null, transaction).ConfigureAwait(false);
                    await transaction.Connection.ExecuteAsync($@"DELETE FROM {Tables.NotificationSubscription} WHERE NotificationType = '{NotificationType.TournamentComments.ToString()}'", null, transaction).ConfigureAwait(false);
                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Save the supplied match comment subscription to the database
        /// </summary>
        public async Task<MigratedMatchCommentSubscription> MigrateMatchCommentSubscription(MigratedMatchCommentSubscription subscription)
        {
            if (subscription is null)
            {
                throw new System.ArgumentNullException(nameof(subscription));
            }

            var migratedMatchCommentSubscription = new MigratedMatchCommentSubscription
            {
                MatchCommentSubscriptionId = Guid.NewGuid(),
                MigratedMatchId = subscription.MigratedMatchId,
                MigratedMemberId = subscription.MigratedMemberId,
                MigratedMemberEmail = subscription.MigratedMemberEmail,
                DisplayName = subscription.DisplayName,
                SubscriptionDate = subscription.SubscriptionDate,
            };

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    migratedMatchCommentSubscription.MatchId = await GetMatchId(migratedMatchCommentSubscription.MigratedMatchId, transaction).ConfigureAwait(false);
                    if (migratedMatchCommentSubscription.MatchId.HasValue)
                    {
                        (migratedMatchCommentSubscription.MemberKey, migratedMatchCommentSubscription.MemberName) = GetMember(subscription.MigratedMemberEmail);
                        await CreateSubscription(migratedMatchCommentSubscription, NotificationType.MatchComments, transaction).ConfigureAwait(false);
                    }
                    else
                    {
                        migratedMatchCommentSubscription.MatchId = await GetTournamentId(migratedMatchCommentSubscription.MigratedMatchId, transaction).ConfigureAwait(false);
                        (migratedMatchCommentSubscription.MemberKey, migratedMatchCommentSubscription.MemberName) = GetMember(subscription.MigratedMemberEmail);
                        await CreateSubscription(migratedMatchCommentSubscription, NotificationType.TournamentComments, transaction).ConfigureAwait(false);
                    }
                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Migrated, migratedMatchCommentSubscription, GetType(), nameof(MigrateMatchCommentSubscription));
                }
            }

            return migratedMatchCommentSubscription;
        }

        private static async Task<Guid?> GetMatchId(int migratedMatchId, IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            return await transaction.Connection.ExecuteScalarAsync<Guid?>($@"SELECT MatchId FROM {Tables.Match} WHERE MigratedMatchId = @migratedMatchId", new { migratedMatchId }, transaction).ConfigureAwait(false);
        }

        private static async Task<Guid?> GetTournamentId(int migratedMatchId, IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            return await transaction.Connection.ExecuteScalarAsync<Guid?>($@"SELECT TournamentId FROM {Tables.Tournament} WHERE MigratedTournamentId = @migratedMatchId", new { migratedMatchId }, transaction).ConfigureAwait(false);
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

        private async Task CreateSubscription(MigratedMatchCommentSubscription migratedSubscription, NotificationType notificationType, IDbTransaction transaction)
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
                            NotificationSubscriptionId = migratedSubscription.MatchCommentSubscriptionId,
                            migratedSubscription.MemberKey,
                            NotificationType = notificationType.ToString(),
                            Query = (notificationType == NotificationType.MatchComments ? "match=" : "tournament=") + migratedSubscription.MatchId,
                            migratedSubscription.DisplayName,
                            DateSubscribed = migratedSubscription.SubscriptionDate
                        },
                        transaction).ConfigureAwait(false);

            await _auditRepository.CreateAudit(new AuditRecord
            {
                Action = AuditAction.Create,
                MemberKey = migratedSubscription.MemberKey,
                ActorName = migratedSubscription.MemberName,
                AuditDate = migratedSubscription.SubscriptionDate,
                EntityUri = new Uri($"https://www.stoolball.org.uk/id/notification-subscription/{migratedSubscription.MatchCommentSubscriptionId}"),
                State = JsonConvert.SerializeObject(migratedSubscription),
                RedactedState = JsonConvert.SerializeObject(migratedSubscription),
            }, transaction).ConfigureAwait(false);
        }
    }
}
