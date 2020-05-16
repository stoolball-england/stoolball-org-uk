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
		private readonly IAuditHistoryBuilder _auditHistoryBuilder;
		private readonly IAuditRepository _auditRepository;
		private readonly ILogger _logger;

		public SqlServerClubDataMigrator(IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository, ILogger logger)
		{
			_redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
			_scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
			_auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
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
		public async Task<MigratedClub> MigrateClub(MigratedClub club)
		{
			if (club is null)
			{
				throw new System.ArgumentNullException(nameof(club));
			}

			var migratedClub = new MigratedClub
			{
				ClubId = Guid.NewGuid(),
				MigratedClubId = club.MigratedClubId,
				ClubName = club.ClubName,
				Twitter = club.Twitter,
				Facebook = club.Facebook,
				Instagram = club.Instagram,
				ClubMark = club.ClubMark,
				MemberGroupId = club.MemberGroupId,
				ClubRoute = "/clubs/" + club.ClubRoute
			};

			if (migratedClub.ClubRoute.EndsWith("club", StringComparison.OrdinalIgnoreCase))
			{
				migratedClub.ClubRoute = migratedClub.ClubRoute.Substring(0, migratedClub.ClubRoute.Length - 4);
			}

			_auditHistoryBuilder.BuildInitialAuditHistory(club, migratedClub, nameof(SqlServerClubDataMigrator));

			using (var scope = _scopeProvider.CreateScope())
			{
				try
				{
					var database = scope.Database;
					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($@"INSERT INTO {Tables.Club}
						(ClubId, MigratedClubId, Twitter, Facebook, Instagram, ClubMark, MemberGroupId, ClubRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7)",
							migratedClub.ClubId,
							migratedClub.MigratedClubId,
							migratedClub.Twitter,
							migratedClub.Facebook,
							migratedClub.Instagram,
							migratedClub.ClubMark,
							migratedClub.MemberGroupId,
							migratedClub.ClubRoute).ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.ClubName} 
							(ClubNameId, ClubId, ClubName, FromDate) VALUES (@0, @1, @2, @3)",
							Guid.NewGuid(),
							migratedClub.ClubId,
							migratedClub.ClubName,
							migratedClub.History[0].AuditDate
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

			foreach (var audit in migratedClub.History)
			{
				await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
			}

			return migratedClub;
		}
	}
}
