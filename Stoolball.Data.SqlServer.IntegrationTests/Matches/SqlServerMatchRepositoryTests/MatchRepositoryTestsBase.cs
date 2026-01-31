using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.SqlServerMatchRepositoryTests
{
    public class MatchRepositoryTestsBase : IDisposable
    {
        protected SqlServerTestDataFixture DatabaseFixture { get; init; }
        protected TransactionScope Scope { get; init; }
        protected SqlServerMatchRepository Repository => CreateRepository(StatisticsRepository.Object);
        protected Mock<IAuditRepository> AuditRepository { get; init; } = new();
        protected Mock<ILogger<SqlServerMatchRepository>> Logger { get; init; } = new();
        protected Mock<IRouteGenerator> RouteGenerator { get; init; } = new();
        protected Mock<IRedirectsRepository> RedirectsRepository { get; init; } = new();
        protected Mock<IMatchNameBuilder> MatchNameBuilder { get; init; } = new();
        protected Mock<IPlayerTypeSelector> PlayerTypeSelector { get; init; } = new();
        protected Mock<IDataRedactor> DataRedactor { get; init; } = new();
        protected DapperWrapper DapperWrapper { get; init; } = new();
        protected StoolballEntityCopier Copier { get; init; }
        protected SqlServerPlayerRepository PlayerRepository { get; init; }
        protected Mock<IStatisticsRepository> StatisticsRepository { get; init; } = new();
        protected Mock<IMatchInningsFactory> MatchInningsFactory { get; init; } = new();
        protected Mock<ISeasonDataSource> SeasonDataSource { get; init; } = new();
        protected Guid MemberKey { get; init; }
        protected string MemberName { get; init; }

        public MatchRepositoryTestsBase(SqlServerTestDataFixture databaseFixture)
        {
            DatabaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            Scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            MemberKey = DatabaseFixture.TestData.Members[0].memberKey;
            MemberName = DatabaseFixture.TestData.Members[0].memberName;

            Copier = new StoolballEntityCopier(DataRedactor.Object);

            RouteGenerator.Setup(x => x.GenerateRoute("/matches", It.IsAny<string>(), NoiseWords.MatchRoute)).Returns($"/matches-{Guid.NewGuid()}");

            PlayerRepository = new SqlServerPlayerRepository(
                DatabaseFixture.ConnectionFactory,
                DapperWrapper,
                AuditRepository.Object,
                Mock.Of<ILogger<SqlServerPlayerRepository>>(),
                RedirectsRepository.Object,
                RouteGenerator.Object,
                Copier,
                new PlayerNameFormatter(),
                new BestRouteSelector(new RouteTokeniser()),
                Mock.Of<IPlayerCacheInvalidator>()
                );

        }
        protected SqlServerMatchRepository CreateRepository(IStatisticsRepository statisticsRepository)
        {
            var oversHelper = new OversHelper();

            return new SqlServerMatchRepository(
                            DatabaseFixture.ConnectionFactory,
                            DapperWrapper,
                            AuditRepository.Object,
                            Logger.Object,
                            RouteGenerator.Object,
                            RedirectsRepository.Object,
                            new Html.HtmlSanitizer(),
                            MatchNameBuilder.Object,
                            PlayerTypeSelector.Object,
                            new BowlingScorecardComparer(),
                            new BattingScorecardComparer(),
                            PlayerRepository,
                            DataRedactor.Object,
                            statisticsRepository,
                            oversHelper,
                            new PlayerInMatchStatisticsBuilder(new PlayerIdentityFinder(), oversHelper),
                            MatchInningsFactory.Object,
                            SeasonDataSource.Object,
                            Copier);
        }

        protected Stoolball.Matches.Match CloneValidMatch(Stoolball.Matches.Match matchToCopy)
        {
            var match = new Stoolball.Matches.Match
            {
                MatchId = matchToCopy.MatchId,
                MatchName = matchToCopy.MatchName,
                MatchLocation = matchToCopy.MatchLocation is not null ? new MatchLocation
                {
                    MatchLocationId = matchToCopy.MatchLocation!.MatchLocationId
                } : null,
                StartTime = matchToCopy.StartTime,
                PlayersPerTeam = matchToCopy.PlayersPerTeam,
                MatchType = matchToCopy.MatchType,
                MatchResultType = matchToCopy.MatchResultType,
                MatchRoute = matchToCopy.MatchRoute,
                UpdateMatchNameAutomatically = matchToCopy.UpdateMatchNameAutomatically,
            };

            foreach (var teamInMatch in matchToCopy.Teams)
            {
                match.Teams.Add(new TeamInMatch
                {
                    MatchTeamId = teamInMatch.MatchTeamId,
                    TeamRole = teamInMatch.TeamRole,
                    Team = new Team
                    {
                        TeamId = teamInMatch.Team!.TeamId,
                        TeamRoute = teamInMatch.Team.TeamRoute,
                        TeamName = teamInMatch.Team.TeamName
                    }
                });
            }

            if (matchToCopy.Season is not null)
            {
                match.Season = new Season
                {
                    SeasonId = matchToCopy.Season!.SeasonId,
                    SeasonRoute = matchToCopy.Season.SeasonRoute,
                    Competition = new Competition
                    {
                        CompetitionId = matchToCopy.Season.Competition!.CompetitionId,
                        CompetitionName = matchToCopy.Season.Competition.CompetitionName,
                        CompetitionRoute = matchToCopy.Season.Competition.CompetitionRoute
                    }
                };
            }

            foreach (var inningsToCopy in matchToCopy.MatchInnings)
            {
                match.MatchInnings.Add(new MatchInnings
                {
                    MatchInningsId = inningsToCopy.MatchInningsId,
                    InningsOrderInMatch = inningsToCopy.InningsOrderInMatch,
                    BattingMatchTeamId = inningsToCopy.BattingMatchTeamId,
                    BowlingMatchTeamId = inningsToCopy.BowlingMatchTeamId,
                    BattingTeam = inningsToCopy.BattingTeam is not null ? new TeamInMatch
                    {
                        Team = new Team
                        {
                            TeamId = inningsToCopy.BattingTeam!.Team!.TeamId
                        }
                    } : null,
                    BowlingTeam = inningsToCopy.BowlingTeam is not null ? new TeamInMatch
                    {
                        Team = new Team
                        {
                            TeamId = inningsToCopy.BowlingTeam!.Team!.TeamId
                        }
                    } : null,
                    OverSets = inningsToCopy.OverSets,
                    Byes = inningsToCopy.Byes,
                    Wides = inningsToCopy.Wides,
                    NoBalls = inningsToCopy.NoBalls,
                    BonusOrPenaltyRuns = inningsToCopy.BonusOrPenaltyRuns,
                    Runs = inningsToCopy.Runs,
                    Wickets = inningsToCopy.Wickets
                });

                foreach (var playerInnings in inningsToCopy.PlayerInnings)
                {
                    match.MatchInnings[^1].PlayerInnings.Add(new PlayerInnings
                    {
                        PlayerInningsId = playerInnings.PlayerInningsId,
                        BattingPosition = playerInnings.BattingPosition,
                        Batter = playerInnings.Batter,
                        DismissalType = playerInnings.DismissalType,
                        DismissedBy = playerInnings.DismissedBy,
                        Bowler = playerInnings.Bowler,
                        RunsScored = playerInnings.RunsScored,
                        BallsFaced = playerInnings.BallsFaced,
                    });
                }
                foreach (var over in inningsToCopy.OversBowled)
                {
                    match.MatchInnings[^1].OversBowled.Add(new Over
                    {
                        OverId = over.OverId,
                        OverSet = over.OverSet,
                        OverNumber = over.OverNumber,
                        Bowler = over.Bowler,
                        BallsBowled = over.BallsBowled,
                        Wides = over.Wides,
                        NoBalls = over.NoBalls,
                        RunsConceded = over.RunsConceded
                    });
                }
                foreach (var figures in inningsToCopy.BowlingFigures)
                {
                    match.MatchInnings[^1].BowlingFigures.Add(new BowlingFigures
                    {
                        BowlingFiguresId = figures.BowlingFiguresId,
                        MatchInnings = match.MatchInnings.SingleOrDefault(mi => mi.MatchInningsId == figures.MatchInnings?.MatchInningsId),
                        Bowler = figures.Bowler,
                        Overs = figures.Overs,
                        Maidens = figures.Maidens,
                        RunsConceded = figures.RunsConceded,
                        Wickets = figures.Wickets
                    });
                }
            }

            foreach (var award in matchToCopy.Awards)
            {
                match.Awards.Add(new MatchAward
                {
                    AwardedToId = award.AwardedToId,
                    Award = award.Award,
                    PlayerIdentity = award.PlayerIdentity,
                    Reason = award.Reason
                });
            }

            return match;
        }

        protected async Task TestThatRouteGetsNumericSuffixIfRouteHasChangedAndNewRouteInUse(Func<Stoolball.Matches.Match, Task<Stoolball.Matches.Match>> methodToTest, Func<Stoolball.Matches.Match, bool> additionalMatchFilter)
        {
            var routeInUse = DatabaseFixture.TestData.Matches.First(m => m.MatchRoute is not null && Regex.IsMatch(m.MatchRoute, "[a-z]$")).MatchRoute!;
            var expectedRoute = $"/matches/expected-route-{Guid.NewGuid()}-1";
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(
                            m => m.MatchRoute is not null
                            && Regex.IsMatch(m.MatchRoute, "[a-z]$")
                            && m.MatchRoute != routeInUse
                            && m.Teams.Any(t => t.TeamRole == TeamRole.Home)
                            && m.Teams.Any(t => t.TeamRole == TeamRole.Away)
                            && m.UpdateMatchNameAutomatically == false
                            && additionalMatchFilter(m)));

            RouteGenerator.Setup(x => x.GenerateRoute("/matches", $"{match.MatchName} {match.StartTime.ToString("dMMMyyyy", CultureInfo.CurrentCulture)}", NoiseWords.MatchRoute)).Returns(routeInUse);
            RouteGenerator.Setup(x => x.IsMatchingRoute(match.MatchRoute!, routeInUse)).Returns(false);
            RouteGenerator.Setup(x => x.IncrementRoute(routeInUse)).Returns(expectedRoute);

            var updated = await methodToTest(match);

            Assert.Equal(expectedRoute, updated.MatchRoute);

            await AssertMatchRouteSaved(updated.MatchId, updated.MatchRoute).ConfigureAwait(false);
        }

        protected async Task TestThatRouteWhenUnchangedIsNotIncremented(Func<Stoolball.Matches.Match, Task<Stoolball.Matches.Match>> methodToTest, Func<Stoolball.Matches.Match, bool> additionalMatchFilter)
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m => m.MatchRoute is not null &&
                                                                                    m.Teams.Any(t => t.TeamRole == TeamRole.Home) &&
                                                                                    m.Teams.Any(t => t.TeamRole == TeamRole.Away) &&
                                                                                    m.UpdateMatchNameAutomatically == false &&
                                                                                    additionalMatchFilter(m)));

            RouteGenerator.Setup(x => x.GenerateRoute("/matches", $"{match.MatchName} {match.StartTime.ToString("dMMMyyyy", CultureInfo.CurrentCulture)}", NoiseWords.MatchRoute)).Returns(match.MatchRoute!);
            RouteGenerator.Setup(x => x.IsMatchingRoute(match.MatchRoute!, match.MatchRoute!)).Returns(true);
            RouteGenerator.Verify(x => x.IncrementRoute(It.IsAny<string>()), Times.Never);

            var updated = await methodToTest(match);

            Assert.Equal(match.MatchRoute, updated.MatchRoute);

            await AssertMatchRouteSaved(updated.MatchId, updated.MatchRoute).ConfigureAwait(false);
        }

        protected async Task AssertMatchRouteSaved(Guid? matchId, string? expectedRoute)
        {
            Assert.NotNull(matchId);
            Assert.NotNull(expectedRoute);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = await connection.QuerySingleOrDefaultAsync<string>(
                    $"SELECT MatchRoute FROM {Tables.Match} WHERE MatchId = @MatchId", new { MatchId = matchId }).ConfigureAwait(false);

                Assert.Equal(expectedRoute, saved);
            }
        }

        public void Dispose() => Scope.Dispose();

    }
}
