using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Management.Smo;
using Stoolball.Awards;
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
        public Club ClubWithMinimalDetails { get; private set; }
        public Club ClubWithTeams { get; private set; }
        public Team TeamWithMinimalDetails { get; private set; }
        public List<Competition> Competitions { get; internal set; } = new List<Competition>();
        public List<Match> Matches { get; internal set; } = new List<Match>();
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

                ClubWithMinimalDetails = seedDataGenerator.CreateClubWithMinimalDetails();
                CreateClubInDatabase(ClubWithMinimalDetails, connection);

                ClubWithTeams = seedDataGenerator.CreateClubWithTeams();
                CreateClubInDatabase(ClubWithTeams, connection);

                TeamWithMinimalDetails = seedDataGenerator.CreateTeamWithMinimalDetails("Team minimal");
                CreateTeamInDatabase(TeamWithMinimalDetails, connection);

                MatchInThePastWithMinimalDetails = seedDataGenerator.CreateMatchInThePastWithMinimalDetails();
                CreateMatchInDatabase(MatchInThePastWithMinimalDetails, connection);

                MatchInTheFutureWithMinimalDetails = seedDataGenerator.CreateMatchInThePastWithMinimalDetails();
                MatchInTheFutureWithMinimalDetails.StartTime = DateTime.UtcNow.AddMonths(1);
                CreateMatchInDatabase(MatchInTheFutureWithMinimalDetails, connection);

                MatchInThePastWithFullDetails = seedDataGenerator.CreateMatchInThePastWithFullDetails();
                CreateMatchInDatabase(MatchInThePastWithFullDetails, connection);

                TournamentInThePastWithMinimalDetails = seedDataGenerator.CreateTournamentInThePastWithMinimalDetails();
                CreateTournamentInDatabase(TournamentInThePastWithMinimalDetails, connection);

                MatchInThePastWithFullDetailsAndTournament = seedDataGenerator.CreateMatchInThePastWithFullDetails();
                MatchInThePastWithFullDetailsAndTournament.Tournament = TournamentInThePastWithMinimalDetails;
                MatchInThePastWithFullDetailsAndTournament.Season.FromYear = MatchInThePastWithFullDetailsAndTournament.Season.UntilYear = 2018;
                CreateMatchInDatabase(MatchInThePastWithFullDetailsAndTournament, connection);

                CompetitionWithMinimalDetails = seedDataGenerator.CreateCompetitionWithMinimalDetails();
                CreateCompetitionInDatabase(CompetitionWithMinimalDetails, connection);

                CompetitionWithFullDetails = seedDataGenerator.CreateCompetitionWithFullDetails();
                CreateCompetitionInDatabase(CompetitionWithFullDetails, connection);

                MatchLocationWithMinimalDetails = MatchInThePastWithFullDetails.MatchLocation;

                Competitions.AddRange(new[] { CompetitionWithMinimalDetails, CompetitionWithFullDetails, MatchInThePastWithFullDetails.Season.Competition, MatchInThePastWithFullDetailsAndTournament.Season.Competition });
                for (var i = 0; i < 30; i++)
                {
                    var competition = seedDataGenerator.CreateCompetitionWithMinimalDetails();
                    CreateCompetitionInDatabase(competition, connection);
                    Competitions.Add(competition);
                }

                Teams.AddRange(new[] { TeamWithMinimalDetails });
                Matches.AddRange(new[] { MatchInThePastWithMinimalDetails, MatchInThePastWithFullDetails });
            }
        }

        private void CreateTournamentInDatabase(Tournament tournament, IDbConnection connection)
        {
            connection.Execute($@"INSERT INTO {Tables.Tournament} 
                    (TournamentId, TournamentName, MatchLocationId, StartTime, StartTimeIsKnown, PlayerType, PlayersPerTeam, QualificationType, MaximumTeamsInTournament, SpacesInTournament, TournamentNotes, TournamentRoute, MemberKey)
                    VALUES
                    (@TournamentId, @TournamentName, @MatchLocationId, @StartTime, @StartTimeIsKnown, @PlayerType, @PlayersPerTeam, @QualificationType, @MaximumTeamsInTournament, @SpacesInTournament, @TournamentNotes, @TournamentRoute, @MemberKey)",
                   new
                   {
                       tournament.TournamentId,
                       tournament.TournamentName,
                       MatchLocationId = tournament.TournamentLocation?.MatchLocationId,
                       tournament.StartTime,
                       tournament.StartTimeIsKnown,
                       PlayerType = tournament.PlayerType.ToString(),
                       tournament.PlayersPerTeam,
                       tournament.QualificationType,
                       tournament.MaximumTeamsInTournament,
                       tournament.SpacesInTournament,
                       tournament.TournamentNotes,
                       tournament.TournamentRoute,
                       tournament.MemberKey
                   });
        }

        private static void CreateClubInDatabase(Club club, IDbConnection connection)
        {
            connection.Execute($@"INSERT INTO {Tables.Club} 
                    (ClubId, MemberGroupKey, MemberGroupName, ClubRoute)
                    VALUES
                    (@ClubId, @MemberGroupKey, @MemberGroupName, @ClubRoute)",
                   new
                   {
                       club.ClubId,
                       club.MemberGroupKey,
                       club.MemberGroupName,
                       club.ClubRoute
                   });

            connection.Execute($@"INSERT INTO {Tables.ClubVersion}
                    (ClubVersionId, ClubId, ClubName, ComparableName, FromDate, UntilDate)
                    VALUES
                    (@ClubVersionId, @ClubId, @ClubName, @ComparableName, @FromDate, @UntilDate)",
                    new
                    {
                        ClubVersionId = Guid.NewGuid(),
                        club.ClubId,
                        club.ClubName,
                        ComparableName = club.ComparableName(),
                        FromDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        UntilDate = club.UntilDate
                    });

            foreach (var team in club.Teams)
            {
                CreateTeamInDatabase(team, connection);
            }
        }

        private static void CreateMatchInDatabase(Match match, IDbConnection connection)
        {
            if (match.MatchLocation != null)
            {
                CreateMatchLocationInDatabase(match.MatchLocation, connection);
            }

            if (match.Season != null)
            {
                CreateCompetitionInDatabase(match.Season.Competition, connection);
            }

            connection.Execute($@"INSERT INTO {Tables.Match} 
                    (MatchId, MatchName, UpdateMatchNameAutomatically, MatchType, PlayerType, StartTime, StartTimeIsKnown, MatchRoute, MatchLocationId, PlayersPerTeam, 
                     EnableBonusOrPenaltyRuns, LastPlayerBatsOn, InningsOrderIsKnown, MatchResultType, MatchNotes, SeasonId, TournamentId, OrderInTournament, MemberKey)
                    VALUES
                    (@MatchId, @MatchName, @UpdateMatchNameAutomatically, @MatchType, @PlayerType, @StartTime, @StartTimeIsKnown, @MatchRoute, @MatchLocationId, @PlayersPerTeam, 
                     @EnableBonusOrPenaltyRuns, @LastPlayerBatsOn, @InningsOrderIsKnown, @MatchResultType, @MatchNotes, @SeasonId, @TournamentId, @OrderInTournament, @MemberKey)", new
            {

                match.MatchId,
                match.MatchName,
                match.UpdateMatchNameAutomatically,
                MatchType = match.MatchType.ToString(),
                PlayerType = match.PlayerType.ToString(),
                match.StartTime,
                match.StartTimeIsKnown,
                match.MatchRoute,
                match.MatchLocation?.MatchLocationId,
                match.PlayersPerTeam,
                match.EnableBonusOrPenaltyRuns,
                match.LastPlayerBatsOn,
                match.InningsOrderIsKnown,
                MatchResultType = match.MatchResultType?.ToString(),
                match.MatchNotes,
                match.Season?.SeasonId,
                match.Tournament?.TournamentId,
                match.OrderInTournament,
                match.MemberKey
            });

            foreach (var teamInMatch in match.Teams)
            {
                CreateTeamInDatabase(teamInMatch.Team, connection);

                connection.Execute($@"INSERT INTO {Tables.MatchTeam} 
                    (MatchTeamId, TeamId, PlayingAsTeamName, MatchId, TeamRole, WonToss)
                    VALUES
                    (@MatchTeamId, @TeamId, @PlayingAsTeamName, @MatchId, @TeamRole, @WonToss)",
                   new
                   {
                       teamInMatch.MatchTeamId,
                       teamInMatch.Team.TeamId,
                       teamInMatch.PlayingAsTeamName,
                       match.MatchId,
                       TeamRole = teamInMatch.TeamRole.ToString(),
                       teamInMatch.WonToss
                   });
            }

            foreach (var innings in match.MatchInnings)
            {
                connection.Execute($@"INSERT INTO {Tables.MatchInnings} 
                    (MatchInningsId, MatchId, InningsOrderInMatch, BattingMatchTeamId, BowlingMatchTeamId, Byes, Wides, NoBalls, BonusOrPenaltyRuns, Runs, Wickets)
                    VALUES
                    (@MatchInningsId, @MatchId, @InningsOrderInMatch, @BattingMatchTeamId, @BowlingMatchTeamId, @Byes, @Wides, @NoBalls, @BonusOrPenaltyRuns, @Runs, @Wickets)",
                    new
                    {
                        innings.MatchInningsId,
                        match.MatchId,
                        innings.InningsOrderInMatch,
                        innings.BattingMatchTeamId,
                        innings.BowlingMatchTeamId,
                        innings.Byes,
                        innings.Wides,
                        innings.NoBalls,
                        innings.BonusOrPenaltyRuns,
                        innings.Runs,
                        innings.Wickets
                    });
            }

            var playerIdentities = new List<PlayerIdentity>();
            playerIdentities.AddRange(match.Awards.Select(x => x.PlayerIdentity));
            foreach (var innings in match.MatchInnings)
            {
                playerIdentities.AddRange(innings.PlayerInnings.Select(x => x.PlayerIdentity));
                playerIdentities.AddRange(innings.PlayerInnings.Select(x => x.DismissedBy).OfType<PlayerIdentity>());
                playerIdentities.AddRange(innings.PlayerInnings.Select(x => x.Bowler).OfType<PlayerIdentity>());
                playerIdentities.AddRange(innings.OversBowled.Select(x => x.PlayerIdentity));
                playerIdentities.AddRange(innings.BowlingFigures.Select(x => x.Bowler));
            }
            foreach (var playerIdentity in playerIdentities.Distinct(new PlayerIdentityEqualityComparer()))
            {
                CreatePlayerIdentityInDatabase(playerIdentity, connection);
            }

            foreach (var award in match.Awards)
            {
                connection.Execute($@"INSERT INTO {Tables.Award} 
                    (AwardId, AwardName, AwardScope)
                    VALUES
                    (@AwardId, @AwardName, @AwardScope)",
                  new
                  {
                      award.Award.AwardId,
                      award.Award.AwardName,
                      AwardScope = AwardScope.Match
                  });

                connection.Execute($@"INSERT INTO {Tables.AwardedTo} 
                    (AwardedToId, AwardId, PlayerIdentityId, MatchId, Reason)
                    VALUES
                    (@AwardedToId, @AwardId, @PlayerIdentityId, @MatchId, @Reason)",
                  new
                  {
                      award.AwardedToId,
                      award.Award.AwardId,
                      award.PlayerIdentity.PlayerIdentityId,
                      match.MatchId,
                      award.Reason
                  });
            }

            foreach (var innings in match.MatchInnings)
            {
                foreach (var playerInnings in innings.PlayerInnings)
                {
                    connection.Execute($@"INSERT INTO {Tables.PlayerInnings} 
                    (PlayerInningsId, MatchInningsId, PlayerIdentityId, BattingPosition, DismissalType, DismissedById, BowlerId, RunsScored, BallsFaced)
                    VALUES
                    (@PlayerInningsId, @MatchInningsId, @PlayerIdentityId, @BattingPosition, @DismissalType, @DismissedById, @BowlerId, @RunsScored, @BallsFaced)",
                  new
                  {
                      playerInnings.PlayerInningsId,
                      innings.MatchInningsId,
                      playerInnings.PlayerIdentity.PlayerIdentityId,
                      playerInnings.BattingPosition,
                      DismissalType = playerInnings.DismissalType.ToString(),
                      DismissedById = playerInnings.DismissedBy?.PlayerIdentityId,
                      BowlerId = playerInnings.Bowler?.PlayerIdentityId,
                      playerInnings.RunsScored,
                      playerInnings.BallsFaced
                  });
                }

                foreach (var overSet in innings.OverSets)
                {
                    CreateOverSetInDatabase(overSet, innings.MatchInningsId, null, connection);
                }

                foreach (var overBowled in innings.OversBowled)
                {
                    connection.Execute($@"INSERT INTO {Tables.Over} 
                    (OverId, MatchInningsId, OverSetId, PlayerIdentityId, OverNumber, BallsBowled, NoBalls, Wides, RunsConceded)
                    VALUES
                    (@OverId, @MatchInningsId, @OverSetId, @PlayerIdentityId, @OverNumber, @BallsBowled, @NoBalls, @Wides, @RunsConceded)",
                  new
                  {
                      overBowled.OverId,
                      innings.MatchInningsId,
                      overBowled.OverSet.OverSetId,
                      overBowled.PlayerIdentity.PlayerIdentityId,
                      overBowled.OverNumber,
                      overBowled.BallsBowled,
                      overBowled.NoBalls,
                      overBowled.Wides,
                      overBowled.RunsConceded
                  });
                }

                var i = 1;
                foreach (var bowlingFigures in innings.BowlingFigures)
                {
                    connection.Execute($@"INSERT INTO {Tables.BowlingFigures} 
                    (BowlingFiguresId, MatchInningsId, BowlingOrder, PlayerIdentityId, Overs, Maidens, RunsConceded, Wickets, IsFromOversBowled)
                    VALUES
                    (@BowlingFiguresId, @MatchInningsId, @BowlingOrder, @PlayerIdentityId, @Overs, @Maidens, @RunsConceded, @Wickets, @IsFromOversBowled)",
                  new
                  {
                      bowlingFigures.BowlingFiguresId,
                      innings.MatchInningsId,
                      BowlingOrder = 1,
                      bowlingFigures.Bowler.PlayerIdentityId,
                      bowlingFigures.Overs,
                      bowlingFigures.Maidens,
                      bowlingFigures.RunsConceded,
                      bowlingFigures.Wickets,
                      IsFromOversBowled = true
                  });
                    i++;
                }
            }
        }

        private static void CreateOverSetInDatabase(OverSet overSet, Guid? matchInningsId, Guid? seasonId, IDbConnection connection)
        {
            connection.Execute($@"INSERT INTO {Tables.OverSet} 
                    (OverSetId, MatchInningsId, SeasonId, OverSetNumber, Overs, BallsPerOver)
                    VALUES
                    (@OverSetId, @MatchInningsId, @SeasonId, @OverSetNumber, @Overs, @BallsPerOver)",
            new
            {
                overSet.OverSetId,
                MatchInningsId = matchInningsId,
                SeasonId = seasonId,
                overSet.OverSetNumber,
                overSet.Overs,
                overSet.BallsPerOver
            });
        }

        private static void CreatePlayerIdentityInDatabase(PlayerIdentity playerIdentity, IDbConnection connection)
        {
            var playerId = Guid.NewGuid();
            connection.Execute($@"INSERT INTO {Tables.Player} 
                    (PlayerId, PlayerName, PlayerRoute)
                    VALUES
                    (@PlayerId, @PlayerName, @PlayerRoute)",
                   new
                   {
                       PlayerId = playerId,
                       PlayerName = playerIdentity.PlayerIdentityName,
                       PlayerRoute = "/players/" + playerId
                   });

            connection.Execute($@"INSERT INTO {Tables.PlayerIdentity} 
                    (PlayerIdentityId, PlayerId, TeamId, PlayerIdentityName, ComparableName, FirstPlayed, LastPlayed, TotalMatches, MissedMatches, Probability)
                    VALUES
                    (@PlayerIdentityId, @PlayerId, @TeamId, @PlayerIdentityName, @ComparableName, @FirstPlayed, @LastPlayed, @TotalMatches, @MissedMatches, @Probability)",
                   new
                   {
                       playerIdentity.PlayerIdentityId,
                       PlayerId = playerId,
                       playerIdentity.Team.TeamId,
                       playerIdentity.PlayerIdentityName,
                       ComparableName = playerIdentity.ComparableName(),
                       playerIdentity.FirstPlayed,
                       playerIdentity.LastPlayed,
                       playerIdentity.TotalMatches,
                       playerIdentity.MissedMatches,
                       playerIdentity.Probability
                   });
        }

        private static void CreateMatchLocationInDatabase(MatchLocation matchLocation, IDbConnection connection)
        {
            connection.Execute($@"INSERT INTO {Tables.MatchLocation} 
                    (MatchLocationId, ComparableName, SecondaryAddressableObjectName, PrimaryAddressableObjectName, StreetDescription, Locality, Town, AdministrativeArea, Postcode,
                    Latitude, Longitude, GeoPrecision, MatchLocationNotes, MemberGroupKey, MemberGroupName, MatchLocationRoute)
                    VALUES
                    (@MatchLocationId, @ComparableName, @SecondaryAddressableObjectName, @PrimaryAddressableObjectName, @StreetDescription, @Locality, @Town, @AdministrativeArea, @Postcode,
                    @Latitude, @Longitude, @GeoPrecision, @MatchLocationNotes, @MemberGroupKey, @MemberGroupName, @MatchLocationRoute)",
                    new
                    {
                        matchLocation.MatchLocationId,
                        ComparableName = matchLocation.ComparableName(),
                        matchLocation.SecondaryAddressableObjectName,
                        matchLocation.PrimaryAddressableObjectName,
                        matchLocation.StreetDescription,
                        matchLocation.Locality,
                        matchLocation.Town,
                        matchLocation.AdministrativeArea,
                        matchLocation.Postcode,
                        matchLocation.Latitude,
                        matchLocation.Longitude,
                        matchLocation.GeoPrecision,
                        matchLocation.MatchLocationNotes,
                        matchLocation.MemberGroupKey,
                        matchLocation.MemberGroupName,
                        matchLocation.MatchLocationRoute
                    }
                    );
        }

        private static void CreateTeamInDatabase(Team team, IDbConnection connection)
        {
            connection.Execute($@"INSERT INTO {Tables.Team} 
                    (TeamId, ClubId, ClubMark, SchoolId, TeamType, PlayerType, Introduction, AgeRangeLower, AgeRangeUpper, Website, Twitter, Facebook, Instagram, YouTube, 
                     PublicContactDetails, PrivateContactDetails, PlayingTimes, Cost, MemberGroupKey, MemberGroupName, TeamRoute)
                    VALUES
                    (@TeamId, @ClubId, @ClubMark, @SchoolId, @TeamType, @PlayerType, @Introduction, @AgeRangeLower, @AgeRangeUpper, @Website, @Twitter, @Facebook, @Instagram, @YouTube, 
                     @PublicContactDetails, @PrivateContactDetails, @PlayingTimes, @Cost, @MemberGroupKey, @MemberGroupName, @TeamRoute)",
                  new
                  {
                      team.TeamId,
                      team.Club?.ClubId,
                      team.ClubMark,
                      team.School?.SchoolId,
                      TeamType = team.TeamType.ToString(),
                      PlayerType = team.PlayerType.ToString(),
                      team.Introduction,
                      team.AgeRangeLower,
                      team.AgeRangeUpper,
                      team.Website,
                      team.Twitter,
                      team.Facebook,
                      team.Instagram,
                      team.YouTube,
                      team.PublicContactDetails,
                      team.PrivateContactDetails,
                      team.PlayingTimes,
                      team.Cost,
                      team.MemberGroupKey,
                      team.MemberGroupName,
                      team.TeamRoute
                  });

            connection.Execute($@"INSERT INTO {Tables.TeamVersion}
                    (TeamVersionId, TeamId, TeamName, ComparableName, FromDate, UntilDate)
                    VALUES
                    (@TeamVersionId, @TeamId, @TeamName, @ComparableName, @FromDate, @UntilDate)",
                    new
                    {
                        TeamVersionId = Guid.NewGuid(),
                        team.TeamId,
                        team.TeamName,
                        ComparableName = team.ComparableName(),
                        FromDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        UntilDate = team.UntilYear.HasValue ? new DateTimeOffset(team.UntilYear.Value, 12, 31, 23, 59, 59, TimeSpan.Zero) : (DateTimeOffset?)null
                    });
        }

        private static void CreateCompetitionInDatabase(Competition competition, IDbConnection connection)
        {
            connection.Execute($@"INSERT INTO {Tables.Competition} 
                    (CompetitionId, Introduction, PublicContactDetails, PrivateContactDetails, Website, Twitter,
                    Facebook, Instagram, YouTube, PlayerType, MemberGroupKey, MemberGroupName, CompetitionRoute)
                    VALUES
                    (@CompetitionId, @Introduction, @PublicContactDetails, @PrivateContactDetails, @Website, @Twitter,
                    @Facebook, @Instagram, @YouTube, @PlayerType, @MemberGroupKey, @MemberGroupName, @CompetitionRoute)",
                    new
                    {
                        competition.CompetitionId,
                        competition.Introduction,
                        competition.PublicContactDetails,
                        competition.PrivateContactDetails,
                        competition.Website,
                        competition.Twitter,
                        competition.Facebook,
                        competition.Instagram,
                        competition.YouTube,
                        PlayerType = competition.PlayerType.ToString(),
                        competition.MemberGroupKey,
                        competition.MemberGroupName,
                        competition.CompetitionRoute
                    }
                    );

            connection.Execute($@"INSERT INTO {Tables.CompetitionVersion}
                    (CompetitionVersionId, CompetitionId, CompetitionName, ComparableName, FromDate, UntilDate)
                    VALUES
                    (@CompetitionVersionId, @CompetitionId, @CompetitionName, @ComparableName, @FromDate, @UntilDate)",
                  new
                  {
                      CompetitionVersionId = Guid.NewGuid(),
                      competition.CompetitionId,
                      competition.CompetitionName,
                      ComparableName = competition.ComparableName(),
                      FromDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                      UntilDate = competition.UntilYear.HasValue ? new DateTimeOffset(competition.UntilYear.Value, 12, 31, 23, 59, 59, TimeSpan.Zero) : (DateTimeOffset?)null
                  });

            foreach (var season in competition.Seasons)
            {
                CreateSeasonInDatabase(season, competition.CompetitionId.Value, connection);
            }
        }

        private static void CreateSeasonInDatabase(Season season, Guid competitionId, IDbConnection connection)
        {
            connection.Execute($@"INSERT INTO {Tables.Season} 
                    (SeasonId, CompetitionId, FromYear, UntilYear, Introduction, Results, PlayersPerTeam, EnableTournaments, EnableBonusOrPenaltyRuns,
                    ResultsTableType, EnableRunsScored, EnableRunsConceded, EnableLastPlayerBatsOn, SeasonRoute)
                    VALUES
                    (@SeasonId, @CompetitionId, @FromYear, @UntilYear, @Introduction, @Results, @PlayersPerTeam, @EnableTournaments, @EnableBonusOrPenaltyRuns,
                    @ResultsTableType, @EnableRunsScored, @EnableRunsConceded, @EnableLastPlayerBatsOn, @SeasonRoute)",
                    new
                    {
                        season.SeasonId,
                        CompetitionId = competitionId,
                        season.FromYear,
                        season.UntilYear,
                        season.Introduction,
                        season.Results,
                        season.PlayersPerTeam,
                        season.EnableTournaments,
                        season.EnableBonusOrPenaltyRuns,
                        ResultsTableType = season.ResultsTableType.ToString(),
                        season.EnableRunsScored,
                        season.EnableRunsConceded,
                        season.EnableLastPlayerBatsOn,
                        season.SeasonRoute
                    }
                    );

            foreach (var matchType in season.MatchTypes)
            {
                connection.Execute($@"INSERT INTO {Tables.SeasonMatchType}
                    (SeasonMatchTypeId, SeasonId, MatchType)
                    VALUES
                    (@SeasonMatchTypeId, @SeasonId, @MatchType)",
                  new
                  {
                      SeasonMatchTypeId = Guid.NewGuid(),
                      season.SeasonId,
                      MatchType = matchType.ToString()
                  });
            }

            foreach (var overSet in season.DefaultOverSets)
            {
                CreateOverSetInDatabase(overSet, null, season.SeasonId, connection);
            }
        }
        public void Dispose()
        {
            RemoveIntegrationTestsDatabaseIfExists();
        }
    }
}
