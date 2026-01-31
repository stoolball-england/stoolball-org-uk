using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Matches;
using Match = Stoolball.Matches.Match;

namespace Stoolball.Testing.MatchDataProviders
{
    internal class EveryMatchResultType : BaseMatchDataProvider
    {
        private readonly MatchFactory _matchFactory;

        public EveryMatchResultType(MatchFactory matchFactory)
        {
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            var matches = new List<Match>();

            // No result is also a valid result type
            MatchResultType?[] allResultTypesAndNone = [.. Enum.GetValues(typeof(MatchResultType)).Cast<MatchResultType?>(), null];

            foreach (MatchResultType? resultType in allResultTypesAndNone)
            {
                var matchInThePastWithTeams = _matchFactory.CreateMatchInThePast(true, readOnlyTestData, nameof(MatchesInTheFuture));
                matchInThePastWithTeams.MatchResultType = resultType;
                matchInThePastWithTeams.UpdateMatchNameAutomatically = true;
                matchInThePastWithTeams.InningsOrderIsKnown = true;

                var homeTeam = matchInThePastWithTeams.Teams.Single(t => t.TeamRole == TeamRole.Home);
                var awayTeam = matchInThePastWithTeams.Teams.Single(t => t.TeamRole == TeamRole.Away);
                matchInThePastWithTeams.SetBattedFirst(homeTeam.MatchTeamId!.Value);
                homeTeam.BattedFirst = true;
                foreach (var innings in matchInThePastWithTeams.MatchInnings)
                {
                    innings.BattingMatchTeamId = innings.InningsOrderInMatch % 2 == 1 ? homeTeam.MatchTeamId : awayTeam.MatchTeamId;
                    innings.BowlingMatchTeamId = innings.InningsOrderInMatch % 2 == 0 ? homeTeam.MatchTeamId : awayTeam.MatchTeamId;
                }
                matches.Add(matchInThePastWithTeams);

                var matchInThePastWithTeamsAndFixedName = _matchFactory.CreateMatchInThePast(true, readOnlyTestData, nameof(MatchesInTheFuture));
                matchInThePastWithTeamsAndFixedName.MatchResultType = resultType;
                matchInThePastWithTeamsAndFixedName.UpdateMatchNameAutomatically = false;
                matchInThePastWithTeamsAndFixedName.InningsOrderIsKnown = false;
                matches.Add(matchInThePastWithTeamsAndFixedName);
            }

            return matches;
        }
    }
}
