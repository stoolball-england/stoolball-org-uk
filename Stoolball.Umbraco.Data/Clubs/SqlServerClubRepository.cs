using Dapper;
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
            _routeGenerator = routeGenerator;
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
        }

        /// <summary>
        /// Creates a stoolball club and populates the <see cref="Club.ClubId"/>
        /// </summary>
        public async Task<Club> CreateClub(Club club)
        {
            if (club is null)
            {
                throw new ArgumentNullException(nameof(club));
            }

            try
            {
                /* using (var scope = _scopeProvider.CreateScope())
                 {
                     using (var transaction = scope.Database.GetTransaction())
                     {
                         club.ClubId = Guid.NewGuid();

                         await scope.Database.ExecuteAsync($@"INSERT INTO {Tables.Club}
                         (ClubId, Twitter, Facebook, Instagram, ClubMark, ClubRoute)
                         VALUES (@0, @1, @2, @3, @4, @5)",
                             club.ClubId,
                             PrefixAtSign(club.Twitter),
                             PrefixUrlProtocol(club.Facebook),
                             PrefixAtSign(club.Instagram),
                             club.ClubMark,
                             club.ClubRoute).ConfigureAwait(false);
                         await scope.Database.ExecuteAsync($@"INSERT INTO {Tables.ClubName} 
                             (ClubNameId, ClubId, ClubName, FromDate) VALUES (@0, @1, @2, @3)",
                             Guid.NewGuid(),
                             club.ClubId,
                             club.ClubName,
                             DateTime.UtcNow
                             ).ConfigureAwait(false);
                         transaction.Complete();
                     }
                     scope.Complete();
                 }

                 await _auditRepository.CreateAudit(new AuditRecord
                 {
                     Action = AuditAction.Create,
                     ActorName = nameof(SqlServerClubRepository),
                     EntityUri = club.EntityUri,
                     State = JsonConvert.SerializeObject(club),
                     AuditDate = DateTime.UtcNow
                 }).ConfigureAwait(false);
                 */
                return club;
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerClubRepository), ex);
                throw;
            }
        }


        /// <summary>
        /// Updates a stoolball club
        /// </summary>
        public async Task<Club> UpdateClub(Club club)
        {
            if (club is null)
            {
                throw new ArgumentNullException(nameof(club));
            }

            try
            {
                string updatedRoute = null;
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {

                        updatedRoute = _routeGenerator.GenerateRoute("/clubs", club.ClubName);
                        if (updatedRoute != club.ClubRoute)
                        {
                            int count;
                            do
                            {
                                count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Club} WHERE ClubRoute = @ClubRoute", new { ClubRoute = updatedRoute }, transaction).ConfigureAwait(false);
                                if (count > 0)
                                {
                                    updatedRoute = _routeGenerator.IncrementRoute(updatedRoute);
                                }
                            }
                            while (count > 0);
                        }

                        await connection.ExecuteAsync(
                            $@"UPDATE {Tables.Club} SET
                                ClubMark = @ClubMark,
                                Facebook = @Facebook,
                                Twitter = @Twitter,
                                Instagram = @Instagram,
                                YouTube = @YouTube,
                                Website = @Website,
                                ClubRoute = @ClubRoute
						        WHERE ClubId = @ClubId",
                            new
                            {
                                club.ClubMark,
                                Facebook = PrefixUrlProtocol(club.Facebook),
                                Twitter = PrefixAtSign(club.Twitter),
                                Instagram = PrefixAtSign(club.Instagram),
                                YouTube = PrefixUrlProtocol(club.YouTube),
                                Website = PrefixUrlProtocol(club.Website),
                                ClubRoute = updatedRoute,
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

                    await _redirectsRepository.InsertRedirect(club.ClubRoute, updatedRoute, null).ConfigureAwait(false);
                    club.ClubRoute = updatedRoute;
                }

                /* await _auditRepository.CreateAudit(new AuditRecord
                 {
                     Action = AuditAction.Create,
                     ActorName = nameof(SqlServerClubRepository),
                     EntityUri = club.EntityUri,
                     State = JsonConvert.SerializeObject(club),
                     AuditDate = DateTime.UtcNow
                 }).ConfigureAwait(false);*/

            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerClubRepository), ex);
            }

            return club;
        }

        private string PrefixUrlProtocol(string url)
        {
            url = url?.Trim();
            if (!string.IsNullOrEmpty(url) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }
            return url;
        }

        private string PrefixAtSign(string account)
        {
            account = account?.Trim();
            if (!string.IsNullOrEmpty(account) && !account.StartsWith("@", StringComparison.OrdinalIgnoreCase))
            {
                account = "@" + account;
            }
            return account;
        }
    }
}
