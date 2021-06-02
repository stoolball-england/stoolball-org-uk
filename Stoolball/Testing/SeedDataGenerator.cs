using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Stoolball.Awards;
using Stoolball.Clubs;
using Stoolball.Comments;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Testing
{
    public class SeedDataGenerator
    {
        private readonly Random _randomiser = new Random();
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;
        private readonly IPlayerIdentityFinder _playerIdentityFinder;
        private readonly IOversHelper _oversHelper;
        private List<(Team team, List<PlayerIdentity> identities)> _teams;
        private List<MatchLocation> _matchLocations = new List<MatchLocation>();
        private List<Competition> _competitions = new List<Competition>();

        public SeedDataGenerator(IOversHelper oversHelper, IBowlingFiguresCalculator bowlingFiguresCalculator, IPlayerIdentityFinder playerIdentityFinder)
        {
            _oversHelper = oversHelper ?? throw new ArgumentNullException(nameof(oversHelper));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
            _playerIdentityFinder = playerIdentityFinder ?? throw new ArgumentNullException(nameof(playerIdentityFinder));
        }

        public Club CreateClubWithMinimalDetails()
        {
            return new Club
            {
                ClubId = Guid.NewGuid(),
                ClubName = "Club minimal",
                ClubRoute = "/clubs/club-minimal-" + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Club minimal owners"
            };
        }

        public Club CreateClubWithTeams()
        {
            var club = new Club
            {
                ClubId = Guid.NewGuid(),
                ClubName = "Club with teams",
                ClubRoute = "/clubs/club-with-teams-" + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Club with teams owners",
            };

            var inactiveAlphabeticallyFirst = CreateTeamWithMinimalDetails("Inactive team");
            inactiveAlphabeticallyFirst.Club = club;
            inactiveAlphabeticallyFirst.TeamType = TeamType.Representative;
            inactiveAlphabeticallyFirst.UntilYear = 2019;

            var activeAlphabeticallySecond = CreateTeamWithMinimalDetails("Sort me first in club");
            activeAlphabeticallySecond.TeamType = TeamType.Regular;
            activeAlphabeticallySecond.Club = club;

            var activeAlphabeticallyThird = CreateTeamWithMinimalDetails("Sort me second in club");
            activeAlphabeticallyThird.TeamType = TeamType.Occasional;
            activeAlphabeticallyThird.Club = club;

            // Teams should come back with active sorted before inactive, alphabetically within those groups
            club.Teams.Add(activeAlphabeticallySecond);
            club.Teams.Add(activeAlphabeticallyThird);
            club.Teams.Add(inactiveAlphabeticallyFirst);

            return club;
        }

        public Competition CreateCompetitionWithMinimalDetails()
        {
            return new Competition
            {
                CompetitionId = Guid.NewGuid(),
                CompetitionName = "Minimal league",
                CompetitionRoute = "/competitions/minimal-league-" + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Minimal league owners"
            };
        }

        public Competition CreateCompetitionWithFullDetails()
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

        public Season CreateSeasonWithMinimalDetails(Competition competition, int fromYear, int untilYear)
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

        public Season CreateSeasonWithFullDetails(Competition competition, int fromYear, int untilYear)
        {
            var team1 = CreateTeamWithMinimalDetails("Team 1 in season");
            var team2 = CreateTeamWithMinimalDetails("Team 2 in season");
            var team3 = CreateTeamWithMinimalDetails("Team 3 in season");

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
                    new TeamInSeason { Team = team2 },
                    new TeamInSeason { Team = team3, WithdrawnDate = new DateTimeOffset(fromYear, 6, 1, 0, 0, 0, TimeSpan.FromHours(1)) }
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

            foreach (var team in season.Teams)
            {
                team.Season = season;
            }

            return season;
        }

        public Team CreateTeamWithMinimalDetails(string teamName)
        {
            return new Team
            {
                TeamId = Guid.NewGuid(),
                TeamName = teamName,
                TeamRoute = "/teams/" + teamName.Kebaberize() + "-" + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = teamName + " owners"
            };
        }
        public Team CreateTeamWithFullDetails()
        {
            var competition = CreateCompetitionWithMinimalDetails();
            var teamName = "Team with full details";
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
                    CreateMatchLocationWithMinimalDetails(),
                    CreateMatchLocationWithMinimalDetails()
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
            foreach (var season in team.Seasons)
            {
                season.Team = team;
            }
            competition.Seasons.AddRange(team.Seasons.Select(x => x.Season));
            return team;
        }

        public MatchLocation CreateMatchLocationWithMinimalDetails()
        {
            return new MatchLocation
            {
                MatchLocationId = Guid.NewGuid(),
                PrimaryAddressableObjectName = "Pitch 1",
                SecondaryAddressableObjectName = "Our ground",
                StreetDescription = "Our street",
                Locality = "Our locality",
                Town = "Our town",
                AdministrativeArea = "Our county",
                Postcode = "AB1 2CD",
                MatchLocationRoute = "/locations/our-ground-" + Guid.NewGuid(),
                GeoPrecision = GeoPrecision.Postcode,
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Our ground owners"
            };
        }

        public MatchLocation CreateMatchLocationWithFullDetails()
        {
            var activeTeam = CreateTeamWithMinimalDetails("Team active");
            var anotherActiveTeam = CreateTeamWithMinimalDetails("Team that plays");
            var transientTeam = CreateTeamWithMinimalDetails("Transient team");
            transientTeam.TeamType = TeamType.Transient;
            var inactiveTeam = CreateTeamWithMinimalDetails("Inactive but alphabetically first");
            inactiveTeam.UntilYear = 2019;

            var matchLocation = new MatchLocation
            {
                MatchLocationId = Guid.NewGuid(),
                PrimaryAddressableObjectName = "Primary Pitch 1",
                SecondaryAddressableObjectName = "Our secondary ground",
                StreetDescription = "Our street",
                Locality = "Our locality",
                Town = "Our town",
                AdministrativeArea = "Our county",
                Postcode = "AB1 2CD",
                MatchLocationRoute = "/locations/our-ground-" + Guid.NewGuid(),
                GeoPrecision = GeoPrecision.Postcode,
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Our ground owners",
                Teams = new List<Team> { inactiveTeam, activeTeam, transientTeam, anotherActiveTeam }
            };

            activeTeam.MatchLocations.Add(matchLocation);
            anotherActiveTeam.MatchLocations.Add(matchLocation);
            transientTeam.MatchLocations.Add(matchLocation);
            inactiveTeam.MatchLocations.Add(matchLocation);

            return matchLocation;
        }

        public Match CreateMatchInThePastWithMinimalDetails()
        {
            return new Match
            {
                MatchId = Guid.NewGuid(),
                MatchName = "To be confirmed vs To be confirmed",
                MatchType = MatchType.KnockoutMatch,
                MatchInnings = new List<MatchInnings>
                {
                    new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 1 },
                    new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 2 }
                },
                MatchRoute = "/matches/minimal-match-" + Guid.NewGuid(),
                StartTime = new DateTimeOffset(2020, 6, 6, 18, 30, 0, TimeSpan.FromHours(1))
            };
        }

        public Match CreateMatchInThePastWithFullDetails(List<(Guid memberId, string memberName)> members)
        {
            // Note: Team names would sort the away team first alphabetically
            var homeTeam = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                Team = CreateTeamWithMinimalDetails("Home team"),
                WonToss = true,
                BattedFirst = true,
                TeamRole = TeamRole.Home
            };

            var awayTeam = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                Team = CreateTeamWithMinimalDetails("Away team"),
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
                    Team = homeTeam.Team
                };
            };

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
                    Team = awayTeam.Team
                };
            };

            var firstInningsOverSets = CreateOverSets();
            var secondInningsOverSets = CreateOverSets();
            var thirdInningsOverSets = CreateOverSets();
            var fourthInningsOverSets = CreateOverSets();

            var competition = CreateCompetitionWithMinimalDetails();
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
                        Award = new Award
                        {
                            AwardId = Guid.NewGuid(),
                            AwardName = "Player of the match"
                        },
                        PlayerIdentity = homePlayers[2],
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
                        PlayerInnings = CreateBattingScorecard(homePlayers, awayPlayers),
                        OverSets = firstInningsOverSets,
                        OversBowled = CreateOversBowled(new List<PlayerIdentity>(awayPlayers), firstInningsOverSets)
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
                        PlayerInnings = CreateBattingScorecard(awayPlayers, homePlayers),
                        OverSets = secondInningsOverSets,
                        OversBowled = CreateOversBowled(new List<PlayerIdentity>(homePlayers), secondInningsOverSets)
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
                        PlayerInnings = CreateBattingScorecard(homePlayers, awayPlayers),
                        OverSets = thirdInningsOverSets,
                        OversBowled = CreateOversBowled(new List<PlayerIdentity>(awayPlayers), thirdInningsOverSets)
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
                        PlayerInnings = CreateBattingScorecard(awayPlayers, homePlayers),
                        OverSets = fourthInningsOverSets,
                        OversBowled = CreateOversBowled(new List<PlayerIdentity>(homePlayers), fourthInningsOverSets)
                    }
                },
                MatchLocation = CreateMatchLocationWithMinimalDetails(),
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

        public List<(Guid memberId, string memberName)> CreateMembers()
        {
            return new List<(Guid memberId, string memberName)> {
                (Guid.NewGuid(), "Jane Smith"),
                (Guid.NewGuid(), "Joe Bloggs"),
                (Guid.NewGuid(), "George Jones"),
                (Guid.NewGuid(), "Jo Bloggs"),
                (Guid.NewGuid(), "John Doe"),
            };
        }

        public List<OverSet> CreateOverSets()
        {
            return new List<OverSet> { new OverSet { OverSetId = Guid.NewGuid(), OverSetNumber = 1, Overs = 15, BallsPerOver = 8 } };
        }

        public Tournament CreateTournamentInThePastWithMinimalDetails()
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

        public Tournament CreateTournamentInThePastWithFullDetails(List<(Guid memberId, string memberName)> members)
        {
            var competition1 = CreateCompetitionWithMinimalDetails();
            var competition2 = CreateCompetitionWithMinimalDetails();
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
                Teams = new List<TeamInTournament> {
                    new TeamInTournament {
                        Team = CreateTeamWithMinimalDetails("Tournament team 1")
                    },
                    new TeamInTournament {
                        Team = CreateTeamWithMinimalDetails("Tournament team 2")
                    },
                    new TeamInTournament {
                        Team = CreateTeamWithMinimalDetails("Tournament team 3")
                    }
                },
                TournamentLocation = CreateMatchLocationWithMinimalDetails(),
                DefaultOverSets = CreateOverSets(),
                Seasons = new List<Season>()
                {
                    CreateSeasonWithMinimalDetails(competition1, 2020, 2020),
                    CreateSeasonWithMinimalDetails(competition2, 2020, 2020)
                },
                Comments = CreateComments(10, members)
            };
            foreach (var season in tournament.Seasons)
            {
                season.Competition.Seasons.Add(season);
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

        public List<Over> CreateOversBowled(List<PlayerIdentity> bowlingTeam, IEnumerable<OverSet> overSets)
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

        public Player CreatePlayer(string playerName)
        {
            return new Player
            {
                PlayerId = Guid.NewGuid(),
                PlayerRoute = $"/players/{playerName.Kebaberize()}-{Guid.NewGuid()}",
                PlayerIdentities = new List<PlayerIdentity>
                {
                    new PlayerIdentity
                    {
                        PlayerIdentityId = Guid.NewGuid(),
                        PlayerIdentityName = playerName,
                        Team = CreateTeamWithMinimalDetails($"Team for {playerName}")
                    }
                }
            };
        }

        public List<HtmlComment> CreateComments(int howMany, List<(Guid memberId, string memberName)> members)
        {
            var randomiser = new Random();
            var comments = new List<HtmlComment>();
            for (var i = howMany; i > 0; i--)
            {
                var member = members[randomiser.Next(members.Count)];

                comments.Add(new HtmlComment
                {
                    CommentId = Guid.NewGuid(),
                    MemberKey = member.memberId,
                    MemberName = member.memberName,
                    CommentDate = DateTimeOffset.UtcNow.AddDays(i * -1),
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
                poolOfTeams.Add(CreateATeamWithPlayers($"Team {i + 1} pool player"));
                if (i % 2 == 0)
                {
                    poolOfTeams[poolOfTeams.Count - 1].team.Club = CreateClubWithMinimalDetails();
                }
            }
            return poolOfTeams;
        }

        public TestData GenerateTestData()
        {
            var testData = new TestData();
            var playerComparer = new PlayerEqualityComparer();

            testData.Matches = GenerateMatchData();
            testData.MatchLocations = testData.Matches.Where(m => m.MatchLocation != null).Select(m => m.MatchLocation).Distinct(new MatchLocationEqualityComparer()).ToList();
            testData.Competitions = testData.Matches.Where(m => m.Season != null).Select(m => m.Season.Competition).Distinct(new CompetitionEqualityComparer()).ToList();
            testData.Teams = testData.Matches.SelectMany(x => x.Teams).Select(x => x.Team).Distinct(new TeamEqualityComparer()).ToList(); // teams that got used
            testData.TeamWithClub = testData.Teams.First(x => x.Club != null);

            testData.PlayerIdentities = testData.Matches.SelectMany(m => _playerIdentityFinder.PlayerIdentitiesInMatch(m)).Distinct(new PlayerIdentityEqualityComparer()).ToList();
            testData.Players = testData.PlayerIdentities.Select(x => x.Player).Distinct(playerComparer).ToList();

            foreach (var identity in testData.PlayerIdentities)
            {
                if (testData.PlayerIdentities.Count(x => x.Player.PlayerId == identity.Player.PlayerId) > 1 &&
                    !testData.PlayersWithMultipleIdentities.Any(x => x.PlayerId == identity.Player.PlayerId))
                {
                    testData.PlayersWithMultipleIdentities.Add(identity.Player);
                }
            }

            // Since all player identities in a team get used, we can update individual participation stats from the team
            foreach (var identity in testData.PlayerIdentities)
            {
                var matchesPlayedByThisTeam = testData.Matches.Where(x => x.Teams.Any(t => t.Team.TeamId == identity.Team.TeamId)).ToList();
                identity.TotalMatches = matchesPlayedByThisTeam.Count;
                identity.FirstPlayed = matchesPlayedByThisTeam.Min(x => x.StartTime);
                identity.LastPlayed = matchesPlayedByThisTeam.Max(x => x.StartTime);
            }

            // Find any player who has multiple identities and bowled
            testData.BowlerWithMultipleIdentities = testData.Matches
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Where(x => testData.PlayersWithMultipleIdentities.Contains(x.Bowler.Player, playerComparer))
                .Select(x => x.Bowler.Player)
                .First();
            testData.BowlerWithMultipleIdentities.PlayerIdentities.Clear();
            testData.BowlerWithMultipleIdentities.PlayerIdentities.AddRange(testData.PlayerIdentities.Where(x => x.Player.PlayerId == testData.BowlerWithMultipleIdentities.PlayerId));

            // Get all batting records
            testData.PlayerInnings = testData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).ToList();

            ForceTheFifthAndSixthMostRunsResultsToBeEqual(testData);

            return testData;
        }

        private static void ForceTheFifthAndSixthMostRunsResultsToBeEqual(TestData testData)
        {
            var allPlayers = testData.Players.Select(x => new
            {
                Player = x,
                Runs = testData.Matches.SelectMany(m => m.MatchInnings)
                                       .SelectMany(mi => mi.PlayerInnings)
                                       .Where(pi => pi.Batter.Player.PlayerId == x.PlayerId)
                                       .Sum(pi => pi.RunsScored)
            }).OrderByDescending(x => x.Runs).ToList();

            var differenceBetweenFifthAndSixth = allPlayers[4].Runs - allPlayers[5].Runs;
            var anyInningsByPlayerSix = testData.Matches.SelectMany(m => m.MatchInnings)
                                       .SelectMany(mi => mi.PlayerInnings)
                                       .First(pi => pi.Batter.Player.PlayerId == allPlayers[5].Player.PlayerId && pi.RunsScored.HasValue);
            anyInningsByPlayerSix.RunsScored += differenceBetweenFifthAndSixth;
        }


        private List<Match> GenerateMatchData()
        {
            // Create utilities to randomise and build data
            _teams = GenerateTeams();

            // Create a pool of match locations 
            for (var i = 0; i < 5; i++)
            {
                _matchLocations.Add(CreateMatchLocationWithMinimalDetails());
            }

            // Create a pool of competitions
            for (var i = 0; i < 5; i++)
            {
                _competitions.Add(CreateCompetitionWithMinimalDetails());
                _competitions[_competitions.Count - 1].Seasons.Add(CreateSeasonWithMinimalDetails(_competitions[_competitions.Count - 1], DateTime.Now.Year - i, DateTime.Now.Year - i));
            }

            // Randomly assign at least two players from each team a second identity - one on the same team, one on a different team.
            // This ensure we always have lots of teams with multiple identities for the same player for both scenarios.
            foreach (var (team, playerIdentities) in _teams)
            {
                // On the same team
                var player1 = playerIdentities[_randomiser.Next(playerIdentities.Count)];
                PlayerIdentity player2;
                do
                {
                    player2 = playerIdentities[_randomiser.Next(playerIdentities.Count)];
                } while (player1.PlayerIdentityId == player2.PlayerIdentityId);
                player2.Player = player1.Player;

                // On a different team
                var player3 = playerIdentities[_randomiser.Next(playerIdentities.Count)];
                (Team targetTeam, List<PlayerIdentity> targetIdentities) = (null, null);
                do
                {
                    (targetTeam, targetIdentities) = _teams[_randomiser.Next(_teams.Count)];
                } while (targetTeam.TeamId == team.TeamId);
                var player4 = targetIdentities[_randomiser.Next(targetIdentities.Count)];
                player4.Player = player3.Player;
            }

            var allIdentities = _teams.SelectMany(x => x.identities);
            foreach (var player in allIdentities.Select(x => x.Player))
            {
                player.PlayerIdentities = allIdentities.Where(x => x.Player.PlayerId == player.PlayerId).ToList();
            }

            // Create matches for them to play in, with scorecards
            var matches = new List<Match>();
            for (var i = 0; i < 40; i++)
            {
                var homeTeamBatsFirst = FiftyFiftyChance();

                var (teamA, teamAPlayers) = _teams[_randomiser.Next(_teams.Count)];
                (Team teamB, List<PlayerIdentity> teamBPlayers) = (null, null);
                do
                {
                    (teamB, teamBPlayers) = _teams[_randomiser.Next(_teams.Count)];
                }
                while (teamA.TeamId == teamB.TeamId);

                matches.Add(CreateMatchBetween(teamA, teamAPlayers, teamB, teamBPlayers, homeTeamBatsFirst));
            }

            // Pick any innings and create a five-wicket haul for someone
            var inningsWithFiveWicketHaul = matches.SelectMany(x => x.MatchInnings).Where(x => x.PlayerInnings.Any(pi => pi.Bowler != null)).First();
            var bowlerWithFiveWicketHaul = inningsWithFiveWicketHaul.PlayerInnings.First(x => x.Bowler != null).Bowler;
            for (var i = 0; i < _randomiser.Next(5, 7); i++)
            {
                inningsWithFiveWicketHaul.PlayerInnings[i].DismissalType = StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER[_randomiser.Next(StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Count)];
                inningsWithFiveWicketHaul.PlayerInnings[i].Bowler = bowlerWithFiveWicketHaul;
            }

            matches.Add(CreateMatchWithFieldingByMultipleIdentities());

            matches.Add(CreateMatchWithDifferentTeamsWhereSomeonePlaysOnBothTeams());

            // Ensure there's always an intra-club match to test
            matches.Add(CreateMatchBetween(_teams[0].team, _teams[0].identities, _teams[0].team, _teams[0].identities, FiftyFiftyChance()));

            // Generate bowling figures for each innings
            foreach (var innings in matches.SelectMany(x => x.MatchInnings))
            {
                innings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(innings);
            }

            return matches;
        }

        private Match CreateMatchWithFieldingByMultipleIdentities()
        {
            var anyTeam1 = _teams[_randomiser.Next(_teams.Count)];
            var anyTeam2 = _teams[_randomiser.Next(_teams.Count)];
            var match = CreateMatchBetween(anyTeam1.team, anyTeam1.identities, anyTeam2.team, anyTeam2.identities, FiftyFiftyChance());

            // in the first innings a fielder should take catches under multiple identities
            var firstInnings = match.MatchInnings[0];
            var firstInningsIdentities = firstInnings.BowlingTeam.Team.TeamId == anyTeam1.team.TeamId ? anyTeam1.identities : anyTeam2.identities;

            var catcherWithMultipleIdentities = firstInningsIdentities.FirstOrDefault(x => firstInningsIdentities.Count(p => p.Player.PlayerId == x.Player.PlayerId) > 1)?.Player.PlayerId;
            if (catcherWithMultipleIdentities.HasValue)
            {
                var catcherIdentities = firstInningsIdentities.Where(x => x.Player.PlayerId == catcherWithMultipleIdentities).ToList();

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
            var secondInningsIdentities = secondInnings.BowlingTeam.Team.TeamId == anyTeam1.team.TeamId ? anyTeam1.identities : anyTeam2.identities;

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

        private Match CreateMatchWithDifferentTeamsWhereSomeonePlaysOnBothTeams()
        {
            // Ensure there's always a match to test where someone swaps sides during the innings (eg a batter is loaned as a fielder and takes a wicket)

            // 1. Find any player with identities on two teams
            var anyPlayerWithIdentitiesOnMultipleTeams = _teams.SelectMany(x => x.identities)
                .GroupBy(x => x.Player.PlayerId, x => x, (playerId, playerIdentities) => new Player { PlayerId = playerId, PlayerIdentities = playerIdentities.ToList() })
                .Where(x => x.PlayerIdentities.Select(t => t.Team.TeamId.Value).Distinct().Count() > 1)
                .First();

            // 2. Create a match between those teams
            var teamsForPlayer = _teams.Where(t => anyPlayerWithIdentitiesOnMultipleTeams.PlayerIdentities.Select(x => x.Team.TeamId).Contains(t.team.TeamId)).ToList();
            var match = CreateMatchBetween(teamsForPlayer[0].team, teamsForPlayer[0].identities, teamsForPlayer[1].team, teamsForPlayer[1].identities, FiftyFiftyChance());

            // 3. We know they'll be recorded as a batter in both innings. Ensure they take a wicket too.
            var wicketTaken = match.MatchInnings.First().PlayerInnings.First();
            wicketTaken.DismissalType = DismissalType.CaughtAndBowled;
            wicketTaken.Bowler = anyPlayerWithIdentitiesOnMultipleTeams.PlayerIdentities.First(x => x.Team.TeamId == match.MatchInnings.First().BowlingTeam.Team.TeamId);

            return match;
        }

        private bool FiftyFiftyChance()
        {
            return _randomiser.Next(2) == 0;
        }

        private Match CreateMatchBetween(Team teamA, List<PlayerIdentity> teamAPlayers, Team teamB, List<PlayerIdentity> teamBPlayers, bool homeTeamBatsFirst)
        {
            var teamAInMatch = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                TeamRole = homeTeamBatsFirst ? TeamRole.Home : TeamRole.Away,
                Team = teamA,
                WonToss = _randomiser.Next(2) == 0,
                BattedFirst = true
            };

            var teamBInMatch = new TeamInMatch
            {
                MatchTeamId = Guid.NewGuid(),
                TeamRole = homeTeamBatsFirst ? TeamRole.Away : TeamRole.Home,
                Team = teamB,
                WonToss = !teamAInMatch.WonToss,
                BattedFirst = false
            };

            var match = CreateMatchInThePastWithMinimalDetails();
            match.StartTime = DateTimeOffset.UtcNow.AddMonths(_randomiser.Next(30) * -1 - 1);
            match.Teams.Add(teamAInMatch);
            match.Teams.Add(teamBInMatch);

            // Some matches should have multiple innings
            if (_randomiser.Next(4) == 0)
            {
                match.MatchInnings.Add(CreateMatchInnings(3));
                match.MatchInnings.Add(CreateMatchInnings(4));
            }

            foreach (var innings in match.MatchInnings.Where(x => x.InningsOrderInMatch % 2 == 1))
            {
                var pairedInnings = match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                CreateRandomScorecardData(match, innings, teamAInMatch, teamBInMatch, teamAPlayers, teamBPlayers);
                CreateRandomScorecardData(match, pairedInnings, teamBInMatch, teamAInMatch, teamBPlayers, teamAPlayers);
            }

            // Most matches have a match location
            if (OneInFourChance())
            {
                match.MatchLocation = _matchLocations[_randomiser.Next(_matchLocations.Count)];
            }

            // Most matches have a season and competition
            if (OneInFourChance())
            {
                match.Season = _competitions[_randomiser.Next(_competitions.Count)].Seasons.First();
            }

            return match;
        }

        private bool OneInFourChance()
        {
            return _randomiser.Next(4) != 0;
        }

        private MatchInnings CreateMatchInnings(int inningsOrderInMatch)
        {
            return new MatchInnings
            {
                MatchInningsId = Guid.NewGuid(),
                InningsOrderInMatch = inningsOrderInMatch,
                NoBalls = _randomiser.Next(30),
                Wides = _randomiser.Next(30),
                Byes = _randomiser.Next(30),
                BonusOrPenaltyRuns = _randomiser.Next(-5, 5),
                Runs = _randomiser.Next(100, 250),
                Wickets = _randomiser.Next(11)
            };
        }

        private void CreateRandomScorecardData(Match match, MatchInnings innings, TeamInMatch battingTeam, TeamInMatch bowlingTeam, List<PlayerIdentity> battingPlayers, List<PlayerIdentity> bowlingPlayers)
        {
            innings.BattingMatchTeamId = battingTeam.MatchTeamId;
            innings.BowlingMatchTeamId = bowlingTeam.MatchTeamId;
            innings.BattingTeam = battingTeam;
            innings.BowlingTeam = bowlingTeam;

            for (var p = 0; p < battingPlayers.Count; p++)
            {
                var fielderOrMissingData = _randomiser.Next(2) == 0 ? bowlingPlayers[_randomiser.Next(bowlingPlayers.Count)] : null;
                var bowlerOrMissingData = _randomiser.Next(2) == 0 ? bowlingPlayers[_randomiser.Next(bowlingPlayers.Count)] : null;
                innings.PlayerInnings.Add(CreateRandomPlayerInnings(match, p + 1, battingPlayers[p], fielderOrMissingData, bowlerOrMissingData));
            }

            // sometimes pick a random player to bat twice in the innings
            if (FiftyFiftyChance())
            {
                innings.PlayerInnings.Add(CreateRandomPlayerInnings(match, battingPlayers.Count, battingPlayers[_randomiser.Next(battingPlayers.Count)], bowlingPlayers[_randomiser.Next(bowlingPlayers.Count)], bowlingPlayers[_randomiser.Next(bowlingPlayers.Count)]));
            }

            // pick 4 players to bowl - note there may be other bowlers recorded as taking wickets on the batting card above
            var bowlers = new List<PlayerIdentity>();
            var comparer = new PlayerIdentityEqualityComparer();

            // if there's a player with multiple identities in this team, let both identities bowl!
            var playerWithMultipleIdentitiesInThisTeam = bowlingPlayers.FirstOrDefault(x => bowlingPlayers.Count(p => p.Player.PlayerId == x.Player.PlayerId) > 1);
            if (playerWithMultipleIdentitiesInThisTeam != null) { bowlers.Add(playerWithMultipleIdentitiesInThisTeam); }

            while (bowlers.Count < 4)
            {
                var potentialBowler = bowlingPlayers[_randomiser.Next(bowlingPlayers.Count)];
                if (!bowlers.Contains(potentialBowler, comparer)) { bowlers.Add(potentialBowler); }
            }

            // Create up to 12 random overs, or a missing bowling card
            var hasBowlingData = _randomiser.Next(5) > 0;
            innings.OverSets.Add(new OverSet
            {
                OverSetId = Guid.NewGuid(),
                OverSetNumber = 1,
                Overs = _randomiser.Next(8, 13),
                BallsPerOver = 8
            });
            if (hasBowlingData)
            {
                for (var i = 1; i <= innings.OverSets[0].Overs; i++)
                {
                    if (i < 6 && i % 2 == 1) { innings.OversBowled.Add(CreateRandomOver(innings.OverSets[0], i, bowlers[0])); }
                    if (i < 6 && i % 2 == 0) { innings.OversBowled.Add(CreateRandomOver(innings.OverSets[0], i, bowlers[1])); }
                    if (i >= 6 && i % 2 == 1) { innings.OversBowled.Add(CreateRandomOver(innings.OverSets[0], i, bowlers[2])); }
                    if (i >= 6 && i % 2 == 0) { innings.OversBowled.Add(CreateRandomOver(innings.OverSets[0], i, bowlers[3])); }
                }
            }
        }

        private Over CreateRandomOver(OverSet overSet, int overNumber, PlayerIdentity playerIdentity)
        {
            // BallsBowled is usually provided but over data beyond the bowler name can be missing. 
            // The last over is often fewer balls.
            var ballsBowled = _randomiser.Next(10) == 0 ? (int?)null : 8;
            if (overNumber == 12) { ballsBowled = _randomiser.Next(9); }

            // Random numbers for the over, simulating missing data
            int? noBalls = null, wides = null, runsConceded = null;
            if (ballsBowled.HasValue)
            {
                noBalls = _randomiser.Next(4) == 0 ? (int?)null : _randomiser.Next(5);
                wides = _randomiser.Next(4) == 0 ? (int?)null : _randomiser.Next(5);
                runsConceded = _randomiser.Next(4) == 0 ? (int?)null : _randomiser.Next(20);
            }

            return new Over
            {
                OverId = Guid.NewGuid(),
                OverSet = overSet,
                Bowler = playerIdentity,
                OverNumber = overNumber,
                BallsBowled = ballsBowled,
                NoBalls = noBalls,
                Wides = wides,
                RunsConceded = runsConceded
            };
        }

        private PlayerInnings CreateRandomPlayerInnings(Match match, int battingPosition, PlayerIdentity batter, PlayerIdentity fielderOrMissingData, PlayerIdentity bowlerOrMissingData)
        {
            var dismissalTypes = Enum.GetValues(typeof(DismissalType));
            var dismissal = (DismissalType)dismissalTypes.GetValue(_randomiser.Next(dismissalTypes.Length));
            if (dismissal != DismissalType.Caught || dismissal != DismissalType.RunOut)
            {
                fielderOrMissingData = null;
            }
            if (!StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(dismissal))
            {
                bowlerOrMissingData = null;
            }
            var runsScored = _randomiser.Next(2) == 0 ? _randomiser.Next(102) : (int?)null; // simulate missing data;
            var ballsFaced = _randomiser.Next(2) == 0 ? _randomiser.Next(151) : (int?)null; // simulate missing data
            if (dismissal == DismissalType.DidNotBat || dismissal == DismissalType.TimedOut)
            {
                runsScored = null;
                ballsFaced = null;
            }

            return new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                Match = match,
                BattingPosition = battingPosition,
                Batter = batter,
                DismissalType = dismissal,
                DismissedBy = fielderOrMissingData,
                Bowler = bowlerOrMissingData,
                RunsScored = runsScored,
                BallsFaced = ballsFaced
            };
        }

        private (Team, List<PlayerIdentity>) CreateATeamWithPlayers(string playerName)
        {
            var poolOfPlayers = new List<PlayerIdentity>();
            for (var i = 0; i < 8; i++)
            {
                var player = CreatePlayer($"{playerName} {i + 1}");
                var playerIdentity = player.PlayerIdentities.First();
                playerIdentity.Player = player;
                poolOfPlayers.Add(playerIdentity);
                poolOfPlayers[i].Team = poolOfPlayers[0].Team;
            }

            return (poolOfPlayers[0].Team, poolOfPlayers);
        }
    }
}
