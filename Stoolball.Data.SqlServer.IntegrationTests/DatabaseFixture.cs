using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Management.Smo;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public sealed class DatabaseFixture : IDisposable
    {
        private const string _INTEGRATION_TESTS_DATABASE = "StoolballIntegrationTests";
        private const string _SQL_SERVER_INSTANCE = @"(LocalDB)\Umbraco";
        private readonly string _umbracoDatabasePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Stoolball.Web\App_Data\Umbraco.mdf"));
        private readonly string _dacpacPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _INTEGRATION_TESTS_DATABASE + ".dacpac");

        public IDatabaseConnectionFactory ConnectionFactory { get; private set; }

        public Match MatchInThePastWithMinimalDetails { get; private set; }
        public Match MatchInTheFutureWithMinimalDetails { get; private set; }

        public Match MatchInThePastWithFullDetails { get; private set; }
        public Match MatchInThePastWithFullDetailsAndTournament { get; private set; }
        public Tournament TournamentInThePastWithMinimalDetails { get; private set; }
        public Tournament TournamentInThePastWithFullDetails { get; private set; }
        public Tournament TournamentInTheFutureWithMinimalDetails { get; private set; }
        public Competition CompetitionWithMinimalDetails { get; private set; }
        public Competition CompetitionWithFullDetails { get; private set; }
        public Competition CompetitionWithFullDetailsForDelete { get; private set; }
        public MatchLocation MatchLocationWithMinimalDetails { get; private set; }
        public MatchLocation MatchLocationWithFullDetails { get; private set; }
        public Club ClubWithMinimalDetails { get; private set; }
        public Club ClubWithTeams { get; private set; }
        public Club ClubWithTeamsForDelete { get; private set; }
        public Team TeamWithMinimalDetails { get; private set; }
        public Team TeamWithFullDetails { get; private set; }
        public List<Competition> Competitions { get; internal set; } = new List<Competition>();
        public List<Match> Matches { get; internal set; } = new List<Match>();
        public List<MatchLocation> MatchLocations { get; internal set; } = new List<MatchLocation>();
        public List<Team> Teams { get; internal set; } = new List<Team>();

        public DatabaseFixture()
        {
            try
            {
                // Clean up from any previous failed test run
                RemoveIntegrationTestsDatabaseIfExists();

                // Connect to the existing Umbraco database, which is named after its full path, and export it as a DACPAC
                var ds = new DacServices(new SqlConnectionStringBuilder { DataSource = _SQL_SERVER_INSTANCE, IntegratedSecurity = true, InitialCatalog = _umbracoDatabasePath }.ToString());
                ds.Extract(_dacpacPath, _umbracoDatabasePath, _INTEGRATION_TESTS_DATABASE, new Version(1, 0, 0));

                // Import the DACPAC with a new name - and all data cleared down ready for testing
                var dacpac = DacPackage.Load(_dacpacPath);
                ds.Deploy(dacpac, _INTEGRATION_TESTS_DATABASE, true, null, new CancellationToken());
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

            // Create a connection factory that connects to the database, and is accessible via a protected property by classes being tested
            ConnectionFactory = new IntegrationTestsDatabaseConnectionFactory(new SqlConnectionStringBuilder { DataSource = _SQL_SERVER_INSTANCE, IntegratedSecurity = true, InitialCatalog = _INTEGRATION_TESTS_DATABASE }.ToString());

            // Populate seed data so that there's a consistent baseline for each test run
            SeedDatabase();
        }

        private static void RemoveIntegrationTestsDatabaseIfExists()
        {
            var smoServer = new Server(_SQL_SERVER_INSTANCE);
            if (smoServer.Databases.Contains(_INTEGRATION_TESTS_DATABASE))
            {
                smoServer.KillDatabase(_INTEGRATION_TESTS_DATABASE);
            }
        }

        private void SeedDatabase()
        {
            var seedDataGenerator = new SeedDataGenerator();
            using (var connection = ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();

                var repo = new SqlServerIntegrationTestsRepository(connection);

                ClubWithMinimalDetails = seedDataGenerator.CreateClubWithMinimalDetails();
                repo.CreateClub(ClubWithMinimalDetails);

                ClubWithTeams = seedDataGenerator.CreateClubWithTeams();
                repo.CreateClub(ClubWithTeams);
                Teams.AddRange(ClubWithTeams.Teams);

                ClubWithTeamsForDelete = seedDataGenerator.CreateClubWithTeams();
                repo.CreateClub(ClubWithTeams);
                Teams.AddRange(ClubWithTeams.Teams);

                TeamWithMinimalDetails = seedDataGenerator.CreateTeamWithMinimalDetails("Team minimal");
                repo.CreateTeam(TeamWithMinimalDetails);
                Teams.Add(TeamWithMinimalDetails);

                TeamWithFullDetails = seedDataGenerator.CreateTeamWithFullDetails();
                repo.CreateTeam(TeamWithFullDetails);
                Teams.Add(TeamWithFullDetails);
                foreach (var matchLocation in TeamWithFullDetails.MatchLocations)
                {
                    repo.CreateMatchLocation(matchLocation);
                    repo.AddTeamToMatchLocation(TeamWithFullDetails, matchLocation);
                    MatchLocations.Add(matchLocation);
                }
                repo.CreateCompetition(TeamWithFullDetails.Seasons[0].Season.Competition);
                Competitions.Add(TeamWithFullDetails.Seasons[0].Season.Competition);
                foreach (var season in TeamWithFullDetails.Seasons)
                {
                    repo.CreateSeason(season.Season, season.Season.Competition.CompetitionId.Value);
                    repo.AddTeamToSeason(season);
                }

                MatchInThePastWithMinimalDetails = seedDataGenerator.CreateMatchInThePastWithMinimalDetails();
                repo.CreateMatch(MatchInThePastWithMinimalDetails);

                MatchInTheFutureWithMinimalDetails = seedDataGenerator.CreateMatchInThePastWithMinimalDetails();
                MatchInTheFutureWithMinimalDetails.StartTime = DateTime.UtcNow.AddMonths(1);
                repo.CreateMatch(MatchInTheFutureWithMinimalDetails);

                MatchInThePastWithFullDetails = seedDataGenerator.CreateMatchInThePastWithFullDetails();
                repo.CreateMatch(MatchInThePastWithFullDetails);
                Teams.AddRange(MatchInThePastWithFullDetails.Teams.Select(x => x.Team));

                TournamentInThePastWithMinimalDetails = seedDataGenerator.CreateTournamentInThePastWithMinimalDetails();
                repo.CreateTournament(TournamentInThePastWithMinimalDetails);

                TournamentInTheFutureWithMinimalDetails = seedDataGenerator.CreateTournamentInThePastWithMinimalDetails();
                TournamentInTheFutureWithMinimalDetails.StartTime = DateTime.UtcNow.AddMonths(1);
                repo.CreateTournament(TournamentInTheFutureWithMinimalDetails);

                TournamentInThePastWithFullDetails = seedDataGenerator.CreateTournamentInThePastWithFullDetails();
                Teams.AddRange(TournamentInThePastWithFullDetails.Teams.Select(x => x.Team));
                foreach (var team in TournamentInThePastWithFullDetails.Teams)
                {
                    repo.CreateTeam(team.Team);
                }
                MatchLocations.Add(TournamentInThePastWithFullDetails.TournamentLocation);
                repo.CreateMatchLocation(TournamentInThePastWithFullDetails.TournamentLocation);
                repo.CreateTournament(TournamentInThePastWithFullDetails);
                foreach (var team in TournamentInThePastWithFullDetails.Teams)
                {
                    repo.AddTeamToTournament(team, TournamentInThePastWithFullDetails);
                }
                foreach (var season in TournamentInThePastWithFullDetails.Seasons)
                {
                    repo.CreateCompetition(season.Competition);
                    Competitions.Add(season.Competition);
                    repo.CreateSeason(season, season.Competition.CompetitionId.Value);
                    repo.AddTournamentToSeason(TournamentInThePastWithFullDetails, season);
                }

                MatchInThePastWithFullDetailsAndTournament = seedDataGenerator.CreateMatchInThePastWithFullDetails();
                MatchInThePastWithFullDetailsAndTournament.Tournament = TournamentInThePastWithMinimalDetails;
                MatchInThePastWithFullDetailsAndTournament.Season.FromYear = MatchInThePastWithFullDetailsAndTournament.Season.UntilYear = 2018;
                repo.CreateMatch(MatchInThePastWithFullDetailsAndTournament);
                Teams.AddRange(MatchInThePastWithFullDetailsAndTournament.Teams.Select(x => x.Team));

                CompetitionWithMinimalDetails = seedDataGenerator.CreateCompetitionWithMinimalDetails();
                repo.CreateCompetition(CompetitionWithMinimalDetails);

                CompetitionWithFullDetails = CreateCompetitionWithFullDetails(seedDataGenerator, repo);
                CompetitionWithFullDetailsForDelete = CreateCompetitionWithFullDetails(seedDataGenerator, repo);

                MatchLocationWithMinimalDetails = MatchInThePastWithFullDetails.MatchLocation;
                MatchLocationWithFullDetails = seedDataGenerator.CreateMatchLocationWithFullDetails();
                repo.CreateMatchLocation(MatchLocationWithFullDetails);
                Teams.AddRange(MatchLocationWithFullDetails.Teams);

                Competitions.AddRange(new[] { CompetitionWithMinimalDetails, CompetitionWithFullDetails, CompetitionWithFullDetailsForDelete, MatchInThePastWithFullDetails.Season.Competition, MatchInThePastWithFullDetailsAndTournament.Season.Competition });
                MatchLocations.AddRange(new[] { MatchInThePastWithFullDetails.MatchLocation, MatchInThePastWithFullDetailsAndTournament.MatchLocation, MatchLocationWithFullDetails });

                for (var i = 0; i < 30; i++)
                {
                    var competition = seedDataGenerator.CreateCompetitionWithMinimalDetails();
                    repo.CreateCompetition(competition);
                    Competitions.Add(competition);

                    var matchLocation = seedDataGenerator.CreateMatchLocationWithMinimalDetails();
                    repo.CreateMatchLocation(matchLocation);
                    MatchLocations.Add(matchLocation);
                }

                Matches.AddRange(new[] { MatchInThePastWithMinimalDetails, MatchInThePastWithFullDetails });
            }
        }

        private static Competition CreateCompetitionWithFullDetails(SeedDataGenerator seedDataGenerator, SqlServerIntegrationTestsRepository repo)
        {
            var competition = seedDataGenerator.CreateCompetitionWithFullDetails();
            repo.CreateCompetition(competition);
            foreach (var season in competition.Seasons)
            {
                repo.CreateSeason(season, competition.CompetitionId.Value);
            }
            return competition;
        }

        public void Dispose()
        {
            //            RemoveIntegrationTestsDatabaseIfExists();
        }
    }
}
