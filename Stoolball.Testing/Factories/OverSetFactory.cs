namespace Stoolball.Testing.Factories
{
    public class OverSetFactory
    {
        public Faker<OverSet> CreateFaker()
        {
            var overSetNumber = 1;

            return new Faker<OverSet>()
                 .RuleFor(x => x.OverSetId, () => Guid.NewGuid())
                 .RuleFor(x => x.OverSetNumber, faker => overSetNumber++)
                 .RuleFor(x => x.Overs, faker => faker.Random.Int(8, 15))
                 .RuleFor(x => x.BallsPerOver, faker => faker.Random.Int(6, 10));
        }
    }
}
