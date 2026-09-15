namespace Stoolball.Testing.Factories
{
    public class SeasonFactory(OverSetFactory _overSetFactory)
    {
        /// <summary>
        /// Creates a faker for seasons in the past (or specified years).
        /// </summary>
        /// <param name="competition">The competition the seasons belong to.</param>
        /// <param name="fromYear">Fixes the season's start year instead of randomising it.</param>
        /// <param name="untilYear">Fixes the season's end year instead of randomising it.</param>
        /// <returns></returns>
        public Faker<Season> CreateFaker(Competition competition, int? fromYear = null, int? untilYear = null)
        {
            return new Faker<Season>()
                    .RuleFor(x => x.SeasonId, () => Guid.NewGuid())
                    .RuleFor(x => x.Competition, () => competition)
                    .RuleFor(x => x.FromYear, faker => fromYear ?? faker.Random.Int(2000, 2025))
                    .RuleFor(x => x.UntilYear, (faker, season) => untilYear ?? season.FromYear + faker.Random.Int(0, 1))
                    .RuleFor(x => x.SeasonRoute, (faker, season) => season.UntilYear > season.FromYear
                        ? $"{competition.CompetitionRoute}/{season.FromYear}-{season.UntilYear.ToString(CultureInfo.InvariantCulture).Substring(2)}"
                        : $"{competition.CompetitionRoute}/{season.FromYear}")
                    .RuleFor(x => x.ResultsTableType, faker => faker.Random.Enum<ResultsTableType>())
                    .RuleFor(x => x.EnableRunsScored, faker => faker.Random.Bool())
                    .RuleFor(x => x.EnableRunsConceded, faker => faker.Random.Bool())
                    .RuleFor(x => x.EnableBonusOrPenaltyRuns, faker => faker.Random.Bool())
                    .RuleFor(x => x.EnableLastPlayerBatsOn, faker => faker.Random.Bool())
                    .RuleFor(x => x.EnableTournaments, faker => faker.Random.Bool())
                    .RuleFor(x => x.MatchTypes, faker => new List<MatchType> { MatchType.LeagueMatch, MatchType.FriendlyMatch })
                    .RuleFor(x => x.DefaultOverSets, () => _overSetFactory.CreateFaker().Generate(1));
        }

        /// <summary>
        /// Creates a season with two teams, points rules and a points adjustment, for tests that need a season with everything populated.
        /// </summary>
        public Season CreateSeasonWithFullDetails(Competition competition, int fromYear, int untilYear, Team team1, Team team2)
        {
            var season = new Season
            {
                SeasonId = Guid.NewGuid(),
                Competition = competition,
                FromYear = fromYear,
                UntilYear = untilYear,
                SeasonRoute = competition?.CompetitionRoute + "/" + fromYear + "-" + untilYear,
                DefaultOverSets = _overSetFactory.CreateFaker().Generate(1),
                MatchTypes = new List<MatchType> { MatchType.LeagueMatch, MatchType.FriendlyMatch },
                EnableBonusOrPenaltyRuns = true,
                EnableLastPlayerBatsOn = true,
                EnableRunsConceded = true,
                EnableRunsScored = true,
                EnableTournaments = true,
                Introduction = "Introduction to the season",
                PlayersPerTeam = 12,
                Results = "Some description of results",
                ResultsTableType = ResultsTableType.LeagueTable,
                Teams = new List<TeamInSeason> {
                    new TeamInSeason { Team = team1 },
                    new TeamInSeason { Team = team2, WithdrawnDate = new DateTimeOffset(fromYear, 6, 1, 0, 0, 0, TimeSpan.FromHours(1)) }
                },
                PointsRules = new List<PointsRule> {
                    new PointsRule{ PointsRuleId = Guid.NewGuid(), MatchResultType = MatchResultType.HomeWin, HomePoints=2, AwayPoints = 0 },
                    new PointsRule{ PointsRuleId = Guid.NewGuid(), MatchResultType = MatchResultType.AwayWin, HomePoints=0, AwayPoints = 2 },
                    new PointsRule{ PointsRuleId = Guid.NewGuid(), MatchResultType = MatchResultType.Tie, HomePoints=1, AwayPoints = 1 },
                    new PointsRule{ PointsRuleId = Guid.NewGuid(), MatchResultType = MatchResultType.Cancelled, HomePoints=1, AwayPoints =1 },
                    new PointsRule{ PointsRuleId = Guid.NewGuid(), MatchResultType = MatchResultType.Postponed, HomePoints=0, AwayPoints = 0 },
                    new PointsRule{ PointsRuleId = Guid.NewGuid(), MatchResultType = MatchResultType.AbandonedDuringPlayAndCancelled, HomePoints=1, AwayPoints =1 },
                    new PointsRule{ PointsRuleId = Guid.NewGuid(), MatchResultType = MatchResultType.AbandonedDuringPlayAndPostponed, HomePoints=0, AwayPoints = 0 },
                    new PointsRule{ PointsRuleId = Guid.NewGuid(), MatchResultType = MatchResultType.AwayWinByForfeit, HomePoints=0, AwayPoints =2 },
                    new PointsRule{ PointsRuleId = Guid.NewGuid(), MatchResultType = MatchResultType.HomeWinByForfeit, HomePoints=2, AwayPoints =0 }
                },
                PointsAdjustments = new List<PointsAdjustment>
                {
                    new PointsAdjustment { PointsAdjustmentId = Guid.NewGuid(), Team = team1, Points = 2, Reason = "Testing" }
                }
            };

            foreach (var teamInSeason in season.Teams)
            {
                teamInSeason.Season = season;
                teamInSeason.Team?.Seasons.Add(teamInSeason);
            }

            return season;
        }
    }
}