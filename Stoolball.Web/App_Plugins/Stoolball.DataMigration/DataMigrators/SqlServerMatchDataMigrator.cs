using Stoolball.Matches;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
	public class SqlServerMatchDataMigrator : IMatchDataMigrator
	{
		private readonly IRedirectsRepository _redirectsRepository;
		private readonly IScopeProvider _scopeProvider;
		private readonly IAuditHistoryBuilder _auditHistoryBuilder;
		private readonly IAuditRepository _auditRepository;
		private readonly ILogger _logger;

		public SqlServerMatchDataMigrator(IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository, ILogger logger)
		{
			_redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
			_scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
			_auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
			_auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Clear down all the match data ready for a fresh import
		/// </summary>
		/// <returns></returns>
		public async Task DeleteMatches()
		{
			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"DELETE FROM {Tables.SeasonMatch}").ConfigureAwait(false);
						await database.ExecuteAsync($"DELETE FROM {Tables.MatchInnings}").ConfigureAwait(false);
						await database.ExecuteAsync($"DELETE FROM {Tables.MatchTeam}").ConfigureAwait(false);
						await database.ExecuteAsync($"DELETE FROM {Tables.Match}").ConfigureAwait(false);
						transaction.Complete();
					}

					scope.Complete();
				}
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerMatchDataMigrator>(e);
				throw;
			}

			await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/matches/").ConfigureAwait(false);
			await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/tournaments/").ConfigureAwait(false);
		}

		/// <summary>
		/// Save the supplied match to the database with its existing <see cref="Match.MatchId"/>
		/// </summary>
		public async Task<Match> MigrateMatch(Match match)
		{
			if (match is null)
			{
				throw new System.ArgumentNullException(nameof(match));
			}

			var migratedMatch = new Match
			{
				MatchId = match.MatchId,
				MatchName = match.MatchName,
				UpdateMatchNameAutomatically = match.UpdateMatchNameAutomatically,
				MatchLocation = match.MatchLocation,
				MatchType = match.MatchType,
				PlayerType = match.PlayerType,
				PlayersPerTeam = match.PlayersPerTeam,
				MatchInnings = match.MatchInnings,
				InningsOrderIsKnown = match.InningsOrderIsKnown,
				OversPerInningsDefault = match.OversPerInningsDefault,
				Tournament = match.Tournament,
				OrderInTournament = match.OrderInTournament,
				StartTime = match.StartTime,
				StartTimeIsKnown = match.StartTimeIsKnown,
				Teams = match.Teams,
				Seasons = match.Seasons,
				MatchResultType = match.MatchResultType,
				MatchNotes = match.MatchNotes,
				MatchRoute = match.MatchRoute
			};

			if (migratedMatch.MatchRoute.StartsWith("match/", StringComparison.OrdinalIgnoreCase))
			{
				migratedMatch.MatchRoute = migratedMatch.MatchRoute.Substring(6);
			}

			migratedMatch.MatchRoute = "/matches/" + migratedMatch.MatchRoute;

			_auditHistoryBuilder.BuildInitialAuditHistory(match, migratedMatch, nameof(SqlServerMatchDataMigrator));

			using (var scope = _scopeProvider.CreateScope())
			{
				try
				{
					var database = scope.Database;
					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.Match} ON").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.Match}
						(MatchId, MatchName, UpdateMatchNameAutomatically, MatchLocationId, MatchType, PlayerType, PlayersPerTeam,
						 InningsOrderIsKnown, OversPerInningsDefault, TournamentId, OrderInTournament, StartTime, StartTimeIsKnown, MatchResultType, 
						 MatchNotes, MatchRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14, @15)",
							migratedMatch.MatchId,
							migratedMatch.MatchName,
							migratedMatch.UpdateMatchNameAutomatically,
							migratedMatch.MatchLocation?.MatchLocationId,
							migratedMatch.MatchType.ToString(),
							migratedMatch.PlayerType.ToString(),
							migratedMatch.PlayersPerTeam,
							migratedMatch.InningsOrderIsKnown,
							migratedMatch.OversPerInningsDefault,
							migratedMatch.Tournament?.TournamentId,
							migratedMatch.OrderInTournament,
							migratedMatch.StartTime,
							migratedMatch.StartTimeIsKnown,
							migratedMatch.MatchResultType?.ToString(),
							migratedMatch.MatchNotes,
							migratedMatch.MatchRoute).ConfigureAwait(false);
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.Match} OFF").ConfigureAwait(false);
						foreach (var innings in migratedMatch.MatchInnings)
						{
							await database.ExecuteAsync($@"INSERT INTO {Tables.MatchInnings} 
								(MatchInningsId, MatchId, TeamId, InningsOrderInMatch, Overs, Runs, Wickets)
								VALUES (@0, @1, @2, @3, @4, @5, @6)",
								Guid.NewGuid(),
								migratedMatch.MatchId,
								innings.Team?.TeamId,
								innings.InningsOrderInMatch,
								innings.Overs,
								innings.Runs,
								innings.Wickets).ConfigureAwait(false);
						}
						foreach (var team in migratedMatch.Teams)
						{
							await database.ExecuteAsync($@"INSERT INTO {Tables.MatchTeam} 
								(MatchTeamId, MatchId, TeamId, TeamRole, WonToss) VALUES (@0, @1, @2, @3, @4)",
								Guid.NewGuid(),
								migratedMatch.MatchId,
								team.Team.TeamId,
								team.TeamRole.ToString(),
								team.WonToss).ConfigureAwait(false);
						}
						foreach (var season in migratedMatch.Seasons)
						{
							await database.ExecuteAsync($@"INSERT INTO {Tables.SeasonMatch} 
								(SeasonMatchId, MatchId, SeasonId) VALUES (@0, @1, @2)",
								Guid.NewGuid(),
								migratedMatch.MatchId,
								season.SeasonId).ConfigureAwait(false);
						}
						transaction.Complete();
					}

				}
				catch (Exception e)
				{
					_logger.Error<SqlServerMatchDataMigrator>(e);
					throw;
				}
				scope.Complete();
			}

			await _redirectsRepository.InsertRedirect(match.MatchRoute, migratedMatch.MatchRoute, string.Empty).ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(match.MatchRoute, migratedMatch.MatchRoute, "/statistics").ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(match.MatchRoute, migratedMatch.MatchRoute, "/calendar.ics").ConfigureAwait(false);

			foreach (var audit in migratedMatch.History)
			{
				await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
			}

			return migratedMatch;
		}

		/// <summary>
		/// Save the supplied tournament to the database with its existing <see cref="Match.MatchId"/>
		/// </summary>
		public async Task<Tournament> MigrateTournament(Tournament tournament)
		{
			if (tournament is null)
			{
				throw new System.ArgumentNullException(nameof(tournament));
			}

			var migratedTournament = new Tournament
			{
				TournamentId = tournament.TournamentId,
				TournamentName = tournament.TournamentName,
				TournamentLocation = tournament.TournamentLocation,
				QualificationType = tournament.QualificationType,
				PlayerType = tournament.PlayerType,
				PlayersPerTeam = tournament.PlayersPerTeam,
				OversPerInningsDefault = tournament.OversPerInningsDefault,
				MaximumTeamsInTournament = tournament.MaximumTeamsInTournament,
				SpacesInTournament = tournament.SpacesInTournament,
				StartTime = tournament.StartTime,
				StartTimeIsKnown = tournament.StartTimeIsKnown,
				Teams = tournament.Teams,
				Seasons = tournament.Seasons,
				TournamentRoute = tournament.TournamentRoute,
				MatchNotes = tournament.MatchNotes,
			};

			if (migratedTournament.TournamentRoute.StartsWith("match/", StringComparison.OrdinalIgnoreCase))
			{
				migratedTournament.TournamentRoute = migratedTournament.TournamentRoute.Substring(6);
			}

			if (migratedTournament.TournamentRoute.EndsWith("-tournament", StringComparison.OrdinalIgnoreCase))
			{
				migratedTournament.TournamentRoute = migratedTournament.TournamentRoute.Substring(0, migratedTournament.TournamentRoute.Length - 6);
			}
			migratedTournament.TournamentRoute = "/tournaments/" + migratedTournament.TournamentRoute;

			_auditHistoryBuilder.BuildInitialAuditHistory(tournament, migratedTournament, nameof(SqlServerMatchDataMigrator));

			using (var scope = _scopeProvider.CreateScope())
			{
				try
				{
					var database = scope.Database;
					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.Match} ON").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.Match}
						(MatchId, MatchName, UpdateMatchNameAutomatically, MatchLocationId, MatchType, TournamentQualificationType, PlayerType, PlayersPerTeam, 
						 OversPerInningsDefault, MaximumTeamsInTournament, SpacesInTournament, StartTime, StartTimeIsKnown, MatchNotes, MatchRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14)",
							migratedTournament.TournamentId,
							migratedTournament.TournamentName,
							false,
							migratedTournament.TournamentLocation?.MatchLocationId,
							MatchType.Tournament.ToString(),
							migratedTournament.QualificationType.ToString(),
							migratedTournament.PlayerType.ToString(),
							migratedTournament.PlayersPerTeam,
							migratedTournament.OversPerInningsDefault,
							migratedTournament.MaximumTeamsInTournament,
							migratedTournament.SpacesInTournament,
							migratedTournament.StartTime,
							migratedTournament.StartTimeIsKnown,
							migratedTournament.MatchNotes,
							migratedTournament.TournamentRoute).ConfigureAwait(false);
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.Match} OFF").ConfigureAwait(false);
						foreach (var team in migratedTournament.Teams)
						{
							await database.ExecuteAsync($@"INSERT INTO {Tables.MatchTeam} 
								(MatchTeamId, MatchId, TeamId, TeamRole) VALUES (@0, @1, @2, @3)",
								Guid.NewGuid(),
								migratedTournament.TournamentId,
								team.Team.TeamId,
								team.TeamRole.ToString()).ConfigureAwait(false);
						}
						foreach (var season in migratedTournament.Seasons)
						{
							await database.ExecuteAsync($@"INSERT INTO {Tables.SeasonMatch} 
								(SeasonMatchId, MatchId, SeasonId) VALUES (@0, @1, @2)",
								Guid.NewGuid(),
								migratedTournament.TournamentId,
								season.SeasonId).ConfigureAwait(false);
						}
						await database.ExecuteAsync($@"UPDATE {Tables.Team} SET 
							TeamRoute = CONCAT(@0, '/teams/', SUBSTRING(TeamRoute, 6, LEN(TeamRoute)-5)),
							FromDate = @1,
							UntilDate = @2
							WHERE TeamType = 'Once' 
							AND TeamRoute NOT LIKE '/tournaments%'
							AND TeamId IN (
								SELECT TeamId FROM {Tables.MatchTeam} WHERE MatchId = @3
							)",
							migratedTournament.TournamentRoute, migratedTournament.StartTime.Date, migratedTournament.StartTime.Date.AddDays(1).AddMinutes(-1), migratedTournament.TournamentId).ConfigureAwait(false);
						transaction.Complete();
					}

				}
				catch (Exception e)
				{
					_logger.Error<SqlServerMatchDataMigrator>(e);
					throw;
				}
				scope.Complete();
			}

			await _redirectsRepository.InsertRedirect(tournament.TournamentRoute, migratedTournament.TournamentRoute, string.Empty).ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(tournament.TournamentRoute, migratedTournament.TournamentRoute, "/statistics").ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(tournament.TournamentRoute, migratedTournament.TournamentRoute, "/calendar.ics").ConfigureAwait(false);

			foreach (var audit in migratedTournament.History)
			{
				await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
			}

			return migratedTournament;
		}
	}
}
