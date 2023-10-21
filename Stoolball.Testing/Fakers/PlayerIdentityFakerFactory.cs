using System;
using Bogus;
using Humanizer;
using Stoolball.Statistics;

namespace Stoolball.Testing.Fakers
{
    public class PlayerIdentityFakerFactory : IFakerFactory<PlayerIdentity>
    {
        public Faker<PlayerIdentity> Create()
        {
            return new Faker<PlayerIdentity>()
                 .RuleFor(x => x.PlayerIdentityId, () => Guid.NewGuid())
                 .RuleFor(x => x.PlayerIdentityName, faker => faker.Person.FullName)
                 .RuleFor(x => x.Player, () => new Player { PlayerId = Guid.NewGuid(), PlayerRoute = "/players/" + Guid.NewGuid() })
                 .RuleFor(x => x.RouteSegment, (faker, identity) => identity.PlayerIdentityName.Kebaberize());
        }
    }
}
