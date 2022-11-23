using System;
using System.Collections.Generic;
using Stoolball.Awards;
using Stoolball.Competitions;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Testing;
using Stoolball.Testing.Fakers;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Fixtures
{
    public sealed class SqlServerDataSourceFixture : BaseSqlServerFixture
    {
        public Match MatchInThePastWithMinimalDetails { get; private set; }
        public Match MatchInTheFutureWithMinimalDetails { get; private set; }

        public Match MatchInThePastWithFullDetails { get; private set; }
        public Match MatchInThePastWithFullDetailsAndTournament { get; private set; }
        public Tournament TournamentInThePastWithFullDetails { get; private set; }
        public Competition CompetitionWithMinimalDetails { get; private set; }
        public Competition CompetitionWithFullDetails { get; private set; }
        public Season SeasonWithMinimalDetails { get; private set; }
        public List<Competition> Competitions { get; internal set; } = new List<Competition>();
        public List<Season> Seasons { get; internal set; } = new List<Season>();
        public List<Match> Matches { get; internal set; } = new List<Match>();
        public List<MatchListing> MatchListings { get; internal set; } = new List<MatchListing>();
        public Season SeasonWithFullDetails { get; private set; }
        public List<MatchListing> TournamentMatchListings { get; private set; } = new List<MatchListing>();

        public SqlServerDataSourceFixture() : base("StoolballDataSourceIntegrationTests")
        {
            // Populate seed data so that there's a consistent baseline for each test run
            // Create dates accurate to the minute, otherwise integration tests can fail due to fractions of a second which are never seen in real data
            var randomiser = new Randomiser(new Random());
            var oversHelper = new OversHelper();
            var playerIdentityFinder = new PlayerIdentityFinder();
            var matchFinder = new MatchFinder();
            var playerInMatchStatisticsBuilder = new PlayerInMatchStatisticsBuilder(playerIdentityFinder, oversHelper);
            var playerOfTheMatchAward = new Award { AwardId = Guid.NewGuid(), AwardName = "Player of the match" };
            var seedDataGenerator = new SeedDataGenerator(randomiser, oversHelper, new BowlingFiguresCalculator(oversHelper), playerIdentityFinder, matchFinder,
                new TeamFakerFactory(), new MatchLocationFakerFactory(), new SchoolFakerFactory(), playerOfTheMatchAward);
            var matchFactory = new MatchFactory(randomiser, playerOfTheMatchAward);
            using (var connection = ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();

                var repo = new SqlServerIntegrationTestsRepository(connection, playerInMatchStatisticsBuilder);

                repo.CreateUmbracoBaseRecords();
                var members = seedDataGenerator.CreateMembers();
                foreach (var member in members)
                {
                    repo.CreateMember(member);
                }

                var teamWithFullDetails = seedDataGenerator.CreateTeamWithFullDetails("Team with full details");
                repo.CreateTeam(teamWithFullDetails);
                foreach (var matchLocation in teamWithFullDetails.MatchLocations)
                {
                    repo.CreateMatchLocation(matchLocation);
                    foreach (var team in matchLocation.Teams)
                    {
                        if (team.TeamId != teamWithFullDetails.TeamId)
                        {
                            repo.CreateTeam(team);
                        }
                        repo.AddTeamToMatchLocation(team, matchLocation);
                    }
                }
                repo.CreateCompetition(teamWithFullDetails.Seasons[0].Season.Competition);
                Competitions.Add(teamWithFullDetails.Seasons[0].Season.Competition);
                foreach (var season in teamWithFullDetails.Seasons)
                {
                    repo.CreateSeason(season.Season, season.Season.Competition.CompetitionId!.Value);
                    repo.AddTeamToSeason(season);
                    Seasons.Add(season.Season);
                }

                MatchInThePastWithMinimalDetails = matchFactory.CreateMatchInThePastWithMinimalDetails();
                repo.CreateMatch(MatchInThePastWithMinimalDetails);
                Matches.Add(MatchInThePastWithMinimalDetails);
                MatchListings.Add(MatchInThePastWithMinimalDetails.ToMatchListing());

                MatchInTheFutureWithMinimalDetails = matchFactory.CreateMatchInThePastWithMinimalDetails();
                MatchInTheFutureWithMinimalDetails.StartTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow.AccurateToTheMinute().AddMonths(1), UkTimeZone());
                repo.CreateMatch(MatchInTheFutureWithMinimalDetails);
                Matches.Add(MatchInTheFutureWithMinimalDetails);
                MatchListings.Add(MatchInTheFutureWithMinimalDetails.ToMatchListing());

                MatchInThePastWithFullDetails = seedDataGenerator.CreateMatchInThePastWithFullDetails(members);
                repo.CreateMatchLocation(MatchInThePastWithFullDetails.MatchLocation);
                var playersForMatchInThePastWithFullDetails = playerIdentityFinder.PlayerIdentitiesInMatch(MatchInThePastWithFullDetails);
                MatchInThePastWithFullDetails.Teams[0].Team.UntilYear = 2020;
                foreach (var team in MatchInThePastWithFullDetails.Teams)
                {
                    repo.CreateTeam(team.Team);
                }
                foreach (var player in playersForMatchInThePastWithFullDetails)
                {
                    repo.CreatePlayer(player.Player);
                    repo.CreatePlayerIdentity(player);
                }
                repo.CreateCompetition(MatchInThePastWithFullDetails.Season.Competition);
                repo.CreateSeason(MatchInThePastWithFullDetails.Season, MatchInThePastWithFullDetails.Season.Competition.CompetitionId!.Value);
                repo.CreateMatch(MatchInThePastWithFullDetails);
                Competitions.Add(MatchInThePastWithFullDetails.Season.Competition);
                Seasons.Add(MatchInThePastWithFullDetails.Season);
                Matches.Add(MatchInThePastWithFullDetails);
                MatchListings.Add(MatchInThePastWithFullDetails.ToMatchListing());
                MatchInThePastWithFullDetails.History.AddRange(new[] { new AuditRecord {
                    Action = AuditAction.Create,
                    ActorName = nameof(SqlServerDataSourceFixture),
                    AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute().AddMonths(-1),
                    EntityUri = MatchInThePastWithFullDetails.EntityUri
                }, new AuditRecord {
                    Action = AuditAction.Update,
                    ActorName = nameof(SqlServerDataSourceFixture),
                    AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute(),
                    EntityUri = MatchInThePastWithFullDetails.EntityUri
                } });
                foreach (var audit in MatchInThePastWithFullDetails.History)
                {
                    repo.CreateAudit(audit);
                }
                MatchInThePastWithFullDetails.Teams[0].Team.UntilYear = 2020;

                var tournamentInThePastWithMinimalDetails = seedDataGenerator.CreateTournamentInThePastWithMinimalDetails();
                repo.CreateTournament(tournamentInThePastWithMinimalDetails);
                MatchListings.Add(tournamentInThePastWithMinimalDetails.ToMatchListing());

                var tournamentInTheFutureWithMinimalDetails = seedDataGenerator.CreateTournamentInThePastWithMinimalDetails();
                tournamentInTheFutureWithMinimalDetails.StartTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow.AccurateToTheMinute().AddMonths(1), UkTimeZone());
                repo.CreateTournament(tournamentInTheFutureWithMinimalDetails);
                MatchListings.Add(tournamentInTheFutureWithMinimalDetails.ToMatchListing());

                TournamentInThePastWithFullDetails = seedDataGenerator.CreateTournamentInThePastWithFullDetailsExceptMatches(members);
                foreach (var team in TournamentInThePastWithFullDetails.Teams)
                {
                    repo.CreateTeam(team.Team);
                }
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
                    repo.CreateSeason(season, season.Competition.CompetitionId!.Value);
                    repo.AddTournamentToSeason(TournamentInThePastWithFullDetails, season);
                    Seasons.Add(season);
                }
                MatchListings.Add(TournamentInThePastWithFullDetails.ToMatchListing());
                for (var i = 0; i < 5; i++)
                {
                    var tournamentMatch = matchFactory.CreateMatchInThePastWithMinimalDetails();
                    tournamentMatch.Tournament = TournamentInThePastWithFullDetails;
                    tournamentMatch.StartTime = TournamentInThePastWithFullDetails.StartTime.AddHours(i);
                    tournamentMatch.OrderInTournament = i + 1;
                    repo.CreateMatch(tournamentMatch);
                    Matches.Add(tournamentMatch);
                    TournamentMatchListings.Add(tournamentMatch.ToMatchListing());
                }
                TournamentInThePastWithFullDetails.History.AddRange(new[] { new AuditRecord {
                    Action = AuditAction.Create,
                    ActorName = nameof(SqlServerDataSourceFixture),
                    AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute().AddMonths(-2),
                    EntityUri = TournamentInThePastWithFullDetails.EntityUri
                }, new AuditRecord {
                    Action = AuditAction.Update,
                    ActorName = nameof(SqlServerDataSourceFixture),
                    AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute().AddDays(-7),
                    EntityUri = TournamentInThePastWithFullDetails.EntityUri
                } });
                foreach (var audit in TournamentInThePastWithFullDetails.History)
                {
                    repo.CreateAudit(audit);
                }

                MatchInThePastWithFullDetailsAndTournament = seedDataGenerator.CreateMatchInThePastWithFullDetails(members);
                MatchInThePastWithFullDetailsAndTournament.Tournament = tournamentInThePastWithMinimalDetails;
                MatchInThePastWithFullDetailsAndTournament.Season.FromYear = MatchInThePastWithFullDetailsAndTournament.Season.UntilYear = 2018;
                repo.CreateMatchLocation(MatchInThePastWithFullDetailsAndTournament.MatchLocation);
                foreach (var team in MatchInThePastWithFullDetailsAndTournament.Teams)
                {
                    repo.CreateTeam(team.Team);
                }
                var playersForMatchInThePastWithFullDetailsAndTournament = playerIdentityFinder.PlayerIdentitiesInMatch(MatchInThePastWithFullDetailsAndTournament);
                foreach (var player in playersForMatchInThePastWithFullDetailsAndTournament)
                {
                    repo.CreatePlayer(player.Player);
                    repo.CreatePlayerIdentity(player);
                }
                repo.CreateCompetition(MatchInThePastWithFullDetailsAndTournament.Season.Competition);
                repo.CreateSeason(MatchInThePastWithFullDetailsAndTournament.Season, MatchInThePastWithFullDetailsAndTournament.Season.Competition.CompetitionId!.Value);
                repo.CreateMatch(MatchInThePastWithFullDetailsAndTournament);
                Competitions.Add(MatchInThePastWithFullDetailsAndTournament.Season.Competition);
                Matches.Add(MatchInThePastWithFullDetailsAndTournament);
                TournamentMatchListings.Add(MatchInThePastWithFullDetailsAndTournament.ToMatchListing());
                Seasons.Add(MatchInThePastWithFullDetailsAndTournament.Season);

                CompetitionWithMinimalDetails = seedDataGenerator.CreateCompetitionWithMinimalDetails();
                repo.CreateCompetition(CompetitionWithMinimalDetails);
                Competitions.Add(CompetitionWithMinimalDetails);

                CompetitionWithFullDetails = seedDataGenerator.CreateCompetitionWithFullDetails();
                repo.CreateCompetition(CompetitionWithFullDetails);
                foreach (var season in CompetitionWithFullDetails.Seasons)
                {
                    repo.CreateSeason(season, CompetitionWithFullDetails.CompetitionId!.Value);
                    Seasons.Add(season);
                }
                Competitions.Add(CompetitionWithFullDetails);

                var competitionForSeason = seedDataGenerator.CreateCompetitionWithMinimalDetails();
                competitionForSeason.UntilYear = 2021;
                SeasonWithMinimalDetails = seedDataGenerator.CreateSeasonWithMinimalDetails(competitionForSeason, 2020, 2020);
                competitionForSeason.Seasons.Add(SeasonWithMinimalDetails);
                repo.CreateCompetition(competitionForSeason);
                repo.CreateSeason(SeasonWithMinimalDetails, competitionForSeason.CompetitionId!.Value);
                Competitions.Add(competitionForSeason);
                Seasons.Add(SeasonWithMinimalDetails);

                SeasonWithFullDetails = seedDataGenerator.CreateSeasonWithFullDetails(competitionForSeason, 2021, 2021, seedDataGenerator.CreateTeamWithMinimalDetails("Team 1 in season"), seedDataGenerator.CreateTeamWithMinimalDetails("Team 2 in season"));
                competitionForSeason.Seasons.Add(SeasonWithFullDetails);
                foreach (var team in SeasonWithFullDetails.Teams)
                {
                    repo.CreateTeam(team.Team);
                }
                repo.CreateSeason(SeasonWithFullDetails, competitionForSeason.CompetitionId.Value);
                foreach (var team in SeasonWithFullDetails.Teams)
                {
                    repo.AddTeamToSeason(team);
                }
                Seasons.Add(SeasonWithFullDetails);

                for (var i = 0; i < 30; i++)
                {
                    var competition = seedDataGenerator.CreateCompetitionWithMinimalDetails();
                    repo.CreateCompetition(competition);
                    Competitions.Add(competition);

                    var matchLocation = seedDataGenerator.CreateMatchLocationWithMinimalDetails();
                    repo.CreateMatchLocation(matchLocation);

                    var match = matchFactory.CreateMatchInThePastWithMinimalDetails();
                    match.MatchLocation = matchLocation;
                    match.StartTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow.AccurateToTheMinute().AddMonths(i - 15), UkTimeZone());
                    match.MatchType = i % 2 == 0 ? MatchType.FriendlyMatch : MatchType.LeagueMatch;
                    match.PlayerType = i % 3 == 0 ? PlayerType.Mixed : PlayerType.Ladies;
                    match.Comments = seedDataGenerator.CreateComments(i, members);
                    repo.CreateMatch(match);
                    Matches.Add(match);
                    MatchListings.Add(match.ToMatchListing());

                    var tournament = seedDataGenerator.CreateTournamentInThePastWithMinimalDetails();
                    tournament.TournamentLocation = matchLocation;
                    tournament.StartTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow.AccurateToTheMinute().AddMonths(i - 20).AddDays(5), UkTimeZone());
                    tournament.Comments = seedDataGenerator.CreateComments(i, members);
                    repo.CreateTournament(tournament);
                    MatchListings.Add(tournament.ToMatchListing());
                }
            }
        }


    }
}
