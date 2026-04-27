namespace Stoolball.Testing.Factories
{
    public class UmbracoMemberFactory
    {
        public Faker<UmbracoMember> CreateFaker()
        {
            return new Faker<UmbracoMember>()
                    .RuleFor(x => x.Key, () => Guid.NewGuid())
                    .RuleFor(x => x.Name, faker => faker.Name.FullName());
        }
    }
}
