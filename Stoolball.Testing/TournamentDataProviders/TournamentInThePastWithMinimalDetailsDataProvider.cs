namespace Stoolball.Testing.TournamentDataProviders
{
    /// <summary>
    /// Ensures there's always a tournament in the past which has minimal details.
    /// </summary>
    internal class TournamentInThePastWithMinimalDetailsDataProvider : BaseTournamentDataProvider
    {
        private readonly TournamentFactory _tournamentFactory;

        internal TournamentInThePastWithMinimalDetailsDataProvider(TournamentFactory tournamentFactory)
        {
            _tournamentFactory = tournamentFactory ?? throw new ArgumentNullException(nameof(tournamentFactory));
        }

        internal override IEnumerable<(Tournament Tournament, IEnumerable<Match> Matches)> CreateTournaments(TestData readOnlyTestData)
        {
            return [(_tournamentFactory.CreateFaker().Generate(), Enumerable.Empty<Match>())];
        }
    }
}
