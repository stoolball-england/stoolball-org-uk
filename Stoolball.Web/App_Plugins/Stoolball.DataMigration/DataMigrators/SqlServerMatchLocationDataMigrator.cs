using System;
using System.Threading.Tasks;
using Stoolball.Logging;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Umbraco.Core.Scoping;
using static Stoolball.Data.SqlServer.Constants;
using Tables = Stoolball.Data.SqlServer.Constants.Tables;
using UmbracoLogging = Umbraco.Core.Logging;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class SqlServerMatchLocationDataMigrator : IMatchLocationDataMigrator
    {
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IScopeProvider _scopeProvider;
        private readonly IAuditHistoryBuilder _auditHistoryBuilder;
        private readonly IAuditRepository _auditRepository;
        private readonly UmbracoLogging.ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;

        public SqlServerMatchLocationDataMigrator(IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditHistoryBuilder auditHistoryBuilder,
            IAuditRepository auditRepository, UmbracoLogging.ILogger logger, IRouteGenerator routeGenerator)
        {
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
            _auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
        }

        /// <summary>
        /// Clear down all the match location data ready for a fresh import
        /// </summary>
        /// <returns></returns>
        public async Task DeleteMatchLocations()
        {
            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;

                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($@"DELETE FROM {Tables.MatchLocation}").ConfigureAwait(false);
                        transaction.Complete();
                    }

                    scope.Complete();
                }

                await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/locations/").ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerMatchLocationDataMigrator), e);
                throw;
            }
        }

        /// <summary>
        /// Save the supplied match location to the database with its existing <see cref="MatchLocation.MatchLocationId"/>
        /// </summary>
        public async Task<MatchLocation> MigrateMatchLocation(MigratedMatchLocation matchLocation)
        {
            if (matchLocation is null)
            {
                throw new System.ArgumentNullException(nameof(matchLocation));
            }

            var migratedMatchLocation = new MigratedMatchLocation
            {
                MatchLocationId = Guid.NewGuid(),
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


            // if there's only a SAON, move it to PAON
            if (string.IsNullOrEmpty(migratedMatchLocation.PrimaryAddressableObjectName) && !string.IsNullOrEmpty(migratedMatchLocation.SecondaryAddressableObjectName))
            {
                migratedMatchLocation.PrimaryAddressableObjectName = migratedMatchLocation.SecondaryAddressableObjectName;
                migratedMatchLocation.SecondaryAddressableObjectName = string.Empty;
            }


            using (var scope = _scopeProvider.CreateScope())
            {
                migratedMatchLocation.MatchLocationRoute = _routeGenerator.GenerateRoute("/locations", migratedMatchLocation.NameAndLocalityOrTownIfDifferent(), NoiseWords.MatchLocationRoute);
                int count;
                do
                {
                    count = await scope.Database.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.MatchLocation} WHERE MatchLocationRoute = @MatchLocationRoute", new { migratedMatchLocation.MatchLocationRoute }).ConfigureAwait(false);
                    if (count > 0)
                    {
                        migratedMatchLocation.MatchLocationRoute = _routeGenerator.IncrementRoute(migratedMatchLocation.MatchLocationRoute);
                    }
                }
                while (count > 0);
                scope.Complete();
            }

            _auditHistoryBuilder.BuildInitialAuditHistory(matchLocation, migratedMatchLocation, nameof(SqlServerMatchLocationDataMigrator));

            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    var database = scope.Database;
                    using (var transaction = database.GetTransaction())
                    {
                        await database.ExecuteAsync($@"INSERT INTO {Tables.MatchLocation}
						(MatchLocationId, MigratedMatchLocationId, SortName, SecondaryAddressableObjectName, PrimaryAddressableObjectName, StreetDescription, 
						 Locality, Town, AdministrativeArea, Postcode, Latitude, Longitude, GeoPrecision, MatchLocationNotes, MemberGroupKey, MemberGroupName, MatchLocationRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14, @15, @16)",
                            migratedMatchLocation.MatchLocationId,
                            migratedMatchLocation.MigratedMatchLocationId,
                            migratedMatchLocation.SortName(),
                            migratedMatchLocation.SecondaryAddressableObjectName,
                            migratedMatchLocation.PrimaryAddressableObjectName,
                            migratedMatchLocation.StreetDescription,
                            migratedMatchLocation.Locality,
                            migratedMatchLocation.Town,
                            migratedMatchLocation.AdministrativeArea,
                            migratedMatchLocation.Postcode,
                            migratedMatchLocation.Latitude,
                            migratedMatchLocation.Longitude,
                            migratedMatchLocation.GeoPrecision?.ToString(),
                            migratedMatchLocation.MatchLocationNotes,
                            migratedMatchLocation.MemberGroupKey,
                            migratedMatchLocation.MemberGroupName,
                            migratedMatchLocation.MatchLocationRoute).ConfigureAwait(false);

                        transaction.Complete();
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                _logger.Error(typeof(SqlServerMatchLocationDataMigrator), e);
                throw;
            }

            await _redirectsRepository.InsertRedirect(matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, string.Empty).ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, "/matches").ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, "/statistics").ConfigureAwait(false);
            await _redirectsRepository.InsertRedirect(matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, "/calendar.ics").ConfigureAwait(false);

            foreach (var audit in migratedMatchLocation.History)
            {
                await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
            }

            return migratedMatchLocation;
        }
    }
}
