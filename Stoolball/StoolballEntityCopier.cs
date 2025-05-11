using System.Collections.Generic;
using System.Linq;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball
{
    public class StoolballEntityCopier : IStoolballEntityCopier
    {
        private readonly IDataRedactor _dataRedactor;

        public StoolballEntityCopier(IDataRedactor dataRedactor)
        {
            _dataRedactor = dataRedactor ?? throw new System.ArgumentNullException(nameof(dataRedactor));
        }

        public Club? CreateAuditableCopy(Club? club)
        {
            if (club == null) { return null; }
            return new Club
            {
                ClubId = club.ClubId,
                ClubName = club.ClubName,
                Teams = club.Teams.Select(x => CreateAuditableCopy(x)).OfType<Team>().ToList(),
                ClubRoute = club.ClubRoute,
                MemberGroupKey = club.MemberGroupKey,
                MemberGroupName = club.MemberGroupName
            };
        }

        public Player? CreateAuditableCopy(Player? player)
        {
            if (player == null) { return null; }
            return new Player
            {
                PlayerId = player.PlayerId,
                PlayerRoute = player.PlayerRoute,
                MemberKey = player.MemberKey,
                PlayerIdentities = new PlayerIdentityList(player.PlayerIdentities.Select(x => CreateAuditableCopy(x)).OfType<PlayerIdentity>())
            };
        }

        public PlayerIdentity? CreateAuditableCopy(PlayerIdentity? playerIdentity)
        {
            if (playerIdentity == null) { return null; }
            var copy = new PlayerIdentity
            {
                PlayerIdentityId = playerIdentity.PlayerIdentityId,
                PlayerIdentityName = playerIdentity.PlayerIdentityName,
                Team = CreateAuditableCopy(playerIdentity.Team)
            };

            if (playerIdentity.Player != null)
            {
                copy.Player = new Player
                {
                    PlayerId = playerIdentity.Player.PlayerId,
                    PlayerRoute = playerIdentity.Player.PlayerRoute
                };
            }
            return copy;
        }

        public Team? CreateAuditableCopy(Team? team)
        {
            if (team == null) { return null; }
            return new Team
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                TeamType = team.TeamType,
                AgeRangeLower = team.AgeRangeLower,
                AgeRangeUpper = team.AgeRangeUpper,
                UntilYear = team.UntilYear,
                PlayerType = team.PlayerType,
                Introduction = team.Introduction,
                PlayingTimes = team.PlayingTimes,
                Cost = team.Cost,
                ClubMark = team.ClubMark,
                MatchLocations = team.MatchLocations.Select(x => new MatchLocation { MatchLocationId = x.MatchLocationId }).ToList(),
                PublicContactDetails = team.PublicContactDetails,
                PrivateContactDetails = team.PrivateContactDetails,
                Facebook = team.Facebook,
                Twitter = team.Twitter,
                Instagram = team.Instagram,
                YouTube = team.YouTube,
                Website = team.Website,
                TeamRoute = team.TeamRoute,
                MemberGroupKey = team.MemberGroupKey,
                MemberGroupName = team.MemberGroupName
            };
        }

        public Team? CreateRedactedCopy(Team? team)
        {
            if (team == null) { return null; }
            var redacted = CreateAuditableCopy(team);
            if (redacted is not null)
            {
                redacted.Introduction = _dataRedactor.RedactPersonalData(team.Introduction);
                redacted.PlayingTimes = _dataRedactor.RedactPersonalData(team.PlayingTimes);
                redacted.Cost = _dataRedactor.RedactPersonalData(team.Cost);
                redacted.PublicContactDetails = _dataRedactor.RedactAll(team.PublicContactDetails);
                redacted.PrivateContactDetails = _dataRedactor.RedactAll(team.PrivateContactDetails);
            }
            return redacted;
        }

        public TeamInMatch? CreateAuditableCopy(TeamInMatch? teamInMatch)
        {
            if (teamInMatch == null) { return null; }
            return new TeamInMatch
            {
                MatchTeamId = teamInMatch.MatchTeamId,
                Team = new Team
                {
                    TeamId = teamInMatch.Team?.TeamId,
                    TeamName = teamInMatch.Team?.TeamName,
                    TeamRoute = teamInMatch.Team?.TeamRoute
                },
                PlayingAsTeamName = teamInMatch.PlayingAsTeamName,
                TeamRole = teamInMatch.TeamRole,
                WonToss = teamInMatch.WonToss,
                BattedFirst = teamInMatch.BattedFirst
            };
        }

        public MatchLocation? CreateAuditableCopy(MatchLocation? matchLocation)
        {
            if (matchLocation == null) { return null; }
            return new MatchLocation
            {
                MatchLocationId = matchLocation.MatchLocationId,
                SecondaryAddressableObjectName = matchLocation.SecondaryAddressableObjectName,
                PrimaryAddressableObjectName = matchLocation.PrimaryAddressableObjectName,
                StreetDescription = matchLocation.StreetDescription,
                Locality = matchLocation.Locality,
                Town = matchLocation.Town,
                AdministrativeArea = matchLocation.AdministrativeArea,
                Postcode = matchLocation.Postcode?.ToUpperInvariant(),
                GeoPrecision = matchLocation.GeoPrecision,
                Latitude = matchLocation.Latitude,
                Longitude = matchLocation.Longitude,
                MatchLocationNotes = matchLocation.MatchLocationNotes,
                MatchLocationRoute = matchLocation.MatchLocationRoute,
                MemberGroupKey = matchLocation.MemberGroupKey,
                MemberGroupName = matchLocation.MemberGroupName
            };
        }
        public MatchLocation? CreateRedactedCopy(MatchLocation? matchLocation)
        {
            var redacted = CreateAuditableCopy(matchLocation);
            if (redacted is not null)
            {
                redacted.MatchLocationNotes = _dataRedactor.RedactPersonalData(redacted.MatchLocationNotes);
            }
            return redacted;
        }

        public Match? CreateAuditableCopy(Match? match)
        {
            if (match == null) { return null; }
            return new Match
            {
                MatchId = match.MatchId,
                MatchName = match.MatchName,
                UpdateMatchNameAutomatically = match.UpdateMatchNameAutomatically,
                MatchType = match.MatchType,
                PlayerType = match.PlayerType,
                Tournament = match.Tournament != null ? new Tournament { TournamentId = match.Tournament.TournamentId } : null,
                MatchResultType = match.MatchResultType,
                Teams = match.Teams.Select(x => CreateAuditableCopy(x)).OfType<TeamInMatch>().ToList(),
                MatchLocation = match.MatchLocation != null ? new MatchLocation { MatchLocationId = match.MatchLocation.MatchLocationId } : null,
                StartTime = match.StartTime,
                StartTimeIsKnown = match.StartTimeIsKnown,
                Season = match.Season != null ? new Season { SeasonId = match.Season.SeasonId } : null,
                PlayersPerTeam = match.PlayersPerTeam,
                InningsOrderIsKnown = match.InningsOrderIsKnown,
                LastPlayerBatsOn = match.LastPlayerBatsOn,
                EnableBonusOrPenaltyRuns = match.EnableBonusOrPenaltyRuns,
                MatchInnings = match.MatchInnings.Select(x => CreateAuditableCopy(x)).OfType<MatchInnings>().ToList(),
                Awards = match.Awards.Select(x => CreateAuditableCopy(x)).OfType<MatchAward>().ToList(),
                MatchNotes = match.MatchNotes,
                MatchRoute = match.MatchRoute,
                MemberKey = match.MemberKey
            };
        }

        public Match? CreateRedactedCopy(Match? match)
        {
            var redacted = CreateAuditableCopy(match);
            if (redacted is not null)
            {
                redacted.MatchNotes = _dataRedactor.RedactPersonalData(match!.MatchNotes);
            }
            return redacted;
        }

        public MatchInnings? CreateAuditableCopy(MatchInnings? innings)
        {
            if (innings == null) { return null; }
            return new MatchInnings
            {
                MatchInningsId = innings.MatchInningsId,
                InningsOrderInMatch = innings.InningsOrderInMatch,
                BattingMatchTeamId = innings.BattingMatchTeamId,
                BowlingMatchTeamId = innings.BowlingMatchTeamId,
                PlayerInnings = innings.PlayerInnings.Select(x => new PlayerInnings
                {
                    PlayerInningsId = x.PlayerInningsId,
                    Batter = CreateAuditableCopy(x.Batter),
                    DismissedBy = x.DismissedBy != null ? CreateAuditableCopy(x.DismissedBy) : null,
                    Bowler = x.Bowler != null ? CreateAuditableCopy(x.Bowler) : null,
                    DismissalType = x.DismissalType,
                    RunsScored = x.RunsScored,
                    BallsFaced = x.BallsFaced
                }).ToList(),
                OversBowled = innings.OversBowled.Select(x => new Over
                {
                    OverId = x.OverId,
                    OverNumber = x.OverNumber,
                    Bowler = CreateAuditableCopy(x.Bowler),
                    BallsBowled = x.BallsBowled,
                    NoBalls = x.NoBalls,
                    Wides = x.Wides,
                    RunsConceded = x.RunsConceded
                }).ToList(),
                BowlingFigures = innings.BowlingFigures.Select(x => new BowlingFigures
                {
                    BowlingFiguresId = x.BowlingFiguresId,
                    Bowler = CreateAuditableCopy(x.Bowler),
                    Overs = x.Overs,
                    Maidens = x.Maidens,
                    RunsConceded = x.RunsConceded,
                    Wickets = x.Wickets
                }).ToList(),
                BattingTeam = innings.BattingTeam != null ? CreateAuditableCopy(innings.BattingTeam) : null,
                BowlingTeam = innings.BowlingTeam != null ? CreateAuditableCopy(innings.BowlingTeam) : null,
                OverSets = innings.OverSets,
                Byes = innings.Byes,
                NoBalls = innings.NoBalls,
                Wides = innings.Wides,
                BonusOrPenaltyRuns = innings.BonusOrPenaltyRuns,
                Runs = innings.Runs,
                Wickets = innings.Wickets
            };
        }

        public MatchAward? CreateAuditableCopy(MatchAward? award)
        {
            if (award == null) { return null; }
            return new MatchAward
            {
                AwardedToId = award.AwardedToId,
                Award = award.Award,
                PlayerIdentity = CreateAuditableCopy(award.PlayerIdentity),
                Reason = award.Reason
            };
        }

        public Season? CreateAuditableCopy(Season? season)
        {
            if (season == null) { return null; }
            return new Season
            {
                SeasonId = season.SeasonId,
                FromYear = season.FromYear,
                UntilYear = season.UntilYear,
                Competition = new Competition
                {
                    CompetitionId = season.Competition?.CompetitionId,
                    CompetitionRoute = season.Competition?.CompetitionRoute
                },
                Introduction = season.Introduction,
                MatchTypes = season.MatchTypes,
                EnableTournaments = season.EnableTournaments,
                EnableBonusOrPenaltyRuns = season.EnableBonusOrPenaltyRuns,
                PlayersPerTeam = season.PlayersPerTeam,
                DefaultOverSets = season.DefaultOverSets,
                EnableLastPlayerBatsOn = season.EnableLastPlayerBatsOn,
                PointsRules = season.PointsRules,
                ResultsTableType = season.ResultsTableType,
                EnableRunsScored = season.EnableRunsScored,
                EnableRunsConceded = season.EnableRunsConceded,
                Teams = season.Teams.Select(x => new TeamInSeason { WithdrawnDate = x.WithdrawnDate, Team = CreateAuditableCopy(x.Team) }).ToList(),
                Results = season.Results,
                SeasonRoute = season.SeasonRoute
            };
        }

        public Season? CreateRedactedCopy(Season? season)
        {
            var redacted = CreateAuditableCopy(season);
            if (redacted is not null)
            {
                redacted.Introduction = _dataRedactor.RedactPersonalData(redacted.Introduction);
                redacted.Results = _dataRedactor.RedactPersonalData(redacted.Results);
            }
            return redacted;
        }

        public Competition? CreateAuditableCopy(Competition? competition)
        {
            if (competition == null) { return null; }
            return new Competition
            {
                CompetitionId = competition.CompetitionId,
                CompetitionName = competition.CompetitionName,
                FromYear = competition.FromYear,
                UntilYear = competition.UntilYear,
                PlayerType = competition.PlayerType,
                Introduction = competition.Introduction,
                PublicContactDetails = competition.PublicContactDetails,
                PrivateContactDetails = competition.PrivateContactDetails,
                Facebook = competition.Facebook,
                Twitter = competition.Twitter,
                Instagram = competition.Instagram,
                YouTube = competition.YouTube,
                Website = competition.Website,
                CompetitionRoute = competition.CompetitionRoute,
                MemberGroupKey = competition.MemberGroupKey,
                MemberGroupName = competition.MemberGroupName,
                Seasons = competition.Seasons.Select(x => CreateAuditableCopy(x)).OfType<Season>().ToList()
            };
        }

        public Tournament? CreateAuditableCopy(Tournament? tournament)
        {
            if (tournament == null) { return null; }
            return new Tournament
            {
                TournamentId = tournament.TournamentId,
                TournamentName = tournament.TournamentName,
                StartTime = tournament.StartTime,
                StartTimeIsKnown = tournament.StartTimeIsKnown,
                TournamentLocation = tournament.TournamentLocation != null ? new MatchLocation { MatchLocationId = tournament.TournamentLocation.MatchLocationId } : null,
                PlayerType = tournament.PlayerType,
                PlayersPerTeam = tournament.PlayersPerTeam,
                DefaultOverSets = tournament.DefaultOverSets,
                QualificationType = tournament.QualificationType,
                SpacesInTournament = tournament.SpacesInTournament,
                MaximumTeamsInTournament = tournament.MaximumTeamsInTournament,
                Teams = CreateAuditableCopy(tournament.Teams),
                Seasons = tournament.Seasons.Select(x => new Season { SeasonId = x.SeasonId }).ToList(),
                Matches = tournament.Matches.Select(x => new MatchInTournament { MatchId = x.MatchId, MatchName = x.MatchName, Teams = CreateAuditableCopy(x.Teams) }).ToList(),
                TournamentNotes = tournament.TournamentNotes,
                TournamentRoute = tournament.TournamentRoute
            };
        }

        public List<TeamInTournament> CreateAuditableCopy(List<TeamInTournament> teams)
        {
            return teams.Select(x => new TeamInTournament
            {
                TournamentTeamId = x.TournamentTeamId,
                Team = x.Team != null ? new Team { TeamId = x.Team.TeamId, TeamName = x.Team.TeamName } : null,
                TeamRole = x.TeamRole
            }).ToList();
        }

        public Competition? CreateRedactedCopy(Competition? competition)
        {
            var redacted = CreateAuditableCopy(competition);
            if (redacted is not null)
            {
                redacted.Introduction = _dataRedactor.RedactPersonalData(redacted.Introduction);
                redacted.PrivateContactDetails = _dataRedactor.RedactAll(redacted.PrivateContactDetails);
                redacted.PublicContactDetails = _dataRedactor.RedactAll(redacted.PublicContactDetails);
            }
            return redacted;
        }

        public Tournament? CreateRedactedCopy(Tournament? tournament)
        {
            var redacted = CreateAuditableCopy(tournament);
            if (redacted is not null)
            {
                redacted.TournamentNotes = _dataRedactor.RedactPersonalData(redacted.TournamentNotes);
            }
            return redacted;
        }
    }
}
