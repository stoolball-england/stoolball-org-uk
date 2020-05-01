using Stoolball.Matches;
using Stoolball.Umbraco.Data.Audit;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
	public class SqlServerPlayerPerformanceDataMigrator : IPlayerPerformanceDataMigrator
	{
		private readonly IScopeProvider _scopeProvider;
		private readonly IAuditHistoryBuilder _auditHistoryBuilder;
		private readonly IAuditRepository _auditRepository;
		private readonly ILogger _logger;

		public SqlServerPlayerPerformanceDataMigrator(IScopeProvider scopeProvider, IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository, ILogger logger)
		{
			_scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
			_auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
			_auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Clear down all the batting data ready for a fresh import
		/// </summary>
		/// <returns></returns>
		public async Task DeleteBatting()
		{
			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"DELETE FROM {Tables.Batting}").ConfigureAwait(false);
						transaction.Complete();
					}

					scope.Complete();
				}
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerPlayerPerformanceDataMigrator>(e);
				throw;
			}
		}

		/// <summary>
		/// Save the supplied batting performance to the database
		/// </summary>
		public async Task<Batting> MigrateBatting(Batting batting)
		{
			if (batting is null)
			{
				throw new System.ArgumentNullException(nameof(batting));
			}
			try
			{
				var migratedBatting = new Batting
				{
					BattingId = Guid.NewGuid(),
					Match = batting.Match,
					PlayerIdentity = batting.PlayerIdentity,
					BattingPosition = batting.BattingPosition,
					HowOut = batting.HowOut,
					DismissedBy = batting.DismissedBy,
					Bowler = batting.Bowler,
					RunsScored = batting.RunsScored,
					BallsFaced = batting.BallsFaced
				};

				using (var scope = _scopeProvider.CreateScope())
				{
					try
					{
						var database = scope.Database;


						_auditHistoryBuilder.BuildInitialAuditHistory(batting, migratedBatting, nameof(SqlServerPlayerPerformanceDataMigrator));

						using (var transaction = database.GetTransaction())
						{
							var matchTeamId = (await database.ExecuteScalarAsync<int>($"SELECT MatchTeamId FROM {Tables.MatchTeam} WHERE MatchId = @0 AND TeamId = @1", batting.Match.MatchId, batting.PlayerIdentity.Team.TeamId).ConfigureAwait(false));

							await database.ExecuteAsync($@"INSERT INTO {Tables.Batting}
						(BattingId, MatchTeamId, PlayerIdentityId, BattingPosition, HowOut, DismissedById, BowlerId, RunsScored, BallsFaced)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8)",
								migratedBatting.BattingId,
								matchTeamId,
								migratedBatting.PlayerIdentity.PlayerIdentityId,
								migratedBatting.BattingPosition,
								migratedBatting.HowOut?.ToString(),
								migratedBatting.DismissedBy?.PlayerIdentityId,
								migratedBatting.Bowler?.PlayerIdentityId,
								migratedBatting.RunsScored,
								migratedBatting.BallsFaced).ConfigureAwait(false);
							transaction.Complete();
						}

					}
					catch (Exception e)
					{
						_logger.Error<SqlServerPlayerDataMigrator>(e);
						throw;
					}
					scope.Complete();
				}

				foreach (var audit in migratedBatting.History)
				{
					await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
				}
				return migratedBatting;
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerPlayerPerformanceDataMigrator>(e);
				throw;
			}
		}
	}
}
