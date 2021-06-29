using System;
using System.IO;
using System.Threading;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Management.Smo;

namespace Stoolball.Data.SqlServer.IntegrationTests.Fixtures
{
    public abstract class BaseSqlServerFixture : IDisposable
    {
        private const string _SQL_SERVER_INSTANCE = @"(LocalDB)\Umbraco";
        private readonly string _databaseName;
        private readonly string _umbracoDatabasePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Stoolball.Web\App_Data\Umbraco.mdf"));
        private readonly string _dacpacPath;
        private bool _isDisposed;

        public IDatabaseConnectionFactory ConnectionFactory { get; private set; }

        protected BaseSqlServerFixture(string databaseName)
        {
            _databaseName = databaseName;
            _dacpacPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{_databaseName}-{Guid.NewGuid()}.dacpac");

            try
            {

                // Clean up from any previous failed test run
                RemoveIntegrationTestsDatabaseIfExists();

                // Connect to the existing Umbraco database, which is named after its full path, and export it as a DACPAC
                var ds = new DacServices(new SqlConnectionStringBuilder { DataSource = _SQL_SERVER_INSTANCE, IntegratedSecurity = true, InitialCatalog = _umbracoDatabasePath }.ToString());
                ds.Extract(_dacpacPath, _umbracoDatabasePath, _databaseName, new Version(1, 0, 0));

                // Import the DACPAC with a new name - and all data cleared down ready for testing
                var dacpac = DacPackage.Load(_dacpacPath);
                ds.Deploy(dacpac, _databaseName, true, null, new CancellationToken());
            }
            catch (DacServicesException ex)
            {
                throw new InvalidOperationException("IIS Express must be stopped for integration tests to run.", ex);
            }
            finally
            {
                if (File.Exists(_dacpacPath))
                {
                    File.Delete(_dacpacPath);
                }
            }

            // Configure Dapper
            SqlMapper.AddTypeHandler(new DapperUriTypeHandler());

            // Create a connection factory that connects to the database, and is accessible via a protected property by classes being tested
            ConnectionFactory = new IntegrationTestsDatabaseConnectionFactory(new SqlConnectionStringBuilder { DataSource = _SQL_SERVER_INSTANCE, IntegratedSecurity = true, InitialCatalog = _databaseName }.ToString());
        }

        private void RemoveIntegrationTestsDatabaseIfExists()
        {
            var smoServer = new Server(_SQL_SERVER_INSTANCE);
            if (smoServer.Databases.Contains(_databaseName))
            {
                smoServer.KillDatabase(_databaseName);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) { return; }

            //RemoveIntegrationTestsDatabaseIfExists();

            _isDisposed = true;
        }
    }
}
