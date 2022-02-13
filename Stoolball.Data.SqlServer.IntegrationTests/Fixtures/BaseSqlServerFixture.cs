using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Management.Smo;

namespace Stoolball.Data.SqlServer.IntegrationTests.Fixtures
{
    public abstract class BaseSqlServerFixture : IDisposable
    {
        private const string _localDbInstance = @"(LocalDB)\MSSQLLocalDb";
        private string _sqlServerContainerInstance;
        private readonly string _databaseName;
        private readonly string _umbracoDatabasePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"../../../../Stoolball.Web/umbraco/Data/Umbraco.mdf"));
        private readonly string _dacpacPath;
        private bool _isDisposed;
        DacServices _dacServices;

        public IDatabaseConnectionFactory ConnectionFactory { get; private set; }

        protected BaseSqlServerFixture(string databaseName)
        {
            var testEnvironmentIsLocalDb = File.Exists(_umbracoDatabasePath);
            _databaseName = databaseName;
            _dacpacPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"../../../{_databaseName}.dacpac");
            string connectionStringForTests;

            try
            {
                if (testEnvironmentIsLocalDb)
                {
                    // Clean up from any previous failed test run - not relevant (and causes a Kerberos error) in CI
                    RemoveIntegrationTestsDatabaseIfExists();

                    // In local dev, connect to the existing Umbraco database, which is named after its full path, and export it as a DACPAC
                    // The DACPAC is committed and used by CI without needing to connect to an Umbraco database which isn't there.
                    _dacServices = new DacServices(new SqlConnectionStringBuilder
                    {
                        DataSource = _localDbInstance,
                        IntegratedSecurity = true,
                        InitialCatalog = _umbracoDatabasePath
                    }.ToString());

                    CreateOrReplaceDacPac();

                    connectionStringForTests = new SqlConnectionStringBuilder
                    {
                        DataSource = _localDbInstance,
                        IntegratedSecurity = true,
                        InitialCatalog = _databaseName,
                        MultipleActiveResultSets = true
                    }.ToString();
                }
                else
                {
                    _sqlServerContainerInstance = GetContainerIpAddress("integration-tests-sql");
                    _dacServices = new DacServices(new SqlConnectionStringBuilder
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
                        InitialCatalog = _databaseName,
                        MultipleActiveResultSets = true
                    }.ToString();
                }

                // Import the DACPAC with a new name - and all data cleared down ready for testing
                var dacpac = DacPackage.Load(_dacpacPath);
                _dacServices.Deploy(dacpac, _databaseName, true, null, new CancellationToken());
            }
            catch (DacServicesException ex)
            {
                throw new InvalidOperationException("IIS Express must be stopped for integration tests to run.", ex);
            }

            // Configure Dapper
            SqlMapper.AddTypeHandler(new DapperUriTypeHandler());

            // Tell Dapper that dates going into and out of the database are in UTC
            SqlMapper.AddTypeHandler(new DapperDateTimeHandler());

            // Create a connection factory that connects to the database, and is accessible via a protected property by classes being tested
            ConnectionFactory = new IntegrationTestsDatabaseConnectionFactory(connectionStringForTests);
        }

        private void CreateOrReplaceDacPac()
        {
            if (File.Exists(_dacpacPath))
            {
                // When a .dacpac is regenerated there is a generated id and timestamp for the operation, which causes a git commit even if nothing
                // else has changed. To prevent that, only replace the dacpac if the actual model inside it has changed.
                var tempDacpacPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".dacpac");
                _dacServices.Extract(tempDacpacPath, _umbracoDatabasePath, _databaseName, new Version(1, 0, 0));

                var checksumBefore = ReadDacpacModelChecksum(_dacpacPath);
                var checksumAfter = ReadDacpacModelChecksum(tempDacpacPath);

                if (checksumAfter != checksumBefore)
                {
                    File.Copy(tempDacpacPath, _dacpacPath, true);
                }

                File.Delete(tempDacpacPath);
            }
            else
            {
                _dacServices.Extract(_dacpacPath, _umbracoDatabasePath, _databaseName, new Version(1, 0, 0));
            }
        }

        private static string ReadDacpacModelChecksum(string dacpacPath)
        {
            var tempFolder = dacpacPath + "-extract";
            ZipFile.ExtractToDirectory(dacpacPath, tempFolder);

            var doc = XDocument.Load(Path.Join(tempFolder, "Origin.xml"));
            var root = doc.Document.Root;
            var checksum = root.Element("{" + root.GetDefaultNamespace() + "}Checksums").Element("{" + root.GetDefaultNamespace() + "}Checksum").Value;

            Directory.Delete(tempFolder, true);

            return checksum;
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
