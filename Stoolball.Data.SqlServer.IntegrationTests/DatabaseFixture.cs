using System;
using System.Collections.Generic;
using System.IO;
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
        private const string _SQL_SERVER_INSTANCE = @"(LocalDB)\MSSQLLocalDB";
        private readonly string _umbracoDatabasePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Stoolball.Web\App_Data\Umbraco.mdf"));
        private readonly string _dacpacPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _INTEGRATION_TESTS_DATABASE + ".dacpac");

        public IDatabaseConnectionFactory ConnectionFactory { get; private set; }

        public Match MatchInThePastWithMinimalDetails { get; private set; }
        public Match MatchInTheFutureWithMinimalDetails { get; private set; }

        public Match MatchInThePastWithFullDetails { get; private set; }
        public Match MatchInThePastWithFullDetailsAndTournament { get; private set; }
        public Tournament TournamentInThePastWithMinimalDetails { get; private set; }
        public Competition CompetitionWithMinimalDetails { get; private set; }
        public Competition CompetitionWithFullDetails { get; private set; }
        public MatchLocation MatchLocationWithMinimalDetails { get; private set; }
        public MatchLocation MatchLocationWithFullDetails { get; private set; }
        public Club ClubWithMinimalDetails { get; private set; }
        public Club ClubWithTeams { get; private set; }
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

                TournamentInThePastWithMinimalDetails = seedDataGenerator.CreateTournamentInThePastWithMinimalDetails();
                repo.CreateTournament(TournamentInThePastWithMinimalDetails);

                MatchInThePastWithFullDetailsAndTournament = seedDataGenerator.CreateMatchInThePastWithFullDetails();
                MatchInThePastWithFullDetailsAndTournament.Tournament = TournamentInThePastWithMinimalDetails;
                MatchInThePastWithFullDetailsAndTournament.Season.FromYear = MatchInThePastWithFullDetailsAndTournament.Season.UntilYear = 2018;
                repo.CreateMatch(MatchInThePastWithFullDetailsAndTournament);

                CompetitionWithMinimalDetails = seedDataGenerator.CreateCompetitionWithMinimalDetails();
                repo.CreateCompetition(CompetitionWithMinimalDetails);

                CompetitionWithFullDetails = seedDataGenerator.CreateCompetitionWithFullDetails();
                repo.CreateCompetition(CompetitionWithFullDetails);
                foreach (var season in CompetitionWithFullDetails.Seasons)
                {
                    repo.CreateSeason(season, CompetitionWithFullDetails.CompetitionId.Value);
                }

                MatchLocationWithMinimalDetails = MatchInThePastWithFullDetails.MatchLocation;
                MatchLocationWithFullDetails = seedDataGenerator.CreateMatchLocationWithFullDetails();
                repo.CreateMatchLocation(MatchLocationWithFullDetails);

                Competitions.AddRange(new[] { CompetitionWithMinimalDetails, CompetitionWithFullDetails, MatchInThePastWithFullDetails.Season.Competition, MatchInThePastWithFullDetailsAndTournament.Season.Competition });
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

        public void Dispose()
        {
            RemoveIntegrationTestsDatabaseIfExists();
        }
    }
}
