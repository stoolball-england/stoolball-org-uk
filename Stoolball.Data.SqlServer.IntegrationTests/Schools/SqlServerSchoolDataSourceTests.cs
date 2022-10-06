using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Navigation;
using Stoolball.Routing;
using Stoolball.Schools;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Schools
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerSchoolDataSourceTests
    {
        private readonly SqlServerTestDataFixture _databaseFixture;

        public SqlServerSchoolDataSourceTests(SqlServerTestDataFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_total_schools_supports_no_filter()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());

            var result = await schoolDataSource.ReadTotalSchools(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.Schools.Count, result);
        }

        private List<School> SchoolsMatchingQuery(string query)
        {
            return _databaseFixture.TestData.Schools.Where(x =>
                x.SchoolName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.Teams.Any(t => t.MatchLocations.Any(ml => ml.Locality != null && ml.Locality.Contains(query, StringComparison.OrdinalIgnoreCase))) ||
                x.Teams.Any(t => t.MatchLocations.Any(ml => ml.Town != null && ml.Town.Contains(query, StringComparison.OrdinalIgnoreCase))) ||
                x.Teams.Any(t => t.MatchLocations.Any(ml => ml.AdministrativeArea != null && ml.AdministrativeArea.Contains(query, StringComparison.OrdinalIgnoreCase)))
            ).ToList();
        }
        private string PartialNameOfAnySchool()
        {
            // Multiple words, starts with a capital, doesn't start with 'The'
            var school = _databaseFixture.TestData.Schools.First(x => Regex.IsMatch(x.SchoolName, "^(?!(THE|The|the))[A-Z].* .*[A-Za-z]"));

            // Return the first word of the name
            return school.SchoolName.Substring(0, school.SchoolName.IndexOf(" ", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Read_total_schools_supports_case_insensitive_filter_by_partial_name()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());
            var schoolName = PartialNameOfAnySchool();

            var query = new SchoolFilter { Query = schoolName.ToLower(CultureInfo.CurrentCulture) };

            var result = await schoolDataSource.ReadTotalSchools(query).ConfigureAwait(false);

            Assert.Equal(SchoolsMatchingQuery(query.Query).Count, result);
        }

        [Fact]
        public async Task Read_total_schools_supports_case_insensitive_filter_by_locality()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());

            var schoolWithLocality = _databaseFixture.TestData.Schools.First(x => x.Teams.Any(t => t.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.Locality))));
            var locality = schoolWithLocality.Teams.First(x => x.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.Locality))).MatchLocations.First(ml => !string.IsNullOrEmpty(ml.Locality)).Locality;

            var query = new SchoolFilter { Query = locality.ToLower(CultureInfo.CurrentCulture) };

            var result = await schoolDataSource.ReadTotalSchools(query).ConfigureAwait(false);

            Assert.Equal(SchoolsMatchingQuery(query.Query).Count, result);
        }

        [Fact]
        public async Task Read_total_schools_supports_case_insensitive_filter_by_town()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());

            var schoolWithTown = _databaseFixture.TestData.Schools.First(x => x.Teams.Any(t => t.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.Town))));
            var locality = schoolWithTown.Teams.First(x => x.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.Town))).MatchLocations.First(ml => !string.IsNullOrEmpty(ml.Town)).Town;

            var query = new SchoolFilter { Query = locality.ToLower(CultureInfo.CurrentCulture) };

            var result = await schoolDataSource.ReadTotalSchools(query).ConfigureAwait(false);

            Assert.Equal(SchoolsMatchingQuery(query.Query).Count, result);
        }

        [Fact]
        public async Task Read_total_schools_supports_case_insensitive_filter_by_administrative_area()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());

            var schoolWithAdministrativeArea = _databaseFixture.TestData.Schools.First(x => x.Teams.Any(t => t.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.AdministrativeArea))));
            var locality = schoolWithAdministrativeArea.Teams.First(x => x.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.AdministrativeArea))).MatchLocations.First(ml => !string.IsNullOrEmpty(ml.AdministrativeArea)).AdministrativeArea;

            var query = new SchoolFilter { Query = locality.ToLower(CultureInfo.CurrentCulture) };

            var result = await schoolDataSource.ReadTotalSchools(query).ConfigureAwait(false);

            Assert.Equal(SchoolsMatchingQuery(query.Query).Count, result);
        }

        [Fact]
        public async Task Read_schools_returns_basic_fields()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());

            var results = await schoolDataSource.ReadSchools(null).ConfigureAwait(false);

            foreach (var school in _databaseFixture.TestData.Schools)
            {
                var result = results.SingleOrDefault(x => x.SchoolId == school.SchoolId);

                Assert.NotNull(result);
                Assert.Equal(school.SchoolName, result!.SchoolName);
                Assert.Equal(school.SchoolRoute, result.SchoolRoute);
                Assert.Equal(school.UntilYear, result.UntilYear);
            }
        }

        [Fact]
        public async Task Read_schools_returns_basic_fields_for_teams()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());

            var results = await schoolDataSource.ReadSchools(null).ConfigureAwait(false);

            foreach (var school in _databaseFixture.TestData.Schools)
            {
                var schoolResult = results.SingleOrDefault(x => x.SchoolId == school.SchoolId);
                Assert.NotNull(schoolResult);
                Assert.Equal(school.Teams.Count, schoolResult!.Teams.Count);

                foreach (var team in school.Teams)
                {
                    var schoolTeamResult = schoolResult.Teams.SingleOrDefault(x => x.TeamId == team.TeamId);
                    Assert.NotNull(schoolTeamResult);

                    Assert.Equal(team.TeamType, schoolTeamResult!.TeamType);
                    Assert.Equal(team.UntilYear, schoolTeamResult.UntilYear);
                    Assert.Equal(team.PlayerType, schoolTeamResult.PlayerType);
                }
            }
        }

        [Fact]
        public async Task Read_schools_supports_no_filter()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());

            var result = await schoolDataSource.ReadSchools(new SchoolFilter { Paging = new Paging { PageSize = _databaseFixture.TestData.Schools.Count } }).ConfigureAwait(false);

            foreach (var school in _databaseFixture.TestData.Schools)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SchoolId == school.SchoolId));
            }
        }

        [Fact]
        public async Task Read_schools_supports_case_insensitive_filter_by_partial_name()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());
            var schoolName = PartialNameOfAnySchool();
            var query = new SchoolFilter
            {
                Query = schoolName.ToLower(CultureInfo.CurrentCulture),
                Paging = new Paging { PageSize = _databaseFixture.TestData.Schools.Count }
            };

            var result = await schoolDataSource.ReadSchools(query).ConfigureAwait(false);

            foreach (var school in SchoolsMatchingQuery(query.Query))
            {
                Assert.NotNull(result.Single(x => x.SchoolId == school.SchoolId));
            }
        }


        [Fact]
        public async Task Read_schools_supports_case_insensitive_filter_by_locality()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());
            var schoolWithLocality = _databaseFixture.TestData.Schools.First(x => x.Teams.Any(t => t.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.Locality))));
            var locality = schoolWithLocality.Teams.First(x => x.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.Locality))).MatchLocations.First(ml => !string.IsNullOrEmpty(ml.Locality)).Locality;
            var query = new SchoolFilter
            {
                Query = locality.ToLower(CultureInfo.CurrentCulture),
                Paging = new Paging { PageSize = _databaseFixture.TestData.Schools.Count }
            };

            var result = await schoolDataSource.ReadSchools(query).ConfigureAwait(false);

            foreach (var school in SchoolsMatchingQuery(query.Query))
            {
                Assert.NotNull(result.Single(x => x.SchoolId == school.SchoolId));
            }
        }

        [Fact]
        public async Task Read_schools_supports_case_insensitive_filter_by_town()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());
            var schoolWithLocality = _databaseFixture.TestData.Schools.First(x => x.Teams.Any(t => t.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.Town))));
            var town = schoolWithLocality.Teams.First(x => x.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.Town))).MatchLocations.First(ml => !string.IsNullOrEmpty(ml.Town)).Town;
            var query = new SchoolFilter
            {
                Query = town.ToLower(CultureInfo.CurrentCulture),
                Paging = new Paging { PageSize = _databaseFixture.TestData.Schools.Count }
            };

            var result = await schoolDataSource.ReadSchools(query).ConfigureAwait(false);

            foreach (var school in SchoolsMatchingQuery(query.Query))
            {
                Assert.NotNull(result.Single(x => x.SchoolId == school.SchoolId));
            }
        }

        [Fact]
        public async Task Read_schools_supports_case_insensitive_filter_by_administrative_area()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());
            var schoolWithLocality = _databaseFixture.TestData.Schools.First(x => x.Teams.Any(t => t.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.AdministrativeArea))));
            var administrativeArea = schoolWithLocality.Teams.First(x => x.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.AdministrativeArea))).MatchLocations.First(ml => !string.IsNullOrEmpty(ml.AdministrativeArea)).AdministrativeArea;
            var query = new SchoolFilter
            {
                Query = administrativeArea.ToLower(CultureInfo.CurrentCulture),
                Paging = new Paging { PageSize = _databaseFixture.TestData.Schools.Count }
            };

            var result = await schoolDataSource.ReadSchools(query).ConfigureAwait(false);

            foreach (var school in SchoolsMatchingQuery(query.Query))
            {
                Assert.NotNull(result.Single(x => x.SchoolId == school.SchoolId));
            }
        }

        [Fact]
        public async Task Read_schools_subsorts_by_name()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());

            var results = await schoolDataSource.ReadSchools(null).ConfigureAwait(false);

            // Active teams
            var sortedResults = new List<string>(results.Where(x => x.IsActive()).Select(x => x.SchoolName));
            sortedResults.Sort();
            var queue = new Queue<string>(sortedResults);

            foreach (var result in results)
            {
                if (!result.IsActive()) { continue; }

                Assert.Equal(queue.Dequeue(), result.SchoolName);
            }

            // Then inactive teams
            sortedResults = new List<string>(results.Where(x => !x.IsActive()).Select(x => x.SchoolName));
            sortedResults.Sort();
            queue = new Queue<string>(sortedResults);

            foreach (var result in results)
            {
                if (result.IsActive()) { continue; }

                Assert.Equal(queue.Dequeue(), result.SchoolName);
            }
        }

        [Fact]
        public async Task Read_schools_sorts_inactive_last()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());

            var results = await schoolDataSource.ReadSchools(null).ConfigureAwait(false);

            var expectedActiveStatus = true;
            foreach (var school in results)
            {
                // The first time a school that is not active is seen, set a flag to say they must all be inactive
                if (expectedActiveStatus && !school.IsActive())
                {
                    expectedActiveStatus = false;
                }
                Assert.Equal(expectedActiveStatus, school.IsActive());
            }
            Assert.False(expectedActiveStatus);
        }

        [Fact]
        public async Task Read_schools_pages_results()
        {
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, Mock.Of<IRouteNormaliser>());

            const int pageSize = 10;
            var pageNumber = 1;
            var remaining = _databaseFixture.TestData.Schools.Count;
            while (remaining > 0)
            {
                var result = await schoolDataSource.ReadSchools(new SchoolFilter { Paging = new Paging { PageNumber = pageNumber, PageSize = pageSize } }).ConfigureAwait(false);

                var expected = pageSize > remaining ? remaining : pageSize;
                Assert.Equal(expected, result.Count);

                pageNumber++;
                remaining -= pageSize;
            }
        }
    }
}
