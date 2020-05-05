using Stoolball.Clubs;
using Stoolball.MatchLocations;
using Stoolball.Schools;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
	public class SqlServerTeamDataMigrator : ITeamDataMigrator
	{
		private readonly ServiceContext _serviceContext;
		private readonly IRedirectsRepository _redirectsRepository;
		private readonly IScopeProvider _scopeProvider;
		private readonly IAuditHistoryBuilder _auditHistoryBuilder;
		private readonly IAuditRepository _auditRepository;
		private readonly ILogger _logger;

		public SqlServerTeamDataMigrator(ServiceContext serviceContext, IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditHistoryBuilder auditHistoryBuilder, IAuditRepository auditRepository, ILogger logger)
		{
			_serviceContext = serviceContext ?? throw new ArgumentNullException(nameof(serviceContext));
			_redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
			_scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
			_auditHistoryBuilder = auditHistoryBuilder ?? throw new ArgumentNullException(nameof(auditHistoryBuilder));
			_auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Clear down all the team data ready for a fresh import
		/// </summary>
		/// <returns></returns>
		public async Task DeleteTeams()
		{
			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"DELETE FROM {Tables.PlayerIdentity}").ConfigureAwait(false);
						await database.ExecuteAsync($"DELETE FROM {Tables.TeamMatchLocation}").ConfigureAwait(false);
						await database.ExecuteAsync($"DELETE FROM {Tables.TeamName}").ConfigureAwait(false);
						await database.ExecuteAsync($@"DELETE FROM {Tables.Team}").ConfigureAwait(false);
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

			await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/teams/").ConfigureAwait(false);
		}

		/// <summary>
		/// Save the supplied team to the database with its existing <see cref="Team.TeamId"/>
		/// </summary>
		public async Task<Team> MigrateTeam(MigratedTeam team)
		{
			if (team is null)
			{
				throw new ArgumentNullException(nameof(team));
			}

			var migratedTeam = new MigratedTeam
			{
				TeamId = Guid.NewGuid(),
				MigratedTeamId = team.MigratedTeamId,
				TeamName = team.TeamName,
				MigratedClubId = team.MigratedClubId,
				MigratedSchoolId = team.MigratedSchoolId,
				MigratedMatchLocationId = team.MigratedMatchLocationId,
				TeamType = team.TeamType,
				PlayerType = team.PlayerType,
				Introduction = team.Introduction,
				AgeRangeLower = team.AgeRangeLower,
				AgeRangeUpper = team.AgeRangeUpper,
				FromDate = System.Data.SqlTypes.SqlDateTime.MinValue.Value,
				UntilDate = team.UntilDate,
				Website = team.Website,
				PublicContactDetails = team.PublicContactDetails,
				PrivateContactDetails = team.PrivateContactDetails,
				PlayingTimes = team.PlayingTimes,
				Cost = team.Cost,
				MemberGroupId = ReadMemberGroupId(team),
				TeamRoute = team.TeamRoute
			};

			if (migratedTeam.TeamRoute.EndsWith("team", StringComparison.OrdinalIgnoreCase))
			{
				migratedTeam.TeamRoute = migratedTeam.TeamRoute.Substring(0, migratedTeam.TeamRoute.Length - 4);
			}

			if (migratedTeam.TeamType == TeamType.Transient)
			{
				// Use a partial route that will be updated when the tournament is imported
				var splitRoute = migratedTeam.TeamRoute.Split('/');
				migratedTeam.TeamRoute = migratedTeam.MigratedTeamId.ToString("00000", CultureInfo.InvariantCulture) + splitRoute[splitRoute.Length - 1];
			}
			else
			{
				migratedTeam.TeamRoute = "/teams/" + migratedTeam.TeamRoute;
			}

			_auditHistoryBuilder.BuildInitialAuditHistory(team, migratedTeam, nameof(SqlServerTeamDataMigrator));

			using (var scope = _scopeProvider.CreateScope())
			{
				try
				{
					var database = scope.Database;
					using (var transaction = database.GetTransaction())
					{
						if (migratedTeam.MigratedClubId.HasValue)
						{
							migratedTeam.Club = new Club
							{
								ClubId = await database.ExecuteScalarAsync<Guid>($"SELECT ClubId FROM {Tables.Club} WHERE MigratedClubId = @0", migratedTeam.MigratedClubId).ConfigureAwait(false)
							};
						}
						if (migratedTeam.MigratedSchoolId.HasValue)
						{
							migratedTeam.School = new School
							{
								SchoolId = await database.ExecuteScalarAsync<Guid>($"SELECT SchoolId FROM {Tables.School} WHERE MigratedSchoolId = @0", migratedTeam.MigratedSchoolId).ConfigureAwait(false)
							};
						}
						if (migratedTeam.MigratedMatchLocationId.HasValue)
						{
							migratedTeam.MatchLocations.Add(new MatchLocation
							{
								MatchLocationId = await database.ExecuteScalarAsync<Guid>($"SELECT MatchLocationId FROM {Tables.MatchLocation} WHERE MigratedMatchLocationId = @0", migratedTeam.MigratedMatchLocationId).ConfigureAwait(false)
							});
						}

						await database.ExecuteAsync($@"INSERT INTO {Tables.Team}
						(TeamId, MigratedTeamId, ClubId, SchoolId, TeamType, PlayerType, Introduction, AgeRangeLower, AgeRangeUpper, 
						 FromDate, UntilDate, Website, PublicContactDetails, PrivateContactDetails, PlayingTimes, Cost,
						 MemberGroupId, TeamRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14, @15, @16, @17)",
							migratedTeam.TeamId,
							migratedTeam.MigratedTeamId,
							migratedTeam.Club?.ClubId,
							migratedTeam.School?.SchoolId,
							migratedTeam.TeamType.ToString(),
							migratedTeam.PlayerType.ToString(),
							migratedTeam.Introduction,
							migratedTeam.AgeRangeLower,
							migratedTeam.AgeRangeUpper,
							migratedTeam.FromDate,
							migratedTeam.UntilDate,
							migratedTeam.Website,
							migratedTeam.PublicContactDetails,
							migratedTeam.PrivateContactDetails,
							migratedTeam.PlayingTimes,
							migratedTeam.Cost,
							migratedTeam.MemberGroupId,
							migratedTeam.TeamRoute).ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.TeamName} 
							(TeamNameId, TeamId, TeamName, TeamComparableName, FromDate) VALUES (@0, @1, @2, @3, @4)",
							Guid.NewGuid(),
							migratedTeam.TeamId,
							migratedTeam.TeamName,
							migratedTeam.GenerateComparableName(),
							migratedTeam.History[0].AuditDate
							).ConfigureAwait(false);
						if (migratedTeam.MatchLocations.Count > 0)
						{
							await database.ExecuteAsync($@"INSERT INTO {Tables.TeamMatchLocation} 
							(TeamMatchLocationId, TeamId, MatchLocationId, FromDate) VALUES (@0, @1, @2, @3)",
								Guid.NewGuid(),
								migratedTeam.TeamId,
								migratedTeam.MatchLocations.First().MatchLocationId,
								migratedTeam.History[0].AuditDate
								).ConfigureAwait(false);
						}
						transaction.Complete();
					}

				}
				catch (Exception e)
				{
					_logger.Error<SqlServerTeamDataMigrator>(e);
					throw;
				}
				scope.Complete();
			}

			await _redirectsRepository.InsertRedirect(team.TeamRoute, migratedTeam.TeamRoute, string.Empty).ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(team.TeamRoute, migratedTeam.TeamRoute, "/matches.rss").ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(team.TeamRoute, migratedTeam.TeamRoute, "/statistics").ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(team.TeamRoute, migratedTeam.TeamRoute, "/players").ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(team.TeamRoute, migratedTeam.TeamRoute, "/calendar.ics").ConfigureAwait(false);

			foreach (var audit in migratedTeam.History)
			{
				await _auditRepository.CreateAudit(audit).ConfigureAwait(false);
			}

			return migratedTeam;
		}

		private int ReadMemberGroupId(Team team)
		{
			var groupId = _serviceContext.MemberGroupService.GetByName("team/" + team.TeamRoute)?.Id;
			return (groupId == null) ? 0 : groupId.Value;
		}
	}
}
