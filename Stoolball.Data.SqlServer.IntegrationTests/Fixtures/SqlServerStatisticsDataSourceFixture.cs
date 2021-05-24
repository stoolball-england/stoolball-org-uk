using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing;

namespace Stoolball.Data.SqlServer.IntegrationTests.Fixtures
{
    public class SqlServerStatisticsDataSourceFixture : BaseSqlServerFixture
    {
        public TestData TestData { get; set; }

        public SqlServerStatisticsDataSourceFixture() : base("StoolballStatisticsDataSourceIntegrationTests")
        {
            // Populate seed data so that there's a consistent baseline for each test run
            var oversHelper = new OversHelper();
            var bowlingFiguresCalculator = new BowlingFiguresCalculator(oversHelper);
            var playerIdentityFinder = new PlayerIdentityFinder();
            var randomSeedDataGenerator = new SeedDataGenerator(oversHelper, bowlingFiguresCalculator, playerIdentityFinder);
            TestData = randomSeedDataGenerator.GenerateTestData();

            using (var connection = ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();

                var repo = new SqlServerIntegrationTestsRepository(connection);
                repo.CreateTestData(TestData);
            }
        }
    }
}
