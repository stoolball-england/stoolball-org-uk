using System;
using System.Collections.Generic;
using Bogus;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Testing.Fakers;
using Match = Stoolball.Matches.Match;

namespace Stoolball.Testing.MatchDataProviders
{
    internal class MatchesInTheFuture : BaseMatchDataProvider
    {
        private readonly MatchFactory _matchFactory;
        private readonly Faker<Team> _teamFaker;

        public MatchesInTheFuture(MatchFactory matchFactory, IFakerFactory<Team> teamFakerFactory)
        {
            _matchFactory = matchFactory ?? throw new System.ArgumentNullException(nameof(matchFactory));
            _teamFaker = teamFakerFactory.Create();
        }

        internal override IEnumerable<Match> CreateMatches(TestData readOnlyTestData)
        {
            // Minimal match with no teams
            var matchInTheFutureWithoutTeams = _matchFactory.CreateMatchInThePast(false, readOnlyTestData, nameof(MatchesInTheFuture));
            matchInTheFutureWithoutTeams.StartTime = DateTimeOffset.UtcNow.AddMonths(1).UtcToUkTime();

            // Minimal match with teams
            var matchInTheFutureWithTeams = _matchFactory.CreateMatchInThePast(true, readOnlyTestData, nameof(MatchesInTheFuture));
            matchInTheFutureWithTeams.StartTime = DateTimeOffset.UtcNow.AddMonths(1).UtcToUkTime();

            // Two-innings match with no teams
            var twoInningsMatchInTheFutureWithoutTeams = _matchFactory.CreateMatchInThePast(false, readOnlyTestData, nameof(MatchesInTheFuture));
            twoInningsMatchInTheFutureWithoutTeams.StartTime = DateTimeOffset.UtcNow.AddMonths(1).UtcToUkTime();
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings.Add(new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 3 });
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings.Add(new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 4 });

            // Two-innings match with no teams
            var twoInningsMatchInTheFutureWithTeams = _matchFactory.CreateMatchInThePast(true, readOnlyTestData, nameof(MatchesInTheFuture));
            twoInningsMatchInTheFutureWithTeams.StartTime = DateTimeOffset.UtcNow.AddMonths(1).UtcToUkTime();
            twoInningsMatchInTheFutureWithTeams.MatchInnings.Add(new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 3 });
            twoInningsMatchInTheFutureWithTeams.MatchInnings.Add(new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 4 });

            // Training session with teams
            var trainingSession = _matchFactory.CreateTrainingSessionInTheFuture(2, readOnlyTestData, nameof(MatchesInTheFuture));

            return [
                matchInTheFutureWithoutTeams,
                matchInTheFutureWithTeams,
                twoInningsMatchInTheFutureWithoutTeams,
                twoInningsMatchInTheFutureWithTeams,
                trainingSession
                ];
        }
    }
}
