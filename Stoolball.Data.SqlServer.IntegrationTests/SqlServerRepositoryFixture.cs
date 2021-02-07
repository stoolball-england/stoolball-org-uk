using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public sealed class SqlServerRepositoryFixture : BaseSqlServerFixture
    {
        public Match MatchInThePastWithFullDetailsForDelete { get; private set; }
        public Competition CompetitionWithFullDetailsForDelete { get; private set; }
        public Season SeasonWithFullDetailsForDelete { get; private set; }
        public MatchLocation MatchLocationWithFullDetailsForDelete { get; private set; }
        public Club ClubWithTeamsForDelete { get; private set; }

        public SqlServerRepositoryFixture() : base("StoolballRepositoryIntegrationTests")
        {
            // Populate seed data so that there's a consistent baseline for each test run
            SeedDatabase();
        }

        protected override void SeedDatabase()
        {
            var seedDataGenerator = new SeedDataGenerator();
            using (var connection = ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();

                var repo = new SqlServerIntegrationTestsRepository(connection);

                ClubWithTeamsForDelete = seedDataGenerator.CreateClubWithTeams();
                repo.CreateClub(ClubWithTeamsForDelete);

                MatchInThePastWithFullDetailsForDelete = seedDataGenerator.CreateMatchInThePastWithFullDetails();
                repo.CreateMatch(MatchInThePastWithFullDetailsForDelete);

                CompetitionWithFullDetailsForDelete = seedDataGenerator.CreateCompetitionWithFullDetails();
                repo.CreateCompetition(CompetitionWithFullDetailsForDelete);
                foreach (var season in CompetitionWithFullDetailsForDelete.Seasons)
                {
                    repo.CreateSeason(season, CompetitionWithFullDetailsForDelete.CompetitionId.Value);
                }

                var competitionForSeason = seedDataGenerator.CreateCompetitionWithMinimalDetails();
                SeasonWithFullDetailsForDelete = seedDataGenerator.CreateSeasonWithFullDetails(competitionForSeason, 2021, 2021);
                foreach (var team in SeasonWithFullDetailsForDelete.Teams)
                {
                    repo.CreateTeam(team.Team);
                }
                repo.CreateCompetition(competitionForSeason);
                repo.CreateSeason(SeasonWithFullDetailsForDelete, competitionForSeason.CompetitionId.Value);
                foreach (var team in SeasonWithFullDetailsForDelete.Teams)
                {
                    repo.AddTeamToSeason(team);
                }

                MatchLocationWithFullDetailsForDelete = seedDataGenerator.CreateMatchLocationWithFullDetails();
                repo.CreateMatchLocation(MatchLocationWithFullDetailsForDelete);
            }
        }
    }
}
