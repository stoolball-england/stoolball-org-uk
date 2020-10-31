using Stoolball.Matches;
using System;
using System.Collections.Generic;
using Xunit;

namespace Stoolball.Tests.Matches
{
    public class MatchResultEvaluatorTests
    {
        [Theory]
        [InlineData(111, 110, MatchResultType.HomeWin)]
        [InlineData(111, 111, MatchResultType.Tie)]
        [InlineData(110, 111, MatchResultType.AwayWin)]
        [InlineData(111, null, null)]
        [InlineData(null, null, null)]
        [InlineData(null, 111, null)]
        public void Match_with_two_innings_returns_result_with_runs_or_null(int? homeRuns, int? awayRuns, MatchResultType? expectedResult)
        {
            var homeMatchTeamId = Guid.NewGuid();
            var awayMatchTeamId = Guid.NewGuid();
            var match = new Match
            {
                MatchInnings = new List<MatchInnings>
                {
                    new MatchInnings
                    {
                        BattingMatchTeamId = homeMatchTeamId,
                        Runs = homeRuns
                    },
                    new MatchInnings
                    {
                        BattingMatchTeamId = awayMatchTeamId,
                        Runs = awayRuns
                    }
                },
                Teams = new List<TeamInMatch> {
                    new TeamInMatch
                    {
                        MatchTeamId = homeMatchTeamId,
                        TeamRole = TeamRole.Home
                    },
                    new TeamInMatch
                    {
                        MatchTeamId = awayMatchTeamId,
                        TeamRole= TeamRole.Away
                    }
                }
            };
            var evaluator = new MatchResultEvaluator();

            var result = evaluator.EvaluateMatchResult(match);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(121, 110, 150, 160, MatchResultType.HomeWin)]
        [InlineData(112, 100, 99, 111, MatchResultType.Tie)]
        [InlineData(110, 121, 160, 151, MatchResultType.AwayWin)]
        public void Match_with_four_innings_with_runs_returns_result(int homeFirstInningsRuns, int awayFirstInningsRuns, int homeSecondInningsRuns, int awaySecondInningsRuns, MatchResultType expectedResult)
        {
            var homeMatchTeamId = Guid.NewGuid();
            var awayMatchTeamId = Guid.NewGuid();
            var match = new Match
            {
                MatchInnings = new List<MatchInnings>
                {
                    new MatchInnings
                    {
                        BattingMatchTeamId = homeMatchTeamId,
                        Runs = homeFirstInningsRuns
                    },
                    new MatchInnings
                    {
                        BattingMatchTeamId = awayMatchTeamId,
                        Runs = awayFirstInningsRuns
                    },
                    new MatchInnings
                    {
                        BattingMatchTeamId = homeMatchTeamId,
                        Runs = homeSecondInningsRuns
                    },
                    new MatchInnings
                    {
                        BattingMatchTeamId = awayMatchTeamId,
                        Runs = awaySecondInningsRuns
                    }
                },
                Teams = new List<TeamInMatch> {
                    new TeamInMatch
                    {
                        MatchTeamId = homeMatchTeamId,
                        TeamRole = TeamRole.Home
                    },
                    new TeamInMatch
                    {
                        MatchTeamId = awayMatchTeamId,
                        TeamRole= TeamRole.Away
                    }
                }
            };
            var evaluator = new MatchResultEvaluator();

            var result = evaluator.EvaluateMatchResult(match);

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void Match_with_missing_innings_returns_null()
        {
            var homeMatchTeamId = Guid.NewGuid();
            var awayMatchTeamId = Guid.NewGuid();
            var match = new Match
            {
                MatchInnings = new List<MatchInnings>(),
                Teams = new List<TeamInMatch> {
                    new TeamInMatch
                    {
                        MatchTeamId = homeMatchTeamId,
                        TeamRole = TeamRole.Home
                    },
                    new TeamInMatch
                    {
                        MatchTeamId = awayMatchTeamId,
                        TeamRole= TeamRole.Away
                    }
                }
            };
            var evaluator = new MatchResultEvaluator();

            var result = evaluator.EvaluateMatchResult(match);

            Assert.Null(result);
        }

        [Fact]
        public void Match_with_missing_teams_returns_null()
        {
            var homeMatchTeamId = Guid.NewGuid();
            var awayMatchTeamId = Guid.NewGuid();
            var match = new Match
            {
                MatchInnings = new List<MatchInnings>
                {
                    new MatchInnings
                    {
                        BattingMatchTeamId = homeMatchTeamId,
                        Runs = 100
                    },
                    new MatchInnings
                    {
                        BattingMatchTeamId = awayMatchTeamId,
                        Runs = 100
                    }
                },
                Teams = new List<TeamInMatch>()
            };
            var evaluator = new MatchResultEvaluator();

            var result = evaluator.EvaluateMatchResult(match);

            Assert.Null(result);
        }
    }
}
