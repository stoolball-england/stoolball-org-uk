using Stoolball.Clubs;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using StoolballMigrations = Stoolball.Umbraco.Data.Migrations;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
	public class SqlServerClubDataMigrator : IClubDataMigrator
	{
		private readonly IScopeProvider _scopeProvider;
		private readonly ILogger _logger;

		public SqlServerClubDataMigrator(IScopeProvider scopeProvider, ILogger logger)
		{
			_scopeProvider = scopeProvider;
			_logger = logger;
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

			using (var scope = _scopeProvider.CreateScope())
			{
				var database = scope.Database;
				try
				{
					using (var transaction = database.GetTransaction())
					{
						var revisedRoute = "club/" + club.ClubRoute;
						if (revisedRoute.EndsWith("club", StringComparison.OrdinalIgnoreCase))
						{
							revisedRoute = revisedRoute.Substring(0, revisedRoute.Length - 4);
						}

						await database.ExecuteAsync($"SET IDENTITY_INSERT {StoolballMigrations.Constants.Tables.Club} ON").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {StoolballMigrations.Constants.Tables.Club}
						(ClubId, PlaysOutdoors, PlaysIndoors, Twitter, Facebook, Instagram, ClubMark, HowManyPlayers, ClubRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8)",
							club.ClubId,
							club.PlaysOutdoors,
							club.PlaysIndoors,
							club.Twitter,
							club.Facebook,
							club.Instagram,
							club.ClubMark,
							club.HowManyPlayers,
							revisedRoute).ConfigureAwait(false);
						await database.ExecuteAsync($"SET IDENTITY_INSERT {StoolballMigrations.Constants.Tables.Club} OFF").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {StoolballMigrations.Constants.Tables.ClubName} 
							(ClubId, ClubName, FromDate) VALUES (@0, @1, @2)",
							club.ClubId,
							club.ClubName,
							club.DateCreated.HasValue && club.DateCreated <= club.DateUpdated ? club.DateCreated : System.Data.SqlTypes.SqlDateTime.MinValue.Value
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
		}
	}
}
