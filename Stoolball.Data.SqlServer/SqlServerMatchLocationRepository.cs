using System;
using System.Threading.Tasks;
using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Logging;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Security;
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
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IHtmlSanitizer _htmlSanitiser;
        private readonly IDataRedactor _dataRedactor;

        public SqlServerMatchLocationRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository, IHtmlSanitizer htmlSanitiser, IDataRedactor dataRedactor)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));
            _dataRedactor = dataRedactor ?? throw new ArgumentNullException(nameof(dataRedactor));
            _htmlSanitiser.AllowedTags.Clear();
            _htmlSanitiser.AllowedTags.Add("p");
            _htmlSanitiser.AllowedTags.Add("h2");
            _htmlSanitiser.AllowedTags.Add("strong");
            _htmlSanitiser.AllowedTags.Add("em");
            _htmlSanitiser.AllowedTags.Add("ul");
            _htmlSanitiser.AllowedTags.Add("ol");
            _htmlSanitiser.AllowedTags.Add("li");
            _htmlSanitiser.AllowedTags.Add("a");
            _htmlSanitiser.AllowedTags.Add("br");
            _htmlSanitiser.AllowedAttributes.Clear();
            _htmlSanitiser.AllowedAttributes.Add("href");
            _htmlSanitiser.AllowedCssProperties.Clear();
            _htmlSanitiser.AllowedAtRules.Clear();
        }

        private static MatchLocation CreateAuditableCopy(MatchLocation matchLocation)
        {
            return new MatchLocation
            {
                MatchLocationId = matchLocation.MatchLocationId,
                SecondaryAddressableObjectName = matchLocation.SecondaryAddressableObjectName,
                PrimaryAddressableObjectName = matchLocation.PrimaryAddressableObjectName,
                StreetDescription = matchLocation.StreetDescription,
                Locality = matchLocation.Locality,
                Town = matchLocation.Town,
                AdministrativeArea = matchLocation.AdministrativeArea,
                Postcode = matchLocation.Postcode.ToUpperInvariant(),
                GeoPrecision = matchLocation.GeoPrecision,
                Latitude = matchLocation.Latitude,
                Longitude = matchLocation.Longitude,
                MatchLocationNotes = matchLocation.MatchLocationNotes,
                MatchLocationRoute = matchLocation.MatchLocationRoute,
                MemberGroupKey = matchLocation.MemberGroupKey,
                MemberGroupName = matchLocation.MemberGroupName
            };
        }
        private MatchLocation CreateRedactedCopy(MatchLocation matchLocation)
        {
            var redacted = CreateAuditableCopy(matchLocation);
            redacted.MatchLocationNotes = _dataRedactor.RedactPersonalData(redacted.MatchLocationNotes);
            return redacted;
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

            var auditableMatchLocation = CreateAuditableCopy(matchLocation);
            auditableMatchLocation.MatchLocationId = Guid.NewGuid();
            auditableMatchLocation.MatchLocationNotes = _htmlSanitiser.Sanitize(auditableMatchLocation.MatchLocationNotes);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableMatchLocation.MatchLocationRoute = _routeGenerator.GenerateRoute("/locations", auditableMatchLocation.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute);
                    int count;
                    do
                    {
                        count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.MatchLocation} WHERE MatchLocationRoute = @MatchLocationRoute", new { auditableMatchLocation.MatchLocationRoute }, transaction).ConfigureAwait(false);
                        if (count > 0)
                        {
                            auditableMatchLocation.MatchLocationRoute = _routeGenerator.IncrementRoute(auditableMatchLocation.MatchLocationRoute);
                        }
                    }
                    while (count > 0);

                    await connection.ExecuteAsync(
                        $@"INSERT INTO {Tables.MatchLocation} (MatchLocationId, SecondaryAddressableObjectName, PrimaryAddressableObjectName, StreetDescription, Locality, Town,
                                AdministrativeArea, Postcode, SortName, GeoPrecision, Latitude, Longitude, MatchLocationNotes, MatchLocationRoute, MemberGroupKey, MemberGroupName) 
                                VALUES (@MatchLocationId, @SecondaryAddressableObjectName, @PrimaryAddressableObjectName, @StreetDescription, @Locality, @Town, @AdministrativeArea, 
                                @Postcode, @SortName, @GeoPrecision, @Latitude, @Longitude, @MatchLocationNotes, @MatchLocationRoute, @MemberGroupKey, @MemberGroupName)",
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
                            SortName = auditableMatchLocation.SortName(),
                            GeoPrecision = auditableMatchLocation.GeoPrecision?.ToString(),
                            auditableMatchLocation.Latitude,
                            auditableMatchLocation.Longitude,
                            auditableMatchLocation.MatchLocationNotes,
                            auditableMatchLocation.MatchLocationRoute,
                            auditableMatchLocation.MemberGroupKey,
                            auditableMatchLocation.MemberGroupName
                        }, transaction).ConfigureAwait(false);

                    var redacted = CreateRedactedCopy(auditableMatchLocation);
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

                    _logger.Info(GetType(), LoggingTemplates.Created, redacted, memberName, memberKey, GetType(), nameof(SqlServerMatchLocationRepository.CreateMatchLocation));
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

            var auditableMatchLocation = CreateAuditableCopy(matchLocation);
            auditableMatchLocation.MatchLocationNotes = _htmlSanitiser.Sanitize(auditableMatchLocation.MatchLocationNotes);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {

                    var baseRoute = _routeGenerator.GenerateRoute("/locations", auditableMatchLocation.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute);
                    if (!_routeGenerator.IsMatchingRoute(matchLocation.MatchLocationRoute, baseRoute))
                    {
                        auditableMatchLocation.MatchLocationRoute = baseRoute;
                        int count;
                        do
                        {
                            count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.MatchLocation} WHERE MatchLocationRoute = @MatchLocationRoute", new { auditableMatchLocation.MatchLocationRoute }, transaction).ConfigureAwait(false);
                            if (count > 0)
                            {
                                auditableMatchLocation.MatchLocationRoute = _routeGenerator.IncrementRoute(auditableMatchLocation.MatchLocationRoute);
                            }
                        }
                        while (count > 0);
                    }

                    await connection.ExecuteAsync(
                        $@"UPDATE {Tables.MatchLocation} SET
                                SecondaryAddressableObjectName = @SecondaryAddressableObjectName, 
                                PrimaryAddressableObjectName = @PrimaryAddressableObjectName, 
                                StreetDescription = @StreetDescription, 
                                Locality = @Locality, 
                                Town = @Town,
                                AdministrativeArea = @AdministrativeArea, 
                                Postcode = @Postcode, 
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
                            GeoPrecision = auditableMatchLocation.GeoPrecision?.ToString(),
                            auditableMatchLocation.Latitude,
                            auditableMatchLocation.Longitude,
                            auditableMatchLocation.MatchLocationNotes,
                            auditableMatchLocation.MatchLocationRoute,
                            auditableMatchLocation.MatchLocationId
                        }, transaction).ConfigureAwait(false);

                    var redacted = CreateRedactedCopy(auditableMatchLocation);
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

                    _logger.Info(GetType(), LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerMatchLocationRepository.UpdateMatchLocation));
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
                    await connection.ExecuteAsync($@"UPDATE {Tables.Match} SET MatchLocationId = NULL WHERE MatchLocationId = @MatchLocationId", new { matchLocation.MatchLocationId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.TeamMatchLocation} WHERE MatchLocationId = @MatchLocationId", new { matchLocation.MatchLocationId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.MatchLocation} WHERE MatchLocationId = @MatchLocationId", new { matchLocation.MatchLocationId }, transaction).ConfigureAwait(false);

                    var auditableMatchLocation = CreateAuditableCopy(matchLocation);
                    var redacted = CreateRedactedCopy(auditableMatchLocation);

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

                    _logger.Info(GetType(), LoggingTemplates.Deleted, redacted, memberName, memberKey, GetType(), nameof(SqlServerMatchLocationRepository.DeleteMatchLocation));
                }
            }

        }
    }
}
