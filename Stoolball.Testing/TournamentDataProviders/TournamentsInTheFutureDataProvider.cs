namespace Stoolball.Testing.TournamentDataProviders
{
    internal class TournamentsInTheFutureDataProvider : BaseTournamentDataProvider
    {
        private readonly TournamentFactory _tournamentFactory;

        internal TournamentsInTheFutureDataProvider(TournamentFactory tournamentFactory)
        {
            _tournamentFactory = tournamentFactory ?? throw new ArgumentNullException(nameof(tournamentFactory));
        }

        internal override IEnumerable<(Tournament Tournament, IEnumerable<Match> Matches)> CreateTournaments(TestData readOnlyTestData)
        {
            var tournamentInTheFutureWithMinimalDetails = _tournamentFactory.CreateFaker().Generate();
            tournamentInTheFutureWithMinimalDetails.StartTime = DateTimeOffset.UtcNow.AddMonths(1).UtcToUkTime();

            var tournamentInTheFutureWithSeasons = _tournamentFactory.CreateTournamentInThePastWithFullDetailsExceptMatches();
            tournamentInTheFutureWithSeasons.StartTime = DateTimeOffset.UtcNow.AddMonths(1).UtcToUkTime();

            return [
                (tournamentInTheFutureWithMinimalDetails, Enumerable.Empty<Match>()),
                (tournamentInTheFutureWithSeasons, Enumerable.Empty<Match>())
            ];
        }
    }
}
