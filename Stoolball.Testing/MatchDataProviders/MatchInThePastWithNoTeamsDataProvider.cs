namespace Stoolball.Testing.MatchDataProviders
{
    /// <summary>
    /// Ensures there's always a match in the past which has no teams.
    /// </summary>
    internal class MatchInThePastWithNoTeamsDataProvider : BaseMatchDataProvider
    {
        private readonly MatchFactory _matchFactory;

        public MatchInThePastWithNoTeamsDataProvider(MatchFactory matchFactory)
        {
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            return [_matchFactory.CreateMatchInThePast(false, readOnlyTestData, nameof(MatchInThePastWithNoTeamsDataProvider))];
        }
    }
}
