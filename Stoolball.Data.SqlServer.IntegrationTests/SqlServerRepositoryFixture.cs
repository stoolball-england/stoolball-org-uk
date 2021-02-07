using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public sealed class SqlServerRepositoryFixture : BaseSqlServerFixture
    {
        public Match MatchWithFullDetailsForDelete { get; private set; }
        public Tournament TournamentWithFullDetailsForDelete { get; private set; }
        public Competition CompetitionWithFullDetailsForDelete { get; private set; }
        public Season SeasonWithFullDetailsForDelete { get; private set; }
        public MatchLocation MatchLocationWithFullDetailsForDelete { get; private set; }
        public Club ClubWithTeamsForDelete { get; private set; }
        public Team TeamWithFullDetailsForDelete { get; private set; }

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

                TeamWithFullDetailsForDelete = seedDataGenerator.CreateTeamWithFullDetails();
                repo.CreateTeam(TeamWithFullDetailsForDelete);
                foreach (var matchLocation in TeamWithFullDetailsForDelete.MatchLocations)
                {
                    repo.CreateMatchLocation(matchLocation);
                    repo.AddTeamToMatchLocation(TeamWithFullDetailsForDelete, matchLocation);
                }
                repo.CreateCompetition(TeamWithFullDetailsForDelete.Seasons[0].Season.Competition);
                foreach (var season in TeamWithFullDetailsForDelete.Seasons)
                {
                    repo.CreateSeason(season.Season, season.Season.Competition.CompetitionId.Value);
                    repo.AddTeamToSeason(season);
                }

                MatchWithFullDetailsForDelete = seedDataGenerator.CreateMatchInThePastWithFullDetails();
                repo.CreateMatch(MatchWithFullDetailsForDelete);

                TournamentWithFullDetailsForDelete = seedDataGenerator.CreateTournamentInThePastWithFullDetails();
                foreach (var team in TournamentWithFullDetailsForDelete.Teams)
                {
                    repo.CreateTeam(team.Team);
                }
                repo.CreateMatchLocation(TournamentWithFullDetailsForDelete.TournamentLocation);
                repo.CreateTournament(TournamentWithFullDetailsForDelete);
                foreach (var team in TournamentWithFullDetailsForDelete.Teams)
                {
                    repo.AddTeamToTournament(team, TournamentWithFullDetailsForDelete);
                }
                foreach (var season in TournamentWithFullDetailsForDelete.Seasons)
                {
                    repo.CreateCompetition(season.Competition);
                    repo.CreateSeason(season, season.Competition.CompetitionId.Value);
                    repo.AddTournamentToSeason(TournamentWithFullDetailsForDelete, season);
                }

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
