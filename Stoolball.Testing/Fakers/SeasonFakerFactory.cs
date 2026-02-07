namespace Stoolball.Testing.Fakers
{
    public class SeasonFakerFactory : IFakerFactory<Season>
    {
        public Faker<Season> Create()
        {
            return new Faker<Season>()
                    .RuleFor(x => x.SeasonId, () => Guid.NewGuid())
                    .RuleFor(x => x.FromYear, faker => faker.Random.Int(2000, 2025))
                    .RuleFor(x => x.UntilYear, (faker, season) => season.FromYear + faker.Random.Int(0, 1))
                    .RuleFor(x => x.SeasonRoute, (faker, season) => $"/competition/season-faker/{season.FromYear}-{season.UntilYear}")
                    .RuleFor(x => x.ResultsTableType, faker => faker.Random.Enum<ResultsTableType>())
                    .RuleFor(x => x.EnableRunsScored, faker => faker.Random.Bool())
                    .RuleFor(x => x.EnableRunsConceded, faker => faker.Random.Bool())
                    .RuleFor(x => x.EnableBonusOrPenaltyRuns, faker => faker.Random.Bool())
                    .RuleFor(x => x.EnableLastPlayerBatsOn, faker => faker.Random.Bool())
                    .RuleFor(x => x.EnableTournaments, faker => faker.Random.Bool());
        }
    }
}