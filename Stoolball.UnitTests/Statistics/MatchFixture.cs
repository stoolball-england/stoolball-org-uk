using System;
using System.Collections.Generic;
using Stoolball.Awards;
using Stoolball.Clubs;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Testing;
using Stoolball.Testing.Fakers;

namespace Stoolball.UnitTests.Statistics
{
    public class MatchFixture
    {
        public Match Match { get; private set; }

        public List<PlayerIdentity> HomePlayers { get; } = new List<PlayerIdentity>();
        public List<PlayerIdentity> AwayPlayers { get; } = new List<PlayerIdentity>();

        private readonly OversHelper _oversHelper = new OversHelper();

        public MatchFixture()
        {
            var bowlingFiguresCalculator = new BowlingFiguresCalculator(_oversHelper);
            var playerIdentityFinder = new PlayerIdentityFinder();
            var matchFinder = new MatchFinder();
            var seedDataGenerator = new SeedDataGenerator(_oversHelper, bowlingFiguresCalculator, playerIdentityFinder, matchFinder,
                new TeamFakerFactory(), new MatchLocationFakerFactory(), new SchoolFakerFactory());

            var homeTeam = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                Team = seedDataGenerator.CreateTeamWithMinimalDetails("Home team"),
                WonToss = true,
                BattedFirst = true,
                TeamRole = TeamRole.Home
            };

            var awayTeam = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                Team = seedDataGenerator.CreateTeamWithMinimalDetails("Away team"),
                WonToss = false,
                BattedFirst = false,
                TeamRole = TeamRole.Away
            };

            homeTeam.Team.Club = new Club { ClubId = Guid.NewGuid(), ClubName = "Home club", ClubRoute = "/home-club" };

            for (var i = 0; i < 11; i++)
            {
                HomePlayers.Add(new PlayerIdentity
                {
                    Player = new Player
                    {
                        PlayerId = Guid.NewGuid(),
                        PlayerRoute = "/players/home-" + (i + 1)
                    },
                    PlayerIdentityId = Guid.NewGuid(),
                    PlayerIdentityName = "Home player identity " + (i + 1),
                    Team = homeTeam.Team
                });
            }

            for (var i = 0; i < 11; i++)
            {
                AwayPlayers.Add(new PlayerIdentity
                {
                    Player = new Player
                    {
                        PlayerId = Guid.NewGuid(),
                        PlayerRoute = "/players/away-" + (i + 1)
                    },
                    PlayerIdentityId = Guid.NewGuid(),
                    PlayerIdentityName = "Away player identity " + (i + 12),
                    Team = awayTeam.Team
                });
            }

            var firstInningsOverSets = seedDataGenerator.CreateOverSets();
            var secondInningsOverSets = seedDataGenerator.CreateOverSets();
            var thirdInningsOverSets = seedDataGenerator.CreateOverSets();
            var fourthInningsOverSets = seedDataGenerator.CreateOverSets();

            var competition = seedDataGenerator.CreateCompetitionWithMinimalDetails();
            var season = seedDataGenerator.CreateSeasonWithMinimalDetails(competition, 2020, 2020);
            competition.Seasons.Add(season);

            Match = new Match
            {
                MatchId = Guid.NewGuid(),
                MatchType = MatchType.LeagueMatch,
                PlayerType = PlayerType.Ladies,
                MatchName = "Team A beat Team B",
                UpdateMatchNameAutomatically = true,
                StartTime = new DateTimeOffset(2020, 7, 1, 19, 00, 00, TimeSpan.FromHours(1)),
                StartTimeIsKnown = true,
                Awards = new List<MatchAward> {
                    new MatchAward
                    {
                        AwardedToId = Guid.NewGuid(),
                        Award = new Award
                        {
                            AwardId = Guid.NewGuid(),
                            AwardName = "Champagne moment"
                        },
                        PlayerIdentity = AwayPlayers[4],
                        Reason = "Amazing catch"
                    },
                    new MatchAward {
                        AwardedToId = Guid.NewGuid(),
                        Award = new Award
                        {
                            AwardId = Guid.NewGuid(),
                            AwardName = "Player of the match"
                        },
                        PlayerIdentity = HomePlayers[2],
                        Reason = "Taking wickets"
                    }
                },
                EnableBonusOrPenaltyRuns = true,
                InningsOrderIsKnown = true,
                LastPlayerBatsOn = true,
                PlayersPerTeam = 11,
                Teams = new List<TeamInMatch> {
                    homeTeam,
                    awayTeam
                },
                Season = season,
                MatchInnings = new List<MatchInnings> {
                    new MatchInnings
                    {
                        MatchInningsId = Guid.NewGuid(),
                        InningsOrderInMatch = 1,
                        BattingMatchTeamId = homeTeam.MatchTeamId,
                        BowlingMatchTeamId = awayTeam.MatchTeamId,
                        BattingTeam = homeTeam,
                        BowlingTeam = awayTeam,
                        NoBalls = 20,
                        Wides = 15,
                        Byes = 10,
                        BonusOrPenaltyRuns = 5,
                        Runs = 200,
                        Wickets = 2,
                        PlayerInnings = CreateBattingScorecard(HomePlayers, AwayPlayers),
                        OverSets = firstInningsOverSets,
                        OversBowled = seedDataGenerator.CreateOversBowled(AwayPlayers, firstInningsOverSets)
                    },
                    new MatchInnings
                    {
                        MatchInningsId = Guid.NewGuid(),
                        InningsOrderInMatch = 2,
                        BattingMatchTeamId = awayTeam.MatchTeamId,
                        BowlingMatchTeamId = homeTeam.MatchTeamId,
                        BattingTeam = awayTeam,
                        BowlingTeam = homeTeam,
                        NoBalls = 23,
                        Wides = 12,
                        Byes = 5,
                        BonusOrPenaltyRuns = 0,
                        Runs = 230,
                        Wickets = 7,
                        PlayerInnings = CreateBattingScorecard(AwayPlayers, HomePlayers),
                        OverSets = secondInningsOverSets,
                        OversBowled = seedDataGenerator.CreateOversBowled(HomePlayers, secondInningsOverSets)
                    },
                    new MatchInnings
                    {
                        MatchInningsId = Guid.NewGuid(),
                        InningsOrderInMatch = 3,
                        BattingMatchTeamId = homeTeam.MatchTeamId,
                        BowlingMatchTeamId = awayTeam.MatchTeamId,
                        BattingTeam = homeTeam,
                        BowlingTeam = awayTeam,
                        NoBalls = 31,
                        Wides = 2,
                        Byes = 18,
                        BonusOrPenaltyRuns = -6,
                        Runs = 150,
                        Wickets = 10,
                        PlayerInnings = CreateBattingScorecard(HomePlayers, AwayPlayers),
                        OverSets = thirdInningsOverSets,
                        OversBowled = seedDataGenerator.CreateOversBowled(AwayPlayers, thirdInningsOverSets)
                    },
                    new MatchInnings
                    {
                        MatchInningsId = Guid.NewGuid(),
                        InningsOrderInMatch = 4,
                        BattingMatchTeamId = awayTeam.MatchTeamId,
                        BowlingMatchTeamId = homeTeam.MatchTeamId,
                        BattingTeam = awayTeam,
                        BowlingTeam = homeTeam,
                        NoBalls = 16,
                        Wides = 12,
                        Byes = 8,
                        BonusOrPenaltyRuns = 2,
                        Runs = 210,
                        Wickets = 4,
                        PlayerInnings = CreateBattingScorecard(AwayPlayers, HomePlayers),
                        OverSets = fourthInningsOverSets,
                        OversBowled = seedDataGenerator.CreateOversBowled(HomePlayers, fourthInningsOverSets)
                    }
                },
                MatchLocation = seedDataGenerator.CreateMatchLocationWithMinimalDetails(),
                MatchResultType = MatchResultType.HomeWin,
                MatchNotes = "<p>This is a test match, not a Test Match.</p>",
                MatchRoute = "/matches/team-a-vs-team-b-1jul2020-" + Guid.NewGuid(),
                MemberKey = Guid.NewGuid(),
            };

            var bowlingFigures = new BowlingFiguresCalculator(new OversHelper());
            foreach (var innings in Match.MatchInnings)
            {
                innings.BowlingFigures = bowlingFigures.CalculateBowlingFigures(innings);
            }

            // The last innings will be missing its overs bowled, to simulate bowling figures entered by the user instead of calculated from overs
            Match.MatchInnings[3].OversBowled.Clear();
        }

        private static List<PlayerInnings> CreateBattingScorecard(List<PlayerIdentity> battingTeam, List<PlayerIdentity> bowlingTeam)
        {
            return new List<PlayerInnings>{
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
                                DismissedBy = bowlingTeam[4], // [4] is not on the batting card, but appears as a fielder
                                Bowler = bowlingTeam[7], // Not on bowling card
                                RunsScored = 20,
                                BallsFaced = 15
                            },
                            new PlayerInnings
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 3,
                                Batter = battingTeam[2],
                                DismissalType = DismissalType.RunOut,
                                DismissedBy = bowlingTeam[8],
                                RunsScored = null,
                                BallsFaced = 150
                            },
                            new PlayerInnings
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 4,
                                Batter = battingTeam[3],
                                DismissalType = DismissalType.RunOut, // But fielder not recorded
                                RunsScored = 42,
                                BallsFaced = null
                            },
                            new PlayerInnings
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 5,
                                Batter = battingTeam[0], // the first batter had two turns; Player [4] is not on the batting card but appears as a fielder
                                DismissalType = DismissalType.Retired,
                                RunsScored = 10
                            },
                            new PlayerInnings
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 6,
                                Batter = battingTeam[5],
                                DismissalType = DismissalType.CaughtAndBowled, // should be credited to bowler, not fielder
                                Bowler = bowlingTeam[3]
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
                        };
        }
    }
}
