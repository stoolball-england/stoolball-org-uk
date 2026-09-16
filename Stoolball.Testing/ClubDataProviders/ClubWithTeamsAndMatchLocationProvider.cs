namespace Stoolball.Testing.ClubDataProviders
{
    internal class ClubWithTeamsAndMatchLocationProvider : BaseClubDataProvider
    {
        private readonly ClubFactory _clubFactory;
        private readonly MatchLocationFactory _matchLocationFactory;

        internal ClubWithTeamsAndMatchLocationProvider(ClubFactory clubFactory, MatchLocationFactory matchLocationFactory)
        {
            _clubFactory = clubFactory ?? throw new ArgumentNullException(nameof(clubFactory));
            _matchLocationFactory = matchLocationFactory ?? throw new ArgumentNullException(nameof(matchLocationFactory));
        }

        internal override IEnumerable<Club> CreateClubs(TestData readOnlyTestData)
        {
            var club = _clubFactory.CreateClubWithTeams();

            var matchLocation = _matchLocationFactory.CreateFaker().Generate();
            var teamWithMatchLocation = club.Teams.First(x => !x.UntilYear.HasValue);
            teamWithMatchLocation.MatchLocations.Add(matchLocation);
            matchLocation.Teams.Add(teamWithMatchLocation);

            return [club];
        }
    }
}
