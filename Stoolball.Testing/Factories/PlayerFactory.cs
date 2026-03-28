namespace Stoolball.Testing.Factories
{
    public class PlayerFactory
    {
        public Faker<Player> CreatePlayerFaker()
        {
            return new Faker<Player>()
                 .RuleFor(x => x.PlayerId, () => Guid.NewGuid())
                 .RuleFor(x => x.PlayerRoute, faker => "/players/" + Guid.NewGuid());
        }

        public Faker<PlayerIdentity> CreatePlayerIdentityFaker()
        {
            return new Faker<PlayerIdentity>()
                 .RuleFor(x => x.PlayerIdentityId, () => Guid.NewGuid())
                 .RuleFor(x => x.PlayerIdentityName, faker => faker.Person.FullName)
                 .RuleFor(x => x.Player, () => new Player { PlayerId = Guid.NewGuid(), PlayerRoute = "/players/" + Guid.NewGuid() })
                 .RuleFor(x => x.RouteSegment, (faker, identity) => identity.PlayerIdentityName.Kebaberize());
        }
    }
}
