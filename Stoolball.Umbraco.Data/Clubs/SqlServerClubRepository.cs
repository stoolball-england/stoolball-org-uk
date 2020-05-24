using Dapper;
using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Clubs;
using Stoolball.Routing;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Clubs
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

            try
            {
                club.ClubId = Guid.NewGuid();

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        club.ClubRoute = _routeGenerator.GenerateRoute("/clubs", club.ClubName, NoiseWords.ClubRoute);
                        int count;
                        do
                        {
                            count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Club} WHERE ClubRoute = @ClubRoute", new { club.ClubRoute }, transaction).ConfigureAwait(false);
                            if (count > 0)
                            {
                                club.ClubRoute = _routeGenerator.IncrementRoute(club.ClubRoute);
                            }
                        }
                        while (count > 0);

                        await connection.ExecuteAsync(
                            $@"INSERT INTO {Tables.Club} (ClubId, ClubMark, ClubRoute, MemberGroupId, MemberGroupName) 
                                VALUES (@ClubId, @ClubMark, @ClubRoute, @MemberGroupId, @MemberGroupName)",
                            new
                            {
                                club.ClubId,
                                club.ClubMark,
                                club.ClubRoute,
                                club.MemberGroupId,
                                club.MemberGroupName
                            }, transaction).ConfigureAwait(false);

                        await connection.ExecuteAsync($@"INSERT INTO {Tables.ClubName} 
                                (ClubNameId, ClubId, ClubName, FromDate) VALUES (@ClubNameId, @ClubId, @ClubName, GETUTCDATE())",
                            new
                            {
                                ClubNameId = Guid.NewGuid(),
                                club.ClubId,
                                club.ClubName
                            }, transaction).ConfigureAwait(false);

                        await connection.ExecuteAsync($"UPDATE {Tables.Team} SET ClubId = @ClubId WHERE TeamId IN @TeamIds", new { club.ClubId, TeamIds = club.Teams.Select(x => x.TeamId) }, transaction).ConfigureAwait(false);

                        transaction.Commit();
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Create,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = club.EntityUri,
                    State = JsonConvert.SerializeObject(club),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerClubRepository), ex);
            }

            return club;
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

            try
            {
                string routeBeforeUpdate = club.ClubRoute;

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {

                        club.ClubRoute = _routeGenerator.GenerateRoute("/clubs", club.ClubName, NoiseWords.ClubRoute);
                        if (club.ClubRoute != routeBeforeUpdate)
                        {
                            int count;
                            do
                            {
                                count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Club} WHERE ClubRoute = @ClubRoute", new { club.ClubRoute }, transaction).ConfigureAwait(false);
                                if (count > 0)
                                {
                                    club.ClubRoute = _routeGenerator.IncrementRoute(club.ClubRoute);
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
                                club.ClubMark,
                                club.ClubRoute,
                                club.ClubId
                            }, transaction).ConfigureAwait(false);

                        var currentName = await connection.ExecuteScalarAsync<string>($"SELECT ClubName FROM {Tables.ClubName} WHERE ClubId = @ClubId AND UntilDate IS NULL", new { club.ClubId }, transaction).ConfigureAwait(false);
                        if (club.ClubName?.Trim() != currentName?.Trim())
                        {
                            await connection.ExecuteAsync($"UPDATE {Tables.ClubName} SET UntilDate = GETUTCDATE() WHERE ClubId = @ClubId AND UntilDate IS NULL", new { club.ClubId }, transaction).ConfigureAwait(false);
                            await connection.ExecuteAsync($@"INSERT INTO {Tables.ClubName} 
                                (ClubNameId, ClubId, ClubName, FromDate) VALUES (@ClubNameId, @ClubId, @ClubName, GETUTCDATE())",
                                new
                                {
                                    ClubNameId = Guid.NewGuid(),
                                    club.ClubId,
                                    club.ClubName
                                }, transaction).ConfigureAwait(false);
                        }

                        await connection.ExecuteAsync($"UPDATE {Tables.Team} SET ClubId = NULL WHERE TeamId NOT IN @TeamIds", new { TeamIds = club.Teams.Select(x => x.TeamId) }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"UPDATE {Tables.Team} SET ClubId = @ClubId WHERE TeamId IN @TeamIds", new { club.ClubId, TeamIds = club.Teams.Select(x => x.TeamId) }, transaction).ConfigureAwait(false);

                        transaction.Commit();
                    }

                    if (routeBeforeUpdate != club.ClubRoute)
                    {
                        await _redirectsRepository.InsertRedirect(routeBeforeUpdate, club.ClubRoute, null).ConfigureAwait(false);
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Update,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = club.EntityUri,
                    State = JsonConvert.SerializeObject(club),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);

            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerClubRepository), ex);
            }

            return club;
        }
    }
}
