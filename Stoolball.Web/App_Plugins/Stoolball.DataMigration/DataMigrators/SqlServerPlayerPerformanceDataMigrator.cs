using Stoolball.Matches;
using Stoolball.Teams;
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
		/// Clear down all the player innings data ready for a fresh import
		/// </summary>
		/// <returns></returns>
		public async Task DeletePlayerInnings()
		{
			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"DELETE FROM {Tables.PlayerInnings}").ConfigureAwait(false);
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
		/// Save the supplied player innings to the database
		/// </summary>
		public async Task<PlayerInnings> MigratePlayerInnings(MigratedPlayerInnings innings)
		{
			if (innings is null)
			{
				throw new ArgumentNullException(nameof(innings));
			}
			try
			{
				var migratedInnings = new MigratedPlayerInnings
				{
					PlayerInningsId = Guid.NewGuid(),
					MigratedMatchId = innings.MigratedMatchId,
					MigratedPlayerIdentityId = innings.MigratedPlayerIdentityId,
					MigratedTeamId = innings.MigratedTeamId,
					MigratedDismissedById = innings.MigratedDismissedById,
					MigratedBowlerId = innings.MigratedBowlerId,
					BattingPosition = innings.BattingPosition,
					HowOut = innings.HowOut,
					DismissedBy = innings.DismissedBy,
					Bowler = innings.Bowler,
					RunsScored = innings.RunsScored,
					BallsFaced = innings.BallsFaced
				};

				using (var scope = _scopeProvider.CreateScope())
				{
					try
					{
						var database = scope.Database;


						_auditHistoryBuilder.BuildInitialAuditHistory(innings, migratedInnings, nameof(SqlServerPlayerPerformanceDataMigrator));

						using (var transaction = database.GetTransaction())
						{
							migratedInnings.Match = new Match
							{
								MatchId = (await database.ExecuteScalarAsync<Guid>($"SELECT MatchId FROM {Tables.Match} WHERE MigratedMatchId = @0", migratedInnings.MigratedMatchId).ConfigureAwait(false))
							};
							migratedInnings.PlayerIdentity = new PlayerIdentity
							{
								PlayerIdentityId = (await database.ExecuteScalarAsync<Guid>($"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @0", migratedInnings.MigratedPlayerIdentityId).ConfigureAwait(false)),
								Team = new Team
								{
									TeamId = (await database.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @0", migratedInnings.MigratedTeamId).ConfigureAwait(false))
								}
							};
							if (migratedInnings.MigratedDismissedById.HasValue)
							{
								migratedInnings.DismissedBy = new PlayerIdentity
								{
									PlayerIdentityId = (await database.ExecuteScalarAsync<Guid>($"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @0", migratedInnings.MigratedDismissedById).ConfigureAwait(false)),
								};
							}
							if (migratedInnings.MigratedBowlerId.HasValue)
							{
								migratedInnings.Bowler = new PlayerIdentity
								{
									PlayerIdentityId = (await database.ExecuteScalarAsync<Guid>($"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @0", migratedInnings.MigratedBowlerId).ConfigureAwait(false)),
								};
							}
							var inningsId = (await database.ExecuteScalarAsync<Guid>($"SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId = @0 AND TeamId = @1", migratedInnings.Match.MatchId, migratedInnings.PlayerIdentity.Team.TeamId).ConfigureAwait(false));

							await database.ExecuteAsync($@"INSERT INTO {Tables.PlayerInnings}
						(PlayerInningsId, MatchInningsId, PlayerIdentityId, BattingPosition, HowOut, DismissedById, BowlerId, RunsScored, BallsFaced)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8)",
								migratedInnings.PlayerInningsId,
								inningsId,
								migratedInnings.PlayerIdentity.PlayerIdentityId,
								migratedInnings.BattingPosition,
								migratedInnings.HowOut?.ToString(),
								migratedInnings.DismissedBy?.PlayerIdentityId,
								migratedInnings.Bowler?.PlayerIdentityId,
								migratedInnings.RunsScored,
								migratedInnings.BallsFaced).ConfigureAwait(false);
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

				foreach (var audit in migratedInnings.History)
				{
					await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
				}
				return migratedInnings;
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerPlayerPerformanceDataMigrator>(e);
				throw;
			}
		}


		/// <summary>
		/// Clear down all the bowling data ready for a fresh import
		/// </summary>
		/// <returns></returns>
		public async Task DeleteOvers()
		{
			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"DELETE FROM {Tables.Over}").ConfigureAwait(false);
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
		/// Save the supplied bowling over to the database
		/// </summary>
		public async Task<Over> MigrateOver(MigratedOver over)
		{
			if (over is null)
			{
				throw new ArgumentNullException(nameof(over));
			}
			try
			{
				var migratedOver = new MigratedOver
				{
					OverId = Guid.NewGuid(),
					MigratedMatchId = over.MigratedMatchId,
					MigratedPlayerIdentityId = over.MigratedPlayerIdentityId,
					MigratedTeamId = over.MigratedTeamId,
					OverNumber = over.OverNumber,
					BallsBowled = over.BallsBowled,
					NoBalls = over.NoBalls,
					Wides = over.Wides,
					RunsConceded = over.RunsConceded
				};

				using (var scope = _scopeProvider.CreateScope())
				{
					try
					{
						var database = scope.Database;

						_auditHistoryBuilder.BuildInitialAuditHistory(over, migratedOver, nameof(SqlServerPlayerPerformanceDataMigrator));

						using (var transaction = database.GetTransaction())
						{
							migratedOver.Match = new Match
							{
								MatchId = (await database.ExecuteScalarAsync<Guid>($"SELECT MatchId FROM {Tables.Match} WHERE MigratedMatchId = @0", migratedOver.MigratedMatchId).ConfigureAwait(false))
							};
							migratedOver.PlayerIdentity = new PlayerIdentity
							{
								PlayerIdentityId = (await database.ExecuteScalarAsync<Guid>($"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @0", migratedOver.MigratedPlayerIdentityId).ConfigureAwait(false)),
								Team = new Team
								{
									TeamId = (await database.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @0", migratedOver.MigratedTeamId).ConfigureAwait(false))
								}
							};
							var inningsId = (await database.ExecuteScalarAsync<Guid>($"SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId = @0 AND TeamId = @1", migratedOver.Match.MatchId, migratedOver.PlayerIdentity.Team.TeamId).ConfigureAwait(false));

							await database.ExecuteAsync($@"INSERT INTO {Tables.Over}
						(OverId, MatchInningsId, PlayerIdentityId, OverNumber, BallsBowled, NoBalls, Wides, RunsConceded)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7)",
								migratedOver.OverId,
								inningsId,
								migratedOver.PlayerIdentity.PlayerIdentityId,
								migratedOver.OverNumber,
								migratedOver.BallsBowled,
								migratedOver.NoBalls,
								migratedOver.Wides,
								migratedOver.RunsConceded).ConfigureAwait(false);
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

				foreach (var audit in migratedOver.History)
				{
					await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
				}
				return migratedOver;
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerPlayerPerformanceDataMigrator>(e);
				throw;
			}
		}
	}
}
