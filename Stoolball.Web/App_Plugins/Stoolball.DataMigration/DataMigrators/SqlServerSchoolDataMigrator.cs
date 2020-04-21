using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Schools;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using Tables = Stoolball.Umbraco.Data.Constants.Tables;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
	public class SqlServerSchoolDataMigrator : ISchoolDataMigrator
	{
		private readonly IRedirectsRepository _redirectsRepository;
		private readonly IScopeProvider _scopeProvider;
		private readonly IAuditRepository _auditRepository;
		private readonly ILogger _logger;

		public SqlServerSchoolDataMigrator(IRedirectsRepository redirectsRepository, IScopeProvider scopeProvider, IAuditRepository auditRepository, ILogger logger)
		{
			_redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
			_scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
			_auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Clear down all the school data ready for a fresh import
		/// </summary>
		/// <returns></returns>
		public async Task DeleteSchools()
		{
			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"UPDATE {Tables.Team} SET SchoolId = NULL").ConfigureAwait(false);
						await database.ExecuteAsync($"DELETE FROM {Tables.SchoolName}").ConfigureAwait(false);
						await database.ExecuteAsync($@"DELETE FROM {Tables.School}").ConfigureAwait(false);
						transaction.Complete();
					}

					scope.Complete();
				}
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerSchoolDataMigrator>(e);
				throw;
			}

			await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/schools/").ConfigureAwait(false);
		}

		/// <summary>
		/// Save the supplied School to the database with its existing <see cref="School.SchoolId"/>
		/// </summary>
		public async Task<School> MigrateSchool(School school)
		{
			if (school is null)
			{
				throw new System.ArgumentNullException(nameof(school));
			}

			var migratedSchool = new School
			{
				SchoolId = school.SchoolId,
				SchoolName = school.SchoolName,
				PlaysOutdoors = school.PlaysOutdoors,
				PlaysIndoors = school.PlaysIndoors,
				Twitter = school.Twitter,
				Facebook = school.Facebook,
				Instagram = school.Instagram,
				HowManyPlayers = school.HowManyPlayers,
				SchoolRoute = "/schools" + school.SchoolRoute.Substring(6),
				DateCreated = school.DateCreated,
				DateUpdated = school.DateUpdated
			};

			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;
					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.School} ON").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.School}
						(SchoolId, PlaysOutdoors, PlaysIndoors, Twitter, Facebook, Instagram, HowManyPlayers, SchoolRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7)",
							migratedSchool.SchoolId,
							migratedSchool.PlaysOutdoors,
							migratedSchool.PlaysIndoors,
							migratedSchool.Twitter,
							migratedSchool.Facebook,
							migratedSchool.Instagram,
							migratedSchool.HowManyPlayers,
							migratedSchool.SchoolRoute).ConfigureAwait(false);
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.School} OFF").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.SchoolName} 
							(SchoolId, SchoolName, FromDate) VALUES (@0, @1, @2)",
							migratedSchool.SchoolId,
							migratedSchool.SchoolName,
							migratedSchool.DateCreated
							).ConfigureAwait(false);
						transaction.Complete();
					}

					scope.Complete();
				}
			}
			catch (Exception e)
			{
				_logger.Error<SqlServerSchoolDataMigrator>(e);
				throw;
			}

			await _redirectsRepository.InsertRedirect(school.SchoolRoute, migratedSchool.SchoolRoute, string.Empty).ConfigureAwait(false);
			await _redirectsRepository.InsertRedirect(school.SchoolRoute, migratedSchool.SchoolRoute, "/matches.rss").ConfigureAwait(false);

			await _auditRepository.CreateAudit(new AuditRecord
			{
				Action = AuditAction.Create,
				ActorName = nameof(SqlServerSchoolDataMigrator),
				EntityUri = school.EntityUri,
				State = JsonConvert.SerializeObject(school),
				AuditDate = school.DateCreated.Value
			}).ConfigureAwait(false);

			await _auditRepository.CreateAudit(new AuditRecord
			{
				Action = AuditAction.Update,
				ActorName = nameof(SqlServerSchoolDataMigrator),
				EntityUri = migratedSchool.EntityUri,
				State = JsonConvert.SerializeObject(migratedSchool),
				AuditDate = migratedSchool.DateUpdated.Value
			}).ConfigureAwait(false);

			return migratedSchool;
		}
	}
}
