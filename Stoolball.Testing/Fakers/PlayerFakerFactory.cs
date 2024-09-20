using System;
using Bogus;
using Stoolball.Statistics;

namespace Stoolball.Testing.Fakers
{
    public class PlayerFakerFactory : IFakerFactory<Player>
    {
        public Faker<Player> Create()
        {
            return new Faker<Player>()
                 .RuleFor(x => x.PlayerId, () => Guid.NewGuid())
                 .RuleFor(x => x.PlayerRoute, faker => "/players/" + Guid.NewGuid());
        }
    }
}
