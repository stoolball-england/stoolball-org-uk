using Newtonsoft.Json;
using Stoolball.Clubs;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
	public class SqlServerClubDataMigrator : IClubDataMigrator
	{
		private readonly IRedirectsRepository _redirectsRepository;
		private readonly IScopeProvider _scopeProvider;
		private readonly IAuditRepository _auditRepository;
		private readonly ILogger _logger;

		public SqlServerClubDataMigrator(IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditRepository auditRepository, ILogger logger)
		{
			_redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
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
			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"UPDATE {Tables.Team} SET ClubId = NULL").ConfigureAwait(false);
						await database.ExecuteAsync($"DELETE FROM {Tables.ClubName}").ConfigureAwait(false);
						await database.ExecuteAsync($@"DELETE FROM {Tables.Club}").ConfigureAwait(false);
						transaction.Complete();
					}

					scope.Complete();
				}
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerClubDataMigrator>(e);
				throw;
			}

			await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/clubs/").ConfigureAwait(false);
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
				ClubRoute = "clubs/" + club.ClubRoute,
				DateCreated = club.DateCreated.HasValue && club.DateCreated <= club.DateUpdated ? club.DateCreated : System.Data.SqlTypes.SqlDateTime.MinValue.Value,
				DateUpdated = club.DateUpdated.HasValue && club.DateUpdated >= club.DateCreated ? club.DateUpdated : System.Data.SqlTypes.SqlDateTime.MinValue.Value
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
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.Club} ON").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.Club}
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
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.Club} OFF").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.ClubName} 
							(ClubId, ClubName, FromDate) VALUES (@0, @1, @2)",
							migratedClub.ClubId,
							migratedClub.ClubName,
							migratedClub.DateCreated
							).ConfigureAwait(false);
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

			await _redirectsRepository.InsertRedirect(club.ClubRoute, migratedClub.ClubRoute, string.Empty).ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(club.ClubRoute, migratedClub.ClubRoute, "/matches.rss").ConfigureAwait(false);

			await _auditRepository.CreateAudit(new AuditRecord
			{
				Action = AuditAction.Create,
				ActorName = nameof(SqlServerClubDataMigrator),
				EntityUri = club.EntityUri,
				State = JsonConvert.SerializeObject(club),
				AuditDate = club.DateCreated.Value
			}).ConfigureAwait(false);

			await _auditRepository.CreateAudit(new AuditRecord
			{
				Action = AuditAction.Update,
				ActorName = nameof(SqlServerClubDataMigrator),
				EntityUri = migratedClub.EntityUri,
				State = JsonConvert.SerializeObject(migratedClub),
				AuditDate = migratedClub.DateUpdated.Value
			}).ConfigureAwait(false);
		}
	}
}
