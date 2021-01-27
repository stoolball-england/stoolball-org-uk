using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using Umbraco.Core.Services;
using static Stoolball.Constants;
using Tables = Stoolball.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerMatchCommentDataMigrator : IMatchCommentDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly ServiceContext _serviceContext;

        public SqlServerMatchCommentDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, ServiceContext serviceContext)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
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
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.Comment}", null, transaction).ConfigureAwait(false);

                    transaction.Commit();
                }
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
                CommentId = Guid.NewGuid(),
                MigratedMatchId = comment.MigratedMatchId,
                MigratedMemberId = comment.MigratedMemberId,
                MigratedMemberEmail = comment.MigratedMemberEmail,
                Comment = comment.Comment,
                CommentDate = comment.CommentDate
            };

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    migratedMatchComment.MatchId = await GetMatchId(migratedMatchComment.MigratedMatchId, transaction).ConfigureAwait(false);
                    if (migratedMatchComment.MatchId.HasValue)
                    {
                        (migratedMatchComment.MemberKey, migratedMatchComment.MemberName) = GetMember(comment.MigratedMemberEmail);
                        await CreateMatchComment(migratedMatchComment, transaction).ConfigureAwait(false);
                    }
                    else
                    {
                        migratedMatchComment.MatchId = await GetTournamentId(migratedMatchComment.MigratedMatchId, transaction).ConfigureAwait(false);
                        (migratedMatchComment.MemberKey, migratedMatchComment.MemberName) = GetMember(comment.MigratedMemberEmail);
                        await CreateTournamentComment(migratedMatchComment, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Migrated, migratedMatchComment, GetType(), nameof(MigrateMatchComment));
                }
            }

            return migratedMatchComment;
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

        private async Task CreateMatchComment(MigratedMatchComment migratedMatchComment, IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.Comment} 
                            (CommentId, MatchId, MemberKey, MemberName, Comment, CommentDate)
						    VALUES (@CommentId, @MatchId, @MemberKey, @MemberName, @Comment, @CommentDate)",
                new
                {
                    migratedMatchComment.CommentId,
                    migratedMatchComment.MatchId,
                    migratedMatchComment.MemberKey,
                    migratedMatchComment.MemberName,
                    migratedMatchComment.Comment,
                    migratedMatchComment.CommentDate
                },
                transaction).ConfigureAwait(false);

            await _auditRepository.CreateAudit(new AuditRecord
            {
                Action = AuditAction.Create,
                MemberKey = migratedMatchComment.MemberKey,
                ActorName = migratedMatchComment.MemberName,
                AuditDate = migratedMatchComment.CommentDate,
                EntityUri = new Uri($"https://www.stoolball.org.uk/id/comment/{migratedMatchComment.CommentId}"),
                State = JsonConvert.SerializeObject(migratedMatchComment),
                RedactedState = JsonConvert.SerializeObject(migratedMatchComment)
            }, transaction).ConfigureAwait(false);
        }

        private async Task CreateTournamentComment(MigratedMatchComment migratedMatchComment, IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            await transaction.Connection.ExecuteAsync($@"INSERT INTO {Tables.Comment} 
                (CommentId, TournamentId, MemberKey, MemberName, Comment, CommentDate)
				VALUES (@CommentId, @TournamentId, @MemberKey, @MemberName, @Comment, @CommentDate)",
                new
                {
                    migratedMatchComment.CommentId,
                    TournamentId = migratedMatchComment.MatchId,
                    migratedMatchComment.MemberKey,
                    migratedMatchComment.MemberName,
                    migratedMatchComment.Comment,
                    migratedMatchComment.CommentDate
                },
                transaction).ConfigureAwait(false);

            await _auditRepository.CreateAudit(new AuditRecord
            {
                Action = AuditAction.Create,
                MemberKey = migratedMatchComment.MemberKey,
                ActorName = migratedMatchComment.MemberName,
                AuditDate = migratedMatchComment.CommentDate,
                EntityUri = new Uri($"https://www.stoolball.org.uk/id/comment/{migratedMatchComment.CommentId}"),
                State = JsonConvert.SerializeObject(migratedMatchComment),
                RedactedState = JsonConvert.SerializeObject(migratedMatchComment)
            }, transaction).ConfigureAwait(false);
        }
    }
}
