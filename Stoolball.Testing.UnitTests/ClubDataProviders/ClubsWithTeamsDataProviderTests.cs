using System.Linq;
using Stoolball.Testing.ClubDataProviders;
using Stoolball.Testing.Factories;
using Xunit;

namespace Stoolball.Testing.UnitTests.ClubDataProviders
{
    public class ClubsWithTeamsDataProviderTests(TestServicesFixture _services) : IClassFixture<TestServicesFixture>
    {
        [Fact]
        public void Clubs_have_one_active_team_and_optionally_an_inactive_team()
        {
            var provider = new ClubsWithTeamsDataProvider(_services.Get<ClubFactory>(), _services.Get<TeamFactory>());

            var clubs = provider.CreateClubs(new TestData()).ToList();

            Assert.Equal(2, clubs.Count);

            var clubWithOneActiveTeam = Assert.Single(clubs, x => x.Teams.Count == 1);
            var onlyTeamInClub = clubWithOneActiveTeam.Teams.Single();
            Assert.Equal("Only team in the club", onlyTeamInClub.TeamName);
            Assert.False(onlyTeamInClub.UntilYear.HasValue);
            Assert.Same(clubWithOneActiveTeam, onlyTeamInClub.Club);

            var clubWithOneActiveTeamAndOthersInactive = Assert.Single(clubs, x => x.Teams.Count == 2);
            var activeTeamInClub = Assert.Single(clubWithOneActiveTeamAndOthersInactive.Teams, x => !x.UntilYear.HasValue);
            Assert.Equal("Only active team in the club", activeTeamInClub.TeamName);
            Assert.Same(clubWithOneActiveTeamAndOthersInactive, activeTeamInClub.Club);

            var inactiveTeamInClub = Assert.Single(clubWithOneActiveTeamAndOthersInactive.Teams, x => x.UntilYear.HasValue);
            Assert.Equal("Inactive team in a club with an active team", inactiveTeamInClub.TeamName);
            Assert.Same(clubWithOneActiveTeamAndOthersInactive, inactiveTeamInClub.Club);
        }
    }
}
