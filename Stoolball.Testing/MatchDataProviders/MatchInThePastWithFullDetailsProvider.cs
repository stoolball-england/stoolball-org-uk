using Stoolball.Awards;

namespace Stoolball.Testing.MatchDataProviders
{
    internal class MatchInThePastWithFullDetailsProvider : BaseMatchDataProvider
    {
        private readonly MatchFactory _matchFactory;
        private readonly TeamFactory _teamFactory;
        private readonly CompetitionFactory _competitionFactory;
        private readonly SeasonFactory _seasonFactory;
        private readonly MatchLocationFactory _matchLocationFactory;
        private readonly CommentFactory _commentFactory;
        private readonly OverFactory _overFactory;
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;
        private readonly TournamentFactory _tournamentFactory;
        private readonly Award _playerOfTheMatchAward;

        public MatchInThePastWithFullDetailsProvider(MatchFactory matchFactory, TeamFactory teamFactory, CompetitionFactory competitionFactory,
            SeasonFactory seasonFactory, MatchLocationFactory matchLocationFactory, CommentFactory commentFactory, OverFactory overFactory,
            IBowlingFiguresCalculator bowlingFiguresCalculator, TournamentFactory tournamentFactory, Award playerOfTheMatchAward)
        {
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _teamFactory = teamFactory ?? throw new ArgumentNullException(nameof(teamFactory));
            _competitionFactory = competitionFactory ?? throw new ArgumentNullException(nameof(competitionFactory));
            _seasonFactory = seasonFactory ?? throw new ArgumentNullException(nameof(seasonFactory));
            _matchLocationFactory = matchLocationFactory ?? throw new ArgumentNullException(nameof(matchLocationFactory));
            _commentFactory = commentFactory ?? throw new ArgumentNullException(nameof(commentFactory));
            _overFactory = overFactory ?? throw new ArgumentNullException(nameof(overFactory));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
            _tournamentFactory = tournamentFactory ?? throw new ArgumentNullException(nameof(tournamentFactory));
            _playerOfTheMatchAward = playerOfTheMatchAward ?? throw new ArgumentNullException(nameof(playerOfTheMatchAward));
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            var matches = new List<Match>
            {
                CreateMatchInThePastWithFullDetails()
            };

            var matchInThePastWithFullDetailsAndTournament = CreateMatchInThePastWithFullDetails();
            matchInThePastWithFullDetailsAndTournament.Tournament = _tournamentFactory.CreateFaker().Generate();
            matchInThePastWithFullDetailsAndTournament.Season!.FromYear = matchInThePastWithFullDetailsAndTournament.Season.UntilYear = 2018;
            matches.Add(matchInThePastWithFullDetailsAndTournament);

            return matches;
        }

        private Match CreateMatchInThePastWithFullDetails()
        {
            var teamFaker = _teamFactory.CreateBasicTeamFaker();

            var homeTeam = teamFaker.Generate();
            var homeTeamInMatch = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                Team = homeTeam,
                PlayingAsTeamName = homeTeam.TeamName,
                WonToss = true,
                BattedFirst = true,
                TeamRole = TeamRole.Home
            };

            var awayTeam = teamFaker.Generate();
            var awayTeamInMatch = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                Team = awayTeam,
                PlayingAsTeamName = awayTeam.TeamName,
                WonToss = false,
                BattedFirst = false,
                TeamRole = TeamRole.Away
            };

            var homePlayers = new PlayerIdentity[11];
            for (var i = 0; i < 11; i++)
            {
                homePlayers[i] = new PlayerIdentity
                {
                    Player = new Player
                    {
                        PlayerId = Guid.NewGuid(),
                        PlayerRoute = "/players/home-" + (i + 1)
                    },
                    PlayerIdentityId = Guid.NewGuid(),
                    PlayerIdentityName = "Home player identity " + (i + 1),
                    RouteSegment = "home-player-identity-" + (i + 1),
                    Team = homeTeamInMatch.Team
                };
            }

            var awayPlayers = new PlayerIdentity[11];
            for (var i = 0; i < 11; i++)
            {
                awayPlayers[i] = new PlayerIdentity
                {
                    Player = new Player
                    {
                        PlayerId = Guid.NewGuid(),
                        PlayerRoute = "/players/away-" + (i + 1)
                    },
                    PlayerIdentityId = Guid.NewGuid(),
                    PlayerIdentityName = "Away player identity " + (i + 12),
                    RouteSegment = "away-player-identity-" + (i + 1),
                    Team = awayTeamInMatch.Team
                };
            }

            var competition = _competitionFactory.CreateFaker().Generate();
            var season = _seasonFactory.CreateFaker(competition, 2020, 2020).Generate();
            competition.Seasons.Add(season);

            var match = new Match
            {
                MatchId = Guid.NewGuid(),
                MatchType = MatchType.LeagueMatch,
                PlayerType = PlayerType.Ladies,
                MatchName = "Team A beat Team B",
                UpdateMatchNameAutomatically = true,
                StartTime = new DateTimeOffset(2020, 7, 1, 19, 00, 00, TimeSpan.FromHours(1)),
                StartTimeIsKnown = true,
                Awards = new List<MatchAward> {
                    // Arranged alphabetically by award name to match the data that should be returned
                    new MatchAward
                    {
                        AwardedToId = Guid.NewGuid(),
                        Award = new Award
                        {
                            AwardId = Guid.NewGuid(),
                            AwardName = "Champagne moment"
                        },
                        PlayerIdentity = awayPlayers[4],
                        Reason = "Amazing catch"
                    },
                    new MatchAward {
                        AwardedToId = Guid.NewGuid(),
                        Award = _playerOfTheMatchAward,
                        PlayerIdentity = homePlayers[2],
                        Reason = "Taking wickets"
                    }
                },
                EnableBonusOrPenaltyRuns = true,
                InningsOrderIsKnown = true,
                LastPlayerBatsOn = true,
                PlayersPerTeam = 11,
                Teams = new List<TeamInMatch> {
                    homeTeamInMatch,
                    awayTeamInMatch
                },
                Season = season,
                MatchInnings = new List<MatchInnings> {
                    _matchFactory.CreateMatchInningsWithScores(1),
                    _matchFactory.CreateMatchInningsWithScores(2),
                    _matchFactory.CreateMatchInningsWithScores(3),
                    _matchFactory.CreateMatchInningsWithScores(4)
                },
                MatchLocation = _matchLocationFactory.CreateFaker().Generate(),
                MatchResultType = MatchResultType.HomeWin,
                MatchNotes = "<p>This is a test match, not a Test Match.</p>",
                MatchRoute = "/matches/team-a-vs-team-b-1jul2020-" + Guid.NewGuid(),
                MemberKey = Guid.NewGuid(),
                Comments = _commentFactory.CreateFaker().Generate(10)
            };

            var firstInnings = match.MatchInnings[0];
            firstInnings.BattingMatchTeamId = homeTeamInMatch.MatchTeamId;
            firstInnings.BowlingMatchTeamId = awayTeamInMatch.MatchTeamId;
            firstInnings.BattingTeam = homeTeamInMatch;
            firstInnings.BowlingTeam = awayTeamInMatch;
            firstInnings.PlayerInnings = CreateBattingScorecard(homePlayers, awayPlayers);
            firstInnings.OversBowled = _overFactory.CreateOversBowledIncludingOneWithOnlyName([.. awayPlayers], firstInnings.OverSets);
            firstInnings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(firstInnings);

            var secondInnings = match.MatchInnings[1];
            secondInnings.BattingMatchTeamId = awayTeamInMatch.MatchTeamId;
            secondInnings.BowlingMatchTeamId = homeTeamInMatch.MatchTeamId;
            secondInnings.BattingTeam = awayTeamInMatch;
            secondInnings.BowlingTeam = homeTeamInMatch;
            secondInnings.PlayerInnings = CreateBattingScorecard(awayPlayers, homePlayers);
            secondInnings.OversBowled = _overFactory.CreateOversBowledIncludingOneWithOnlyName([.. homePlayers], secondInnings.OverSets);
            secondInnings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(secondInnings);

            var thirdInnings = match.MatchInnings[2];
            thirdInnings.BattingMatchTeamId = homeTeamInMatch.MatchTeamId;
            thirdInnings.BowlingMatchTeamId = awayTeamInMatch.MatchTeamId;
            thirdInnings.BattingTeam = homeTeamInMatch;
            thirdInnings.BowlingTeam = awayTeamInMatch;
            thirdInnings.PlayerInnings = CreateBattingScorecard(homePlayers, awayPlayers);
            thirdInnings.OversBowled = _overFactory.CreateOversBowledIncludingOneWithOnlyName([.. awayPlayers], thirdInnings.OverSets);
            thirdInnings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(thirdInnings);

            var fourthInnings = match.MatchInnings[3];
            fourthInnings.BattingMatchTeamId = awayTeamInMatch.MatchTeamId;
            fourthInnings.BowlingMatchTeamId = homeTeamInMatch.MatchTeamId;
            fourthInnings.BattingTeam = awayTeamInMatch;
            fourthInnings.BowlingTeam = homeTeamInMatch;
            fourthInnings.PlayerInnings = CreateBattingScorecard(awayPlayers, homePlayers);
            fourthInnings.OversBowled = _overFactory.CreateOversBowledIncludingOneWithOnlyName([.. homePlayers], fourthInnings.OverSets);
            fourthInnings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(fourthInnings);

            return match;
        }

        private static List<PlayerInnings> CreateBattingScorecard(PlayerIdentity[] battingTeam, PlayerIdentity[] bowlingTeam)
        {
            return [
                    new PlayerInnings {
                        PlayerInningsId = Guid.NewGuid(),
                        BattingPosition = 1,
                        Batter = battingTeam[0],
                        DismissalType = DismissalType.Bowled,
                        Bowler = bowlingTeam[3],
                        RunsScored = 50,
                        BallsFaced = 60
                    },
                    new PlayerInnings
                    {
                        PlayerInningsId = Guid.NewGuid(),
                        BattingPosition = 2,
                        Batter = battingTeam[1],
                        DismissalType = DismissalType.Caught,
                        DismissedBy = bowlingTeam[9],
                        Bowler = bowlingTeam[7],
                        RunsScored = 20,
                        BallsFaced = 15
                    },
                    new PlayerInnings
                    {
                        PlayerInningsId = Guid.NewGuid(),
                        BattingPosition = 3,
                        Batter = battingTeam[2],
                        DismissalType = DismissalType.NotOut,
                        RunsScored = 120,
                        BallsFaced = 150
                    },
                    new PlayerInnings
                    {
                        PlayerInningsId = Guid.NewGuid(),
                        BattingPosition = 4,
                        Batter = battingTeam[3],
                        DismissalType = DismissalType.NotOut,
                        RunsScored = 42,
                        BallsFaced = 35
                    },
                    new PlayerInnings
                    {
                        PlayerInningsId = Guid.NewGuid(),
                        BattingPosition = 5,
                        Batter = battingTeam[4],
                        DismissalType = DismissalType.DidNotBat
                    },
                    new PlayerInnings
                    {
                        PlayerInningsId = Guid.NewGuid(),
                        BattingPosition = 6,
                        Batter = battingTeam[5],
                        DismissalType = DismissalType.DidNotBat
                    },
                    new PlayerInnings
                    {
                        PlayerInningsId = Guid.NewGuid(),
                        BattingPosition = 7,
                        Batter = battingTeam[6],
                        DismissalType = DismissalType.DidNotBat
                    },
                    new PlayerInnings
                    {
                        PlayerInningsId = Guid.NewGuid(),
                        BattingPosition = 8,
                        Batter = battingTeam[7],
                        DismissalType = DismissalType.DidNotBat
                    },
                    new PlayerInnings
                    {
                        PlayerInningsId = Guid.NewGuid(),
                        BattingPosition = 9,
                        Batter = battingTeam[8],
                        DismissalType = DismissalType.DidNotBat
                    },
                    new PlayerInnings
                    {
                        PlayerInningsId = Guid.NewGuid(),
                        BattingPosition = 10,
                        Batter = battingTeam[9],
                        DismissalType = DismissalType.DidNotBat
                    },
                    new PlayerInnings
                    {
                        PlayerInningsId = Guid.NewGuid(),
                        BattingPosition = 11,
                        Batter = battingTeam[10],
                        DismissalType = DismissalType.DidNotBat
                    }
                ];
        }
    }
}
