namespace Stoolball.Testing.CompetitionDataProviders
{
    /// <summary>
    /// Ensures there's always a competition with no seasons.
    /// </summary>
    internal class CompetitionWithNoSeasonsDataProvider : BaseCompetitionDataProvider
    {
        private readonly CompetitionFactory _competitionFactory;

        internal CompetitionWithNoSeasonsDataProvider(CompetitionFactory competitionFactory)
        {
            _competitionFactory = competitionFactory ?? throw new ArgumentNullException(nameof(competitionFactory));
        }

        internal override IEnumerable<Competition> CreateCompetitions(TestData readOnlyTestData)
        {
            return [_competitionFactory.CreateFaker().Generate()];
        }
    }
}
