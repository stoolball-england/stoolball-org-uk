using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Competitions;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
	public class SqlServerCompetitionDataMigrator : ICompetitionDataMigrator
	{
		private readonly IRedirectsRepository _redirectsRepository;
		private readonly IScopeProvider _scopeProvider;
		private readonly IAuditRepository _auditRepository;
		private readonly ILogger _logger;

		public SqlServerCompetitionDataMigrator(IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditRepository auditRepository, ILogger logger)
		{
			_redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
			_scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
			_auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Clear down all the competition data ready for a fresh import
		/// </summary>
		/// <returns></returns>
		public async Task DeleteCompetitions()
		{
			try
			{
				await DeleteSeasons().ConfigureAwait(false);

				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"DELETE FROM {Tables.Competition}").ConfigureAwait(false);
						transaction.Complete();
					}

					scope.Complete();
				}
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerCompetitionDataMigrator>(e);
				throw;
			}

			await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/competition/").ConfigureAwait(false);
		}

		/// <summary>
		/// Save the supplied competition to the database with its existing <see cref="Competition.CompetitionId"/>
		/// </summary>
		public async Task<Competition> MigrateCompetition(Competition competition)
		{
			if (competition is null)
			{
				throw new System.ArgumentNullException(nameof(competition));
			}

			var migratedCompetition = new Competition
			{
				CompetitionId = competition.CompetitionId,
				CompetitionName = competition.CompetitionName,
				Introduction = competition.Introduction,
				PublicContactDetails = competition.PublicContactDetails,
				Website = competition.Website,
				Twitter = competition.Twitter,
				Facebook = competition.Facebook,
				Instagram = competition.Instagram,
				PlayersPerTeam = competition.PlayersPerTeam,
				Overs = competition.Overs,
				PlayerType = competition.PlayerType,
				CompetitionRoute = "/competitions/" + competition.CompetitionRoute,
				FromDate = competition.DateCreated <= competition.DateUpdated ? competition.DateCreated.Value : System.Data.SqlTypes.SqlDateTime.MinValue.Value,
				UntilDate = competition.UntilDate,
				DateCreated = competition.DateCreated <= competition.DateUpdated ? competition.DateCreated : System.Data.SqlTypes.SqlDateTime.MinValue.Value,
				DateUpdated = competition.DateUpdated >= competition.DateCreated ? competition.DateUpdated : System.Data.SqlTypes.SqlDateTime.MinValue.Value
			};

			using (var scope = _scopeProvider.CreateScope())
			{
				try
				{
					var database = scope.Database;
					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.Competition} ON").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.Competition}
						(CompetitionId, CompetitionName, Introduction, Twitter, Facebook, Instagram, PublicContactDetails, Website, PlayersPerTeam, 
						 Overs, PlayerType, FromDate, UntilDate, CompetitionRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13)",
							migratedCompetition.CompetitionId,
							migratedCompetition.CompetitionName,
							migratedCompetition.Introduction,
							migratedCompetition.Twitter,
							migratedCompetition.Facebook,
							migratedCompetition.Instagram,
							migratedCompetition.PublicContactDetails,
							migratedCompetition.Website,
							migratedCompetition.PlayersPerTeam,
							migratedCompetition.Overs,
							migratedCompetition.PlayerType,
							migratedCompetition.FromDate,
							migratedCompetition.UntilDate,
							migratedCompetition.CompetitionRoute).ConfigureAwait(false);
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.Competition} OFF").ConfigureAwait(false);
						transaction.Complete();
					}

				}
				catch (Exception e)
				{
					_logger.Error<SqlServerCompetitionDataMigrator>(e);
					throw;
				}
				scope.Complete();
			}

			await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, migratedCompetition.CompetitionRoute, string.Empty).ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, migratedCompetition.CompetitionRoute, "/statistics").ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, migratedCompetition.CompetitionRoute, "/map").ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(competition.CompetitionRoute, migratedCompetition.CompetitionRoute, "/matches.rss").ConfigureAwait(false);

			await _auditRepository.CreateAudit(new AuditRecord
			{
				Action = AuditAction.Create,
				ActorName = nameof(SqlServerCompetitionDataMigrator),
				EntityUri = competition.EntityUri,
				State = JsonConvert.SerializeObject(competition),
				AuditDate = competition.DateCreated.Value
			}).ConfigureAwait(false);

			await _auditRepository.CreateAudit(new AuditRecord
			{
				Action = AuditAction.Update,
				ActorName = nameof(SqlServerCompetitionDataMigrator),
				EntityUri = migratedCompetition.EntityUri,
				State = JsonConvert.SerializeObject(migratedCompetition),
				AuditDate = migratedCompetition.DateUpdated.Value
			}).ConfigureAwait(false);

			return migratedCompetition;
		}

		/// <summary>
		/// Clear down all the season data ready for a fresh import
		/// </summary>
		/// <returns></returns>
		public async Task DeleteSeasons()
		{
			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"DELETE FROM {Tables.SeasonTeam}").ConfigureAwait(false);
						await database.ExecuteAsync($"DELETE FROM {Tables.Season}").ConfigureAwait(false);
						transaction.Complete();
					}

					scope.Complete();
				}
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerCompetitionDataMigrator>(e);
				throw;
			}
		}

		/// <summary>
		/// Save the supplied season to the database with its existing <see cref="Season.SeasonId"/>
		/// </summary>
		public async Task<Season> MigrateSeason(Season season)
		{
			if (season is null)
			{
				throw new System.ArgumentNullException(nameof(season));
			}

			var migratedSeason = new Season
			{
				SeasonId = season.SeasonId,
				Competition = season.Competition,
				IsLatestSeason = season.IsLatestSeason,
				StartYear = season.StartYear,
				EndYear = season.EndYear,
				Introduction = season.Introduction,
				Teams = season.Teams,
				Results = season.Results,
				ShowTable = season.ShowTable,
				ShowRunsScored = season.ShowRunsScored,
				ShowRunsConceded = season.ShowRunsConceded,
				SeasonRoute = "/competitions/" + season.Competition.CompetitionRoute + "/" + season.StartYear + (season.EndYear > season.StartYear ? "-" + season.EndYear.ToString(CultureInfo.CurrentCulture).Substring(2) : string.Empty),
				DateCreated = season.DateCreated <= season.DateUpdated ? season.DateCreated : System.Data.SqlTypes.SqlDateTime.MinValue.Value,
				DateUpdated = season.DateUpdated >= season.DateCreated ? season.DateUpdated : System.Data.SqlTypes.SqlDateTime.MinValue.Value
			};

			using (var scope = _scopeProvider.CreateScope())
			{
				try
				{
					var database = scope.Database;
					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.Season} ON").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.Season}
						(SeasonId, CompetitionId, IsLatestSeason, StartYear, EndYear, Introduction, 
						 Results, ShowTable, ShowRunsScored, ShowRunsConceded, SeasonRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10)",
							migratedSeason.SeasonId,
							migratedSeason.Competition.CompetitionId,
							migratedSeason.IsLatestSeason,
							migratedSeason.StartYear,
							migratedSeason.EndYear,
							migratedSeason.Introduction,
							migratedSeason.Results,
							migratedSeason.ShowTable,
							migratedSeason.ShowRunsScored,
							migratedSeason.ShowRunsConceded,
							migratedSeason.SeasonRoute).ConfigureAwait(false);
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.Season} OFF").ConfigureAwait(false);
						foreach (var teamInSeason in migratedSeason.Teams)
						{
							await database.ExecuteAsync($@"INSERT INTO {Tables.SeasonTeam}
								(SeasonId, TeamId, WithdrawnDate) VALUES (@0, @1, @2)",
								season.SeasonId,
								teamInSeason.Team.TeamId,
								teamInSeason.WithdrawnDate
								).ConfigureAwait(false);
						}
						transaction.Complete();
					}

				}
				catch (Exception e)
				{
					_logger.Error<SqlServerCompetitionDataMigrator>(e);
					throw;
				}
				scope.Complete();
			}

			await _redirectsRepository.InsertRedirect(season.SeasonRoute, migratedSeason.SeasonRoute, string.Empty).ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(season.SeasonRoute, migratedSeason.SeasonRoute, "/statistics").ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(season.SeasonRoute, migratedSeason.SeasonRoute, "/table").ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(season.SeasonRoute, migratedSeason.SeasonRoute, "/map").ConfigureAwait(false);

			await _auditRepository.CreateAudit(new AuditRecord
			{
				Action = AuditAction.Create,
				ActorName = nameof(SqlServerCompetitionDataMigrator),
				EntityUri = season.EntityUri,
				State = JsonConvert.SerializeObject(season),
				AuditDate = season.DateCreated.Value
			}).ConfigureAwait(false);

			await _auditRepository.CreateAudit(new AuditRecord
			{
				Action = AuditAction.Update,
				ActorName = nameof(SqlServerCompetitionDataMigrator),
				EntityUri = migratedSeason.EntityUri,
				State = JsonConvert.SerializeObject(migratedSeason),
				AuditDate = migratedSeason.DateUpdated.Value
			}).ConfigureAwait(false);

			return migratedSeason;
		}
	}
}
