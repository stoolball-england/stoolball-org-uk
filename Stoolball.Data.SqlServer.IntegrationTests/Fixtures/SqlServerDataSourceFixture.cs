using System;
using System.Collections.Generic;
using Stoolball.Awards;
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
        public List<Match> Matches { get; internal set; } = new List<Match>();

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

                MatchInThePastWithMinimalDetails = matchFactory.CreateMatchInThePastWithMinimalDetails();
                repo.CreateMatch(MatchInThePastWithMinimalDetails);
                Matches.Add(MatchInThePastWithMinimalDetails);

                MatchInTheFutureWithMinimalDetails = matchFactory.CreateMatchInThePastWithMinimalDetails();
                MatchInTheFutureWithMinimalDetails.StartTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow.AccurateToTheMinute().AddMonths(1), UkTimeZone());
                repo.CreateMatch(MatchInTheFutureWithMinimalDetails);
                Matches.Add(MatchInTheFutureWithMinimalDetails);

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
                Matches.Add(MatchInThePastWithFullDetails);
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
                Matches.Add(MatchInThePastWithFullDetailsAndTournament);

                for (var i = 0; i < 30; i++)
                {
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

                    var tournament = seedDataGenerator.CreateTournamentInThePastWithMinimalDetails();
                    tournament.TournamentLocation = matchLocation;
                    tournament.StartTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow.AccurateToTheMinute().AddMonths(i - 20).AddDays(5), UkTimeZone());
                    tournament.Comments = seedDataGenerator.CreateComments(i, members);
                    repo.CreateTournament(tournament);
                }
            }
        }


    }
}
