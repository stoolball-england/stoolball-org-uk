using System;
using Bogus;
using Stoolball.Matches;

namespace Stoolball.Testing.Fakers
{
    public class OverSetFakerFactory : IFakerFactory<OverSet>
    {
        public Faker<OverSet> Create()
        {
            return new Faker<OverSet>()
                 .RuleFor(x => x.OverSetId, () => Guid.NewGuid())
                 .RuleFor(x => x.OverSetNumber, faker => faker.UniqueIndex + 1)
                 .RuleFor(x => x.Overs, faker => faker.Random.Int(8, 15))
                 .RuleFor(x => x.BallsPerOver, faker => faker.Random.Int(6, 10));
        }
    }
}
