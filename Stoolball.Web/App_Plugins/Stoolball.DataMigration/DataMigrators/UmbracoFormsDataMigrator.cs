using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Data.SqlServer;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    /// <summary>
    /// Clears down Umbraco Forms and recreates the forms required for launch
    /// </summary>
    public class UmbracoFormsDataMigrator : IUmbracoFormsDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        public UmbracoFormsDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
        }

        public async Task RecreateForms()
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var (userId, email) = await connection.QuerySingleAsync<(int, string)>("SELECT TOP 1 id, userEmail FROM umbracoUser WHERE userLogin LIKE '%@stoolball.org.uk' ORDER BY id ASC", null, transaction).ConfigureAwait(false);

                    var commands = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Forms\RecreateUmbracoForms.sql"));
                    foreach (var command in commands)
                    {
                        if (string.IsNullOrWhiteSpace(command)) { continue; }
                        await connection.ExecuteAsync(command.Replace("email@example.org", email), new { userId = userId.ToString(CultureInfo.InvariantCulture) }, transaction).ConfigureAwait(false);
                    }

                    transaction.Commit();
                }
            }
        }
    }
}
