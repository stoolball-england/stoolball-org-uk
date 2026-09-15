namespace Stoolball.Testing.TournamentDataProviders
{
    internal abstract class BaseTournamentDataProvider
    {
        /// <summary>
        /// Creates tournaments, each optionally paired with the matches played within it. <see cref="Tournament.Matches"/>
        /// only holds lightweight <see cref="MatchInTournament"/> summaries, not full <see cref="Match"/> objects with
        /// scorecards, awards and bowling figures - so a provider that needs those returns them alongside the tournament
        /// rather than via <see cref="Tournament.Matches"/>.
        /// </summary>
        internal abstract IEnumerable<(Tournament Tournament, IEnumerable<Match> Matches)> CreateTournaments(TestData readOnlyTestData);
    }
}
