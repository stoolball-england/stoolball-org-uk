
namespace Stoolball.Testing.Factories
{
    public class ClubFactory(TeamFactory _teamFactory)
    {
        public Faker<Club> CreateFaker()
        {
            return new Faker<Club>()
                    .RuleFor(x => x.ClubId, () => Guid.NewGuid())
                    .RuleFor(x => x.ClubName, faker => $"{faker.Address.City()} {faker.Random.ListItem(["Tigers", "Bears", "Wolves", "Eagles", "Dolphins", "Stars", "Rockets", "Badgers", "Foxes", "Wildcats"])} Stoolball Club")
                    .RuleFor(x => x.MemberGroupKey, () => Guid.NewGuid())
                    .RuleFor(x => x.MemberGroupName, (faker, club) => club.ClubName + " owners")
                    .RuleFor(x => x.ClubRoute, (faker, club) => $"/clubs/{club.ClubName.Kebaberize()}-{club.ClubId}");
        }

        /// <summary>
        /// Creates a club with three teams - one inactive, two active - to test sorting of teams within a club.
        /// </summary>
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

            var teamFaker = _teamFactory.CreateBasicTeamFaker();

            var inactiveAlphabeticallyFirst = teamFaker.Generate();
            inactiveAlphabeticallyFirst.TeamName = "Inactive team";
            inactiveAlphabeticallyFirst.Club = club;
            inactiveAlphabeticallyFirst.TeamType = TeamType.Representative;
            inactiveAlphabeticallyFirst.UntilYear = 2019;

            var activeAlphabeticallySecond = teamFaker.Generate();
            activeAlphabeticallySecond.TeamName = "Sort me first in club";
            activeAlphabeticallySecond.TeamType = TeamType.Regular;
            activeAlphabeticallySecond.Club = club;

            var activeAlphabeticallyThird = teamFaker.Generate();
            activeAlphabeticallyThird.TeamName = "Sort me second in club";
            activeAlphabeticallyThird.TeamType = TeamType.Occasional;
            activeAlphabeticallyThird.Club = club;

            // Teams should come back with active sorted before inactive, alphabetically within those groups
            club.Teams.Add(activeAlphabeticallySecond);
            club.Teams.Add(activeAlphabeticallyThird);
            club.Teams.Add(inactiveAlphabeticallyFirst);

            return club;
        }
    }
}