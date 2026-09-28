using Stoolball.Statistics;

namespace Stoolball.Testing.MatchDataProviders
{
    /// <summary>
    /// Creates random matches with scorecards between teams that have player identities in the test data, other than teams in tournaments.
    /// Matches may or may not be in a season, at a match location, and have comments.
    /// </summary>
    internal class RandomMatchDataProvider(Randomiser _randomiser, MatchFactory _matchFactory, CommentFactory _commentFactory) : BaseMatchDataProvider
    {
        /// <summary>
        /// Runs before the other match providers, so that they can rely on there being matches already.
        /// </summary>
        internal override int Order => -1;

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            // Teams in tournaments are left alone because tests delete tournaments and their teams, which fails if a random match refers to their players.
            var teamsInTournaments = readOnlyTestData.Tournaments.SelectMany(x => x.Teams).Select(x => x.Team?.TeamId).ToList();
            var teamsWithIdentities = readOnlyTestData.Teams
                .Where(team => !teamsInTournaments.Contains(team.TeamId))
                .Select(team => (team, identities: readOnlyTestData.PlayerIdentities.Where(x => x.Team?.TeamId == team.TeamId).ToList()))
                .Where(x => x.identities.Any())
                .ToList();
            if (teamsWithIdentities.Count < 2) { throw new InvalidOperationException($"{nameof(RandomMatchDataProvider)} needs at least two teams with player identities that are not in tournaments."); }

            var matches = new List<Match>();
            for (var i = 0; i < 40; i++)
            {
                var homeTeamBatsFirst = _randomiser.FiftyFiftyChance();

                var (teamA, teamAPlayers) = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
                (Team? teamB, List<PlayerIdentity>? teamBPlayers) = (null, null);
                do
                {
                    (teamB, teamBPlayers) = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
                }
                while (teamA.TeamId == teamB.TeamId);

                var match = _matchFactory.CreateMatchBetween(teamA, teamAPlayers, teamB, teamBPlayers, homeTeamBatsFirst, readOnlyTestData, nameof(RandomMatchDataProvider));
                if (_randomiser.FiftyFiftyChance())
                {
                    match.Comments = _commentFactory.CreateFaker().Generate(_randomiser.Between(1, 15));
                }

                match.MatchResultType = _randomiser.FiftyFiftyChance() ? new MatchResultType[] { MatchResultType.HomeWin, MatchResultType.AwayWin, MatchResultType.Tie }[_randomiser.PositiveIntegerLessThan(3)] : null;

                matches.Add(match);
            }

            return matches;
        }
    }
}
