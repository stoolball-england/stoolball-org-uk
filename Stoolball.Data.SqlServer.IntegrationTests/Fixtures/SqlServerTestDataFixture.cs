using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Stoolball.Statistics;
using Stoolball.Testing;
using Stoolball.Testing.Factories;

namespace Stoolball.Data.SqlServer.IntegrationTests.Fixtures
{
    public class SqlServerTestDataFixture : BaseSqlServerFixture
    {
        public TestData TestData { get; set; }

        internal Randomiser Randomiser { get; }

        internal OverSetFactory OverSetFactory { get; }

        public SqlServerTestDataFixture() : base("StoolballIntegrationTests")
        {
            // Populate seed data so that there's a consistent baseline for each test run
            using var serviceProvider = new ServiceCollection().AddSeedDataGenerator().BuildServiceProvider();

            Randomiser = serviceProvider.GetRequiredService<Randomiser>();
            OverSetFactory = serviceProvider.GetRequiredService<OverSetFactory>();
            var playerIdentityFinder = serviceProvider.GetRequiredService<IPlayerIdentityFinder>();
            var oversHelper = serviceProvider.GetRequiredService<IOversHelper>();
            var playerInMatchStatisticsBuilder = new PlayerInMatchStatisticsBuilder(playerIdentityFinder, oversHelper);
            var randomSeedDataGenerator = serviceProvider.GetRequiredService<SeedDataGenerator>();

            Randomizer.Seed = new Random(85437684);
            TestData = randomSeedDataGenerator.GenerateTestData();

            using (var connection = ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();

                var repo = new SqlServerIntegrationTestsRepository(connection, playerInMatchStatisticsBuilder);
                repo.CreateUmbracoBaseRecords();
                repo.CreateTestData(TestData);
            }
        }

    }
}
