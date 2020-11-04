using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stoolball.Logging;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Tables = Stoolball.Data.SqlServer.Constants.Tables;
using UmbracoLogging = Umbraco.Core.Logging;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerMatchCommentDataMigrator : IMatchCommentDataMigrator
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IAuditRepository _auditRepository;
        private readonly UmbracoLogging.ILogger _logger;
        private readonly ServiceContext _serviceContext;

        public SqlServerMatchCommentDataMigrator(IScopeProvider scopeProvider, IAuditRepository auditRepository, UmbracoLogging.ILogger logger, ServiceContext serviceContext)
        {
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceContext = serviceContext ?? throw new ArgumentNullException(nameof(serviceContext));
        }

        /// <summary>
        /// Clear down all the match comments data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteMatchComments()
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;

                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($@"DELETE FROM {Tables.TournamentComment}").ConfigureAwait(false);
                        await database.ExecuteAsync($@"DELETE FROM {Tables.MatchComment}").ConfigureAwait(false);
                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerMatchCommentDataMigrator), e);
                throw;
            }
        }

        /// <summary>
        /// Save the supplied match comment to the database
        /// </summary>
        public async Task<MigratedMatchComment> MigrateMatchComment(MigratedMatchComment comment)
        {
            if (comment is null)
            {
                throw new System.ArgumentNullException(nameof(comment));
            }

            var migratedMatchComment = new MigratedMatchComment
            {
                MatchCommentId = Guid.NewGuid(),
                MigratedMatchId = comment.MigratedMatchId,
                MigratedMemberId = comment.MigratedMemberId,
                MigratedMemberEmail = comment.MigratedMemberEmail,
                Comment = comment.Comment,
                CommentDate = comment.CommentDate
            };

            migratedMatchComment.MatchId = await GetMatchId(migratedMatchComment.MigratedMatchId).ConfigureAwait(false);
            if (migratedMatchComment.MatchId.HasValue)
            {
                (migratedMatchComment.MemberKey, migratedMatchComment.MemberName) = GetMember(comment.MigratedMemberEmail);
                await CreateMatchComment(migratedMatchComment).ConfigureAwait(false);
            }
            else
            {
                migratedMatchComment.MatchId = await GetTournamentId(migratedMatchComment.MigratedMatchId).ConfigureAwait(false);
                (migratedMatchComment.MemberKey, migratedMatchComment.MemberName) = GetMember(comment.MigratedMemberEmail);
                await CreateTournamentComment(migratedMatchComment).ConfigureAwait(false);
            }

            return migratedMatchComment;
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
                _logger.Error(typeof(SqlServerMatchCommentDataMigrator), e);
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
                _logger.Error(typeof(SqlServerMatchCommentDataMigrator), e);
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

        private async Task CreateMatchComment(MigratedMatchComment migratedMatchComment)
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;
                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($@"INSERT INTO {Tables.MatchComment} 
                            (MatchCommentId, MatchId, MemberKey, MemberName, Comment, CommentDate)
						    VALUES (@0, @1, @2, @3, @4, @5)",
                            migratedMatchComment.MatchCommentId,
                            migratedMatchComment.MatchId,
                            migratedMatchComment.MemberKey,
                            migratedMatchComment.MemberName,
                            migratedMatchComment.Comment,
                            migratedMatchComment.CommentDate
                            ).ConfigureAwait(false);

                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerMatchCommentDataMigrator), e);
                throw;
            }

            await _auditRepository.CreateAudit(new AuditRecord
            {
                Action = AuditAction.Create,
                MemberKey = migratedMatchComment.MemberKey,
                ActorName = migratedMatchComment.MemberName,
                AuditDate = migratedMatchComment.CommentDate,
                EntityUri = new Uri($"https://www.stoolball.org.uk/id/match-comment/{migratedMatchComment.MatchCommentId}"),
                State = JsonConvert.SerializeObject(migratedMatchComment)
            }).ConfigureAwait(false);
        }

        private async Task CreateTournamentComment(MigratedMatchComment migratedMatchComment)
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;
                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($@"INSERT INTO {Tables.TournamentComment} 
                            (TournamentCommentId, TournamentId, MemberKey, MemberName, Comment, CommentDate)
						    VALUES (@0, @1, @2, @3, @4, @5)",
                            migratedMatchComment.MatchCommentId,
                            migratedMatchComment.MatchId,
                            migratedMatchComment.MemberKey,
                            migratedMatchComment.MemberName,
                            migratedMatchComment.Comment,
                            migratedMatchComment.CommentDate
                            ).ConfigureAwait(false);

                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerMatchCommentDataMigrator), e);
                throw;
            }

            await _auditRepository.CreateAudit(new AuditRecord
            {
                Action = AuditAction.Create,
                MemberKey = migratedMatchComment.MemberKey,
                ActorName = migratedMatchComment.MemberName,
                AuditDate = migratedMatchComment.CommentDate,
                EntityUri = new Uri($"https://www.stoolball.org.uk/id/tournament-comment/{migratedMatchComment.MatchCommentId}"),
                State = JsonConvert.SerializeObject(migratedMatchComment)
            }).ConfigureAwait(false);
        }
    }
}
