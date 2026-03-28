namespace Stoolball.Testing.Factories
{
    public class CompetitionFactory
    {
        public Faker<Competition> CreateFaker()
        {
            return new Faker<Competition>()
                    .RuleFor(x => x.CompetitionId, () => Guid.NewGuid())
                    .RuleFor(x => x.CompetitionName, faker => $"{string.Join(' ', faker.Lorem.Words(3))} {faker.Random.ListItem(["League", "Association", "Friendlies", "Group"])}")
                    .RuleFor(x => x.PlayerType, faker => faker.Random.Enum<PlayerType>())
                    .RuleFor(x => x.MemberGroupKey, () => Guid.NewGuid())
                    .RuleFor(x => x.MemberGroupName, (faker, competition) => competition.CompetitionName + " owners")
                    .RuleFor(x => x.CompetitionRoute, (faker, competition) => $"/competition/{competition.CompetitionName.Kebaberize()}-{competition.CompetitionId}");
        }
    }
}