using Newtonsoft.Json;
using Stoolball.MatchLocations;
using Stoolball.Umbraco.Data.Audit;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Scoping;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
	public class SqlServerMatchLocationDataMigrator : IMatchLocationDataMigrator
	{
		private readonly IScopeProvider _scopeProvider;
		private readonly IAuditRepository _auditRepository;
		private readonly ILogger _logger;

		public SqlServerMatchLocationDataMigrator(IScopeProvider scopeProvider, IAuditRepository auditRepository, ILogger logger)
		{
			_scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
			_auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Clear down all the match location data ready for a fresh import
		/// </summary>
		/// <returns></returns>
		public async Task DeleteMatchLocations()
		{
			using (var scope = _scopeProvider.CreateScope())
			{
				var database = scope.Database;
				try
				{
					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($@"DELETE FROM {Tables.MatchLocation}").ConfigureAwait(false);
						await database.ExecuteAsync($@"DELETE FROM SkybrudRedirects WHERE DestinationUrl LIKE '/location/%'").ConfigureAwait(false);
						transaction.Complete();
					}
				}
				catch (Exception e)
				{
					_logger.Error<SqlServerSchoolDataMigrator>(e);
					throw;
				}
				scope.Complete();
			}
		}

		/// <summary>
		/// Save the supplied match location to the database with its existing <see cref="MatchLocation.MatchLocationId"/>
		/// </summary>
		public async Task MigrateMatchLocation(MatchLocation matchLocation)
		{
			if (matchLocation is null)
			{
				throw new System.ArgumentNullException(nameof(matchLocation));
			}

			var migratedMatchLocation = new MatchLocation
			{
				MatchLocationId = matchLocation.MatchLocationId,
				SortName = matchLocation.SortName,
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
				MatchLocationRoute = "location" + matchLocation.MatchLocationRoute.Substring(6)
			};

			using (var scope = _scopeProvider.CreateScope())
			{
				try
				{
					var database = scope.Database;
					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.MatchLocation} ON").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.MatchLocation}
						(MatchLocationId, SortName, SecondaryAddressableObjectName, PrimaryAddressableObjectName, StreetDescription, 
						 Locality, Town, AdministrativeArea, Postcode, Latitude, Longitude, GeoPrecision, MatchLocationNotes, MatchLocationRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13)",
							migratedMatchLocation.MatchLocationId,
							migratedMatchLocation.SortName,
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
							migratedMatchLocation.MatchLocationRoute).ConfigureAwait(false);
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.MatchLocation} OFF").ConfigureAwait(false);

						await InsertRedirect(database, matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, string.Empty).ConfigureAwait(false);
						await InsertRedirect(database, matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, "/matches").ConfigureAwait(false);
						await InsertRedirect(database, matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, "/statistics").ConfigureAwait(false);
						await InsertRedirect(database, matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, "/calendar").ConfigureAwait(false);
						await InsertRedirect(database, matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, "/calendar.ics").ConfigureAwait(false);
						await InsertRedirect(database, matchLocation.MatchLocationRoute, migratedMatchLocation.MatchLocationRoute, "/edit").ConfigureAwait(false);

						transaction.Complete();
					}

				}
				catch (Exception e)
				{
					_logger.Error<SqlServerMatchLocationDataMigrator>(e);
					throw;
				}
				scope.Complete();
			}

			try
			{
				await _auditRepository.CreateAudit(new AuditRecord
				{
					Action = AuditAction.Create,
					ActorName = nameof(SqlServerMatchLocationDataMigrator),
					EntityUri = matchLocation.EntityUri,
					State = JsonConvert.SerializeObject(matchLocation),
					AuditDate = matchLocation.DateCreated.Value
				}).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerMatchLocationDataMigrator>(e);
				throw;
			}
		}

		private static async Task InsertRedirect(IUmbracoDatabase database, string originalRoute, string revisedRoute, string routeSuffix)
		{
			await database.ExecuteAsync($@"INSERT INTO SkybrudRedirects 
							([Key], [RootId], [RootKey], [Url], [QueryString], [DestinationType], [DestinationId], [DestinationKey], 
							 [DestinationUrl], [Created], [Updated], [IsPermanent], [IsRegex], [ForwardQueryString])
							 VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13)",
										 Guid.NewGuid().ToString(),
										 0,
										 "00000000-0000-0000-0000-000000000000",
										 "/" + originalRoute + routeSuffix,
										 string.Empty,
										 "url",
										 0,
										 "00000000-0000-0000-0000-000000000000",
										 "/" + revisedRoute + routeSuffix,
										 DateTime.UtcNow,
										 DateTime.UtcNow,
										 true,
										 false,
										 false
										 ).ConfigureAwait(false);
		}
	}
}
