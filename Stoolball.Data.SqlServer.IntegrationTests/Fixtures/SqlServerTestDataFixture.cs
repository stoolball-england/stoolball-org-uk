using Bogus;
using Stoolball.Awards;
using Stoolball.Statistics;
using Stoolball.Testing;
using Stoolball.Testing.Factories;

namespace Stoolball.Data.SqlServer.IntegrationTests.Fixtures
{
    public class SqlServerTestDataFixture : BaseSqlServerFixture
    {
        public TestData TestData { get; set; }

        internal Randomiser Randomiser { get; set; } = new Randomiser(new Random());

        internal OverSetFactory OverSetFakerFactory { get; set; } = new();

        public SqlServerTestDataFixture() : base("StoolballIntegrationTests")
        {
            // Populate seed data so that there's a consistent baseline for each test run
            var oversHelper = new OversHelper();
            var bowlingFiguresCalculator = new BowlingFiguresCalculator(oversHelper);
            var playerIdentityFinder = new PlayerIdentityFinder();
            var matchFinder = new MatchFinder();
            var playerInMatchStatisticsBuilder = new PlayerInMatchStatisticsBuilder(playerIdentityFinder, oversHelper);
            var competitionFactory = new CompetitionFactory();
            var seasonFactory = new SeasonFactory();
            var teamFactory = new TeamFactory();
            var clubFactory = new ClubFactory();
            var commentFactory = new CommentFactory();
            var tournamentFactory = new TournamentFactory(seasonFactory, commentFactory);
            var matchLocationFactory = new MatchLocationFactory();
            var schoolFactory = new SchoolFactory();
            var playerFakerFactory = new PlayerFactory();
            var playerOfTheMatchAward = new Award { AwardId = Guid.NewGuid(), AwardName = "Player of the match" };
            var randomSeedDataGenerator = new SeedDataGenerator(Randomiser, oversHelper, bowlingFiguresCalculator, playerIdentityFinder, matchFinder,
                competitionFactory, seasonFactory, teamFactory, clubFactory, tournamentFactory, matchLocationFactory, schoolFactory,
                playerFakerFactory, OverSetFakerFactory, commentFactory, playerOfTheMatchAward);

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
