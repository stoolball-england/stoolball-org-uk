using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Humanizer;
using Stoolball.Awards;
using Stoolball.Clubs;
using Stoolball.Comments;
using Stoolball.Competitions;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Schools;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Testing.Fakers;
using Stoolball.Testing.MatchDataProviders;
using Stoolball.Testing.PlayerDataProviders;
using Stoolball.Testing.SchoolDataProviders;

namespace Stoolball.Testing
{
    internal class SeedDataGenerator
    {
        private readonly Randomiser _randomiser;
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;
        private readonly IPlayerIdentityFinder _playerIdentityFinder;
        private readonly IMatchFinder _matchFinder;
        private readonly IFakerFactory<Team> _teamFakerFactory;
        private readonly IFakerFactory<MatchLocation> _matchLocationFakerFactory;
        private readonly IFakerFactory<School> _schoolFakerFactory;
        private readonly IFakerFactory<Player> _playerFakerFactory;
        private readonly IFakerFactory<PlayerIdentity> _playerIdentityFakerFactory;
        private readonly MatchFactory _matchFactory;
        private readonly Faker<Competition> _competitionFaker;
        private readonly Faker<Team> _teamFaker;
        private readonly Faker<Club> _clubFaker;
        private readonly Faker<MatchLocation> _matchLocationFaker;
        private readonly Faker<School> _schoolFaker;
        private readonly Faker<Player> _playerFaker;
        private readonly Faker<PlayerIdentity> _playerIdentityFaker;
        private readonly Award _playerOfTheMatchAward;
        private readonly IOversHelper _oversHelper;

        internal SeedDataGenerator(Randomiser randomiser, IOversHelper oversHelper, IBowlingFiguresCalculator bowlingFiguresCalculator,
            IPlayerIdentityFinder playerIdentityFinder, IMatchFinder matchFinder,
            IFakerFactory<Competition> competitionFakerFactory, IFakerFactory<Team> teamFakerFactory, IFakerFactory<Club> clubFakerFactory,
            IFakerFactory<MatchLocation> matchLocationFakerFactory, IFakerFactory<School> schoolFakerFactory,
            IFakerFactory<Player> playerFakerFactory, IFakerFactory<PlayerIdentity> playerIdentityFakerFactory, Award playerOfTheMatchAward)
        {
            _randomiser = randomiser ?? throw new ArgumentNullException(nameof(randomiser));
            _oversHelper = oversHelper ?? throw new ArgumentNullException(nameof(oversHelper));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
            _playerIdentityFinder = playerIdentityFinder ?? throw new ArgumentNullException(nameof(playerIdentityFinder));
            _matchFinder = matchFinder ?? throw new ArgumentNullException(nameof(matchFinder));
            _teamFakerFactory = teamFakerFactory ?? throw new ArgumentNullException(nameof(teamFakerFactory));
            _matchLocationFakerFactory = matchLocationFakerFactory ?? throw new ArgumentNullException(nameof(matchLocationFakerFactory));
            _schoolFakerFactory = schoolFakerFactory ?? throw new ArgumentNullException(nameof(schoolFakerFactory));
            _playerFakerFactory = playerFakerFactory ?? throw new ArgumentNullException(nameof(playerFakerFactory));
            _playerIdentityFakerFactory = playerIdentityFakerFactory ?? throw new ArgumentNullException(nameof(playerIdentityFakerFactory));
            _competitionFaker = competitionFakerFactory?.Create() ?? throw new ArgumentNullException(nameof(competitionFakerFactory));
            _teamFaker = teamFakerFactory?.Create() ?? throw new ArgumentNullException(nameof(teamFakerFactory));
            _clubFaker = clubFakerFactory?.Create() ?? throw new ArgumentNullException(nameof(clubFakerFactory));
            _matchLocationFaker = matchLocationFakerFactory?.Create() ?? throw new ArgumentNullException(nameof(matchLocationFakerFactory));
            _schoolFaker = schoolFakerFactory?.Create() ?? throw new ArgumentNullException(nameof(schoolFakerFactory));
            _playerFaker = playerFakerFactory?.Create() ?? throw new ArgumentNullException(nameof(playerFakerFactory));
            _playerIdentityFaker = playerIdentityFakerFactory?.Create() ?? throw new ArgumentNullException(nameof(playerIdentityFakerFactory));
            _playerOfTheMatchAward = playerOfTheMatchAward ?? throw new ArgumentNullException(nameof(playerOfTheMatchAward));
            _matchFactory = new MatchFactory(_randomiser, _playerOfTheMatchAward);
        }

        private Club CreateClubWithTeams()
        {
            var club = new Club
            {
                ClubId = Guid.NewGuid(),
                ClubName = "Club with teams",
                ClubRoute = "/clubs/club-with-teams-" + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Club with teams owners",
            };

            var inactiveAlphabeticallyFirst = _teamFaker.Generate();
            inactiveAlphabeticallyFirst.TeamName = "Inactive team";
            inactiveAlphabeticallyFirst.Club = club;
            inactiveAlphabeticallyFirst.TeamType = TeamType.Representative;
            inactiveAlphabeticallyFirst.UntilYear = 2019;

            var activeAlphabeticallySecond = _teamFaker.Generate();
            activeAlphabeticallySecond.TeamName = "Sort me first in club";
            activeAlphabeticallySecond.TeamType = TeamType.Regular;
            activeAlphabeticallySecond.Club = club;

            var activeAlphabeticallyThird = _teamFaker.Generate();
            activeAlphabeticallyThird.TeamName = "Sort me second in club";
            activeAlphabeticallyThird.TeamType = TeamType.Occasional;
            activeAlphabeticallyThird.Club = club;

            // Teams should come back with active sorted before inactive, alphabetically within those groups
            club.Teams.Add(activeAlphabeticallySecond);
            club.Teams.Add(activeAlphabeticallyThird);
            club.Teams.Add(inactiveAlphabeticallyFirst);

            return club;
        }

        private Competition CreateCompetitionWithFullDetails()
        {
            var competition = new Competition
            {
                CompetitionId = Guid.NewGuid(),
                CompetitionName = "Example league",
                PlayerType = PlayerType.JuniorMixed,
                Introduction = "Introduction to the competition",
                UntilYear = 2020,
                PublicContactDetails = "Public contact details",
                PrivateContactDetails = "Private contact details",
                Facebook = "https://facebook.com/example-league",
                Twitter = "@exampleleague",
                Instagram = "@examplephotos",
                YouTube = "https://youtube.com/exampleleague",
                Website = "https://example.org",
                CompetitionRoute = "/competitions/example-league-" + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Example league owners",
            };
            competition.Seasons = new List<Season> {
                    CreateSeasonWithMinimalDetails(competition,2021,2021),
                    CreateSeasonWithMinimalDetails(competition,2020,2021),
                    CreateSeasonWithMinimalDetails(competition,2020,2020)
                };
            return competition;
        }

        internal Season CreateSeasonWithMinimalDetails(Competition competition, int fromYear, int untilYear)
        {
            return new Season
            {
                SeasonId = Guid.NewGuid(),
                Competition = competition,
                FromYear = fromYear,
                UntilYear = untilYear,
                SeasonRoute = competition?.CompetitionRoute + "/" + fromYear + "-" + untilYear,
                DefaultOverSets = CreateOverSets(),
                MatchTypes = new List<MatchType> { MatchType.LeagueMatch, MatchType.FriendlyMatch }
            };
        }

        private Season CreateSeasonWithFullDetails(Competition competition, int fromYear, int untilYear, Team team1, Team team2)
        {
            var season = new Season
            {
                SeasonId = Guid.NewGuid(),
                Competition = competition,
                FromYear = fromYear,
                UntilYear = untilYear,
                SeasonRoute = competition?.CompetitionRoute + "/" + fromYear + "-" + untilYear,
                DefaultOverSets = CreateOverSets(),
                MatchTypes = new List<MatchType> { MatchType.LeagueMatch, MatchType.FriendlyMatch },
                EnableBonusOrPenaltyRuns = true,
                EnableLastPlayerBatsOn = true,
                EnableRunsConceded = true,
                EnableRunsScored = true,
                EnableTournaments = true,
                Introduction = "Introduction to the season",
                PlayersPerTeam = 12,
                Results = "Some description of results",
                ResultsTableType = ResultsTableType.LeagueTable,
                Teams = new List<TeamInSeason> {
                    new TeamInSeason { Team = team1 },
                    new TeamInSeason { Team = team2, WithdrawnDate = new DateTimeOffset(fromYear, 6, 1, 0, 0, 0, TimeSpan.FromHours(1)) }
                },
                PointsRules = new List<PointsRule> {
                    new PointsRule{ PointsRuleId = Guid.NewGuid(), MatchResultType = MatchResultType.HomeWin, HomePoints=2, AwayPoints = 0 },
                    new PointsRule{ PointsRuleId = Guid.NewGuid(), MatchResultType = MatchResultType.AwayWin, HomePoints=0, AwayPoints = 2 }
                },
                PointsAdjustments = new List<PointsAdjustment>
                {
                    new PointsAdjustment { PointsAdjustmentId = Guid.NewGuid(), Team = team1, Points = 2, Reason = "Testing" }
                }
            };

            foreach (var teamInSeason in season.Teams)
            {
                teamInSeason.Season = season;
                teamInSeason.Team?.Seasons.Add(teamInSeason);
            }

            return season;
        }

        private Team CreateTeamWithFullDetails(string teamName)
        {
            var competition = _competitionFaker.Generate();
            competition.PlayerType = PlayerType.Ladies; // Ensures there is always at least one Ladies competition
            var team = new Team
            {
                TeamId = Guid.NewGuid(),
                TeamName = teamName,
                TeamType = TeamType.Representative,
                TeamRoute = "/teams/" + teamName.Kebaberize() + "-" + Guid.NewGuid(),
                PlayerType = PlayerType.Ladies,
                Introduction = "Introduction to the team",
                AgeRangeLower = 11,
                AgeRangeUpper = 21,
                ClubMark = true,
                Facebook = "https://www.facebook.com/example-team",
                Twitter = "@teamtweets",
                Instagram = "@teamphotos",
                YouTube = "https://youtube.com/exampleteam",
                Website = "https://www.example.org",
                PlayingTimes = "Info on when this team plays",
                Cost = "Membership costs",
                UntilYear = 2019,
                PublicContactDetails = "Public contact details",
                PrivateContactDetails = "Private contact details",
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = teamName + " owners",
                MatchLocations = new List<MatchLocation> {
                    _matchLocationFaker.Generate(),
                    CreateMatchLocationWithFullDetails()
                },
                Seasons = new List<TeamInSeason> {
                    new TeamInSeason
                    {
                        Season = CreateSeasonWithMinimalDetails(competition, 2020, 2020)
                    },
                    new TeamInSeason
                    {
                        Season = CreateSeasonWithMinimalDetails(competition, 2019,2019)
                    }
                }
            };
            foreach (var matchLocation in team.MatchLocations)
            {
                matchLocation.Teams.Add(team);
            }
            foreach (var teamInSeason in team.Seasons)
            {
                teamInSeason.Team = team;
                teamInSeason.Season!.Teams.Add(teamInSeason);
            }
            competition.Seasons.AddRange(team.Seasons.Select(x => x.Season)!);
            return team;
        }

        private MatchLocation CreateMatchLocationWithFullDetails()
        {
            var activeTeam = _teamFaker.Generate();
            activeTeam.TeamName = "Team active";
            var anotherActiveTeam = _teamFaker.Generate();
            anotherActiveTeam.TeamName = "Team that plays";
            var transientTeam = _teamFaker.Generate();
            transientTeam.TeamName = "Transient team";
            transientTeam.TeamType = TeamType.Transient;
            var inactiveTeam = _teamFaker.Generate();
            inactiveTeam.TeamName = "Inactive but alphabetically first";
            inactiveTeam.UntilYear = 2019;

            var matchLocation = _matchLocationFaker.Generate();
            matchLocation.Teams = [inactiveTeam, activeTeam, transientTeam, anotherActiveTeam];

            activeTeam.MatchLocations.Add(matchLocation);
            anotherActiveTeam.MatchLocations.Add(matchLocation);
            transientTeam.MatchLocations.Add(matchLocation);
            inactiveTeam.MatchLocations.Add(matchLocation);

            return matchLocation;
        }


        private Match CreateMatchInThePastWithFullDetails(List<(Guid memberId, string memberName)> members)
        {
            // Note: Team names would sort the away team first alphabetically
            var homeTeam = _teamFaker.Generate();
            var homeTeamInMatch = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                Team = homeTeam,
                PlayingAsTeamName = homeTeam.TeamName,
                WonToss = true,
                BattedFirst = true,
                TeamRole = TeamRole.Home
            };

            var awayTeam = _teamFaker.Generate();
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
            ;

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
            ;

            var firstInningsOverSets = CreateOverSets();
            var secondInningsOverSets = CreateOverSets();
            var thirdInningsOverSets = CreateOverSets();
            var fourthInningsOverSets = CreateOverSets();

            var competition = _competitionFaker.Generate();
            var season = CreateSeasonWithMinimalDetails(competition, 2020, 2020);
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
                    new MatchInnings
                    {
                        MatchInningsId = Guid.NewGuid(),
                        InningsOrderInMatch = 1,
                        BattingMatchTeamId = homeTeamInMatch.MatchTeamId,
                        BowlingMatchTeamId = awayTeamInMatch.MatchTeamId,
                        BattingTeam = homeTeamInMatch,
                        BowlingTeam = awayTeamInMatch,
                        NoBalls = 20,
                        Wides = 15,
                        Byes = 10,
                        BonusOrPenaltyRuns = 5,
                        Runs = 200,
                        Wickets = 2,
                        PlayerInnings = CreateBattingScorecard(homePlayers, awayPlayers),
                        OverSets = firstInningsOverSets,
                        OversBowled = CreateOversBowled(new List<PlayerIdentity>(awayPlayers), firstInningsOverSets)
                    },
                    new MatchInnings
                    {
                        MatchInningsId = Guid.NewGuid(),
                        InningsOrderInMatch = 2,
                        BattingMatchTeamId = awayTeamInMatch.MatchTeamId,
                        BowlingMatchTeamId = homeTeamInMatch.MatchTeamId,
                        BattingTeam = awayTeamInMatch,
                        BowlingTeam = homeTeamInMatch,
                        NoBalls = 23,
                        Wides = 12,
                        Byes = 5,
                        BonusOrPenaltyRuns = 0,
                        Runs = 230,
                        Wickets = 7,
                        PlayerInnings = CreateBattingScorecard(awayPlayers, homePlayers),
                        OverSets = secondInningsOverSets,
                        OversBowled = CreateOversBowled(new List<PlayerIdentity>(homePlayers), secondInningsOverSets)
                    },
                    new MatchInnings
                    {
                        MatchInningsId = Guid.NewGuid(),
                        InningsOrderInMatch = 3,
                        BattingMatchTeamId = homeTeamInMatch.MatchTeamId,
                        BowlingMatchTeamId = awayTeamInMatch.MatchTeamId,
                        BattingTeam = homeTeamInMatch,
                        BowlingTeam = awayTeamInMatch,
                        NoBalls = 31,
                        Wides = 2,
                        Byes = 18,
                        BonusOrPenaltyRuns = -6,
                        Runs = 150,
                        Wickets = 10,
                        PlayerInnings = CreateBattingScorecard(homePlayers, awayPlayers),
                        OverSets = thirdInningsOverSets,
                        OversBowled = CreateOversBowled(new List<PlayerIdentity>(awayPlayers), thirdInningsOverSets)
                    },
                    new MatchInnings
                    {
                        MatchInningsId = Guid.NewGuid(),
                        InningsOrderInMatch = 4,
                        BattingMatchTeamId = awayTeamInMatch.MatchTeamId,
                        BowlingMatchTeamId = homeTeamInMatch.MatchTeamId,
                        BattingTeam = awayTeamInMatch,
                        BowlingTeam = homeTeamInMatch,
                        NoBalls = 16,
                        Wides = 12,
                        Byes = 8,
                        BonusOrPenaltyRuns = 2,
                        Runs = 210,
                        Wickets = 4,
                        PlayerInnings = CreateBattingScorecard(awayPlayers, homePlayers),
                        OverSets = fourthInningsOverSets,
                        OversBowled = CreateOversBowled(new List<PlayerIdentity>(homePlayers), fourthInningsOverSets)
                    }
                },
                MatchLocation = _matchLocationFaker.Generate(),
                MatchResultType = MatchResultType.HomeWin,
                MatchNotes = "<p>This is a test match, not a Test Match.</p>",
                MatchRoute = "/matches/team-a-vs-team-b-1jul2020-" + Guid.NewGuid(),
                MemberKey = Guid.NewGuid(),
                Comments = CreateComments(10, members)
            };

            foreach (var innings in match.MatchInnings)
            {
                innings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(innings);
            }
            return match;
        }

        private List<(Guid memberId, string memberName)> CreateMembers()
        {
            return new List<(Guid memberId, string memberName)> {
                (Guid.NewGuid(), "Jane Smith"),
                (Guid.NewGuid(), "Joe Bloggs"),
                (Guid.NewGuid(), "George Jones"),
                (Guid.NewGuid(), "Jo Bloggs"),
                (Guid.NewGuid(), "John Doe"),
            };
        }

        internal List<OverSet> CreateOverSets()
        {
            return new List<OverSet> { new OverSet { OverSetId = Guid.NewGuid(), OverSetNumber = 1, Overs = 15, BallsPerOver = 8 } };
        }

        private Tournament CreateTournamentInThePastWithMinimalDetails()
        {
            return new Tournament
            {
                TournamentId = Guid.NewGuid(),
                StartTime = new DateTimeOffset(2020, 8, 10, 10, 00, 00, TimeSpan.FromHours(1)),
                TournamentName = "Example tournament",
                TournamentRoute = "/tournaments/example-tournament-" + Guid.NewGuid(),
                MemberKey = Guid.NewGuid()
            };
        }

        private Tournament CreateTournamentInThePastWithFullDetailsExceptMatches(List<(Guid memberKey, string memberName)> members)
        {
            var competitions = _competitionFaker.Generate(2);
            var tournament = new Tournament
            {
                TournamentId = Guid.NewGuid(),
                StartTime = new DateTimeOffset(2020, 8, 10, 10, 00, 00, TimeSpan.FromHours(1)),
                StartTimeIsKnown = true,
                TournamentName = "Example tournament",
                TournamentRoute = "/tournaments/example-tournament-" + Guid.NewGuid(),
                PlayerType = PlayerType.JuniorGirls,
                PlayersPerTeam = 10,
                QualificationType = TournamentQualificationType.ClosedTournament,
                MaximumTeamsInTournament = 10,
                SpacesInTournament = 8,
                TournamentNotes = "Notes about the tournament",
                MemberKey = Guid.NewGuid(),
                Teams = [
                    new TeamInTournament {
                        TournamentTeamId = Guid.NewGuid(),
                        Team = _teamFaker.Generate()
                    },
                    new TeamInTournament {
                        TournamentTeamId = Guid.NewGuid(),
                        Team = _teamFaker.Generate()
                    },
                    new TeamInTournament {
                        TournamentTeamId = Guid.NewGuid(),
                        Team = _teamFaker.Generate()
                    }
                ],
                TournamentLocation = _matchLocationFaker.Generate(),
                DefaultOverSets = CreateOverSets(),
                Seasons =
                [
                    CreateSeasonWithMinimalDetails(competitions[0], 2020, 2020),
                    CreateSeasonWithMinimalDetails(competitions[1], 2020, 2020)
                ],
                Comments = CreateComments(10, members)
            };
            foreach (var season in tournament.Seasons)
            {
                season.Competition!.Seasons.Add(season);
            }
            return tournament;
        }

        private static List<PlayerInnings> CreateBattingScorecard(PlayerIdentity[] battingTeam, PlayerIdentity[] bowlingTeam)
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
                        };
        }

        internal List<Over> CreateOversBowled(List<PlayerIdentity> bowlingTeam, IEnumerable<OverSet> overSets)
        {
            var oversBowled = new List<Over>();
            for (var i = 0; i < 15; i++)
            {
                oversBowled.Add(new Over
                {
                    OverId = Guid.NewGuid(),
                    OverSet = _oversHelper.OverSetForOver(overSets, i + 1),
                    OverNumber = i + 1,
                    Bowler = i % 2 == 0 ? bowlingTeam[5] : bowlingTeam[3],
                    BallsBowled = 8,
                    NoBalls = 1,
                    Wides = 0,
                    RunsConceded = 10
                }); ;
            }

            // One over has a known bowler with missing data
            oversBowled[10].BallsBowled = null;
            oversBowled[10].Wides = null;
            oversBowled[10].NoBalls = null;
            oversBowled[10].RunsConceded = null;

            return oversBowled;
        }

        private List<HtmlComment> CreateComments(int howMany, List<(Guid memberKey, string memberName)> members)
        {
            var randomiser = new Random();
            var comments = new List<HtmlComment>();
            for (var i = howMany; i > 0; i--)
            {
                var member = members[randomiser.Next(members.Count)];

                comments.Add(new HtmlComment
                {
                    CommentId = Guid.NewGuid(),
                    MemberKey = member.memberKey,
                    MemberName = member.memberName,
                    CommentDate = DateTimeOffset.UtcNow.AccurateToTheMinute().AddDays(i * -1),
                    Comment = $"<p>This is comment number <b>{i}</b>.</p>"
                });
            }
            return comments;
        }

        internal List<(Team team, List<PlayerIdentity> identities)> GenerateTeams()
        {
            // Create a pool of teams of 8 players
            var poolOfTeams = new List<(Team team, List<PlayerIdentity> identities)>();
            for (var i = 0; i < 5; i++)
            {
                var team = _randomiser.IsEven(i) ? CreateTeamWithFullDetails($"Team {i + 1}") : _teamFaker.Generate();
                poolOfTeams.Add((team, CreatePlayerIdentitiesForTeam(team, $"{team.TeamName} pool player")));
                if (_randomiser.IsEven(i))
                {
                    poolOfTeams[poolOfTeams.Count - 1].team.Club = _clubFaker.Generate();
                }
            }

            foreach (var club in poolOfTeams.Where(x => x.team.Club != null).Select(x => x.team.Club))
            {
                club!.Teams.AddRange(poolOfTeams.Where(x => x.team.Club?.ClubId == club.ClubId).Select(x => x.team));
            }

            return poolOfTeams;
        }

        internal TestData GenerateTestData()
        {
            var testData = new TestData();
            var playerComparer = new PlayerEqualityComparer();

            var poolOfTeamsWithPlayers = GenerateTeams();

            // Create a pool of competitions
            for (var i = 0; i < 10; i++)
            {
                if (_randomiser.IsEven(i))
                {
                    testData.Competitions.Add(CreateCompetitionWithFullDetails());
                    var team1 = poolOfTeamsWithPlayers[_randomiser.PositiveIntegerLessThan(poolOfTeamsWithPlayers.Count)].team;
                    Team team2;
                    do
                    {
                        team2 = poolOfTeamsWithPlayers[_randomiser.PositiveIntegerLessThan(poolOfTeamsWithPlayers.Count)].team;
                    }
                    while (team2.TeamId == team1.TeamId);

                    var existingSummerSeasonsForCompetition = testData.Competitions[testData.Competitions.Count - 1].Seasons.Where(x => x.FromYear == x.UntilYear).Select(x => x.FromYear);
                    var newSummerSeason = DateTime.Now.Year - i;
                    while (existingSummerSeasonsForCompetition.Contains(newSummerSeason))
                    {
                        newSummerSeason--;
                    }

                    var season = CreateSeasonWithFullDetails(testData.Competitions[testData.Competitions.Count - 1], newSummerSeason, newSummerSeason, team1, team2);
                    testData.Competitions[testData.Competitions.Count - 1].Seasons.Add(season);
                }
                else
                {
                    testData.Competitions.Add(_competitionFaker.Generate());
                    testData.Competitions[testData.Competitions.Count - 1].Seasons.Add(CreateSeasonWithMinimalDetails(testData.Competitions[testData.Competitions.Count - 1], DateTime.Now.Year - i, DateTime.Now.Year - i));
                }
            }

            // Create a pool of match locations 
            for (var i = 0; i < 10; i++)
            {
                testData.MatchLocations.Add(_randomiser.IsEven(i) ? CreateMatchLocationWithFullDetails() : _matchLocationFaker.Generate());
            }
            testData.MatchLocations.AddRange(poolOfTeamsWithPlayers.SelectMany(x => x.team.MatchLocations).OfType<MatchLocation>());

            // Create match and tournament data
            testData.Matches = GenerateMatchData(testData, poolOfTeamsWithPlayers);

            testData.MatchInThePastWithMinimalDetails = FindMatchInThePastWithMinimalDetails(testData);

            testData.MatchInThePastWithFullDetails = FindMatchInThePastWithFullDetails(testData);

            testData.MatchInThePastWithFullDetailsAndTournament = FindMatchInThePastWithFullDetailsAndTournament(testData);

            var membersFromMatchComments = testData.Matches.SelectMany(x => x.Comments).Select(x => (memberKey: x.MemberKey, memberName: x.MemberName ?? "No name"));
            testData.Members = membersFromMatchComments.ToList();

            testData.Tournaments.AddRange(testData.Matches.Where(x => x.Tournament != null && !testData.Tournaments.Select(t => t.TournamentId).Contains(x.Tournament.TournamentId)).Select(x => x.Tournament).OfType<Tournament>());
            for (var i = 0; i < 10; i++)
            {
                var tournament = CreateTournamentInThePastWithFullDetailsExceptMatches(testData.Members);
                if (testData.TournamentInThePastWithFullDetails == null) { testData.TournamentInThePastWithFullDetails = tournament; }
                testData.Tournaments.Add(tournament);

                var tournament2 = CreateTournamentInThePastWithMinimalDetails();
                if (!_randomiser.OneInFourChance())
                {
                    tournament2.TournamentLocation = testData.MatchLocations[_randomiser.PositiveIntegerLessThan(testData.MatchLocations.Count)];
                }
                tournament2.StartTime = DateTimeOffset.UtcNow.AddMonths(i - 20).AddDays(5).UtcToUkTime();
                tournament2.Comments = CreateComments(i, testData.Members);
                testData.Tournaments.Add(tournament2);
            }

            testData.TournamentInThePastWithFullDetails!.History.AddRange(new[] { new AuditRecord {
                    Action = AuditAction.Create,
                    ActorName = nameof(SeedDataGenerator),
                    AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute().AddMonths(-2),
                    EntityUri = testData.TournamentInThePastWithFullDetails.EntityUri
                }, new AuditRecord {
                    Action = AuditAction.Update,
                    ActorName = nameof(SeedDataGenerator),
                    AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute().AddDays(-7),
                    EntityUri = testData.TournamentInThePastWithFullDetails.EntityUri
                } });

            testData.TournamentInThePastWithMinimalDetails = CreateTournamentInThePastWithMinimalDetails();
            testData.Tournaments.Add(testData.TournamentInThePastWithMinimalDetails);

            testData.TournamentInTheFutureWithMinimalDetails = CreateTournamentInThePastWithMinimalDetails();
            testData.TournamentInTheFutureWithMinimalDetails.StartTime = DateTimeOffset.UtcNow.AddMonths(1).UtcToUkTime();
            testData.Tournaments.Add(testData.TournamentInTheFutureWithMinimalDetails);

            testData.ClubWithMinimalDetails = _clubFaker.Generate();

            var clubWithOneActiveTeam = _clubFaker.Generate();
            var onlyTeamInClub = _teamFaker.Generate();
            onlyTeamInClub.TeamName = "Only team in the club";
            clubWithOneActiveTeam.Teams.Add(onlyTeamInClub);
            onlyTeamInClub.Club = clubWithOneActiveTeam;

            var clubWithOneActiveTeamAndOthersInactive = _clubFaker.Generate();
            var activeTeamInClub = _teamFaker.Generate();
            activeTeamInClub.TeamName = "Only active team in the club";
            var inactiveTeamInClub = _teamFaker.Generate();
            inactiveTeamInClub.TeamName = "Inactive team in a club with an active team";
            inactiveTeamInClub.UntilYear = DateTimeOffset.UtcNow.Year - 2;
            clubWithOneActiveTeamAndOthersInactive.Teams.AddRange(new[] { activeTeamInClub, inactiveTeamInClub });
            activeTeamInClub.Club = clubWithOneActiveTeamAndOthersInactive;
            inactiveTeamInClub.Club = clubWithOneActiveTeamAndOthersInactive;

            testData.ClubWithTeamsAndMatchLocation = CreateClubWithTeams();
            testData.MatchLocationForClub = _matchLocationFaker.Generate();
            var teamWithMatchLocation = testData.ClubWithTeamsAndMatchLocation.Teams.First(x => !x.UntilYear.HasValue);
            teamWithMatchLocation.MatchLocations.Add(testData.MatchLocationForClub);
            testData.MatchLocationForClub.Teams.Add(teamWithMatchLocation);

            var teamsInMatches = testData.Matches.SelectMany(x => x.Teams).Select(x => x.Team).OfType<Team>();
            var teamsInTournaments = testData.Tournaments.SelectMany(x => x.Teams).Select(x => x.Team).OfType<Team>();
            var teamsInSeasons = testData.Competitions.SelectMany(x => x.Seasons).SelectMany(x => x.Teams).Select(x => x.Team).OfType<Team>();
            var teamsAtMatchLocations = testData.MatchLocations.SelectMany(x => x.Teams);
            testData.Teams = poolOfTeamsWithPlayers.Select(x => x.team)
                            .Union(teamsInMatches)
                            .Union(teamsInTournaments)
                            .Union(teamsInSeasons)
                            .Union(teamsAtMatchLocations).Distinct(new TeamEqualityComparer()).ToList();
            testData.Teams.Add(onlyTeamInClub);
            testData.Teams.Add(activeTeamInClub);
            testData.Teams.Add(inactiveTeamInClub);
            testData.Teams.AddRange(testData.ClubWithTeamsAndMatchLocation.Teams);

            testData.Clubs.Add(testData.ClubWithMinimalDetails);
            testData.Clubs.AddRange(testData.Teams.Select(x => x.Club).OfType<Club>().Distinct(new ClubEqualityComparer()));

            // Get a minimal team
            testData.TeamWithMinimalDetails = FindTeamWithMinimalDetails(testData, teamsInMatches);

            // Get a detailed team that's played a match, then make sure it's played in a tournament too
            testData.TeamWithFullDetails = testData.Teams.First(x =>
                        x.Club != null &&
                        x.MatchLocations.Any() &&
                        x.Seasons.Any() &&
                        teamsInMatches.Select(t => t.TeamId).Contains(x.TeamId)
            );
            if (testData.TeamWithFullDetails == null) { throw new InvalidOperationException($"{nameof(testData.TeamWithFullDetails)} not found"); }

            testData.TournamentInThePastWithFullDetails.Teams.Add(new TeamInTournament
            {
                TournamentTeamId = Guid.NewGuid(),
                Team = testData.TeamWithFullDetails,
                TeamRole = TournamentTeamRole.Confirmed
            });

            var matchOrderInTournament = 1;
            foreach (var teamInTournament in testData.TournamentInThePastWithFullDetails.Teams)
            {
                // Create a tournament match where the fully-detailed team plays everyone including themselves
                var matchInTournament = _matchFactory.CreateMatchBetween(testData.TeamWithFullDetails, new List<PlayerIdentity>(), teamInTournament.Team, new List<PlayerIdentity>(), true, testData, nameof(GenerateTestData) + "TournamentMatch");
                matchInTournament.Tournament = testData.TournamentInThePastWithFullDetails;
                matchInTournament.OrderInTournament = matchOrderInTournament;
                matchInTournament.StartTime = testData.TournamentInThePastWithFullDetails.StartTime.AddMinutes((matchOrderInTournament - 1) * 45);
                matchInTournament.Season = null;
                matchInTournament.MatchLocation = testData.TournamentInThePastWithFullDetails.TournamentLocation;
                matchInTournament.PlayersPerTeam = testData.TournamentInThePastWithFullDetails.PlayersPerTeam;
                matchOrderInTournament++;
                testData.Matches.Add(matchInTournament);
                testData.TournamentInThePastWithFullDetails.Matches.Add(new MatchInTournament
                {
                    MatchId = matchInTournament.MatchId,
                    MatchName = matchInTournament.MatchName,
                    Teams = new List<TeamInTournament> { testData.TournamentInThePastWithFullDetails.Teams.Single(x => x.Team?.TeamId == testData.TeamWithFullDetails.TeamId), teamInTournament }
                });
            }

            testData.MatchLocations.AddRange(testData.Matches.Select(m => m.MatchLocation)
                .Union(testData.Tournaments.Select(t => t.TournamentLocation))
                .Union(testData.Teams.SelectMany(x => x.MatchLocations))
                .OfType<MatchLocation>()
                .Distinct(new MatchLocationEqualityComparer())
                .Where(x => !testData.MatchLocations.Select(ml => ml.MatchLocationId).Contains(x.MatchLocationId)).ToList());

            testData.MatchLocationWithFullDetails = testData.MatchLocations.First(x => x.Teams.Any());
            testData.MatchLocationWithMinimalDetails = testData.MatchLocations.First(x => !x.Teams.Any());

            testData.Competitions = testData.Matches.Where(m => m.Season != null).Select(m => m.Season?.Competition)
                .Union(testData.Tournaments.Where(t => t.Seasons.Any()).SelectMany(t => t.Seasons.Select(s => s.Competition)))
                .Union(testData.Teams.SelectMany(x => x.Seasons).Select(x => x.Season?.Competition))
                .Union(new[] { _competitionFaker.Generate() })
                .OfType<Competition>()
                .Distinct(new CompetitionEqualityComparer()).ToList();
            testData.CompetitionWithMinimalDetails = testData.Competitions.First(x =>
                !x.Seasons.Any() &&
                string.IsNullOrEmpty(x.Introduction) &&
                !x.UntilYear.HasValue &&
                string.IsNullOrEmpty(x.PublicContactDetails) && string.IsNullOrEmpty(x.PrivateContactDetails) &&
                string.IsNullOrEmpty(x.Facebook) && string.IsNullOrEmpty(x.Twitter) && string.IsNullOrEmpty(x.Instagram) && string.IsNullOrEmpty(x.YouTube) && string.IsNullOrEmpty(x.Website)
                );
            testData.CompetitionWithFullDetails = testData.Competitions.First(x => x.Seasons.Any());

            var competitionForSeason = _competitionFaker.Generate();
            competitionForSeason.UntilYear = 2021;
            testData.SeasonWithMinimalDetails = CreateSeasonWithMinimalDetails(competitionForSeason, 2020, 2020);
            competitionForSeason.Seasons.Add(testData.SeasonWithMinimalDetails);
            testData.Competitions.Add(competitionForSeason);

            testData.Seasons = testData.Competitions.SelectMany(x => x.Seasons)
                .Union(testData.Teams.SelectMany(x => x.Seasons).Select(x => x.Season).OfType<Season>())
                .Distinct(new SeasonEqualityComparer()).ToList();

            testData.SeasonWithFullDetails = testData.Seasons.First(x => x.Teams.Any() && x.PointsRules.Any() && x.PointsAdjustments.Any());

            var playerIdentitiesInMatches = testData.Matches.SelectMany(_playerIdentityFinder.PlayerIdentitiesInMatch).Distinct(new PlayerIdentityEqualityComparer());
            testData.PlayerIdentities = playerIdentitiesInMatches.ToList();
            testData.Players = testData.PlayerIdentities.Select(x => x.Player).OfType<Player>().Distinct(playerComparer).ToList();

            var matchProviders = new BaseMatchDataProvider[]{
                new APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam(_randomiser, _matchFactory, _bowlingFiguresCalculator, _playerOfTheMatchAward),
                new APlayerWithTwoIdentitiesOnOneTeamTakesFiveWicketsOnlyWhenBothAreCombined(_randomiser, _matchFactory, _bowlingFiguresCalculator),
                new PlayersOnlyRecordedInOnePlace(_matchFactory, _teamFakerFactory, _playerIdentityFakerFactory, _playerOfTheMatchAward),
                new MatchesInTheFuture(_matchFactory, _teamFakerFactory)
            };
            foreach (var provider in matchProviders)
            {
                var matchesFromProvider = provider.CreateMatches(testData);
                foreach (var match in matchesFromProvider)
                {
                    testData.Matches.Add(match);

                    foreach (var matchInnings in match.MatchInnings)
                    {
                        matchInnings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(matchInnings);
                    }

                    var newTeams = match.Teams.Select(x => x.Team).OfType<Team>().Where(x => !testData.Teams.Any(t => t.TeamId == x.TeamId));
                    testData.Teams.AddRange(newTeams);

                    var newPlayerIdentities = _playerIdentityFinder.PlayerIdentitiesInMatch(match).Where(x => !testData.PlayerIdentities.Select(pi => pi.PlayerIdentityId).Contains(x.PlayerIdentityId)).ToList();
                    if (newPlayerIdentities.Any()) { testData.PlayerIdentities.AddRange(newPlayerIdentities); }

                    var newPlayers = newPlayerIdentities.Select(x => x.Player).Where(x => x != null && !testData.Players.Select(p => p.PlayerId).Contains(x.PlayerId)).OfType<Player>();
                    if (newPlayers.Any()) { testData.Players.AddRange(newPlayers); }
                }
            }

            testData.MatchInTheFutureWithMinimalDetails = FindMatchInTheFutureWithMinimalDetails(testData);

            testData.PlayersWithMultipleIdentities = FindPlayersWithMultipleIdentities(testData);

            // Find any player who has multiple identities and bowled, and associate them to a member
            testData.BowlerWithMultipleIdentities = testData.Matches
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Where(x => testData.PlayersWithMultipleIdentities.Contains(x.Bowler?.Player, playerComparer))
                .Select(x => x.Bowler?.Player)
                .First();
            testData.BowlerWithMultipleIdentities!.PlayerIdentities.Clear();
            testData.BowlerWithMultipleIdentities.PlayerIdentities.AddRange(testData.PlayerIdentities.Where(x => x.Player?.PlayerId == testData.BowlerWithMultipleIdentities.PlayerId));
            testData.BowlerWithMultipleIdentities.MemberKey = testData.AnyMemberNotLinkedToPlayer().memberKey;
            foreach (var identity in testData.BowlerWithMultipleIdentities.PlayerIdentities)
            {
                identity.LinkedBy = PlayerIdentityLinkedBy.Member;
            }

            testData.MatchListings.AddRange(testData.Matches.Where(x => x.Tournament == null).Select(x => x.ToMatchListing()).Union(testData.Tournaments.Select(x => x.ToMatchListing())));
            testData.TournamentMatchListings.AddRange(testData.Matches.Where(x => x.Tournament != null).Select(x => x.ToMatchListing()));

            // Get all batting records
            testData.PlayerInnings = testData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).ToList();

            ForceTheFifthAndSixthMostRunsResultsToBeEqual(testData);
            ForceTheFifthAndSixthCatchesResultsToBeEqual(testData);
            ForceTheFifthAndSixthRunOutsResultsToBeEqual(testData);
            ForceTheFifthAndSixthMostWicketsResultsToBeEqual(testData);

            CreateImmutableTestData(testData);

            BuildCollections(testData);

            EnsureCyclicalRelationshipsArePopulated(testData);

            PopulateCalculatedProperties(testData);

            return testData;
        }

        /// <summary>
        /// Ensure that calculated properties are populated. Must not change any source data or relationships.
        /// </summary>
        /// <param name="testData"></param>
        private void PopulateCalculatedProperties(TestData testData)
        {
            // The following steps must happen after ALL scorecards and awards are finalised
            foreach (var identity in testData.PlayerIdentities)
            {
                var matchesPlayedByThisIdentity = _matchFinder.MatchesPlayedByPlayerIdentity(testData.Matches, identity.PlayerIdentityId!.Value);
                identity.TotalMatches = matchesPlayedByThisIdentity.Select(x => x.MatchId).Distinct().Count();
                if (identity.TotalMatches > 0)
                {
                    identity.FirstPlayed = matchesPlayedByThisIdentity.Min(x => x.StartTime);
                    identity.LastPlayed = matchesPlayedByThisIdentity.Max(x => x.StartTime);
                }
            }
        }

        /// <summary>
        /// Ensure that objects which have cyclical relationships have those relationships populated, in case test data providers did not populate them.
        /// </summary>
        /// <param name="testData"></param>
        private static void EnsureCyclicalRelationshipsArePopulated(TestData testData)
        {
            foreach (var identity in testData.PlayerIdentities)
            {
                // Ensure the cyclical relationship between players and identities is populated
                if (identity.Player is not null && !identity.Player.PlayerIdentities.Contains(identity)) { identity.Player.PlayerIdentities.Add(identity); }
            }
        }

        /// <summary>
        /// Build collections from finalised test data to avoid repeatedly querying to create the same collection.
        /// </summary>
        /// <param name="testData"></param>
        private static void BuildCollections(TestData testData)
        {
            // Add members created to support other objects
            var membersFromPlayers = testData.Players.Where(p => p.MemberKey is not null).Select(p => (memberKey: p.MemberKey!.Value, memberName: "No name"));
            testData.Members = testData.Members
                              .Union(membersFromPlayers, new MemberEqualityComparer())
                              .ToList();

            // Add player identities created to support other objects
            var playerIdentitiesFromPlayers = testData.Players.SelectMany(p => p.PlayerIdentities);
            testData.PlayerIdentities = testData.PlayerIdentities
                                       .Union(playerIdentitiesFromPlayers, new PlayerIdentityEqualityComparer())
                                       .ToList();

            // Add teams created to support other objects
            var teamsFromPlayers = testData.Players.SelectMany(p => p.PlayerIdentities).Where(pi => pi.Team is not null).Select(pi => pi.Team).OfType<Team>();
            var teamsFromSchools = testData.Schools.SelectMany(x => x.Teams);

            var teamComparer = new TeamEqualityComparer();
            testData.Teams = testData.Teams
                            .Union(teamsFromPlayers, teamComparer)
                            .Union(teamsFromSchools, teamComparer)
                            .ToList();

            // Add match locations created to support other objects
            testData.MatchLocations = testData.MatchLocations
                                     .Union(testData.Teams.SelectMany(x => x.MatchLocations), new MatchLocationEqualityComparer())
                                     .ToList();

            // Create collections from team data
            testData.TeamListings = CreateTeamListingsFromClubsAndTeams(testData);

            // Create collections from player data
            testData.PlayersWithMultipleIdentities = FindPlayersWithMultipleIdentities(testData);

            // Create collections from match data
            testData.Awards.AddRange(testData.Matches.SelectMany(m => m.Awards));
            testData.MatchInnings.AddRange(testData.Matches.SelectMany(x => x.MatchInnings));
            testData.BowlingFigures.AddRange(testData.MatchInnings.SelectMany(mi => mi.BowlingFigures));
        }

        private static List<TeamListing> CreateTeamListingsFromClubsAndTeams(TestData testData)
        {
            var teamListings = new List<TeamListing>();
            foreach (var team in testData.Teams.Where(t => t.Club == null ||
                                                                       t.Club.Teams.Count() == 1 ||
                                                                      (t.Club.Teams.Count(x => !x.UntilYear.HasValue) == 1 && !t.UntilYear.HasValue)))
            {
                teamListings.Add(team.ToTeamListing());
            }
            foreach (var club in testData.Clubs.Where(c => c.Teams.Count == 0 ||
                                                           c.Teams.Count(x => !x.UntilYear.HasValue) > 1))
            {
                teamListings.Add(club.ToTeamListing());
            }
            return teamListings;
        }

        /// <summary>
        /// Create test data from providers, for specific scenarios which must not be altered in case they are no longer valid.
        /// </summary>
        /// <param name="testData"></param>
        private void CreateImmutableTestData(TestData testData)
        {
            testData.Schools.AddRange(CreateTestDataFromSchoolProviders(testData));

            testData.Players.AddRange(CreateTestDataFromPlayerProviders(testData));
        }

        private List<School> CreateTestDataFromSchoolProviders(TestData testData)
        {
            var providers = new BaseSchoolDataProvider[]{
                new SchoolDataProvider(_schoolFakerFactory, _teamFakerFactory, _matchLocationFakerFactory)
            };
            var schools = new List<School>();
            foreach (var provider in providers)
            {
                schools.AddRange(provider.CreateSchools());
            }
            return schools;
        }

        private List<Player> CreateTestDataFromPlayerProviders(TestData testData)
        {
            var providers = new BasePlayerDataProvider[]{
                new PlayersLinkedToMembersProvider(_teamFakerFactory, _playerFakerFactory, _playerIdentityFakerFactory),
                new PlayersNotLinkedToMembersProvider(_teamFakerFactory, _playerFakerFactory, _playerIdentityFakerFactory),
                new PlayersLinkedToMembersOnSameTeamAsPlayersNotLinkedToMembersProvider(_teamFakerFactory, _playerFakerFactory, _playerIdentityFakerFactory)
            };
            var players = new List<Player>();
            foreach (var provider in providers)
            {
                players.AddRange(provider.CreatePlayers(testData));
            }
            return players;
        }

        private static List<Player> FindPlayersWithMultipleIdentities(TestData testData)
        {
            var results = new List<Player>();
            foreach (var identity in testData.PlayerIdentities)
            {
                if (testData.PlayerIdentities.Count(x => x.Player?.PlayerId == identity.Player?.PlayerId) > 1 &&
                    !results.Any(x => x.PlayerId == identity.Player?.PlayerId))
                {
                    results.Add(identity.Player!);
                }
            }

            foreach (var player in testData.Players.Where(p => p.PlayerIdentities.Count() > 1))
            {
                if (!results.Any(x => x.PlayerId == player.PlayerId))
                {
                    results.Add(player);
                }
            }
            return results;
        }

        private static Team FindTeamWithMinimalDetails(TestData testData, IEnumerable<Team> teamsInMatches)
        {
            return testData.Teams.FirstOrDefault(x =>
                                    string.IsNullOrEmpty(x.Introduction) &&
                                    !x.AgeRangeLower.HasValue && !x.AgeRangeUpper.HasValue &&
                                    string.IsNullOrEmpty(x.Facebook) && string.IsNullOrEmpty(x.Twitter) && string.IsNullOrEmpty(x.Instagram) && string.IsNullOrEmpty(x.YouTube) && string.IsNullOrEmpty(x.Website) &&
                                    string.IsNullOrEmpty(x.PlayingTimes) && string.IsNullOrEmpty(x.Cost) &&
                                    !x.UntilYear.HasValue &&
                                    string.IsNullOrEmpty(x.PublicContactDetails) && string.IsNullOrEmpty(x.PrivateContactDetails) &&
                                    x.Club == null &&
                                    !x.MatchLocations.Any() &&
                                    !x.Seasons.Any() &&
                                    !teamsInMatches.Any(t => t.TeamId == x.TeamId)
                                    )
                ?? throw new InvalidOperationException($"{nameof(FindTeamWithMinimalDetails)} did not find a team.");
        }

        private static Match FindMatchInThePastWithFullDetailsAndTournament(TestData testData)
        {
            return testData.Matches.FirstOrDefault(x =>
                                x.StartTime < DateTime.UtcNow &&
                                x.Teams.Any() &&
                                x.Season != null && x.Season.Competition != null &&
                                x.MatchLocation != null &&
                                x.Tournament != null &&
                                x.Awards.Any() &&
                                x.Comments.Any() &&
                                x.MatchInnings.Any(i =>
                                        i.BattingTeam != null &&
                                        i.BowlingTeam != null &&
                                        i.PlayerInnings.Any() &&
                                        i.OverSets.Any() &&
                                        i.OversBowled.Any() &&
                                        i.BowlingFigures.Any()
                                    )
                                )
                ?? throw new InvalidOperationException($"{nameof(FindMatchInThePastWithFullDetailsAndTournament)} did not find a match.");
        }

        private static Match FindMatchInThePastWithFullDetails(TestData testData)
        {
            var match = testData.Matches.FirstOrDefault(x =>
                                x.StartTime < DateTime.UtcNow &&
                                x.Teams.Any() &&
                                x.PlayersPerTeam != null &&
                                x.Season != null && x.Season.Competition != null &&
                                x.MatchLocation != null &&
                                x.Tournament == null &&
                                x.Awards.Any() &&
                                x.Comments.Any() &&
                                x.MatchInnings.Any(i =>
                                        i.BattingTeam != null &&
                                        i.BowlingTeam != null &&
                                        i.PlayerInnings.Any() &&
                                        i.OverSets.Any() &&
                                        i.OversBowled.Any() &&
                                        i.BowlingFigures.Any()
                                    )
                                )
                ?? throw new InvalidOperationException($"{nameof(FindMatchInThePastWithFullDetails)} did not find a match.");

            match.Teams[0].Team!.UntilYear = 2020;
            match.History.AddRange(new[] { new AuditRecord {
                    Action = AuditAction.Create,
                    ActorName = nameof(SeedDataGenerator),
                    AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute().AddMonths(-1),
                    EntityUri = match.EntityUri
                }, new AuditRecord {
                    Action = AuditAction.Update,
                    ActorName = nameof(SeedDataGenerator),
                    AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute(),
                    EntityUri = match.EntityUri
                } });

            return match;
        }

        private static Match FindMatchInTheFutureWithMinimalDetails(TestData testData)
        {
            return testData.Matches.FirstOrDefault(x =>
                            x.StartTime > DateTime.UtcNow &&
                            !x.Teams.Any() &&
                            x.Season == null &&
                            x.MatchLocation == null &&
                            x.Tournament == null &&
                            !x.Awards.Any() &&
                            !x.Comments.Any() &&
                            !x.MatchInnings.Any(i =>
                               i.BattingTeam != null &&
                               i.BowlingTeam != null &&
                               i.PlayerInnings.Any() &&
                               i.OverSets.Any() &&
                               i.OversBowled.Any() &&
                               i.BowlingFigures.Any()
                            )
                        )
                ?? throw new InvalidOperationException($"{nameof(FindMatchInTheFutureWithMinimalDetails)} did not find a match.");
        }

        private static Match FindMatchInThePastWithMinimalDetails(TestData testData)
        {
            return testData.Matches.FirstOrDefault(x =>
                            x.StartTime < DateTime.UtcNow &&
                            !x.Teams.Any() &&
                            x.Season == null &&
                            x.MatchLocation == null &&
                            x.Tournament == null &&
                            !x.Awards.Any() &&
                            !x.Comments.Any() &&
                            !x.MatchInnings.Any(i =>
                               i.BattingTeam != null &&
                               i.BowlingTeam != null &&
                               i.PlayerInnings.Any() &&
                               i.OverSets.Any() &&
                               i.OversBowled.Any() &&
                               i.BowlingFigures.Any()
                            )
                        )
                ?? throw new InvalidOperationException($"{nameof(FindMatchInThePastWithFullDetails)} did not find a match.");
        }

        private class PlayerTotal
        {
            internal Player? Player { get; set; }
            internal int Total { get; set; }
        }

        private void ForceTheFifthAndSixthRunOutsResultsToBeEqual(TestData testData)
        {
            var allPlayers = testData.Players.Select(x => new PlayerTotal
            {
                Player = x,
                Total = testData.Matches.SelectMany(m => m.MatchInnings)
                           .SelectMany(mi => mi.PlayerInnings)
                           .Where(pi => pi.DismissalType == DismissalType.RunOut && pi.DismissedBy?.Player.PlayerId == x.PlayerId)
                           .Count()
            }).OrderByDescending(x => x.Total).ToList();

            for (var i = 0; i < 6; i++)
            {
                if (allPlayers[i].Total == 0)
                {
                    AddRunOutForPlayer(testData.Matches, allPlayers[i].Player!);
                    allPlayers[i].Total++;
                }
            }

            var sixthPlayer = allPlayers[5];
            var differenceBetweenFifthAndSixth = allPlayers[4].Total - sixthPlayer.Total;

            while (differenceBetweenFifthAndSixth > 0)
            {
                AddRunOutForPlayer(testData.Matches, sixthPlayer.Player!);

                differenceBetweenFifthAndSixth--;
            }
        }

        private static void AddRunOutForPlayer(List<Match> matches, Player player)
        {
            var matchInningsWherePlayerSixCouldCompleteRunOuts = matches.SelectMany(m => m.MatchInnings)
                                               .Where(mi => player.PlayerIdentities.Select(pi => pi.Team.TeamId!.Value).Contains(mi.BowlingTeam.Team.TeamId!.Value) &&
                                                            mi.PlayerInnings.Any(pi => !StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(pi.DismissalType))
                                               ).First();

            var playerInningsToChange = matchInningsWherePlayerSixCouldCompleteRunOuts.PlayerInnings.First(pi => !StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(pi.DismissalType));
            playerInningsToChange.DismissalType = DismissalType.RunOut;
            playerInningsToChange.DismissedBy = player.PlayerIdentities.First(x => x.Team.TeamId == matchInningsWherePlayerSixCouldCompleteRunOuts.BowlingTeam.Team.TeamId);
        }

        private void ForceTheFifthAndSixthCatchesResultsToBeEqual(TestData testData)
        {
            var allPlayers = testData.Players.Select(x => new PlayerTotal
            {
                Player = x,
                Total = testData.Matches.SelectMany(m => m.MatchInnings)
                           .SelectMany(mi => mi.PlayerInnings)
                           .Where(pi => (pi.DismissalType == DismissalType.Caught && pi.DismissedBy?.Player.PlayerId == x.PlayerId) ||
                                        (pi.DismissalType == DismissalType.CaughtAndBowled && pi.Bowler?.Player.PlayerId == x.PlayerId))
                           .Count()
            }).OrderByDescending(x => x.Total).ToList();

            for (var i = 0; i < 6; i++)
            {
                if (allPlayers[i].Total == 0)
                {
                    AddCatchForPlayer(testData.Matches, allPlayers[i].Player!);
                    allPlayers[i].Total++;
                }
            }

            var sixthPlayer = allPlayers[5];
            var differenceBetweenFifthAndSixth = allPlayers[4].Total - sixthPlayer.Total;

            while (differenceBetweenFifthAndSixth > 0)
            {
                AddCatchForPlayer(testData.Matches, sixthPlayer.Player!);

                differenceBetweenFifthAndSixth--;
            }
        }

        private void AddCatchForPlayer(List<Match> matches, Player player)
        {
            var matchInningsWherePlayerSixCouldTakeCatches = matches.SelectMany(m => m.MatchInnings)
                                               .Where(mi => player.PlayerIdentities.Select(pi => pi.Team.TeamId!.Value).Contains(mi.BowlingTeam.Team.TeamId!.Value) &&
                                                            mi.PlayerInnings.Any(pi => !StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(pi.DismissalType))
                                               ).First();

            var playerInningsToChange = matchInningsWherePlayerSixCouldTakeCatches.PlayerInnings.First(pi => !StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(pi.DismissalType));
            playerInningsToChange.DismissalType = DismissalType.CaughtAndBowled;
            playerInningsToChange.Bowler = player.PlayerIdentities.First(x => x.Team.TeamId == matchInningsWherePlayerSixCouldTakeCatches.BowlingTeam.Team.TeamId);

            matchInningsWherePlayerSixCouldTakeCatches.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(matchInningsWherePlayerSixCouldTakeCatches);
        }

        private static void ForceTheFifthAndSixthMostRunsResultsToBeEqual(TestData testData)
        {
            var allPlayers = testData.Players.Select(x => new
            {
                Player = x,
                Runs = testData.Matches.SelectMany(m => m.MatchInnings)
                                       .SelectMany(mi => mi.PlayerInnings)
                                       .Where(pi => pi.Batter!.Player!.PlayerId == x.PlayerId)
                                       .Sum(pi => pi.RunsScored)
            }).OrderByDescending(x => x.Runs).ToList();

            var differenceBetweenFifthAndSixth = allPlayers[4].Runs - allPlayers[5].Runs;
            var anyInningsByPlayerSix = testData.Matches.SelectMany(m => m.MatchInnings)
                                       .SelectMany(mi => mi.PlayerInnings)
                                       .First(pi => pi.Batter!.Player!.PlayerId == allPlayers[5].Player.PlayerId && pi.RunsScored.HasValue);
            anyInningsByPlayerSix.RunsScored += differenceBetweenFifthAndSixth;
        }

        private void ForceTheFifthAndSixthMostWicketsResultsToBeEqual(TestData testData)
        {
            var allPlayers = testData.Players.Select(x => new
            {
                Player = x,
                Wickets = testData.Matches.SelectMany(m => m.MatchInnings)
                                       .SelectMany(mi => mi.BowlingFigures)
                                       .Where(pi => pi.Bowler!.Player!.PlayerId == x.PlayerId)
                                       .Sum(pi => pi.Wickets)
            }).OrderByDescending(x => x.Wickets).ToList();

            var sixthPlayer = allPlayers[5];
            var differenceBetweenFifthAndSixth = allPlayers[4].Wickets - sixthPlayer.Wickets;

            while (differenceBetweenFifthAndSixth > 0)
            {
                var matchInningsWherePlayerSixCouldTakeWickets = testData.Matches.SelectMany(m => m.MatchInnings)
                                                   .Where(mi => sixthPlayer.Player.PlayerIdentities.Select(pi => pi.Team!.TeamId!.Value).Contains(mi.BowlingTeam!.Team!.TeamId!.Value) &&
                                                                mi.PlayerInnings.Any(pi => !StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(pi.DismissalType))
                                                   ).First();

                var playerInningsToChange = matchInningsWherePlayerSixCouldTakeWickets.PlayerInnings.First(pi => !StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(pi.DismissalType));
                playerInningsToChange.DismissalType = DismissalType.Bowled;
                playerInningsToChange.Bowler = sixthPlayer.Player.PlayerIdentities.First(x => x.Team!.TeamId == matchInningsWherePlayerSixCouldTakeWickets.BowlingTeam!.Team!.TeamId);

                matchInningsWherePlayerSixCouldTakeWickets.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(matchInningsWherePlayerSixCouldTakeWickets);

                differenceBetweenFifthAndSixth--;
            }
        }


        internal List<Match> GenerateMatchData(TestData testData, List<(Team team, List<PlayerIdentity> identities)> teamsWithIdentities)
        {
            var members = CreateMembers();

            // Randomly assign at least two players from each team a second identity - one on the same team, one on a different team.
            // This ensure we always have lots of teams with multiple identities for the same player for both scenarios.
            foreach (var (team, playerIdentities) in teamsWithIdentities)
            {
                // On the same team
                var player1 = playerIdentities[_randomiser.PositiveIntegerLessThan(playerIdentities.Count)];
                PlayerIdentity player2;
                do
                {
                    player2 = playerIdentities[_randomiser.PositiveIntegerLessThan(playerIdentities.Count)];
                } while (player1.PlayerIdentityId == player2.PlayerIdentityId);
                player2.Player = player1.Player;

                // On a different team
                var player3 = playerIdentities[_randomiser.PositiveIntegerLessThan(playerIdentities.Count)];
                (Team? targetTeam, List<PlayerIdentity>? targetIdentities) = (null, null);
                do
                {
                    (targetTeam, targetIdentities) = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
                } while (targetTeam.TeamId == team.TeamId);
                var player4 = targetIdentities[_randomiser.PositiveIntegerLessThan(targetIdentities.Count)];
                player4.Player = player3.Player;
            }

            var allIdentities = teamsWithIdentities.SelectMany(x => x.identities);
            foreach (var player in allIdentities.Select(x => x.Player).OfType<Player>())
            {
                player.PlayerIdentities = new PlayerIdentityList(allIdentities.Where(x => x.Player?.PlayerId == player.PlayerId));
            }

            // Create matches for them to play in, with scorecards
            var matches = new List<Match>();
            for (var i = 0; i < 40; i++)
            {
                var homeTeamBatsFirst = _randomiser.FiftyFiftyChance();

                var (teamA, teamAPlayers) = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
                (Team? teamB, List<PlayerIdentity>? teamBPlayers) = (null, null);
                do
                {
                    (teamB, teamBPlayers) = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
                }
                while (teamA.TeamId == teamB.TeamId);

                var match = _matchFactory.CreateMatchBetween(teamA, teamAPlayers, teamB, teamBPlayers, homeTeamBatsFirst, testData, nameof(GenerateMatchData) + "RandomMatches");
                if (_randomiser.FiftyFiftyChance())
                {
                    match.Comments = CreateComments(_randomiser.Between(1, 15), members);
                }
                matches.Add(match);
            }

            // Pick any innings and create a five-wicket haul for someone
            var inningsWithFiveWicketHaul = matches.SelectMany(x => x.MatchInnings).Where(x => x.PlayerInnings.Any(pi => pi.Bowler != null)).First();
            var bowlerWithFiveWicketHaul = inningsWithFiveWicketHaul.PlayerInnings.First(x => x.Bowler != null).Bowler;
            for (var i = 0; i < _randomiser.Between(5, 6); i++)
            {
                inningsWithFiveWicketHaul.PlayerInnings[i].DismissalType = StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER[_randomiser.PositiveIntegerLessThan(StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Count)];
                inningsWithFiveWicketHaul.PlayerInnings[i].Bowler = bowlerWithFiveWicketHaul;
            }

            matches.Add(CreateMatchWithTeamScoresButNoPlayerData(testData, teamsWithIdentities));

            matches.Add(CreateMatchWithFieldingByMultipleIdentities(testData, teamsWithIdentities));

            matches.Add(CreateMatchWithDifferentTeamsWhereSomeonePlaysOnBothTeams(testData, teamsWithIdentities));

            // Ensure there's always an intra-club match to test
            matches.Add(_matchFactory.CreateMatchBetween(teamsWithIdentities[0].team, teamsWithIdentities[0].identities, teamsWithIdentities[0].team, teamsWithIdentities[0].identities, _randomiser.FiftyFiftyChance(), testData, nameof(GenerateMatchData) + "IntraClub"));

            // Aim to make these obsolete by recreating everything offered by CreateMatchInThePastWithFullDetails in the generated match data above
            matches.Add(_matchFactory.CreateMatchInThePast(false, testData, nameof(GenerateMatchData)));
            matches.Add(CreateMatchInThePastWithFullDetails(members));

            var matchInThePastWithFullDetailsAndTournament = CreateMatchInThePastWithFullDetails(members);
            matchInThePastWithFullDetailsAndTournament.Tournament = CreateTournamentInThePastWithMinimalDetails();
            matchInThePastWithFullDetailsAndTournament.Season!.FromYear = matchInThePastWithFullDetailsAndTournament.Season.UntilYear = 2018;
            matches.Add(matchInThePastWithFullDetailsAndTournament);

            // Generate bowling figures for each innings
            foreach (var innings in matches.SelectMany(x => x.MatchInnings))
            {
                innings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(innings);
            }

            return matches;
        }


        private Match CreateMatchWithTeamScoresButNoPlayerData(TestData testData, List<(Team team, List<PlayerIdentity> identities)> teamsWithIdentities)
        {
            var anyTeam1 = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
            var anyTeam2 = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
            var match = _matchFactory.CreateMatchBetween(anyTeam1.team, anyTeam1.identities, anyTeam2.team, anyTeam2.identities, _randomiser.FiftyFiftyChance(), testData, nameof(CreateMatchWithTeamScoresButNoPlayerData));

            // remove any generated player data
            foreach (var innings in match.MatchInnings)
            {
                innings.PlayerInnings.Clear();
                innings.OversBowled.Clear();
                innings.BowlingFigures.Clear();
            }
            match.Awards.Clear();

            // add team scores for the first and second innings - these must be included in team score averages but will be missing from player data
            match.MatchInnings[0].Byes = 3;
            match.MatchInnings[0].Wides = 5;
            match.MatchInnings[0].NoBalls = 7;
            match.MatchInnings[0].BonusOrPenaltyRuns = 1;
            match.MatchInnings[0].Runs = 123;
            match.MatchInnings[0].Wickets = 6;

            match.MatchInnings[1].Byes = 2;
            match.MatchInnings[1].Wides = 4;
            match.MatchInnings[1].NoBalls = 6;
            match.MatchInnings[1].BonusOrPenaltyRuns = 2;
            match.MatchInnings[1].Runs = 144;
            match.MatchInnings[1].Wickets = 3;

            return match;
        }

        private Match CreateMatchWithFieldingByMultipleIdentities(TestData testData, List<(Team team, List<PlayerIdentity> identities)> teamsWithIdentities)
        {
            var anyTeam1 = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
            var anyTeam2 = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
            var match = _matchFactory.CreateMatchBetween(anyTeam1.team, anyTeam1.identities, anyTeam2.team, anyTeam2.identities, _randomiser.FiftyFiftyChance(), testData, nameof(CreateMatchWithFieldingByMultipleIdentities));

            // in the first innings a fielder should take catches under multiple identities
            var firstInnings = match.MatchInnings[0];
            var firstInningsIdentities = firstInnings.BowlingTeam!.Team!.TeamId == anyTeam1.team.TeamId ? anyTeam1.identities : anyTeam2.identities;

            var catcherWithMultipleIdentities = firstInningsIdentities.FirstOrDefault(x => firstInningsIdentities.Count(p => p.Player!.PlayerId == x.Player!.PlayerId) > 1)?.Player!.PlayerId;
            if (catcherWithMultipleIdentities.HasValue)
            {
                var catcherIdentities = firstInningsIdentities.Where(x => x.Player!.PlayerId == catcherWithMultipleIdentities).ToList();

                for (var i = 0; i < 6; i++)
                {
                    if (i % 2 == 0)
                    {
                        firstInnings.PlayerInnings[i].DismissalType = DismissalType.Caught;
                        firstInnings.PlayerInnings[i].DismissedBy = catcherIdentities[0];
                    }
                    else
                    {
                        firstInnings.PlayerInnings[i].DismissalType = DismissalType.CaughtAndBowled;
                        firstInnings.PlayerInnings[i].DismissedBy = null;
                        firstInnings.PlayerInnings[i].Bowler = catcherIdentities[1];
                    }
                }
            }

            // in the second innings a fielder should complete run-outs under multiple identities
            var secondInnings = match.MatchInnings[1];
            var secondInningsIdentities = secondInnings.BowlingTeam.Team!.TeamId == anyTeam1.team.TeamId ? anyTeam1.identities : anyTeam2.identities;

            var fielderWithMultipleIdentities = secondInningsIdentities.FirstOrDefault(x => secondInningsIdentities.Count(p => p.Player.PlayerId == x.Player.PlayerId) > 1)?.Player.PlayerId;
            if (fielderWithMultipleIdentities.HasValue)
            {
                var fielderIdentities = secondInningsIdentities.Where(x => x.Player.PlayerId == fielderWithMultipleIdentities).ToList();

                for (var i = 0; i < 6; i++)
                {
                    secondInnings.PlayerInnings[i].DismissalType = DismissalType.RunOut;
                    secondInnings.PlayerInnings[i].DismissedBy = fielderIdentities[i % 2];
                    secondInnings.PlayerInnings[i].Bowler = null;
                }
            }

            return match;
        }

        private Match CreateMatchWithDifferentTeamsWhereSomeonePlaysOnBothTeams(TestData testData, List<(Team team, List<PlayerIdentity> identities)> teamsWithIdentities)
        {
            // Ensure there's always a match to test where someone swaps sides during the innings (eg a batter is loaned as a fielder and takes a wicket)

            // 1. Find any player with identities on two teams
            var anyPlayerWithIdentitiesOnMultipleTeams = teamsWithIdentities.SelectMany(x => x.identities)
                .GroupBy(x => x.Player!.PlayerId, x => x, (playerId, playerIdentities) => new Player { PlayerId = playerId, PlayerRoute = $"/players/{playerId}", PlayerIdentities = new PlayerIdentityList(playerIdentities) })
                .Where(x => x.PlayerIdentities.Select(t => t.Team!.TeamId!.Value).Distinct().Count() > 1)
                .First();

            // 2. Create a match between those teams
            var teamsForPlayer = teamsWithIdentities.Where(t => anyPlayerWithIdentitiesOnMultipleTeams.PlayerIdentities.Select(x => x.Team!.TeamId).Contains(t.team.TeamId)).ToList();
            var match = _matchFactory.CreateMatchBetween(teamsForPlayer[0].team, teamsForPlayer[0].identities, teamsForPlayer[1].team, teamsForPlayer[1].identities, _randomiser.FiftyFiftyChance(), testData, nameof(CreateMatchWithDifferentTeamsWhereSomeonePlaysOnBothTeams));

            // 3. We know they'll be recorded as a batter in both innings. Ensure they take a wicket too.
            var wicketTaken = match.MatchInnings.First().PlayerInnings.First();
            wicketTaken.DismissalType = DismissalType.CaughtAndBowled;
            wicketTaken.Bowler = anyPlayerWithIdentitiesOnMultipleTeams.PlayerIdentities.First(x => x.Team!.TeamId == match.MatchInnings.First().BowlingTeam!.Team!.TeamId);

            return match;
        }

        private List<PlayerIdentity> CreatePlayerIdentitiesForTeam(Team team, string playerName)
        {
            var poolOfPlayers = new List<PlayerIdentity>();
            for (var i = 0; i < 8; i++)
            {
                var player = _playerFaker.Generate();
                var playerIdentity = _playerIdentityFaker.Generate();
                playerIdentity.Player = player;
                playerIdentity.Team = team;
                player.PlayerIdentities.Add(playerIdentity);
                poolOfPlayers.Add(playerIdentity);
            }

            return poolOfPlayers;
        }
    }
}