using Newtonsoft.Json;
using Stoolball.Competitions;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
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
		public async Task MigrateCompetition(Competition competition)
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
		}
	}
}
