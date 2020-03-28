using Newtonsoft.Json;
using Stoolball.Clubs;
using Stoolball.Umbraco.Data.Audit;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Scoping;
using StoolballMigrations = Stoolball.Umbraco.Data.Migrations;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
	public class SqlServerClubDataMigrator : IClubDataMigrator
	{
		private readonly IScopeProvider _scopeProvider;
		private readonly IAuditRepository _auditRepository;
		private readonly ILogger _logger;

		public SqlServerClubDataMigrator(IScopeProvider scopeProvider, IAuditRepository auditRepository, ILogger logger)
		{
			_scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
			_auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Clear down all the club data ready for a fresh import
		/// </summary>
		/// <returns></returns>
		public async Task DeleteClubs()
		{
			using (var scope = _scopeProvider.CreateScope())
			{
				var database = scope.Database;
				try
				{
					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"DELETE FROM {StoolballMigrations.Constants.Tables.ClubName}").ConfigureAwait(false);
						await database.ExecuteAsync($@"DELETE FROM {StoolballMigrations.Constants.Tables.Club}").ConfigureAwait(false);
						await database.ExecuteAsync($@"DELETE FROM SkybrudRedirects WHERE DestinationUrl LIKE '/club/%'").ConfigureAwait(false);
						transaction.Complete();
					}
				}
				catch (Exception e)
				{
					_logger.Error<SqlServerClubDataMigrator>(e);
					throw;
				}
				scope.Complete();
			}
		}

		/// <summary>
		/// Save the supplied Club to the database with its existing <see cref="Club.ClubId"/>
		/// </summary>
		public async Task MigrateClub(Club club)
		{
			if (club is null)
			{
				throw new System.ArgumentNullException(nameof(club));
			}

			var migratedClub = new Club
			{
				ClubId = club.ClubId,
				ClubName = club.ClubName,
				PlaysOutdoors = club.PlaysOutdoors,
				PlaysIndoors = club.PlaysIndoors,
				Twitter = club.Twitter,
				Facebook = club.Facebook,
				Instagram = club.Instagram,
				ClubMark = club.ClubMark,
				HowManyPlayers = club.HowManyPlayers,
				ClubRoute = "club/" + club.ClubRoute,
				DateCreated = club.DateCreated.HasValue && club.DateCreated <= club.DateUpdated ? club.DateCreated : System.Data.SqlTypes.SqlDateTime.MinValue.Value
			};

			if (migratedClub.ClubRoute.EndsWith("club", StringComparison.OrdinalIgnoreCase))
			{
				migratedClub.ClubRoute = migratedClub.ClubRoute.Substring(0, migratedClub.ClubRoute.Length - 4);
			}

			using (var scope = _scopeProvider.CreateScope())
			{
				try
				{
					var database = scope.Database;
					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"SET IDENTITY_INSERT {StoolballMigrations.Constants.Tables.Club} ON").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {StoolballMigrations.Constants.Tables.Club}
						(ClubId, PlaysOutdoors, PlaysIndoors, Twitter, Facebook, Instagram, ClubMark, HowManyPlayers, ClubRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8)",
							migratedClub.ClubId,
							migratedClub.PlaysOutdoors,
							migratedClub.PlaysIndoors,
							migratedClub.Twitter,
							migratedClub.Facebook,
							migratedClub.Instagram,
							migratedClub.ClubMark,
							migratedClub.HowManyPlayers,
							migratedClub.ClubRoute).ConfigureAwait(false);
						await database.ExecuteAsync($"SET IDENTITY_INSERT {StoolballMigrations.Constants.Tables.Club} OFF").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {StoolballMigrations.Constants.Tables.ClubName} 
							(ClubId, ClubName, FromDate) VALUES (@0, @1, @2)",
							migratedClub.ClubId,
							migratedClub.ClubName,
							migratedClub.DateCreated
							).ConfigureAwait(false);
						await InsertRedirect(database, club.ClubRoute, migratedClub.ClubRoute, string.Empty).ConfigureAwait(false);
						await InsertRedirect(database, club.ClubRoute, migratedClub.ClubRoute, "/edit").ConfigureAwait(false);
						await InsertRedirect(database, club.ClubRoute, migratedClub.ClubRoute, "/matches.rss").ConfigureAwait(false);
						transaction.Complete();
					}

				}
				catch (Exception e)
				{
					_logger.Error<SqlServerClubDataMigrator>(e);
					throw;
				}
				scope.Complete();
			}

			try
			{
				await _auditRepository.CreateAudit(new AuditRecord
				{
					Action = AuditAction.Create,
					ActorName = nameof(SqlServerClubDataMigrator),
					EntityUri = migratedClub.EntityUri,
					State = JsonConvert.SerializeObject(migratedClub),
					AuditDate = migratedClub.DateCreated.Value
				}).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerClubDataMigrator>(e);
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
