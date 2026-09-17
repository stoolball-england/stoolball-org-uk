namespace Stoolball.Testing.TournamentDataProviders
{
    internal class RandomTournamentDataProvider : BaseTournamentDataProvider
    {
        private readonly Randomiser _randomiser;
        private readonly TournamentFactory _tournamentFactory;
        private readonly CommentFactory _commentFactory;
        private readonly Faker<Tournament> _tournamentFaker;

        internal RandomTournamentDataProvider(Randomiser randomiser, TournamentFactory tournamentFactory, CommentFactory commentFactory)
        {
            _randomiser = randomiser ?? throw new ArgumentNullException(nameof(randomiser));
            _tournamentFactory = tournamentFactory ?? throw new ArgumentNullException(nameof(tournamentFactory));
            _commentFactory = commentFactory ?? throw new ArgumentNullException(nameof(commentFactory));
            _tournamentFaker = tournamentFactory.CreateFaker();
        }

        internal override IEnumerable<(Tournament Tournament, IEnumerable<Match> Matches)> CreateTournaments(TestData readOnlyTestData)
        {
            var tournaments = new List<(Tournament Tournament, IEnumerable<Match> Matches)>();
            for (var i = 0; i < 10; i++)
            {
                var tournament = _tournamentFactory.CreateTournamentInThePastWithFullDetailsExceptMatches();
                tournaments.Add((tournament, Enumerable.Empty<Match>()));

                var tournament2 = _tournamentFaker.Generate();
                if (!_randomiser.OneInFourChance())
                {
                    tournament2.TournamentLocation = readOnlyTestData.MatchLocations[_randomiser.PositiveIntegerLessThan(readOnlyTestData.MatchLocations.Count)];
                }
                tournament2.StartTime = DateTimeOffset.UtcNow.AddMonths(i - 20).AddDays(5).UtcToUkTime();
                tournament2.Comments = _commentFactory.CreateFaker().Generate(i);
                tournaments.Add((tournament2, Enumerable.Empty<Match>()));
            }

            return tournaments;
        }
    }
}
