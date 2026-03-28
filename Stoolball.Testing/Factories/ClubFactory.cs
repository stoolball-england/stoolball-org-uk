
namespace Stoolball.Testing.Factories
{
    public class ClubFactory
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
    }
}