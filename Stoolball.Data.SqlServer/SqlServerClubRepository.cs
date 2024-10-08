﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Stoolball.Clubs;
using Stoolball.Data.Abstractions;
using Stoolball.Logging;
using Stoolball.Routing;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Writes stoolball club data to the Umbraco database
    /// </summary>
    public class SqlServerClubRepository : IClubRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<SqlServerClubRepository> _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IStoolballEntityCopier _copier;

        public SqlServerClubRepository(IDatabaseConnectionFactory databaseConnectionFactory,
            IAuditRepository auditRepository,
            ILogger<SqlServerClubRepository> logger,
            IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository,
            IStoolballEntityCopier copier)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _copier = copier ?? throw new ArgumentNullException(nameof(copier));
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

            var auditableClub = _copier.CreateAuditableCopy(club);
            auditableClub.ClubId = Guid.NewGuid();

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableClub.ClubRoute = await _routeGenerator.GenerateUniqueRoute(
                       "/clubs", auditableClub.ClubName, NoiseWords.ClubRoute,
                       async route => await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Club} WHERE ClubRoute = @ClubRoute", new { ClubRoute = route }, transaction).ConfigureAwait(false)
                   ).ConfigureAwait(false);

                    await connection.ExecuteAsync(
                        $@"INSERT INTO {Tables.Club} (ClubId, ClubRoute, MemberGroupKey, MemberGroupName) 
                                VALUES (@ClubId, @ClubRoute, @MemberGroupKey, @MemberGroupName)",
                        new
                        {
                            auditableClub.ClubId,
                            auditableClub.ClubRoute,
                            auditableClub.MemberGroupKey,
                            auditableClub.MemberGroupName
                        }, transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.ClubVersion} 
                                (ClubVersionId, ClubId, ClubName, ComparableName, FromDate) VALUES (@ClubVersionId, @ClubId, @ClubName, @ComparableName, @FromDate)",
                        new
                        {
                            ClubVersionId = Guid.NewGuid(),
                            auditableClub.ClubId,
                            auditableClub.ClubName,
                            ComparableName = auditableClub.ComparableName(),
                            FromDate = DateTime.UtcNow.Date
                        }, transaction).ConfigureAwait(false);

                    // Check for ClubId IS NULL, otherwise the owner of Club B can edit Club A by reassigning its team
                    await connection.ExecuteAsync($"UPDATE {Tables.Team} SET ClubId = @ClubId WHERE TeamId IN @TeamIds AND ClubId IS NULL", new { auditableClub.ClubId, TeamIds = auditableClub.Teams.Select(x => x.TeamId) }, transaction).ConfigureAwait(false);

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

                    _logger.Info(LoggingTemplates.Created, auditableClub, memberName, memberKey, GetType(), nameof(CreateClub));
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

            var auditableClub = _copier.CreateAuditableCopy(club);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableClub.ClubRoute = await _routeGenerator.GenerateUniqueRoute(
                        club.ClubRoute,
                        "/clubs", auditableClub.ClubName, NoiseWords.ClubRoute,
                        async route => await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Club} WHERE ClubRoute = @ClubRoute", new { ClubRoute = route }, transaction).ConfigureAwait(false)
                    ).ConfigureAwait(false);

                    await connection.ExecuteAsync(
                        $@"UPDATE {Tables.Club} SET
                                ClubRoute = @ClubRoute
						        WHERE ClubId = @ClubId",
                        new
                        {
                            auditableClub.ClubRoute,
                            auditableClub.ClubId
                        }, transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($"UPDATE {Tables.ClubVersion} SET ClubName = @ClubName, ComparableName = @ComparableName WHERE ClubId = @ClubId",
                        new
                        {
                            auditableClub.ClubName,
                            ComparableName = auditableClub.ComparableName(),
                            auditableClub.ClubId
                        },
                        transaction).ConfigureAwait(false);

                    // Add any newly-assigned teams to this club, and set ClubMark = 1 if any other team in this club has ClubMark = 1 (therefore removing teams has to come later)
                    // Check for ClubId IS NULL, otherwise the owner of Club B can edit Club A by reassigning its team.
                    await connection.ExecuteAsync($@"UPDATE {Tables.Team} SET 
                        ClubId = @ClubId,
                        ClubMark = (SELECT CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END FROM {Tables.Team} WHERE ClubId = @ClubId AND ClubMark = 1)
                        WHERE TeamId IN @TeamIds AND ClubId IS NULL",
                        new
                        {
                            auditableClub.ClubId,
                            TeamIds = auditableClub.Teams.Select(x => x.TeamId)
                        },
                        transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($"UPDATE {Tables.Team} SET ClubId = NULL, ClubMark = 0 WHERE ClubId = @ClubId AND TeamId NOT IN @TeamIds", new { auditableClub.ClubId, TeamIds = auditableClub.Teams.Select(x => x.TeamId) }, transaction).ConfigureAwait(false);

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

                    _logger.Info(LoggingTemplates.Updated, auditableClub, memberName, memberKey, GetType(), nameof(SqlServerClubRepository.UpdateClub));
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
                    await connection.ExecuteAsync($@"UPDATE {Tables.PlayerInMatchStatistics} SET ClubId = NULL WHERE ClubId = @ClubId", new { club.ClubId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"UPDATE {Tables.Team} SET ClubId = NULL, ClubMark = 0 WHERE ClubId = @ClubId", new { club.ClubId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.AwardedTo} WHERE ClubId = @ClubId", new { club.ClubId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.ClubVersion} WHERE ClubId = @ClubId", new { club.ClubId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.Club} WHERE ClubId = @ClubId", new { club.ClubId }, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix(club.ClubRoute, transaction).ConfigureAwait(false);

                    var auditableClub = _copier.CreateAuditableCopy(club);
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

                    _logger.Info(LoggingTemplates.Deleted, club, memberName, memberKey, GetType(), nameof(SqlServerClubRepository.DeleteClub));
                }
            }
        }
    }
}
