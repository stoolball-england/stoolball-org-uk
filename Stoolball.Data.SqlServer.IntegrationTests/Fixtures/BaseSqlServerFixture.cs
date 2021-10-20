using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Management.Smo;

namespace Stoolball.Data.SqlServer.IntegrationTests.Fixtures
{
    public abstract class BaseSqlServerFixture : IDisposable
    {
        private const string _localDbInstance = @"(LocalDB)\Umbraco";
        private string _sqlServerContainerInstance;
        private readonly string _databaseName;
        private readonly string _umbracoDatabasePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Stoolball.Web\App_Data\Umbraco.mdf"));
        private readonly string _dacpacPath;
        private bool _isDisposed;

        public IDatabaseConnectionFactory ConnectionFactory { get; private set; }

        protected BaseSqlServerFixture(string databaseName)
        {
            var testEnvironmentIsLocalDb = File.Exists(_umbracoDatabasePath);
            _databaseName = databaseName;
            _dacpacPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{_databaseName}.dacpac");
            string connectionStringForTests;

            try
            {
                DacServices dacServices;

                if (testEnvironmentIsLocalDb)
                {
                    // Clean up from any previous failed test run - not relevant (and causes a Kerberos error) in CI
                    RemoveIntegrationTestsDatabaseIfExists();

                    // In local dev, connect to the existing Umbraco database, which is named after its full path, and export it as a DACPAC
                    // The DACPAC is committed and used by CI without needing to connect to an Umbraco database which isn't there.
                    dacServices = new DacServices(new SqlConnectionStringBuilder { DataSource = _localDbInstance, IntegratedSecurity = true, InitialCatalog = _umbracoDatabasePath }.ToString());
                    dacServices.Extract(_dacpacPath, _umbracoDatabasePath, _databaseName, new Version(1, 0, 0));

                    connectionStringForTests = new SqlConnectionStringBuilder { DataSource = _localDbInstance, IntegratedSecurity = true, InitialCatalog = _databaseName }.ToString();
                }
                else
                {
                    _sqlServerContainerInstance = GetContainerIpAddress("integration-tests-sql");
                    dacServices = new DacServices(new SqlConnectionStringBuilder
                    {
                        DataSource = _sqlServerContainerInstance,
                        UserID = Environment.GetEnvironmentVariable("SQL_SERVER_USERNAME"),
                        Password = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD")
                    }.ToString());

                    connectionStringForTests = new SqlConnectionStringBuilder
                    {
                        DataSource = _sqlServerContainerInstance,
                        UserID = Environment.GetEnvironmentVariable("SQL_SERVER_USERNAME"),
                        Password = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD"),
                        InitialCatalog = _databaseName
                    }.ToString();
                }

                // Import the DACPAC with a new name - and all data cleared down ready for testing
                var dacpac = DacPackage.Load(_dacpacPath);
                dacServices.Deploy(dacpac, _databaseName, true, null, new CancellationToken());
            }
            catch (DacServicesException ex)
            {
                throw new InvalidOperationException("IIS Express must be stopped for integration tests to run.", ex);
            }

            // Configure Dapper
            SqlMapper.AddTypeHandler(new DapperUriTypeHandler());

            // Create a connection factory that connects to the database, and is accessible via a protected property by classes being tested
            ConnectionFactory = new IntegrationTestsDatabaseConnectionFactory(connectionStringForTests);
        }


        private static string GetContainerIpAddress(string containerName)
        {
            string inspectCommand = string.Concat("inspect -f ", "\"{{range.NetworkSettings.Networks}}{{.IPAddress}}{{end}}\"", $" {containerName}");
            var processInfo = new ProcessStartInfo("docker", $"{inspectCommand}");

            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.RedirectStandardError = true;

            using (var process = new Process())
            {
                process.StartInfo = processInfo;
                var started = process.Start();

                using (var reader = process.StandardOutput)
                {
                    //to remove any unwanted char if appended 
                    var ip = Regex.Replace(reader.ReadToEnd(), @"\t|\n|\r", string.Empty);
                    if (!string.IsNullOrEmpty(ip))
                    {
                        return ip;
                    }
                }
            }
            throw new Exception("Couldn't find the IP of the container");
        }

        private void RemoveIntegrationTestsDatabaseIfExists()
        {
            var smoServer = new Server(_localDbInstance);
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

            _isDisposed = true;
        }
    }
}
