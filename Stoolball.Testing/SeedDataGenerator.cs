using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Humanizer;
using Stoolball.Awards;
using Stoolball.Clubs;
using Stoolball.Comments;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Schools;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Testing.Fakers;
using Stoolball.Testing.MatchDataProviders;

namespace Stoolball.Testing
{
    internal class SeedDataGenerator
    {
        private readonly Randomiser _randomiser;
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;
        private readonly IPlayerIdentityFinder _playerIdentityFinder;
        private readonly IMatchFinder _matchFinder;
        private readonly MatchFactory _matchFactory;
        private readonly IFakerFactory<Team> _teamFakerFactory;
        private readonly IFakerFactory<MatchLocation> _matchLocationFakerFactory;
        private readonly IFakerFactory<School> _schoolFakerFactory;
        private readonly Award _playerOfTheMatchAward;
        private readonly IOversHelper _oversHelper;
        private List<(Team team, List<PlayerIdentity> identities)> _teams = new();

        internal SeedDataGenerator(Randomiser randomiser, IOversHelper oversHelper, IBowlingFiguresCalculator bowlingFiguresCalculator, IPlayerIdentityFinder playerIdentityFinder,
            IMatchFinder matchFinder, IFakerFactory<Team> teamFakerFactory, IFakerFactory<MatchLocation> matchLocationFakerFactory, IFakerFactory<School> schoolFakerFactory,
            Award playerOfTheMatchAward)
        {
            _randomiser = randomiser ?? throw new ArgumentNullException(nameof(randomiser));
            _oversHelper = oversHelper ?? throw new ArgumentNullException(nameof(oversHelper));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
            _playerIdentityFinder = playerIdentityFinder ?? throw new ArgumentNullException(nameof(playerIdentityFinder));
            _matchFinder = matchFinder ?? throw new ArgumentNullException(nameof(matchFinder));
            _teamFakerFactory = teamFakerFactory ?? throw new ArgumentNullException(nameof(teamFakerFactory));
            _matchLocationFakerFactory = matchLocationFakerFactory ?? throw new ArgumentNullException(nameof(matchLocationFakerFactory));
            _schoolFakerFactory = schoolFakerFactory ?? throw new ArgumentNullException(nameof(schoolFakerFactory));
            _playerOfTheMatchAward = playerOfTheMatchAward ?? throw new ArgumentNullException(nameof(playerOfTheMatchAward));
            _matchFactory = new MatchFactory(_randomiser, _playerOfTheMatchAward);
        }

        internal Club CreateClubWithMinimalDetails()
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

        internal Club CreateClubWithTeams()
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

        internal Competition CreateCompetitionWithMinimalDetails()
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

        internal Competition CreateCompetitionWithFullDetails()
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

        internal Season CreateSeasonWithFullDetails(Competition competition, int fromYear, int untilYear, Team team1, Team team2)
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

            foreach (var team in season.Teams)
            {
                team.Season = season;
            }

            return season;
        }

        internal Team CreateTeamWithMinimalDetails(string teamName)
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
        internal Team CreateTeamWithFullDetails(string teamName)
        {
            var competition = CreateCompetitionWithMinimalDetails();
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
                teamInSeason.Season.Teams.Add(teamInSeason);
            }
            competition.Seasons.AddRange(team.Seasons.Select(x => x.Season));
            return team;
        }

        internal MatchLocation CreateMatchLocationWithMinimalDetails()
        {
            return new MatchLocation
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
                MemberGroupName = "Our ground owners"
            };
        }

        internal MatchLocation CreateMatchLocationWithFullDetails()
        {
            var activeTeam = CreateTeamWithMinimalDetails("Team active");
            var anotherActiveTeam = CreateTeamWithMinimalDetails("Team that plays");
            var transientTeam = CreateTeamWithMinimalDetails("Transient team");
            transientTeam.TeamType = TeamType.Transient;
            var inactiveTeam = CreateTeamWithMinimalDetails("Inactive but alphabetically first");
            inactiveTeam.UntilYear = 2019;

            var matchLocation = CreateMatchLocationWithMinimalDetails();
            matchLocation.Teams = new List<Team> { inactiveTeam, activeTeam, transientTeam, anotherActiveTeam };

            activeTeam.MatchLocations.Add(matchLocation);
            anotherActiveTeam.MatchLocations.Add(matchLocation);
            transientTeam.MatchLocations.Add(matchLocation);
            inactiveTeam.MatchLocations.Add(matchLocation);

            return matchLocation;
        }


        internal Match CreateMatchInThePastWithFullDetails(List<(Guid memberId, string memberName)> members)
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

        internal List<(Guid memberId, string memberName)> CreateMembers()
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

        internal Tournament CreateTournamentInThePastWithMinimalDetails()
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

        internal Tournament CreateTournamentInThePastWithFullDetailsExceptMatches(List<(Guid memberKey, string memberName)> members)
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
                        TournamentTeamId = Guid.NewGuid(),
                        Team = CreateTeamWithMinimalDetails("Tournament team 1")
                    },
                    new TeamInTournament {
                        TournamentTeamId = Guid.NewGuid(),
                        Team = CreateTeamWithMinimalDetails("Tournament team 2")
                    },
                    new TeamInTournament {
                        TournamentTeamId = Guid.NewGuid(),
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

        internal static List<PlayerInnings> CreateBattingScorecard(PlayerIdentity[] battingTeam, PlayerIdentity[] bowlingTeam)
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

        private Player CreatePlayer(string playerName, Team team)
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
                        Team = team
                    }
                }
            };
        }

        internal List<HtmlComment> CreateComments(int howMany, List<(Guid memberKey, string memberName)> members)
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
                var team = IsEven(i) ? CreateTeamWithFullDetails($"Team {i + 1}") : CreateTeamWithMinimalDetails($"Team {i + 1}");
                poolOfTeams.Add((team, CreatePlayerIdentitiesForTeam(team, $"{team.TeamName} pool player")));
                if (i % 2 == 0)
                {
                    poolOfTeams[poolOfTeams.Count - 1].team.Club = CreateClubWithMinimalDetails();
                }
            }

            foreach (var club in poolOfTeams.Where(x => x.team.Club != null).Select(x => x.team.Club))
            {
                club.Teams.AddRange(poolOfTeams.Where(x => x.team.Club?.ClubId == club.ClubId).Select(x => x.team));
            }

            return poolOfTeams;
        }

        internal TestData GenerateTestData()
        {
            var testData = new TestData();
            var playerComparer = new PlayerEqualityComparer();

            _teams = GenerateTeams();

            // Create a pool of competitions
            for (var i = 0; i < 10; i++)
            {
                testData.Competitions.Add(IsEven(i) ? CreateCompetitionWithFullDetails() : CreateCompetitionWithMinimalDetails());
                if (IsEven(i))
                {
                    var team1 = _teams[_randomiser.PositiveIntegerLessThan(_teams.Count)].team;
                    Team team2;
                    do
                    {
                        team2 = _teams[_randomiser.PositiveIntegerLessThan(_teams.Count)].team;
                    }
                    while (team2.TeamId == team1.TeamId);
                    var season = CreateSeasonWithFullDetails(testData.Competitions[testData.Competitions.Count - 1], DateTime.Now.Year - i, DateTime.Now.Year - i, team1, team2);
                    testData.Competitions[testData.Competitions.Count - 1].Seasons.Add(season);
                }
                else
                {
                    testData.Competitions[testData.Competitions.Count - 1].Seasons.Add(CreateSeasonWithMinimalDetails(testData.Competitions[testData.Competitions.Count - 1], DateTime.Now.Year - i, DateTime.Now.Year - i));
                }
            }

            // Create a pool of match locations 
            for (var i = 0; i < 10; i++)
            {
                testData.MatchLocations.Add(IsEven(i) ? CreateMatchLocationWithFullDetails() : CreateMatchLocationWithMinimalDetails());
            }

            // Create match and tournament data
            testData.Matches = GenerateMatchData(testData, _teams);
            testData.MatchInThePastWithFullDetails = testData.Matches.First(x =>
                    x.StartTime < DateTime.UtcNow &&
                    x.Teams.Any() &&
                    x.Season != null && x.Season.Competition != null &&
                    x.MatchLocation != null &&
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
                    );
            testData.Members = testData.Matches.SelectMany(x => x.Comments).Select(x => (memberKey: x.MemberKey, memberName: x.MemberName)).Distinct(new MemberEqualityComparer()).ToList();
            for (var i = 0; i < 10; i++)
            {
                var tournament = CreateTournamentInThePastWithFullDetailsExceptMatches(testData.Members);
                if (testData.TournamentInThePastWithFullDetails == null) { testData.TournamentInThePastWithFullDetails = tournament; }
                testData.Tournaments.Add(tournament);
            }

            testData.TournamentInThePastWithMinimalDetails = CreateTournamentInThePastWithMinimalDetails();
            testData.Tournaments.Add(testData.TournamentInThePastWithMinimalDetails);

            testData.TournamentInTheFutureWithMinimalDetails = CreateTournamentInThePastWithMinimalDetails();
            testData.TournamentInTheFutureWithMinimalDetails.StartTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow.AccurateToTheMinute().AddMonths(1), Constants.UkTimeZone());
            testData.Tournaments.Add(testData.TournamentInTheFutureWithMinimalDetails);

            var teamsInMatches = testData.Matches.SelectMany(x => x.Teams).Select(x => x.Team);
            var teamsInTournaments = testData.Tournaments.SelectMany(x => x.Teams).Select(x => x.Team);
            testData.Teams = teamsInMatches.Union(teamsInTournaments).Distinct(new TeamEqualityComparer()).ToList(); // teams that got used

            // Get a detailed team that's played a match, then make sure it's played in a tournament too
            testData.TeamWithFullDetails = testData.Teams.First(x =>
                        x.Club != null &&
                        x.MatchLocations.Any() &&
                        x.Seasons.Any() &&
                        teamsInMatches.Select(t => t.TeamId).Contains(x.TeamId)
            );

            testData.ClubWithMinimalDetails = CreateClubWithMinimalDetails();

            testData.ClubWithTeamsAndMatchLocation = CreateClubWithTeams();
            testData.MatchLocationForClub = CreateMatchLocationWithMinimalDetails();
            testData.MatchLocationForClub.PrimaryAddressableObjectName = "Club PAON";
            testData.MatchLocationForClub.SecondaryAddressableObjectName = "Club SAON";
            testData.MatchLocationForClub.Locality = "Club locality";
            testData.MatchLocationForClub.Town = "Club town";
            testData.MatchLocationForClub.AdministrativeArea = "Club area";
            var teamWithMatchLocation = testData.ClubWithTeamsAndMatchLocation.Teams.First(x => !x.UntilYear.HasValue);
            teamWithMatchLocation.MatchLocations.Add(testData.MatchLocationForClub);
            testData.MatchLocationForClub.Teams.Add(teamWithMatchLocation);

            testData.Teams.AddRange(testData.ClubWithTeamsAndMatchLocation.Teams);
            testData.Clubs.Add(testData.ClubWithMinimalDetails);
            testData.Clubs.AddRange(testData.Teams.Select(x => x.Club).OfType<Club>().Distinct(new ClubEqualityComparer()));

            testData.TournamentInThePastWithFullDetails!.Teams.Add(new TeamInTournament
            {
                TournamentTeamId = Guid.NewGuid(),
                Team = testData.TeamWithFullDetails,
                TeamRole = TournamentTeamRole.Confirmed
            });

            var matchOrderInTournament = 1;
            foreach (var teamInTournament in testData.TournamentInThePastWithFullDetails.Teams)
            {
                // Create a tournament match where the fully-detailed team plays everyone including themselves
                var matchInTournament = _matchFactory.CreateMatchBetween(testData.TeamWithFullDetails, new List<PlayerIdentity>(), teamInTournament.Team, new List<PlayerIdentity>(), true, testData);
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

            testData.MatchLocations = testData.Matches.Where(m => m.MatchLocation != null).Select(m => m.MatchLocation)
                .Union(testData.Tournaments.Select(t => t.TournamentLocation))
                .Union(testData.Teams.SelectMany(x => x.MatchLocations))
                .OfType<MatchLocation>()
                .Distinct(new MatchLocationEqualityComparer()).ToList();

            testData.MatchLocationWithFullDetails = testData.MatchLocations.First(x => x.Teams.Count > 0);
            testData.Competitions = testData.Matches.Where(m => m.Season != null).Select(m => m.Season?.Competition)
                .Union(testData.Tournaments.Where(t => t.Seasons.Any()).SelectMany(t => t.Seasons.Select(s => s.Competition)))
                .Union(testData.Teams.SelectMany(x => x.Seasons).Select(x => x.Season?.Competition))
                .OfType<Competition>()
                .Distinct(new CompetitionEqualityComparer()).ToList();
            testData.CompetitionWithFullDetails = testData.Competitions.First(x => x.Seasons.Any());
            testData.Seasons = testData.Competitions.SelectMany(x => x.Seasons).Distinct(new SeasonEqualityComparer()).ToList();
            testData.SeasonWithFullDetails = testData.Seasons.First(x => x.Teams.Any() && x.PointsRules.Any() && x.PointsAdjustments.Any());

            testData.PlayerIdentities = testData.Matches.SelectMany(m => _playerIdentityFinder.PlayerIdentitiesInMatch(m)).Distinct(new PlayerIdentityEqualityComparer()).ToList();
            testData.Players = testData.PlayerIdentities.Select(x => x.Player).OfType<Player>().Distinct(playerComparer).ToList();

            foreach (var identity in testData.PlayerIdentities)
            {
                if (testData.PlayerIdentities.Count(x => x.Player.PlayerId == identity.Player.PlayerId) > 1 &&
                    !testData.PlayersWithMultipleIdentities.Any(x => x.PlayerId == identity.Player.PlayerId))
                {
                    testData.PlayersWithMultipleIdentities.Add(identity.Player);
                }
            }

            foreach (var identity in testData.PlayerIdentities)
            {
                var matchesPlayedByThisIdentity = _matchFinder.MatchesPlayedByPlayerIdentity(testData.Matches, identity.PlayerIdentityId!.Value);
                identity.TotalMatches = matchesPlayedByThisIdentity.Select(x => x.MatchId).Distinct().Count();
                identity.FirstPlayed = matchesPlayedByThisIdentity.Min(x => x.StartTime);
                identity.LastPlayed = matchesPlayedByThisIdentity.Max(x => x.StartTime);
            }

            // Find any player who has multiple identities and bowled, and associate them to a member
            testData.BowlerWithMultipleIdentities = testData.Matches
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Where(x => testData.PlayersWithMultipleIdentities.Contains(x.Bowler.Player, playerComparer))
                .Select(x => x.Bowler.Player)
                .First();
            testData.BowlerWithMultipleIdentities.PlayerIdentities.Clear();
            testData.BowlerWithMultipleIdentities.PlayerIdentities.AddRange(testData.PlayerIdentities.Where(x => x.Player?.PlayerId == testData.BowlerWithMultipleIdentities.PlayerId));
            testData.BowlerWithMultipleIdentities.MemberKey = testData.Members.First().memberKey;

            var matchProviders = new BaseMatchDataProvider[]{
                new APlayerOnlyWinsAnAwardButHasPlayedOtherMatchesWithADifferentTeam(_randomiser, _matchFactory, _bowlingFiguresCalculator, _playerOfTheMatchAward)
            };
            foreach (var provider in matchProviders)
            {
                var matchesFromProvider = provider.CreateMatches(testData);
                foreach (var match in matchesFromProvider)
                {
                    testData.Matches.Add(match);

                    var newPlayerIdentities = _playerIdentityFinder.PlayerIdentitiesInMatch(match).Where(x => !testData.PlayerIdentities.Select(pi => pi.PlayerIdentityId).Contains(x.PlayerIdentityId)).ToList();
                    if (newPlayerIdentities.Any()) { testData.PlayerIdentities.AddRange(newPlayerIdentities); }

                    var newPlayers = newPlayerIdentities.Select(x => x.Player).Where(x => x != null && !testData.Players.Select(p => p.PlayerId).Contains(x.PlayerId)).OfType<Player>();
                    if (newPlayers.Any()) { testData.Players.AddRange(newPlayers); }
                }
            }

            // Find any player who has a single identity, and associate them to a different member
            testData.Players.First(x => x.PlayerIdentities.Count == 1).MemberKey = testData.Members[1].memberKey;

            // Get all batting records
            testData.PlayerInnings = testData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).ToList();

            ForceTheFifthAndSixthMostRunsResultsToBeEqual(testData);
            ForceTheFifthAndSixthCatchesResultsToBeEqual(testData);
            ForceTheFifthAndSixthRunOutsResultsToBeEqual(testData);
            ForceTheFifthAndSixthMostWicketsResultsToBeEqual(testData);

            // Create schools data, with extra separate teams and locations
            var schoolFaker = _schoolFakerFactory.Create();
            testData.Schools = schoolFaker.Generate(20);

            CreateSchoolTeamsForSchools(testData.Schools, _teamFakerFactory.Create, _matchLocationFakerFactory.Create);

            testData.Teams.AddRange(testData.Schools.SelectMany(x => x.Teams));
            testData.MatchLocations.AddRange(testData.Schools.SelectMany(x => x.Teams).SelectMany(x => x.MatchLocations).Distinct(new MatchLocationEqualityComparer()));

            return testData;
        }


        private void CreateSchoolTeamsForSchools(List<School> schools, Func<Faker<Team>> teamFakerMaker, Func<Faker<MatchLocation>> locationFakerMaker)
        {
            var schoolTeamFaker = teamFakerMaker()
                .RuleFor(x => x.TeamType, faker => faker.Random.ListItem<TeamType>(new List<TeamType> { TeamType.SchoolAgeGroup, TeamType.SchoolClub, TeamType.SchoolOther }));
            var locationFaker = locationFakerMaker();

            for (var i = 0; i < schools.Count; i++)
            {
                // First 3 schools have no teams therefore the school should be inactive.
                if (i < 3)
                {
                    schools[i].UntilYear = 2019;
                    continue;
                }

                // Then even indexes get one team, odd get multiple.
                schools[i].Teams = schoolTeamFaker.Generate((i % 2) + 1);
                schools[i].Teams.ForEach(t => t.School = schools[i]);

                // Up to 7 they don't get a match location. Above that they do.
                // At index 9 the school with two teams gets the same location twice. 
                // At index 11 the school gets multiple teams with different locations AND multiple locations for a team.
                if (i == 9)
                {
                    // handle special case
                    var location = locationFaker.Generate(1).First();
                    schools[i].Teams.ForEach(x => x.MatchLocations.Add(location));
                }
                else if (i == 11)
                {
                    // handle special case
                    schools[i].Teams[0].MatchLocations.AddRange(locationFaker.Generate(2));
                    schools[i].Teams[1].MatchLocations.Add(locationFaker.Generate(1).First());
                }
                else if (i > 7)
                {
                    // add a match location to each team
                    schools[i].Teams.ForEach(x => x.MatchLocations.Add(locationFaker.Generate(1).First()));
                }

                // Up to 11, all teams are active.
                // Above 11, one team in multiple is inactive.
                // Above 15, both teams are inactive therefore the school should be inactive.
                if (i > 15 && schools[i].Teams.Count > 1)
                {
                    schools[i].Teams.ForEach(t => t.UntilYear = DateTimeOffset.UtcNow.AddYears(-1).Year);
                }
                else if (i > 11 && schools[i].Teams.Count > 1)
                {
                    schools[i].Teams[0].UntilYear = DateTimeOffset.UtcNow.AddYears(-2).Year;
                }
            }
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
                                       .Where(pi => pi.Batter.Player.PlayerId == x.PlayerId)
                                       .Sum(pi => pi.RunsScored)
            }).OrderByDescending(x => x.Runs).ToList();

            var differenceBetweenFifthAndSixth = allPlayers[4].Runs - allPlayers[5].Runs;
            var anyInningsByPlayerSix = testData.Matches.SelectMany(m => m.MatchInnings)
                                       .SelectMany(mi => mi.PlayerInnings)
                                       .First(pi => pi.Batter.Player.PlayerId == allPlayers[5].Player.PlayerId && pi.RunsScored.HasValue);
            anyInningsByPlayerSix.RunsScored += differenceBetweenFifthAndSixth;
        }

        private void ForceTheFifthAndSixthMostWicketsResultsToBeEqual(TestData testData)
        {
            var allPlayers = testData.Players.Select(x => new
            {
                Player = x,
                Wickets = testData.Matches.SelectMany(m => m.MatchInnings)
                                       .SelectMany(mi => mi.BowlingFigures)
                                       .Where(pi => pi.Bowler.Player.PlayerId == x.PlayerId)
                                       .Sum(pi => pi.Wickets)
            }).OrderByDescending(x => x.Wickets).ToList();

            var sixthPlayer = allPlayers[5];
            var differenceBetweenFifthAndSixth = allPlayers[4].Wickets - sixthPlayer.Wickets;

            while (differenceBetweenFifthAndSixth > 0)
            {
                var matchInningsWherePlayerSixCouldTakeWickets = testData.Matches.SelectMany(m => m.MatchInnings)
                                                   .Where(mi => sixthPlayer.Player.PlayerIdentities.Select(pi => pi.Team.TeamId!.Value).Contains(mi.BowlingTeam.Team.TeamId!.Value) &&
                                                                mi.PlayerInnings.Any(pi => !StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(pi.DismissalType))
                                                   ).First();

                var playerInningsToChange = matchInningsWherePlayerSixCouldTakeWickets.PlayerInnings.First(pi => !StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(pi.DismissalType));
                playerInningsToChange.DismissalType = DismissalType.Bowled;
                playerInningsToChange.Bowler = sixthPlayer.Player.PlayerIdentities.First(x => x.Team.TeamId == matchInningsWherePlayerSixCouldTakeWickets.BowlingTeam.Team.TeamId);

                matchInningsWherePlayerSixCouldTakeWickets.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(matchInningsWherePlayerSixCouldTakeWickets);

                differenceBetweenFifthAndSixth--;
            }
        }


        internal List<Match> GenerateMatchData(TestData testData, List<(Team team, List<PlayerIdentity> identities)> teamsWithIdentities)
        {
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
            foreach (var player in allIdentities.Select(x => x.Player))
            {
                player.PlayerIdentities = allIdentities.Where(x => x.Player.PlayerId == player.PlayerId).ToList();
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

                matches.Add(_matchFactory.CreateMatchBetween(teamA, teamAPlayers, teamB, teamBPlayers, homeTeamBatsFirst, testData));
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
            matches.Add(_matchFactory.CreateMatchBetween(teamsWithIdentities[0].team, teamsWithIdentities[0].identities, teamsWithIdentities[0].team, teamsWithIdentities[0].identities, _randomiser.FiftyFiftyChance(), testData));

            // Aim to make this one obsolete by recreating everything offered by CreateMatchInThePastWithFullDetails in the generated match data above
            matches.Add(CreateMatchInThePastWithFullDetails(CreateMembers()));

            // Generate bowling figures for each innings
            foreach (var innings in matches.SelectMany(x => x.MatchInnings))
            {
                innings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(innings);
            }

            return matches;
        }


        private static bool IsEven(int i)
        {
            return i % 2 == 0;
        }

        private Match CreateMatchWithTeamScoresButNoPlayerData(TestData testData, List<(Team team, List<PlayerIdentity> identities)> teamsWithIdentities)
        {
            var anyTeam1 = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
            var anyTeam2 = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
            var match = _matchFactory.CreateMatchBetween(anyTeam1.team, anyTeam1.identities, anyTeam2.team, anyTeam2.identities, _randomiser.FiftyFiftyChance(), testData);

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
            var match = _matchFactory.CreateMatchBetween(anyTeam1.team, anyTeam1.identities, anyTeam2.team, anyTeam2.identities, _randomiser.FiftyFiftyChance(), testData);

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

        private Match CreateMatchWithDifferentTeamsWhereSomeonePlaysOnBothTeams(TestData testData, List<(Team team, List<PlayerIdentity> identities)> teamsWithIdentities)
        {
            // Ensure there's always a match to test where someone swaps sides during the innings (eg a batter is loaned as a fielder and takes a wicket)

            // 1. Find any player with identities on two teams
            var anyPlayerWithIdentitiesOnMultipleTeams = teamsWithIdentities.SelectMany(x => x.identities)
                .GroupBy(x => x.Player.PlayerId, x => x, (playerId, playerIdentities) => new Player { PlayerId = playerId, PlayerIdentities = playerIdentities.ToList() })
                .Where(x => x.PlayerIdentities.Select(t => t.Team.TeamId!.Value).Distinct().Count() > 1)
                .First();

            // 2. Create a match between those teams
            var teamsForPlayer = teamsWithIdentities.Where(t => anyPlayerWithIdentitiesOnMultipleTeams.PlayerIdentities.Select(x => x.Team.TeamId).Contains(t.team.TeamId)).ToList();
            var match = _matchFactory.CreateMatchBetween(teamsForPlayer[0].team, teamsForPlayer[0].identities, teamsForPlayer[1].team, teamsForPlayer[1].identities, _randomiser.FiftyFiftyChance(), testData);

            // 3. We know they'll be recorded as a batter in both innings. Ensure they take a wicket too.
            var wicketTaken = match.MatchInnings.First().PlayerInnings.First();
            wicketTaken.DismissalType = DismissalType.CaughtAndBowled;
            wicketTaken.Bowler = anyPlayerWithIdentitiesOnMultipleTeams.PlayerIdentities.First(x => x.Team.TeamId == match.MatchInnings.First().BowlingTeam.Team.TeamId);

            return match;
        }

        private List<PlayerIdentity> CreatePlayerIdentitiesForTeam(Team team, string playerName)
        {
            var poolOfPlayers = new List<PlayerIdentity>();
            for (var i = 0; i < 8; i++)
            {
                var player = CreatePlayer($"{playerName} {i + 1}", team);
                var playerIdentity = player.PlayerIdentities.First();
                playerIdentity.Player = player;
                poolOfPlayers.Add(playerIdentity);
            }

            return poolOfPlayers;
        }
    }
}