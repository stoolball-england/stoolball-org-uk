using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.MatchLocations
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

        public SqlServerMatchLocationRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator,
            IRedirectsRepository redirectsRepository, IHtmlSanitizer htmlSanitiser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));

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

            try
            {
                matchLocation.MatchLocationId = Guid.NewGuid();
                matchLocation.MatchLocationNotes = _htmlSanitiser.Sanitize(matchLocation.MatchLocationNotes);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        matchLocation.MatchLocationRoute = _routeGenerator.GenerateRoute("/locations", matchLocation.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute);
                        int count;
                        do
                        {
                            count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.MatchLocation} WHERE MatchLocationRoute = @MatchLocationRoute", new { matchLocation.MatchLocationRoute }, transaction).ConfigureAwait(false);
                            if (count > 0)
                            {
                                matchLocation.MatchLocationRoute = _routeGenerator.IncrementRoute(matchLocation.MatchLocationRoute);
                            }
                        }
                        while (count > 0);

                        await connection.ExecuteAsync(
                            $@"INSERT INTO {Tables.MatchLocation} (MatchLocationId, SecondaryAddressableObjectName, PrimaryAddressableObjectName, StreetDescription, Locality, Town,
                                AdministrativeArea, Postcode, SortName, GeoPrecision, Latitude, Longitude, MatchLocationNotes, MatchLocationRoute, MemberGroupId, MemberGroupName) 
                                VALUES (@MatchLocationId, @SecondaryAddressableObjectName, @PrimaryAddressableObjectName, @StreetDescription, @Locality, @Town, @AdministrativeArea, 
                                @Postcode, @SortName, @GeoPrecision, @Latitude, @Longitude, @MatchLocationNotes, @MatchLocationRoute, @MemberGroupId, @MemberGroupName)",
                            new
                            {
                                matchLocation.MatchLocationId,
                                matchLocation.SecondaryAddressableObjectName,
                                matchLocation.PrimaryAddressableObjectName,
                                matchLocation.StreetDescription,
                                matchLocation.Locality,
                                matchLocation.Town,
                                matchLocation.AdministrativeArea,
                                matchLocation.Postcode,
                                SortName = matchLocation.SortName(),
                                GeoPrecision = matchLocation.GeoPrecision?.ToString(),
                                matchLocation.Latitude,
                                matchLocation.Longitude,
                                matchLocation.MatchLocationNotes,
                                matchLocation.MatchLocationRoute,
                                matchLocation.MemberGroupId,
                                matchLocation.MemberGroupName
                            }, transaction).ConfigureAwait(false);

                        transaction.Commit();
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Create,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = matchLocation.EntityUri,
                    State = JsonConvert.SerializeObject(matchLocation),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerMatchLocationRepository), ex);
            }

            return matchLocation;
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

            try
            {
                matchLocation.MatchLocationNotes = _htmlSanitiser.Sanitize(matchLocation.MatchLocationNotes);

                string routeBeforeUpdate = matchLocation.MatchLocationRoute;

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {

                        matchLocation.MatchLocationRoute = _routeGenerator.GenerateRoute("/locations", matchLocation.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute);
                        if (matchLocation.MatchLocationRoute != routeBeforeUpdate)
                        {
                            int count;
                            do
                            {
                                count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.MatchLocation} WHERE MatchLocationRoute = @MatchLocationRoute", new { matchLocation.MatchLocationRoute }, transaction).ConfigureAwait(false);
                                if (count > 0)
                                {
                                    matchLocation.MatchLocationRoute = _routeGenerator.IncrementRoute(matchLocation.MatchLocationRoute);
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
                                matchLocation.SecondaryAddressableObjectName,
                                matchLocation.PrimaryAddressableObjectName,
                                matchLocation.StreetDescription,
                                matchLocation.Locality,
                                matchLocation.Town,
                                matchLocation.AdministrativeArea,
                                matchLocation.Postcode,
                                GeoPrecision = matchLocation.GeoPrecision?.ToString(),
                                matchLocation.Latitude,
                                matchLocation.Longitude,
                                matchLocation.MatchLocationNotes,
                                matchLocation.MatchLocationRoute,
                                matchLocation.MatchLocationId
                            }, transaction).ConfigureAwait(false);

                        transaction.Commit();
                    }

                    if (routeBeforeUpdate != matchLocation.MatchLocationRoute)
                    {
                        await _redirectsRepository.InsertRedirect(routeBeforeUpdate, matchLocation.MatchLocationRoute, null).ConfigureAwait(false);
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Update,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = matchLocation.EntityUri,
                    State = JsonConvert.SerializeObject(matchLocation),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);

            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerMatchLocationRepository), ex);
            }

            return matchLocation;
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

            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        await connection.ExecuteAsync($@"UPDATE {Tables.Match} SET MatchLocationId = NULL WHERE MatchLocationId = @MatchLocationId", new { matchLocation.MatchLocationId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($@"DELETE FROM {Tables.TeamMatchLocation} WHERE MatchLocationId = @MatchLocationId", new { matchLocation.MatchLocationId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($@"DELETE FROM {Tables.MatchLocation} WHERE MatchLocationId = @MatchLocationId", new { matchLocation.MatchLocationId }, transaction).ConfigureAwait(false);
                        transaction.Commit();
                    }
                }

                await _redirectsRepository.DeleteRedirectsByDestinationPrefix(matchLocation.MatchLocationRoute).ConfigureAwait(false);

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Delete,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = matchLocation.EntityUri,
                    State = JsonConvert.SerializeObject(matchLocation),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error<SqlServerMatchLocationRepository>(e);
                throw;
            }
        }
    }
}
