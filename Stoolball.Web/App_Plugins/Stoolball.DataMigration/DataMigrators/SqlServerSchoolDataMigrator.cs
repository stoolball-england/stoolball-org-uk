using Newtonsoft.Json;
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
		}

		/// <summary>
		/// Save the supplied School to the database with its existing <see cref="School.SchoolId"/>
		/// </summary>
		public async Task MigrateSchool(School school)
		{
			if (school is null)
			{
				throw new System.ArgumentNullException(nameof(school));
			}

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
							school.SchoolId,
							school.PlaysOutdoors,
							school.PlaysIndoors,
							school.Twitter,
							school.Facebook,
							school.Instagram,
							school.HowManyPlayers,
							school.SchoolRoute).ConfigureAwait(false);
						await database.ExecuteAsync($"SET IDENTITY_INSERT {Tables.School} OFF").ConfigureAwait(false);
						await database.ExecuteAsync($@"INSERT INTO {Tables.SchoolName} 
							(SchoolId, SchoolName, FromDate) VALUES (@0, @1, @2)",
							school.SchoolId,
							school.SchoolName,
							school.DateCreated
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

			await _auditRepository.CreateAudit(new AuditRecord
			{
				Action = AuditAction.Create,
				ActorName = nameof(SqlServerSchoolDataMigrator),
				EntityUri = school.EntityUri,
				State = JsonConvert.SerializeObject(school),
				AuditDate = school.DateCreated.Value
			}).ConfigureAwait(false);
		}
	}
}
