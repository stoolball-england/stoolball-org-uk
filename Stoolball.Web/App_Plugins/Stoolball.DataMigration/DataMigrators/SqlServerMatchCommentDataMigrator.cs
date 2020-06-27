using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Umbraco.Data.Audit;
using System;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerMatchCommentDataMigrator : IMatchCommentDataMigrator
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly ServiceContext _serviceContext;

        public SqlServerMatchCommentDataMigrator(IScopeProvider scopeProvider, IAuditRepository auditRepository, ILogger logger, ServiceContext serviceContext)
        {
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceContext = serviceContext;
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
                _logger.Error<SqlServerMatchCommentDataMigrator>(e);
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
                Comment = comment.Comment,
                CommentDate = comment.CommentDate
            };

            migratedMatchComment.MatchId = await GetMatchId(migratedMatchComment.MigratedMatchId).ConfigureAwait(false);
            if (migratedMatchComment.MatchId.HasValue)
            {
                (migratedMatchComment.MemberKey, migratedMatchComment.MemberName) = GetMember(comment.MigratedMemberId);
                await CreateMatchComment(migratedMatchComment).ConfigureAwait(false);
            }
            else
            {
                migratedMatchComment.MatchId = await GetTournamentId(migratedMatchComment.MigratedMatchId).ConfigureAwait(false);
                (migratedMatchComment.MemberKey, migratedMatchComment.MemberName) = GetMember(comment.MigratedMemberId);
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

        private (Guid? memberKey, string memberName) GetMember(int migratedMemberId)
        {
            var member = _serviceContext.MemberService.GetMembersByPropertyValue("migratedMemberId", migratedMemberId).SingleOrDefault();
            if (member != null)
            {
                return (member.Key, member.Name);
            }
            return (null, null);
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
                            (MatchCommentId, MatchId, MemberKey, Comment, CommentDate)
						    VALUES (@0, @1, @2, @3, @4)",
                            migratedMatchComment.MatchCommentId,
                            migratedMatchComment.MatchId,
                            migratedMatchComment.MemberKey,
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
                _logger.Error<SqlServerMatchCommentDataMigrator>(e);
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
                            (TournamentCommentId, TournamentId, MemberKey, Comment, CommentDate)
						    VALUES (@0, @1, @2, @3, @4)",
                            migratedMatchComment.MatchCommentId,
                            migratedMatchComment.MatchId,
                            migratedMatchComment.MemberKey,
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
                _logger.Error<SqlServerMatchCommentDataMigrator>(e);
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
