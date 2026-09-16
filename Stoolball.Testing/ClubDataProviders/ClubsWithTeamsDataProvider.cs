namespace Stoolball.Testing.ClubDataProviders
{
    internal class ClubsWithTeamsDataProvider : BaseClubDataProvider
    {
        private readonly ClubFactory _clubFactory;
        private readonly TeamFactory _teamFactory;

        internal ClubsWithTeamsDataProvider(ClubFactory clubFactory, TeamFactory teamFactory)
        {
            _clubFactory = clubFactory ?? throw new ArgumentNullException(nameof(clubFactory));
            _teamFactory = teamFactory ?? throw new ArgumentNullException(nameof(teamFactory));
        }

        internal override IEnumerable<Club> CreateClubs(TestData readOnlyTestData)
        {
            var clubFaker = _clubFactory.CreateFaker();
            var teamFaker = _teamFactory.CreateFaker();

            var clubWithOneActiveTeam = clubFaker.Generate();
            var onlyTeamInClub = teamFaker.Generate();
            onlyTeamInClub.TeamName = "Only team in the club";
            clubWithOneActiveTeam.Teams.Add(onlyTeamInClub);
            onlyTeamInClub.Club = clubWithOneActiveTeam;

            var clubWithOneActiveTeamAndOthersInactive = clubFaker.Generate();
            var activeTeamInClub = teamFaker.Generate();
            activeTeamInClub.TeamName = "Only active team in the club";
            var inactiveTeamInClub = teamFaker.Generate();
            inactiveTeamInClub.TeamName = "Inactive team in a club with an active team";
            inactiveTeamInClub.UntilYear = DateTimeOffset.UtcNow.Year - 2;
            clubWithOneActiveTeamAndOthersInactive.Teams.AddRange(new[] { activeTeamInClub, inactiveTeamInClub });
            activeTeamInClub.Club = clubWithOneActiveTeamAndOthersInactive;
            inactiveTeamInClub.Club = clubWithOneActiveTeamAndOthersInactive;

            return [clubWithOneActiveTeam, clubWithOneActiveTeamAndOthersInactive];
        }
    }
}
