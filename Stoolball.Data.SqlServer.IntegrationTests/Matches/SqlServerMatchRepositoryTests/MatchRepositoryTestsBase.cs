using System;
using System.Linq;
using System.Transactions;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.SqlServerMatchRepositoryTests
{
    public class MatchRepositoryTestsBase : IDisposable
    {
        protected SqlServerTestDataFixture DatabaseFixture { get; init; }
        protected TransactionScope Scope { get; init; }
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
        protected SqlServerMatchRepository CreateRepository(IStatisticsRepository? statisticsRepository = null)
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
                            statisticsRepository ?? new SqlServerStatisticsRepository(PlayerRepository),
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
                Teams = matchToCopy.Teams,
                PlayersPerTeam = matchToCopy.PlayersPerTeam,
                MatchResultType = matchToCopy.MatchResultType
            };

            foreach (var inningsToCopy in matchToCopy.MatchInnings)
            {
                match.MatchInnings.Add(new MatchInnings
                {
                    MatchInningsId = inningsToCopy.MatchInningsId,
                    InningsOrderInMatch = inningsToCopy.InningsOrderInMatch,
                    BattingMatchTeamId = inningsToCopy.BattingMatchTeamId,
                    BowlingMatchTeamId = inningsToCopy.BowlingMatchTeamId,
                    BattingTeam = new TeamInMatch
                    {
                        Team = new Team
                        {
                            TeamId = inningsToCopy.BattingTeam!.Team!.TeamId
                        }
                    },
                    BowlingTeam = new TeamInMatch
                    {
                        Team = new Team
                        {
                            TeamId = inningsToCopy.BowlingTeam!.Team!.TeamId
                        }
                    },
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

        public void Dispose() => Scope.Dispose();
    }
}
