using Newtonsoft.Json;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Clubs;
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
	public class SqlServerTeamDataMigrator : ITeamDataMigrator
	{
		private readonly ServiceContext _serviceContext;
		private readonly IRedirectsRepository _redirectsRepository;
		private readonly IScopeProvider _scopeProvider;
		private readonly IAuditRepository _auditRepository;
		private readonly ILogger _logger;
		private readonly IClubDataSource _clubDataSource;

		public SqlServerTeamDataMigrator(ServiceContext serviceContext, IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditRepository auditRepository, ILogger logger, IClubDataSource clubDataSource)
		{
			_serviceContext = serviceContext ?? throw new ArgumentNullException(nameof(serviceContext));
			_redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
			_scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
			_auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
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
		public async Task<Team> MigrateTeam(Team team)
		{
			if (team is null)
			{
				throw new System.ArgumentNullException(nameof(team));
			}

			var migratedTeam = new Team
			{
				TeamId = team.TeamId,
				TeamName = team.TeamName,
				Club = team.Club != null && team.Club.ClubId.HasValue ? team.Club : null,
				School = team.School != null && team.School.SchoolId.HasValue ? team.School : null,
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
				TeamRoute = "/teams/" + team.TeamRoute,
				DateCreated = team.DateCreated,
				DateUpdated = team.DateUpdated
			};
			migratedTeam.MatchLocations.AddRange(team.MatchLocations);

			if (migratedTeam.TeamRoute.EndsWith("team", StringComparison.OrdinalIgnoreCase))
			{
				migratedTeam.TeamRoute = migratedTeam.TeamRoute.Substring(0, migratedTeam.TeamRoute.Length - 4);
			}

			using (var scope = _scopeProvider.CreateScope())
			{
				try
				{
					var database = scope.Database;
					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.Team} ON").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.Team}
						(TeamId, ClubId, SchoolId, TeamType, PlayerType, Introduction, AgeRangeLower, AgeRangeUpper, 
						 FromDate, UntilDate, Website, PublicContactDetails, PrivateContactDetails, PlayingTimes, Cost,
						 MemberGroupId, TeamRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14, @15, @16)",
							migratedTeam.TeamId,
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
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.Team} OFF").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.TeamName} 
							(TeamId, TeamName, TeamComparableName, FromDate) VALUES (@0, @1, @2, @3)",
							migratedTeam.TeamId,
							migratedTeam.TeamName,
							migratedTeam.GenerateComparableName(),
							migratedTeam.DateCreated
							).ConfigureAwait(false);
						if (migratedTeam.MatchLocations.Count > 0)
						{
							await database.ExecuteAsync($@"INSERT INTO {Tables.TeamMatchLocation} 
							(TeamId, MatchLocationId, FromDate) VALUES (@0, @1, @2)",
								migratedTeam.TeamId,
								migratedTeam.MatchLocations.First().MatchLocationId,
								migratedTeam.DateCreated
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

			await _auditRepository.CreateAudit(new AuditRecord
			{
				Action = AuditAction.Create,
				ActorName = nameof(SqlServerTeamDataMigrator),
				EntityUri = team.EntityUri,
				State = JsonConvert.SerializeObject(team),
				AuditDate = team.DateCreated.Value
			}).ConfigureAwait(false);

			if (migratedTeam.DateUpdated > migratedTeam.DateCreated)
			{
				await _auditRepository.CreateAudit(new AuditRecord
				{
					Action = AuditAction.Update,
					ActorName = nameof(SqlServerTeamDataMigrator),
					EntityUri = migratedTeam.EntityUri,
					State = JsonConvert.SerializeObject(migratedTeam),
					AuditDate = migratedTeam.DateUpdated.Value
				}).ConfigureAwait(false);
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
