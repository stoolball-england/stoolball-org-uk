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
        private readonly IFakerFactory<OverSet> _oversetFakerFactory;

        public MatchesInTheFuture(MatchFactory matchFactory, IFakerFactory<Team> teamFakerFactory, IFakerFactory<OverSet> oversetFakerFactory)
        {
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _teamFaker = teamFakerFactory.Create();
            _oversetFakerFactory = oversetFakerFactory ?? throw new ArgumentNullException(nameof(oversetFakerFactory));
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
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings[2].OverSets = _oversetFakerFactory.Create().Generate(2).ToList();
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings[2].BattingTeam = twoInningsMatchInTheFutureWithoutTeams.MatchInnings[0].BattingTeam;
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings[2].BattingMatchTeamId = twoInningsMatchInTheFutureWithoutTeams.MatchInnings[0].BattingMatchTeamId;
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings[2].BowlingTeam = twoInningsMatchInTheFutureWithoutTeams.MatchInnings[0].BowlingTeam;
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings[2].BowlingMatchTeamId = twoInningsMatchInTheFutureWithoutTeams.MatchInnings[0].BowlingMatchTeamId;
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings.Add(new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 4 });
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings[3].OverSets = _oversetFakerFactory.Create().Generate(2).ToList();
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings[3].BattingTeam = twoInningsMatchInTheFutureWithoutTeams.MatchInnings[1].BattingTeam;
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings[3].BattingMatchTeamId = twoInningsMatchInTheFutureWithoutTeams.MatchInnings[1].BattingMatchTeamId;
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings[3].BowlingTeam = twoInningsMatchInTheFutureWithoutTeams.MatchInnings[1].BowlingTeam;
            twoInningsMatchInTheFutureWithoutTeams.MatchInnings[3].BowlingMatchTeamId = twoInningsMatchInTheFutureWithoutTeams.MatchInnings[1].BowlingMatchTeamId;

            // Two-innings match with teams
            var twoInningsMatchInTheFutureWithTeams = _matchFactory.CreateMatchInThePast(true, readOnlyTestData, nameof(MatchesInTheFuture));
            twoInningsMatchInTheFutureWithTeams.StartTime = DateTimeOffset.UtcNow.AddMonths(1).UtcToUkTime();
            twoInningsMatchInTheFutureWithTeams.MatchInnings.Add(new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 3 });
            twoInningsMatchInTheFutureWithTeams.MatchInnings[0].OverSets = _oversetFakerFactory.Create().Generate(2).ToList();
            twoInningsMatchInTheFutureWithTeams.MatchInnings[2].BattingTeam = twoInningsMatchInTheFutureWithTeams.MatchInnings[0].BattingTeam;
            twoInningsMatchInTheFutureWithTeams.MatchInnings[2].BattingMatchTeamId = twoInningsMatchInTheFutureWithTeams.MatchInnings[0].BattingMatchTeamId;
            twoInningsMatchInTheFutureWithTeams.MatchInnings[2].BowlingTeam = twoInningsMatchInTheFutureWithTeams.MatchInnings[0].BowlingTeam;
            twoInningsMatchInTheFutureWithTeams.MatchInnings[2].BowlingMatchTeamId = twoInningsMatchInTheFutureWithTeams.MatchInnings[0].BowlingMatchTeamId;
            twoInningsMatchInTheFutureWithTeams.MatchInnings.Add(new MatchInnings { MatchInningsId = Guid.NewGuid(), InningsOrderInMatch = 4 });
            twoInningsMatchInTheFutureWithTeams.MatchInnings[1].OverSets = _oversetFakerFactory.Create().Generate(2).ToList();
            twoInningsMatchInTheFutureWithTeams.MatchInnings[3].BattingTeam = twoInningsMatchInTheFutureWithTeams.MatchInnings[1].BattingTeam;
            twoInningsMatchInTheFutureWithTeams.MatchInnings[3].BattingMatchTeamId = twoInningsMatchInTheFutureWithTeams.MatchInnings[1].BattingMatchTeamId;
            twoInningsMatchInTheFutureWithTeams.MatchInnings[3].BowlingTeam = twoInningsMatchInTheFutureWithTeams.MatchInnings[1].BowlingTeam;
            twoInningsMatchInTheFutureWithTeams.MatchInnings[3].BowlingMatchTeamId = twoInningsMatchInTheFutureWithTeams.MatchInnings[1].BowlingMatchTeamId;

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
