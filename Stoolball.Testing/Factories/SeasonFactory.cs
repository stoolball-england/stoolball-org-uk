namespace Stoolball.Testing.Factories
{
    public class SeasonFactory(OverSetFactory _overSetFactory)
    {
        /// <summary>
        /// Creates a faker for seasons in the past (or specified years).
        /// </summary>
        /// <param name="competition">The competition the seasons belong to.</param>
        /// <param name="fromYear">Fixes the season's start year instead of randomising it.</param>
        /// <param name="untilYear">Fixes the season's end year instead of randomising it.</param>
        /// <returns></returns>
        public Faker<Season> CreateFaker(Competition competition, int? fromYear = null, int? untilYear = null)
        {
            return new Faker<Season>()
                    .RuleFor(x => x.SeasonId, () => Guid.NewGuid())
                    .RuleFor(x => x.Competition, () => competition)
                    .RuleFor(x => x.FromYear, faker => fromYear ?? faker.Random.Int(2000, 2025))
                    .RuleFor(x => x.UntilYear, (faker, season) => untilYear ?? season.FromYear + faker.Random.Int(0, 1))
                    .RuleFor(x => x.SeasonRoute, (faker, season) => season.UntilYear > season.FromYear
                        ? $"{competition.CompetitionRoute}/{season.FromYear}-{season.UntilYear.ToString(CultureInfo.InvariantCulture).Substring(2)}"
                        : $"{competition.CompetitionRoute}/{season.FromYear}")
                    .RuleFor(x => x.ResultsTableType, faker => faker.Random.Enum<ResultsTableType>())
                    .RuleFor(x => x.EnableRunsScored, faker => faker.Random.Bool())
                    .RuleFor(x => x.EnableRunsConceded, faker => faker.Random.Bool())
                    .RuleFor(x => x.EnableBonusOrPenaltyRuns, faker => faker.Random.Bool())
                    .RuleFor(x => x.EnableLastPlayerBatsOn, faker => faker.Random.Bool())
                    .RuleFor(x => x.EnableTournaments, faker => faker.Random.Bool())
                    .RuleFor(x => x.MatchTypes, faker => new List<MatchType> { MatchType.LeagueMatch, MatchType.FriendlyMatch })
                    .RuleFor(x => x.DefaultOverSets, () => _overSetFactory.CreateFaker().Generate(1));
        }
    }
}