using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Stoolball.Clubs;
using Stoolball.Logging;
using Stoolball.Routing;
using Stoolball.Teams;
using static Stoolball.Data.SqlServer.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Writes stoolball club data to the Umbraco database
    /// </summary>
    public class SqlServerClubRepository : IClubRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRedirectsRepository _redirectsRepository;

        public SqlServerClubRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator, IRedirectsRepository redirectsRepository)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
        }

        private static Club CreateAuditableCopy(Club club)
        {
            return new Club
            {
                ClubId = club.ClubId,
                ClubName = club.ClubName,
                Teams = club.Teams.Select(x => new Team { TeamId = x.TeamId }).ToList(),
                ClubMark = club.ClubMark,
                ClubRoute = club.ClubRoute,
                MemberGroupKey = club.MemberGroupKey,
                MemberGroupName = club.MemberGroupName
            };
        }

        /// <summary>
        /// Creates a stoolball club and populates the <see cref="Club.ClubId"/>
        /// </summary>
        public async Task<Club> CreateClub(Club club, Guid memberKey, string memberName)
        {
            if (club is null)
            {
                throw new ArgumentNullException(nameof(club));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableClub = CreateAuditableCopy(club);
            auditableClub.ClubId = Guid.NewGuid();

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableClub.ClubRoute = _routeGenerator.GenerateRoute("/clubs", auditableClub.ClubName, NoiseWords.ClubRoute);
                    int count;
                    do
                    {
                        count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Club} WHERE ClubRoute = @ClubRoute", new { auditableClub.ClubRoute }, transaction).ConfigureAwait(false);
                        if (count > 0)
                        {
                            auditableClub.ClubRoute = _routeGenerator.IncrementRoute(auditableClub.ClubRoute);
                        }
                    }
                    while (count > 0);

                    await connection.ExecuteAsync(
                        $@"INSERT INTO {Tables.Club} (ClubId, ClubMark, ClubRoute, MemberGroupKey, MemberGroupName) 
                                VALUES (@ClubId, @ClubMark, @ClubRoute, @MemberGroupKey, @MemberGroupName)",
                        new
                        {
                            auditableClub.ClubId,
                            auditableClub.ClubMark,
                            auditableClub.ClubRoute,
                            auditableClub.MemberGroupKey,
                            auditableClub.MemberGroupName
                        }, transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.ClubName} 
                                (ClubNameId, ClubId, ClubName, FromDate) VALUES (@ClubNameId, @ClubId, @ClubName, GETUTCDATE())",
                        new
                        {
                            ClubNameId = Guid.NewGuid(),
                            auditableClub.ClubId,
                            auditableClub.ClubName
                        }, transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($"UPDATE {Tables.Team} SET ClubId = @ClubId WHERE TeamId IN @TeamIds", new { auditableClub.ClubId, TeamIds = auditableClub.Teams.Select(x => x.TeamId) }, transaction).ConfigureAwait(false);

                    var serialisedClub = JsonConvert.SerializeObject(auditableClub);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Create,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableClub.EntityUri,
                        State = serialisedClub,
                        RedactedState = serialisedClub,
                        AuditDate = DateTime.UtcNow
                    },
                    transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(typeof(SqlServerClubRepository), LoggingTemplates.Created, auditableClub, memberName, memberKey);
                }
            }

            return auditableClub;
        }


        /// <summary>
        /// Updates a stoolball club
        /// </summary>
        public async Task<Club> UpdateClub(Club club, Guid memberKey, string memberName)
        {
            if (club is null)
            {
                throw new ArgumentNullException(nameof(club));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableClub = CreateAuditableCopy(club);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {

                    auditableClub.ClubRoute = _routeGenerator.GenerateRoute("/clubs", auditableClub.ClubName, NoiseWords.ClubRoute);
                    if (auditableClub.ClubRoute != club.ClubRoute)
                    {
                        int count;
                        do
                        {
                            count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Club} WHERE ClubRoute = @ClubRoute", new { auditableClub.ClubRoute }, transaction).ConfigureAwait(false);
                            if (count > 0)
                            {
                                auditableClub.ClubRoute = _routeGenerator.IncrementRoute(auditableClub.ClubRoute);
                            }
                        }
                        while (count > 0);
                    }

                    await connection.ExecuteAsync(
                        $@"UPDATE {Tables.Club} SET
                                ClubMark = @ClubMark,
                                ClubRoute = @ClubRoute
						        WHERE ClubId = @ClubId",
                        new
                        {
                            auditableClub.ClubMark,
                            auditableClub.ClubRoute,
                            auditableClub.ClubId
                        }, transaction).ConfigureAwait(false);

                    var currentName = await connection.ExecuteScalarAsync<string>($"SELECT ClubName FROM {Tables.ClubName} WHERE ClubId = @ClubId AND UntilDate IS NULL", new { auditableClub.ClubId }, transaction).ConfigureAwait(false);
                    if (auditableClub.ClubName?.Trim() != currentName?.Trim())
                    {
                        await connection.ExecuteAsync($"UPDATE {Tables.ClubName} SET UntilDate = GETUTCDATE() WHERE ClubId = @ClubId AND UntilDate IS NULL", new { auditableClub.ClubId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($@"INSERT INTO {Tables.ClubName} 
                                (ClubNameId, ClubId, ClubName, FromDate) VALUES (@ClubNameId, @ClubId, @ClubName, GETUTCDATE())",
                            new
                            {
                                ClubNameId = Guid.NewGuid(),
                                auditableClub.ClubId,
                                auditableClub.ClubName
                            }, transaction).ConfigureAwait(false);
                    }

                    await connection.ExecuteAsync($"UPDATE {Tables.Team} SET ClubId = NULL WHERE ClubId = @ClubId AND TeamId NOT IN @TeamIds", new { auditableClub.ClubId, TeamIds = auditableClub.Teams.Select(x => x.TeamId) }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"UPDATE {Tables.Team} SET ClubId = @ClubId WHERE TeamId IN @TeamIds", new { auditableClub.ClubId, TeamIds = auditableClub.Teams.Select(x => x.TeamId) }, transaction).ConfigureAwait(false);

                    if (club.ClubRoute != auditableClub.ClubRoute)
                    {
                        await _redirectsRepository.InsertRedirect(club.ClubRoute, auditableClub.ClubRoute, null, transaction).ConfigureAwait(false);
                    }

                    var serialisedClub = JsonConvert.SerializeObject(auditableClub);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableClub.EntityUri,
                        State = serialisedClub,
                        RedactedState = serialisedClub,
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(typeof(SqlServerClubRepository), LoggingTemplates.Updated, auditableClub, memberName, memberKey);
                }
            }

            return auditableClub;
        }

        /// <summary>
        /// Delete a stoolball club
        /// </summary>
        public async Task DeleteClub(Club club, Guid memberKey, string memberName)
        {
            if (club is null)
            {
                throw new ArgumentNullException(nameof(club));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($@"UPDATE {Tables.Team} SET ClubId = NULL WHERE ClubId = @ClubId", new { club.ClubId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.ClubName} WHERE ClubId = @ClubId", new { club.ClubId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.Club} WHERE ClubId = @ClubId", new { club.ClubId }, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix(club.ClubRoute, transaction).ConfigureAwait(false);

                    var auditableClub = CreateAuditableCopy(club);
                    var serialisedClub = JsonConvert.SerializeObject(auditableClub);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Delete,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableClub.EntityUri,
                        State = serialisedClub,
                        RedactedState = serialisedClub,
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(typeof(SqlServerClubRepository), LoggingTemplates.Deleted, club, memberName, memberKey);
                }
            }
        }
    }
}
