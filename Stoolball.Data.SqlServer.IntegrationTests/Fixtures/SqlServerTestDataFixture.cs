using Bogus;
using Stoolball.Awards;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Testing;
using Stoolball.Testing.Fakers;

namespace Stoolball.Data.SqlServer.IntegrationTests.Fixtures
{
    public class SqlServerTestDataFixture : BaseSqlServerFixture
    {
        public TestData TestData { get; set; }

        internal Randomiser Randomiser { get; set; } = new Randomiser(new Random());

        internal OverSetFakerFactory OverSetFakerFactory { get; set; } = new();

        public SqlServerTestDataFixture() : base("StoolballIntegrationTests")
        {
            // Populate seed data so that there's a consistent baseline for each test run
            var oversHelper = new OversHelper();
            var bowlingFiguresCalculator = new BowlingFiguresCalculator(oversHelper);
            var playerIdentityFinder = new PlayerIdentityFinder();
            var matchFinder = new MatchFinder();
            var playerInMatchStatisticsBuilder = new PlayerInMatchStatisticsBuilder(playerIdentityFinder, oversHelper);
            var competitionFakerFactory = new CompetitionFakerFactory();
            var seasonFakerFactory = new SeasonFakerFactory();
            var teamFakerFactory = new TeamFakerFactory();
            var clubFakerFactory = new ClubFakerFactory();
            var matchLocationFakerFactory = new MatchLocationFakerFactory();
            var schoolFakerFactory = new SchoolFakerFactory();
            var playerIdentityFakerFactory = new PlayerIdentityFakerFactory();
            var playerFakerFactory = new PlayerFakerFactory();
            var playerOfTheMatchAward = new Award { AwardId = Guid.NewGuid(), AwardName = "Player of the match" };
            var randomSeedDataGenerator = new SeedDataGenerator(Randomiser, oversHelper, bowlingFiguresCalculator, playerIdentityFinder, matchFinder,
                competitionFakerFactory, seasonFakerFactory, teamFakerFactory, clubFakerFactory, matchLocationFakerFactory, schoolFakerFactory,
                playerFakerFactory, playerIdentityFakerFactory, OverSetFakerFactory, playerOfTheMatchAward);

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
