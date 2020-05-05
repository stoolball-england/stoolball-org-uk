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
		public async Task<Batting> MigrateBatting(MigratedBatting batting)
		{
			if (batting is null)
			{
				throw new ArgumentNullException(nameof(batting));
			}
			try
			{
				var migratedBatting = new MigratedBatting
				{
					BattingId = Guid.NewGuid(),
					MigratedMatchId = batting.MigratedMatchId,
					MigratedPlayerIdentityId = batting.MigratedPlayerIdentityId,
					MigratedTeamId = batting.MigratedTeamId,
					MigratedDismissedById = batting.MigratedDismissedById,
					MigratedBowlerId = batting.MigratedBowlerId,
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
							migratedBatting.Match = new Match
							{
								MatchId = (await database.ExecuteScalarAsync<Guid>($"SELECT MatchId FROM {Tables.Match} WHERE MigratedMatchId = @0", migratedBatting.MigratedMatchId).ConfigureAwait(false))
							};
							migratedBatting.PlayerIdentity = new PlayerIdentity
							{
								PlayerIdentityId = (await database.ExecuteScalarAsync<Guid>($"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @0", migratedBatting.MigratedPlayerIdentityId).ConfigureAwait(false)),
								Team = new Team
								{
									TeamId = (await database.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @0", migratedBatting.MigratedTeamId).ConfigureAwait(false))
								}
							};
							if (migratedBatting.MigratedDismissedById.HasValue)
							{
								migratedBatting.DismissedBy = new PlayerIdentity
								{
									PlayerIdentityId = (await database.ExecuteScalarAsync<Guid>($"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @0", migratedBatting.MigratedDismissedById).ConfigureAwait(false)),
								};
							}
							if (migratedBatting.MigratedBowlerId.HasValue)
							{
								migratedBatting.Bowler = new PlayerIdentity
								{
									PlayerIdentityId = (await database.ExecuteScalarAsync<Guid>($"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @0", migratedBatting.MigratedBowlerId).ConfigureAwait(false)),
								};
							}
							var inningsId = (await database.ExecuteScalarAsync<Guid>($"SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId = @0 AND TeamId = @1", migratedBatting.Match.MatchId, migratedBatting.PlayerIdentity.Team.TeamId).ConfigureAwait(false));

							await database.ExecuteAsync($@"INSERT INTO {Tables.Batting}
						(BattingId, MatchInningsId, PlayerIdentityId, BattingPosition, HowOut, DismissedById, BowlerId, RunsScored, BallsFaced)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8)",
								migratedBatting.BattingId,
								inningsId,
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


		/// <summary>
		/// Clear down all the bowling data ready for a fresh import
		/// </summary>
		/// <returns></returns>
		public async Task DeleteBowling()
		{
			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"DELETE FROM {Tables.BowlingOver}").ConfigureAwait(false);
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
		public async Task<BowlingOver> MigrateBowling(MigratedBowlingOver bowling)
		{
			if (bowling is null)
			{
				throw new ArgumentNullException(nameof(bowling));
			}
			try
			{
				var migratedBowling = new MigratedBowlingOver
				{
					BowlingOverId = Guid.NewGuid(),
					MigratedMatchId = bowling.MigratedMatchId,
					MigratedPlayerIdentityId = bowling.MigratedPlayerIdentityId,
					MigratedTeamId = bowling.MigratedTeamId,
					OverNumber = bowling.OverNumber,
					BallsBowled = bowling.BallsBowled,
					NoBalls = bowling.NoBalls,
					Wides = bowling.Wides,
					RunsConceded = bowling.RunsConceded
				};

				using (var scope = _scopeProvider.CreateScope())
				{
					try
					{
						var database = scope.Database;

						_auditHistoryBuilder.BuildInitialAuditHistory(bowling, migratedBowling, nameof(SqlServerPlayerPerformanceDataMigrator));

						using (var transaction = database.GetTransaction())
						{
							migratedBowling.Match = new Match
							{
								MatchId = (await database.ExecuteScalarAsync<Guid>($"SELECT MatchId FROM {Tables.Match} WHERE MigratedMatchId = @0", migratedBowling.MigratedMatchId).ConfigureAwait(false))
							};
							migratedBowling.PlayerIdentity = new PlayerIdentity
							{
								PlayerIdentityId = (await database.ExecuteScalarAsync<Guid>($"SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE MigratedPlayerIdentityId = @0", migratedBowling.MigratedPlayerIdentityId).ConfigureAwait(false)),
								Team = new Team
								{
									TeamId = (await database.ExecuteScalarAsync<Guid>($"SELECT TeamId FROM {Tables.Team} WHERE MigratedTeamId = @0", migratedBowling.MigratedTeamId).ConfigureAwait(false))
								}
							};
							var inningsId = (await database.ExecuteScalarAsync<Guid>($"SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId = @0 AND TeamId = @1", migratedBowling.Match.MatchId, migratedBowling.PlayerIdentity.Team.TeamId).ConfigureAwait(false));

							await database.ExecuteAsync($@"INSERT INTO {Tables.BowlingOver}
						(BowlingOverId, MatchInningsId, PlayerIdentityId, OverNumber, BallsBowled, NoBalls, Wides, RunsConceded)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7)",
								migratedBowling.BowlingOverId,
								inningsId,
								migratedBowling.PlayerIdentity.PlayerIdentityId,
								migratedBowling.OverNumber,
								migratedBowling.BallsBowled,
								migratedBowling.NoBalls,
								migratedBowling.Wides,
								migratedBowling.RunsConceded).ConfigureAwait(false);
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

				foreach (var audit in migratedBowling.History)
				{
					await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
				}
				return migratedBowling;
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerPlayerPerformanceDataMigrator>(e);
				throw;
			}
		}
	}
}
