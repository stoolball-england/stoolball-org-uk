using System;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Data.SqlServer;
using Stoolball.Logging;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Security;
using static Stoolball.Constants;
using Tables = Stoolball.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerMatchLocationDataMigrator : IMatchLocationDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IDataRedactor _dataRedactor;

        public SqlServerMatchLocationDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory, IRedirectsRepository redirectsRepository, IAuditHistoryBuilder auditHistoryBuilder,
            IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator, IDataRedactor dataRedactor)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _dataRedactor = dataRedactor ?? throw new ArgumentNullException(nameof(dataRedactor));
        }

        private static MigratedMatchLocation CreateAuditableCopy(MigratedMatchLocation matchLocation)
        {
            return new MigratedMatchLocation
            {
                MatchLocationId = matchLocation.MatchLocationId,
                MigratedMatchLocationId = matchLocation.MigratedMatchLocationId,
                SecondaryAddressableObjectName = matchLocation.SecondaryAddressableObjectName,
                PrimaryAddressableObjectName = matchLocation.PrimaryAddressableObjectName,
                StreetDescription = matchLocation.StreetDescription,
                Locality = matchLocation.Locality,
                Town = matchLocation.Town,
                AdministrativeArea = matchLocation.AdministrativeArea,
                Postcode = matchLocation.Postcode,
                Latitude = matchLocation.Latitude,
                Longitude = matchLocation.Longitude,
                GeoPrecision = matchLocation.GeoPrecision,
                MatchLocationNotes = matchLocation.MatchLocationNotes,
                MemberGroupKey = matchLocation.MemberGroupKey,
                MemberGroupName = matchLocation.MemberGroupName,
            };
        }

        private MigratedMatchLocation CreateRedactedCopy(MigratedMatchLocation matchLocation)
        {
            var redacted = CreateAuditableCopy(matchLocation);
            redacted.MatchLocationNotes = _dataRedactor.RedactPersonalData(redacted.MatchLocationNotes);
            redacted.History.Clear();
            return redacted;
        }

        /// <summary>
        /// Clear down all the match location data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteMatchLocations()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($@"DELETE FROM {Tables.MatchLocation}", null, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/locations/", transaction).ConfigureAwait(false);

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Save the supplied match location to the database with its existing <see cref="MatchLocation.MatchLocationId"/>
        /// </summary>
        public async Task<MatchLocation> MigrateMatchLocation(MigratedMatchLocation matchLocation)
        {
            if (matchLocation is null)
            {
                throw new ArgumentNullException(nameof(matchLocation));
            }

            var migratedMatchLocation = CreateAuditableCopy(matchLocation);
            migratedMatchLocation.MatchLocationId = Guid.NewGuid();

            // if there's only a SAON, move it to PAON
            if (string.IsNullOrEmpty(migratedMatchLocation.PrimaryAddressableObjectName) && !string.IsNullOrEmpty(migratedMatchLocation.SecondaryAddressableObjectName))
            {
                migratedMatchLocation.PrimaryAddressableObjectName = migratedMatchLocation.SecondaryAddressableObjectName;
                migratedMatchLocation.SecondaryAddressableObjectName = string.Empty;
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    migratedMatchLocation.MatchLocationRoute = _routeGenerator.GenerateRoute("/locations", migratedMatchLocation.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute);
                    int count;
                    do
                    {
                        count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.MatchLocation} WHERE MatchLocationRoute = @MatchLocationRoute", new { migratedMatchLocation.MatchLocationRoute }, transaction).ConfigureAwait(false);
                        if (count > 0)
                        {
                            migratedMatchLocation.MatchLocationRoute = _routeGenerator.IncrementRoute(migratedMatchLocation.MatchLocationRoute);
                        }
                    }
                    while (count > 0);

                    _auditHistoryBuilder.BuildInitialAuditHistory(matchLocation, migratedMatchLocation, nameof(SqlServerMatchLocationDataMigrator), CreateRedactedCopy);

                    await connection.ExecuteAsync($@"INSERT INTO {Tables.MatchLocation}
						(MatchLocationId, MigratedMatchLocationId, SortName, SecondaryAddressableObjectName, PrimaryAddressableObjectName, StreetDescription, 
						 Locality, Town, AdministrativeArea, Postcode, Latitude, Longitude, GeoPrecision, MatchLocationNotes, MemberGroupKey, MemberGroupName, MatchLocationRoute)
						VALUES 
                        (@MatchLocationId, @MigratedMatchLocationId, @SortName, @SecondaryAddressableObjectName, @PrimaryAddressableObjectName, @StreetDescription, 
                        @Locality, @Town, @AdministrativeArea, @Postcode, @Latitude, @Longitude, @GeoPrecision, @MatchLocationNotes, @MemberGroupKey, @MemberGroupName, @MatchLocationRoute)",
                    new
                    {
                        migratedMatchLocation.MatchLocationId,
                        migratedMatchLocation.MigratedMatchLocationId,
                        SortName = migratedMatchLocation.SortName(),
                        migratedMatchLocation.SecondaryAddressableObjectName,
                        migratedMatchLocation.PrimaryAddressableObjectName,
                        migratedMatchLocation.StreetDescription,
                        migratedMatchLocation.Locality,
                        migratedMatchLocation.Town,
                        migratedMatchLocation.AdministrativeArea,
                        migratedMatchLocation.Postcode,
                        migratedMatchLocation.Latitude,
                        migratedMatchLocation.Longitude,
                        GeoPrecision = migratedMatchLocation.GeoPrecision?.ToString(),
                        migratedMatchLocation.MatchLocationNotes,
                        migratedMatchLocation.MemberGroupKey,
                        migratedMatchLocation.MemberGroupName,
                        migratedMatchLocation.MatchLocationRoute
                    },
                    transaction).ConfigureAwait(false);

                    await _redirectsRepository.InsertRedirect(matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, string.Empty, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, "/matches", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, "/statistics", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect(matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, "/calendar.ics", transaction).ConfigureAwait(false);

                    foreach (var audit in migratedMatchLocation.History)
                    {
                        await _auditRepository.CreateAudit(audit, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Migrated, CreateRedactedCopy(migratedMatchLocation), GetType(), nameof(MigrateMatchLocation));
                }
            }

            return migratedMatchLocation;
        }
    }
}
