using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Notifications;
using Stoolball.Umbraco.Data.Audit;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerMatchCommentSubscriptionDataMigrator : IMatchCommentSubscriptionDataMigrator
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly ServiceContext _serviceContext;

        public SqlServerMatchCommentSubscriptionDataMigrator(IScopeProvider scopeProvider, IAuditRepository auditRepository, ILogger logger, ServiceContext serviceContext)
        {
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
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
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;

                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($@"DELETE FROM {Tables.NotificationSubscription} WHERE NotificationType = '{NotificationType.MatchComments.ToString()}'").ConfigureAwait(false);
                        await database.ExecuteAsync($@"DELETE FROM {Tables.NotificationSubscription} WHERE NotificationType = '{NotificationType.TournamentComments.ToString()}'").ConfigureAwait(false);
                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error<SqlServerMatchCommentSubscriptionDataMigrator>(e);
                throw;
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

            migratedMatchCommentSubscription.MatchId = await GetMatchId(migratedMatchCommentSubscription.MigratedMatchId).ConfigureAwait(false);
            if (migratedMatchCommentSubscription.MatchId.HasValue)
            {
                (migratedMatchCommentSubscription.MemberKey, migratedMatchCommentSubscription.MemberName) = GetMember(subscription.MigratedMemberEmail);
                await CreateSubscription(migratedMatchCommentSubscription, NotificationType.MatchComments).ConfigureAwait(false);
            }
            else
            {
                migratedMatchCommentSubscription.MatchId = await GetTournamentId(migratedMatchCommentSubscription.MigratedMatchId).ConfigureAwait(false);
                (migratedMatchCommentSubscription.MemberKey, migratedMatchCommentSubscription.MemberName) = GetMember(subscription.MigratedMemberEmail);
                await CreateSubscription(migratedMatchCommentSubscription, NotificationType.TournamentComments).ConfigureAwait(false);
            }

            return migratedMatchCommentSubscription;
        }

        private async Task<Guid?> GetMatchId(int migratedMatchId)
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;
                    var matchId = await database.ExecuteScalarAsync<Guid?>($@"SELECT MatchId FROM {Tables.Match} WHERE MigratedMatchId = @migratedMatchId", new { migratedMatchId }).ConfigureAwait(false);
                    scope.Complete();
                    return matchId;
                }
            }
            catch (Exception e)
            {
                _logger.Error<SqlServerMatchCommentDataMigrator>(e);
                throw;
            }
        }

        private async Task<Guid?> GetTournamentId(int migratedMatchId)
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;
                    var tournamentId = await database.ExecuteScalarAsync<Guid?>($@"SELECT TournamentId FROM {Tables.Tournament} WHERE MigratedTournamentId = @migratedMatchId", new { migratedMatchId }).ConfigureAwait(false);
                    scope.Complete();
                    return tournamentId;
                }
            }
            catch (Exception e)
            {
                _logger.Error<SqlServerMatchCommentDataMigrator>(e);
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

        private async Task CreateSubscription(MigratedMatchCommentSubscription migratedSubscription, NotificationType notificationType)
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
                            migratedSubscription.MatchCommentSubscriptionId,
                            migratedSubscription.MemberKey,
                            notificationType.ToString(),
                            (notificationType == NotificationType.MatchComments ? "match=" : "tournament=") + migratedSubscription.MatchId,
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
                _logger.Error<SqlServerMatchCommentDataMigrator>(e);
                throw;
            }

            await _auditRepository.CreateAudit(new AuditRecord
            {
                Action = AuditAction.Create,
                MemberKey = migratedSubscription.MemberKey,
                ActorName = migratedSubscription.MemberName,
                AuditDate = migratedSubscription.SubscriptionDate,
                EntityUri = new Uri($"https://www.stoolball.org.uk/id/notification-subscription/{migratedSubscription.MatchCommentSubscriptionId}"),
                State = JsonConvert.SerializeObject(migratedSubscription)
            }).ConfigureAwait(false);
        }
    }
}
