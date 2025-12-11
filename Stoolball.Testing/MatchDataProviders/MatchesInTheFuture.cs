using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IFakerFactory<OverSet> _oversetFaker;

        public MatchesInTheFuture(MatchFactory matchFactory, IFakerFactory<Team> teamFakerFactory, IFakerFactory<OverSet> oversetFakerFactory)
        {
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _teamFaker = teamFakerFactory.Create();
            _oversetFaker = oversetFakerFactory;
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
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings[0].OverSets = _oversetFaker.Create().Generate(2).ToList();
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings.Add(new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 4 });
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings[1].OverSets = _oversetFaker.Create().Generate(2).ToList();

            // Two-innings match with teams
            var twoInningsMatchInTheFutureWithTeams = _matchFactory.CreateMatchInThePast(true, readOnlyTestData, nameof(MatchesInTheFuture));
            twoInningsMatchInTheFutureWithTeams.StartTime = DateTimeOffset.UtcNow.AddMonths(1).UtcToUkTime();
            twoInningsMatchInTheFutureWithTeams.MatchInnings.Add(new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 3 });
            twoInningsMatchInTheFutureWithTeams.MatchInnings[0].OverSets = _oversetFaker.Create().Generate(2).ToList();
            twoInningsMatchInTheFutureWithTeams.MatchInnings.Add(new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 4 });
            twoInningsMatchInTheFutureWithTeams.MatchInnings[1].OverSets = _oversetFaker.Create().Generate(2).ToList();

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
