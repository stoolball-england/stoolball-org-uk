﻿using Stoolball.Teams;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
	public class SqlServerPlayerDataMigrator : IPlayerDataMigrator
	{
		private readonly ServiceContext _serviceContext;
		private readonly IRedirectsRepository _redirectsRepository;
		private readonly IScopeProvider _scopeProvider;
		private readonly IAuditHistoryBuilder _auditHistoryBuilder;
		private readonly IAuditRepository _auditRepository;
		private readonly ILogger _logger;

		public SqlServerPlayerDataMigrator(ServiceContext serviceContext, IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository, ILogger logger)
		{
			_serviceContext = serviceContext ?? throw new ArgumentNullException(nameof(serviceContext));
			_redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
			_scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
			_auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
			_auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Clear down all the player data ready for a fresh import
		/// </summary>
		/// <returns></returns>
		public async Task DeletePlayers()
		{
			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync("DELETE FROM SkybrudRedirects WHERE DestinationUrl LIKE '/teams/%/players/%'").ConfigureAwait(false);
						await database.ExecuteAsync($"DELETE FROM {Tables.PlayerIdentity}").ConfigureAwait(false);
						transaction.Complete();
					}

					scope.Complete();
				}
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerTeamDataMigrator>(e);
				throw;
			}
		}

		/// <summary>
		/// Save the supplied player to the database with its existing <see cref="PlayerIdentity.PlayerIdentityId"/>
		/// </summary>
		public async Task<PlayerIdentity> MigratePlayer(PlayerIdentity player)
		{
			if (player is null)
			{
				throw new System.ArgumentNullException(nameof(player));
			}
			try
			{
				var migratedPlayer = new PlayerIdentity
				{
					PlayerIdentityId = player.PlayerIdentityId,
					PlayerIdentityName = player.PlayerIdentityName,
					Team = player.Team,
					FirstPlayed = player.FirstPlayed,
					LastPlayed = player.LastPlayed,
					TotalMatches = player.TotalMatches,
					MissedMatches = player.MissedMatches,
					Probability = player.Probability,
					PlayerRole = player.PlayerRole
				};

				using (var scope = _scopeProvider.CreateScope())
				{
					try
					{
						var database = scope.Database;

						var team = (await database.FetchAsync<Team>($"SELECT TeamRoute FROM {Tables.Team} WHERE TeamId = @0", player.Team.TeamId).ConfigureAwait(false)).SingleOrDefault();
						var slash = player.PlayerIdentityRoute.IndexOf("/", StringComparison.OrdinalIgnoreCase);
						var route = slash > -1 ? player.PlayerIdentityRoute.Substring(slash + 1) : player.PlayerIdentityRoute;
						if (player.PlayerRole == PlayerRole.Player)
						{
							migratedPlayer.PlayerIdentityRoute = team.TeamRoute + "/players/" + route;
						}
						else
						{
							if (route.EndsWith("player", StringComparison.OrdinalIgnoreCase))
							{
								route = route.Substring(0, route.Length - 6);
							}
							migratedPlayer.PlayerIdentityRoute = team.TeamRoute + "/" + route;
						}

						_auditHistoryBuilder.BuildInitialAuditHistory(player, migratedPlayer, nameof(SqlServerPlayerDataMigrator));

						using (var transaction = database.GetTransaction())
						{
							await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.PlayerIdentity} ON").ConfigureAwait(false);
							await database.ExecuteAsync($@"INSERT INTO {Tables.PlayerIdentity}
						(PlayerIdentityId, PlayerIdentityName, PlayerIdentityComparableName, TeamId, FirstPlayed, LastPlayed,
						 TotalMatches, MissedMatches, Probability, PlayerRole, PlayerIdentityRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10)",
								migratedPlayer.PlayerIdentityId,
								migratedPlayer.PlayerIdentityName,
								migratedPlayer.GenerateComparableName(),
								migratedPlayer.Team.TeamId,
								migratedPlayer.FirstPlayed,
								migratedPlayer.LastPlayed,
								migratedPlayer.TotalMatches,
								migratedPlayer.MissedMatches,
								migratedPlayer.Probability,
								migratedPlayer.PlayerRole.ToString(),
								migratedPlayer.PlayerIdentityRoute).ConfigureAwait(false);
							await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.PlayerIdentity} OFF").ConfigureAwait(false);
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

				if (player.PlayerRole == PlayerRole.Player)
				{
					await _redirectsRepository.InsertRedirect(player.PlayerIdentityRoute, migratedPlayer.PlayerIdentityRoute + "/batting", string.Empty).ConfigureAwait(false);
					await _redirectsRepository.InsertRedirect(player.PlayerIdentityRoute, migratedPlayer.PlayerIdentityRoute, "/bowling").ConfigureAwait(false);
				}
				else
				{
					await _redirectsRepository.InsertRedirect(player.PlayerIdentityRoute, migratedPlayer.PlayerIdentityRoute, string.Empty).ConfigureAwait(false);
				}

				foreach (var audit in migratedPlayer.History)
				{
					await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
				}
				return migratedPlayer;
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerPlayerDataMigrator>(e);
				throw;
			}
		}
	}
}