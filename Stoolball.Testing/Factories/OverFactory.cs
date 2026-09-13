namespace Stoolball.Testing.Factories
{
    public class OverFactory(IOversHelper _oversHelper)
    {
        public Faker<Over> CreateFaker(List<PlayerIdentity> bowlingTeam, IEnumerable<OverSet> overSets)
        {
            if (bowlingTeam is null || bowlingTeam.Count < 2)
            {
                throw new ArgumentException("At least two bowlers are required to create overs.", nameof(bowlingTeam));
            }

            var bowlers = new Randomizer().ListItems(bowlingTeam, 2);
            var overNumber = 0;

            return new Faker<Over>()
                .RuleFor(x => x.OverId, () => Guid.NewGuid())
                .RuleFor(x => x.OverNumber, () => ++overNumber)
                .RuleFor(x => x.OverSet, (faker, over) => _oversHelper.OverSetForOver(overSets, over.OverNumber))
                .RuleFor(x => x.Bowler, (faker, over) => over.OverNumber % 2 == 1 ? bowlers[0] : bowlers[1])
                .RuleFor(x => x.BallsBowled, () => 8)
                .RuleFor(x => x.NoBalls, () => 1)
                .RuleFor(x => x.Wides, () => 0)
                .RuleFor(x => x.RunsConceded, () => 10);
        }

        public List<Over> CreateOversBowledIncludingOneWithOnlyName(List<PlayerIdentity> bowlingTeam, IEnumerable<OverSet> overSets)
        {
            var oversBowled = CreateFaker(bowlingTeam, overSets).Generate(overSets.Sum(x => x.Overs) ?? 0);

            // One over has a known bowler with missing data
            oversBowled[^1].BallsBowled = null;
            oversBowled[^1].Wides = null;
            oversBowled[^1].NoBalls = null;
            oversBowled[^1].RunsConceded = null;

            return oversBowled;
        }
    }
}
