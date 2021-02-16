using System;
using System.Threading.Tasks;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches
{
    [Collection(IntegrationTestConstants.DataSourceIntegrationTestCollection)]
    public class SqlServerMatchListingDataSourceTests
    {
        private readonly SqlServerDataSourceFixture _databaseFixture;

        public SqlServerMatchListingDataSourceTests(SqlServerDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_matches_supports_no_filter()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_only_matches_filter()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_only_tournaments_filter()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_match_type()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filtering_matches_by_team()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filtering_matches_by_competition()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filtering_matches_by_season()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filtering_matches_by_match_location()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filtering_matches_from_date()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_before_date()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filtering_tournament_matches()
        {
            // Review usage - possibly combine with option in next test?
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filter_by_tournament()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filtering_tournaments_by_team()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filtering_tournaments_by_competition()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filtering_tournaments_by_season()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filtering_tournaments_by_match_location()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filtering_tournaments_from_date()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_total_matches_supports_filtering_tournaments_by_tournamentid()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_returns_match_fields()
        {
            // m.MatchName, m.MatchRoute, m.StartTime, m.StartTimeIsKnown, m.MatchType, m.PlayerType, m.MatchResultType,
            // NULL AS TournamentQualificationType, NULL AS SpacesInTournament, m.OrderInTournament
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_returns_match_teams()
        {
            // mt.TeamRole, mt.MatchTeamId, mt.TeamId
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_returns_match_runs_and_wickets()
        {
            // i.Runs, i.Wickets
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_returns_tournament_fields()
        {
            // ourney.TournamentName AS MatchName, tourney.TournamentRoute AS MatchRoute, tourney.StartTime, tourney.StartTimeIsKnown, 
            // NULL AS MatchType, tourney.PlayerType, NULL AS MatchResultType,
            //  tourney.QualificationType AS TournamentQualificationType, tourney.SpacesInTournament, NULL AS OrderInTournament
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_does_not_return_tournament_teams()
        {
            // mt.TeamRole, mt.MatchTeamId, mt.TeamId
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_sorts_by_start_time()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_no_filter()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_only_matches_filter()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_only_tournaments_filter()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_by_match_type()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filtering_matches_by_team()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filtering_matches_by_competition()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filtering_matches_by_season()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filtering_matches_by_match_location()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filtering_matches_from_date()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_before_date()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filtering_tournament_matches()
        {
            // Review usage - possibly combine with option in next test?
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filter_by_tournament()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filtering_tournaments_by_team()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filtering_tournaments_by_competition()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filtering_tournaments_by_season()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filtering_tournaments_by_match_location()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filtering_tournaments_from_date()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Read_match_listings_supports_filtering_tournaments_by_tournamentid()
        {
            throw new NotImplementedException();
        }
    }
}