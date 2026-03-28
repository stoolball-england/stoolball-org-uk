namespace Stoolball.Testing.Factories
{
    public class TeamFactory
    {
        public Faker<Team> CreateFaker()
        {
            return new Faker<Team>()
                    .RuleFor(x => x.TeamId, () => Guid.NewGuid())
                    .RuleFor(x => x.TeamName, faker => faker.Address.City() + " " + faker.Random.ListItem(["Tigers", "Bears", "Wolves", "Eagles", "Dolphins", "Stars", "Rockets", "Badgers", "Foxes", "Wildcats"]))
                    .RuleFor(x => x.MemberGroupKey, () => Guid.NewGuid())
                    .RuleFor(x => x.MemberGroupName, (faker, team) => team.TeamName + " owners")
                    .RuleFor(x => x.TeamRoute, (faker, team) => $"/teams/{team.TeamName.Kebaberize()}-{team.TeamId}");
        }
    }
}
