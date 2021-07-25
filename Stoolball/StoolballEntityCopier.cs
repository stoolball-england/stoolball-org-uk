using System.Linq;
using Stoolball.Clubs;
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
                Teams = club.Teams.Select(x => new Team { TeamId = x.TeamId }).ToList(),
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
            return new Team { TeamId = team.TeamId };
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
            redacted.MatchLocationNotes = _dataRedactor.RedactPersonalData(redacted.MatchLocationNotes);
            return redacted;
        }
    }
}
