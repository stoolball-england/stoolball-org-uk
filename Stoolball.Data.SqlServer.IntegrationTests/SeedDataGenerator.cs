using System;
using System.Collections.Generic;
using Humanizer;
using Stoolball.Awards;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public class SeedDataGenerator
    {
        public Club CreateClubWithMinimalDetails()
        {
            return new Club
            {
                ClubId = Guid.NewGuid(),
                ClubName = "Club minimal",
                ClubRoute = "/clubs/club-minimal",
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
                ClubRoute = "/clubs/club-with-teams",
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Club with teams owners",
            };

            var inactiveAlphabeticallyFirst = CreateTeamWithMinimalDetails("Inactive team");
            inactiveAlphabeticallyFirst.Club = club;
            inactiveAlphabeticallyFirst.UntilYear = 2019;

            var activeAlphabeticallySecond = CreateTeamWithMinimalDetails("Sort me first in club");
            activeAlphabeticallySecond.Club = club;

            var activeAlphabeticallyThird = CreateTeamWithMinimalDetails("Sort me second in club");
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
            var competitionRoute = "/competitions/example-league-" + Guid.NewGuid();
            return new Competition
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
                CompetitionRoute = competitionRoute,
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Example league owners",
                Seasons = new List<Season> {
                    CreateSeason(competitionRoute,2021,2021),
                    CreateSeason(competitionRoute,2020,2021),
                    CreateSeason(competitionRoute,2020,2020)
                }
            };
        }

        private static Season CreateSeason(string competitionRoute, int fromYear, int untilYear)
        {
            return new Season
            {
                SeasonId = Guid.NewGuid(),
                FromYear = fromYear,
                UntilYear = untilYear,
                SeasonRoute = competitionRoute + "/" + fromYear + "-" + untilYear,
                DefaultOverSets = CreateOverSets(),
                MatchTypes = new List<MatchType> { MatchType.LeagueMatch, MatchType.FriendlyMatch }
            };
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
                        Season = CreateSeason(competition.CompetitionRoute, 2020, 2020)
                    },
                    new TeamInSeason
                    {
                        Season = CreateSeason(competition.CompetitionRoute, 2019,2019)
                    }
                }
            };
            foreach (var season in team.Seasons)
            {
                season.Team = team;
                season.Season.Competition = competition;
            }
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
                MemberGroupName = "Our ground owners",
                Teams = new List<Team> { inactiveTeam, activeTeam, anotherActiveTeam }
            };
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

        public Match CreateMatchInThePastWithFullDetails()
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
                    PlayerIdentityId = Guid.NewGuid(),
                    PlayerIdentityName = "Player " + (i + 1),
                    Team = homeTeam.Team
                };
            };

            var awayPlayers = new PlayerIdentity[11];
            for (var i = 0; i < 11; i++)
            {
                awayPlayers[i] = new PlayerIdentity
                {
                    PlayerIdentityId = Guid.NewGuid(),
                    PlayerIdentityName = "Player " + (i + 12),
                    Team = awayTeam.Team
                };
            };

            var firstInningsOverSets = CreateOverSets();
            var secondInningsOverSets = CreateOverSets();
            var thirdInningsOverSets = CreateOverSets();
            var fourthInningsOverSets = CreateOverSets();

            var competition = CreateCompetitionWithMinimalDetails();
            var season = new Season
            {
                SeasonId = Guid.NewGuid(),
                FromYear = 2020,
                UntilYear = 2020,
                Competition = competition,
                SeasonRoute = competition.CompetitionRoute + "/2020"
            };
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
                        OversBowled = CreateOversBowled(awayPlayers, firstInningsOverSets)
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
                        OversBowled = CreateOversBowled(homePlayers, secondInningsOverSets)
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
                        OversBowled = CreateOversBowled(awayPlayers, thirdInningsOverSets)
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
                        OversBowled = CreateOversBowled(homePlayers, fourthInningsOverSets)
                    }
                },
                MatchLocation = CreateMatchLocationWithMinimalDetails(),
                MatchResultType = MatchResultType.HomeWin,
                MatchNotes = "<p>This is a test match, not a Test Match.</p>",
                MatchRoute = "/matches/team-a-vs-team-b-1jul2020-" + Guid.NewGuid(),
                MemberKey = Guid.NewGuid(),
            };

            var bowlingFigures = new BowlingFiguresCalculator();
            foreach (var innings in match.MatchInnings)
            {
                innings.BowlingFigures = bowlingFigures.CalculateBowlingFigures(innings);
            }
            return match;
        }

        private static List<OverSet> CreateOverSets()
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

        private static List<PlayerInnings> CreateBattingScorecard(PlayerIdentity[] battingTeam, PlayerIdentity[] bowlingTeam)
        {
            return new List<PlayerInnings>{
                            new PlayerInnings {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 1,
                                PlayerIdentity = battingTeam[0],
                                DismissalType = DismissalType.Bowled,
                                Bowler = bowlingTeam[3],
                                RunsScored = 50,
                                BallsFaced = 60
                            },
                            new PlayerInnings
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 2,
                                PlayerIdentity = battingTeam[1],
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
                                PlayerIdentity = battingTeam[2],
                                DismissalType = DismissalType.NotOut,
                                RunsScored = 120,
                                BallsFaced = 150
                            },
                            new PlayerInnings
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 4,
                                PlayerIdentity = battingTeam[3],
                                DismissalType = DismissalType.NotOut,
                                RunsScored = 42,
                                BallsFaced = 35
                            },
                            new PlayerInnings
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 5,
                                PlayerIdentity = battingTeam[4],
                                DismissalType = DismissalType.DidNotBat
                            },
                            new PlayerInnings
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 6,
                                PlayerIdentity = battingTeam[5],
                                DismissalType = DismissalType.DidNotBat
                            },
                            new PlayerInnings
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 7,
                                PlayerIdentity = battingTeam[6],
                                DismissalType = DismissalType.DidNotBat
                            },
                            new PlayerInnings
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 8,
                                PlayerIdentity = battingTeam[7],
                                DismissalType = DismissalType.DidNotBat
                            },
                            new PlayerInnings
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 9,
                                PlayerIdentity = battingTeam[8],
                                DismissalType = DismissalType.DidNotBat
                            },
                            new PlayerInnings
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 10,
                                PlayerIdentity = battingTeam[9],
                                DismissalType = DismissalType.DidNotBat
                            },
                            new PlayerInnings
                            {
                                PlayerInningsId = Guid.NewGuid(),
                                BattingPosition = 11,
                                PlayerIdentity = battingTeam[10],
                                DismissalType = DismissalType.DidNotBat
                            }
                        };
        }

        private static List<Over> CreateOversBowled(PlayerIdentity[] bowlingTeam, IEnumerable<OverSet> overSets)
        {
            var oversBowled = new List<Over>();
            for (var i = 0; i < 15; i++)
            {
                oversBowled.Add(new Over
                {
                    OverId = Guid.NewGuid(),
                    OverSet = OverSet.ForOver(overSets, i + 1),
                    OverNumber = i + 1,
                    PlayerIdentity = (i % 2 == 0) ? bowlingTeam[5] : bowlingTeam[3],
                    BallsBowled = 8,
                    NoBalls = 1,
                    Wides = 0,
                    RunsConceded = 10
                }); ;
            }
            return oversBowled;
        }
    }
}
