namespace Stoolball.Data.SqlServer.IntegrationTests.Fixtures
{
    public class SqlServerStatisticsDataSourceFixture : BaseSqlServerFixture
    {
        public TestData TestData { get; set; }

        public SqlServerStatisticsDataSourceFixture() : base("StoolballStatisticsDataSourceIntegrationTests")
        {
            // Populate seed data so that there's a consistent baseline for each test run
            var seedDataGenerator = new FixedSeedDataGenerator();
            var randomSeedDataGenerator = new RandomSeedDataGenerator(seedDataGenerator);
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
