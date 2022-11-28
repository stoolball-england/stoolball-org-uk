using System;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using Stoolball.Data.Abstractions;
using Stoolball.Html;
using Stoolball.Logging;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Writes stoolball match location data to the Umbraco database
    /// </summary>
    public class SqlServerMatchLocationRepository : IMatchLocationRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<SqlServerMatchLocationRepository> _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IHtmlSanitizer _htmlSanitiser;
        private readonly IStoolballEntityCopier _copier;

        public SqlServerMatchLocationRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository,
            ILogger<SqlServerMatchLocationRepository> logger, IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository, IHtmlSanitizer htmlSanitiser, IStoolballEntityCopier copier)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));
            _copier = copier ?? throw new ArgumentNullException(nameof(copier));
        }

        /// <summary>
        /// Creates a match location and populates the <see cref="MatchLocation.MatchLocationId"/>
        /// </summary>
        /// <returns>The created match location</returns>
        public async Task<MatchLocation> CreateMatchLocation(MatchLocation matchLocation, Guid memberKey, string memberName)
        {
            if (matchLocation is null)
            {
                throw new ArgumentNullException(nameof(matchLocation));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableMatchLocation = _copier.CreateAuditableCopy(matchLocation);
            auditableMatchLocation.MatchLocationId = Guid.NewGuid();
            auditableMatchLocation.MatchLocationNotes = _htmlSanitiser.Sanitize(auditableMatchLocation.MatchLocationNotes);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableMatchLocation.MatchLocationRoute = await _routeGenerator.GenerateUniqueRoute(
                        "/locations", auditableMatchLocation.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute,
                        async route => await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.MatchLocation} WHERE MatchLocationRoute = @MatchLocationRoute", new { MatchLocationRoute = route }, transaction).ConfigureAwait(false)
                    ).ConfigureAwait(false);

                    await connection.ExecuteAsync(
                        $@"INSERT INTO {Tables.MatchLocation} (MatchLocationId, SecondaryAddressableObjectName, PrimaryAddressableObjectName, StreetDescription, Locality, Town,
                                AdministrativeArea, Postcode, ComparableName, GeoPrecision, Latitude, Longitude, MatchLocationNotes, MatchLocationRoute, MemberGroupKey, MemberGroupName) 
                                VALUES (@MatchLocationId, @SecondaryAddressableObjectName, @PrimaryAddressableObjectName, @StreetDescription, @Locality, @Town, @AdministrativeArea, 
                                @Postcode, @ComparableName, @GeoPrecision, @Latitude, @Longitude, @MatchLocationNotes, @MatchLocationRoute, @MemberGroupKey, @MemberGroupName)",
                        new
                        {
                            auditableMatchLocation.MatchLocationId,
                            auditableMatchLocation.SecondaryAddressableObjectName,
                            auditableMatchLocation.PrimaryAddressableObjectName,
                            auditableMatchLocation.StreetDescription,
                            auditableMatchLocation.Locality,
                            auditableMatchLocation.Town,
                            auditableMatchLocation.AdministrativeArea,
                            auditableMatchLocation.Postcode,
                            ComparableName = auditableMatchLocation.ComparableName(),
                            GeoPrecision = auditableMatchLocation.GeoPrecision?.ToString(),
                            auditableMatchLocation.Latitude,
                            auditableMatchLocation.Longitude,
                            auditableMatchLocation.MatchLocationNotes,
                            auditableMatchLocation.MatchLocationRoute,
                            auditableMatchLocation.MemberGroupKey,
                            auditableMatchLocation.MemberGroupName
                        }, transaction).ConfigureAwait(false);

                    var redacted = _copier.CreateRedactedCopy(auditableMatchLocation);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Create,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = matchLocation.EntityUri,
                        State = JsonConvert.SerializeObject(auditableMatchLocation),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Created, redacted, memberName, memberKey, GetType(), nameof(CreateMatchLocation));
                }
            }

            return auditableMatchLocation;
        }

        /// <summary>
        /// Updates a stoolball match location
        /// </summary>
        public async Task<MatchLocation> UpdateMatchLocation(MatchLocation matchLocation, Guid memberKey, string memberName)
        {
            if (matchLocation is null)
            {
                throw new ArgumentNullException(nameof(matchLocation));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableMatchLocation = _copier.CreateAuditableCopy(matchLocation);
            auditableMatchLocation.MatchLocationNotes = _htmlSanitiser.Sanitize(auditableMatchLocation.MatchLocationNotes);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableMatchLocation.MatchLocationRoute = await _routeGenerator.GenerateUniqueRoute(
                        matchLocation.MatchLocationRoute,
                        "/locations", auditableMatchLocation.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute,
                        async route => await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.MatchLocation} WHERE MatchLocationRoute = @MatchLocationRoute", new { MatchLocationRoute = route }, transaction).ConfigureAwait(false)
                    ).ConfigureAwait(false);

                    await connection.ExecuteAsync(
                        $@"UPDATE {Tables.MatchLocation} SET
                                SecondaryAddressableObjectName = @SecondaryAddressableObjectName, 
                                PrimaryAddressableObjectName = @PrimaryAddressableObjectName, 
                                StreetDescription = @StreetDescription, 
                                Locality = @Locality, 
                                Town = @Town,
                                AdministrativeArea = @AdministrativeArea, 
                                Postcode = @Postcode, 
                                ComparableName = @ComparableName,
                                GeoPrecision = @GeoPrecision, 
                                Latitude = @Latitude, 
                                Longitude = @Longitude, 
                                MatchLocationNotes = @MatchLocationNotes, 
                                MatchLocationRoute = @MatchLocationRoute
						        WHERE MatchLocationId = @MatchLocationId",
                        new
                        {
                            auditableMatchLocation.SecondaryAddressableObjectName,
                            auditableMatchLocation.PrimaryAddressableObjectName,
                            auditableMatchLocation.StreetDescription,
                            auditableMatchLocation.Locality,
                            auditableMatchLocation.Town,
                            auditableMatchLocation.AdministrativeArea,
                            auditableMatchLocation.Postcode,
                            ComparableName = auditableMatchLocation.ComparableName(),
                            GeoPrecision = auditableMatchLocation.GeoPrecision?.ToString(),
                            auditableMatchLocation.Latitude,
                            auditableMatchLocation.Longitude,
                            auditableMatchLocation.MatchLocationNotes,
                            auditableMatchLocation.MatchLocationRoute,
                            auditableMatchLocation.MatchLocationId
                        }, transaction).ConfigureAwait(false);

                    var redacted = _copier.CreateRedactedCopy(auditableMatchLocation);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = matchLocation.EntityUri,
                        State = JsonConvert.SerializeObject(auditableMatchLocation),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    if (matchLocation.MatchLocationRoute != auditableMatchLocation.MatchLocationRoute)
                    {
                        await _redirectsRepository.InsertRedirect(matchLocation.MatchLocationRoute, auditableMatchLocation.MatchLocationRoute, null, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(UpdateMatchLocation));
                }

            }

            return auditableMatchLocation;
        }

        /// <summary>
        /// Deletes a stoolball match location
        /// </summary>
        public async Task DeleteMatchLocation(MatchLocation matchLocation, Guid memberKey, string memberName)
        {
            if (matchLocation is null)
            {
                throw new ArgumentNullException(nameof(matchLocation));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($@"UPDATE {Tables.PlayerInMatchStatistics} SET MatchLocationId = NULL WHERE MatchLocationId = @MatchLocationId", new { matchLocation.MatchLocationId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"UPDATE {Tables.Tournament} SET MatchLocationId = NULL WHERE MatchLocationId = @MatchLocationId", new { matchLocation.MatchLocationId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"UPDATE {Tables.Match} SET MatchLocationId = NULL WHERE MatchLocationId = @MatchLocationId", new { matchLocation.MatchLocationId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.TeamMatchLocation} WHERE MatchLocationId = @MatchLocationId", new { matchLocation.MatchLocationId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.MatchLocation} WHERE MatchLocationId = @MatchLocationId", new { matchLocation.MatchLocationId }, transaction).ConfigureAwait(false);

                    var auditableMatchLocation = _copier.CreateAuditableCopy(matchLocation);
                    var redacted = _copier.CreateRedactedCopy(auditableMatchLocation);

                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Delete,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = matchLocation.EntityUri,
                        State = JsonConvert.SerializeObject(auditableMatchLocation),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix(auditableMatchLocation.MatchLocationRoute, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Deleted, redacted, memberName, memberKey, GetType(), nameof(DeleteMatchLocation));
                }
            }

        }
    }
}
