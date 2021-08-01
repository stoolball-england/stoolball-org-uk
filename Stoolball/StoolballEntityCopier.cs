using System.Linq;
using Stoolball.Clubs;
using Stoolball.Competitions;
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

        public Club CreateAuditableCopy(Club club)
        {
            if (club == null) { return null; }
            return new Club
            {
                ClubId = club.ClubId,
                ClubName = club.ClubName,
                Teams = club.Teams.Select(x => CreateAuditableCopy(x)).ToList(),
                ClubRoute = club.ClubRoute,
                MemberGroupKey = club.MemberGroupKey,
                MemberGroupName = club.MemberGroupName
            };
        }

        public PlayerIdentity CreateAuditableCopy(PlayerIdentity playerIdentity)
        {
            if (playerIdentity == null) { return null; }
            return new PlayerIdentity
            {
                PlayerIdentityId = playerIdentity.PlayerIdentityId,
                PlayerIdentityName = playerIdentity.PlayerIdentityName,
                Team = CreateAuditableCopy(playerIdentity.Team)
            };
        }

        public Team CreateAuditableCopy(Team team)
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

        public Team CreateRedactedCopy(Team team)
        {
            if (team == null) { return null; }
            var redacted = CreateAuditableCopy(team);
            redacted.Introduction = _dataRedactor.RedactPersonalData(team.Introduction);
            redacted.PlayingTimes = _dataRedactor.RedactPersonalData(team.PlayingTimes);
            redacted.Cost = _dataRedactor.RedactPersonalData(team.Cost);
            redacted.PublicContactDetails = _dataRedactor.RedactAll(team.PublicContactDetails);
            redacted.PrivateContactDetails = _dataRedactor.RedactAll(team.PrivateContactDetails);
            return redacted;
        }

        public MatchLocation CreateAuditableCopy(MatchLocation matchLocation)
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
                Postcode = matchLocation.Postcode.ToUpperInvariant(),
                GeoPrecision = matchLocation.GeoPrecision,
                Latitude = matchLocation.Latitude,
                Longitude = matchLocation.Longitude,
                MatchLocationNotes = matchLocation.MatchLocationNotes,
                MatchLocationRoute = matchLocation.MatchLocationRoute,
                MemberGroupKey = matchLocation.MemberGroupKey,
                MemberGroupName = matchLocation.MemberGroupName
            };
        }
        public MatchLocation CreateRedactedCopy(MatchLocation matchLocation)
        {
            var redacted = CreateAuditableCopy(matchLocation);
            if (redacted != null)
            {
                redacted.MatchLocationNotes = _dataRedactor.RedactPersonalData(redacted.MatchLocationNotes);
            }
            return redacted;
        }

        public Season CreateAuditableCopy(Season season)
        {
            if (season == null) return null;
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

        public Season CreateRedactedCopy(Season season)
        {
            var redacted = CreateAuditableCopy(season);
            if (redacted != null)
            {
                redacted.Introduction = _dataRedactor.RedactPersonalData(redacted.Introduction);
                redacted.Results = _dataRedactor.RedactPersonalData(redacted.Results);
            }
            return redacted;
        }

        public Competition CreateAuditableCopy(Competition competition)
        {
            if (competition == null) return null;
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
                Seasons = competition.Seasons.Select(x => CreateAuditableCopy(x)).ToList()
            };
        }

        public Competition CreateRedactedCopy(Competition competition)
        {
            var redacted = CreateAuditableCopy(competition);
            if (redacted != null)
            {
                redacted.Introduction = _dataRedactor.RedactPersonalData(redacted.Introduction);
                redacted.PrivateContactDetails = _dataRedactor.RedactAll(redacted.PrivateContactDetails);
                redacted.PublicContactDetails = _dataRedactor.RedactAll(redacted.PublicContactDetails);
            }
            return redacted;
        }
    }
}
