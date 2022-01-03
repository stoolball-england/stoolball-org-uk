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
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

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
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var schoolName = PartialNameOfAnySchool();

            var query = new SchoolFilter { Query = schoolName.ToLower(CultureInfo.CurrentCulture) };

            var result = await schoolDataSource.ReadTotalSchools(query).ConfigureAwait(false);

            Assert.Equal(SchoolsMatchingQuery(query.Query).Count, result);
        }

        [Fact]
        public async Task Read_total_schools_supports_case_insensitive_filter_by_locality()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var schoolWithLocality = _databaseFixture.TestData.Schools.First(x => x.Teams.Any(t => t.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.Locality))));
            var locality = schoolWithLocality.Teams.First(x => x.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.Locality))).MatchLocations.First(ml => !string.IsNullOrEmpty(ml.Locality)).Locality;

            var query = new SchoolFilter { Query = locality.ToLower(CultureInfo.CurrentCulture) };

            var result = await schoolDataSource.ReadTotalSchools(query).ConfigureAwait(false);

            Assert.Equal(SchoolsMatchingQuery(query.Query).Count, result);
        }

        [Fact]
        public async Task Read_total_schools_supports_case_insensitive_filter_by_town()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var schoolWithTown = _databaseFixture.TestData.Schools.First(x => x.Teams.Any(t => t.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.Town))));
            var locality = schoolWithTown.Teams.First(x => x.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.Town))).MatchLocations.First(ml => !string.IsNullOrEmpty(ml.Town)).Town;

            var query = new SchoolFilter { Query = locality.ToLower(CultureInfo.CurrentCulture) };

            var result = await schoolDataSource.ReadTotalSchools(query).ConfigureAwait(false);

            Assert.Equal(SchoolsMatchingQuery(query.Query).Count, result);
        }

        [Fact]
        public async Task Read_total_schools_supports_case_insensitive_filter_by_administrative_area()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var schoolWithAdministrativeArea = _databaseFixture.TestData.Schools.First(x => x.Teams.Any(t => t.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.AdministrativeArea))));
            var locality = schoolWithAdministrativeArea.Teams.First(x => x.MatchLocations.Any(ml => !string.IsNullOrEmpty(ml.AdministrativeArea))).MatchLocations.First(ml => !string.IsNullOrEmpty(ml.AdministrativeArea)).AdministrativeArea;

            var query = new SchoolFilter { Query = locality.ToLower(CultureInfo.CurrentCulture) };

            var result = await schoolDataSource.ReadTotalSchools(query).ConfigureAwait(false);

            Assert.Equal(SchoolsMatchingQuery(query.Query).Count, result);
        }

        [Fact]
        public async Task Read_schools_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await schoolDataSource.ReadSchools(new SchoolFilter { Paging = new Paging { PageSize = _databaseFixture.TestData.Schools.Count } }).ConfigureAwait(false);

            foreach (var school in _databaseFixture.TestData.Schools)
            {
                var result = results.SingleOrDefault(x => x.SchoolId == school.SchoolId);

                Assert.NotNull(result);
                Assert.Equal(school.SchoolName, result.SchoolName);
                Assert.Equal(school.SchoolRoute, result.SchoolRoute);
            }
        }

        [Fact]
        public async Task Read_schools_supports_no_filter()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await schoolDataSource.ReadSchools(new SchoolFilter { Paging = new Paging { PageSize = _databaseFixture.TestData.Schools.Count } }).ConfigureAwait(false);

            foreach (var school in _databaseFixture.TestData.Schools)
            {
                Assert.NotNull(result.SingleOrDefault(x => x.SchoolId == school.SchoolId));
            }
        }

        [Fact]
        public async Task Read_schools_supports_case_insensitive_filter_by_partial_name()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
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
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
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
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
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
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
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
        public async Task Read_schools_sorts_by_name()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await schoolDataSource.ReadSchools(new SchoolFilter { Paging = new Paging { PageSize = _databaseFixture.TestData.Schools.Count } }).ConfigureAwait(false);

            var sortedResults = new List<string>(results.Select(x => x.SchoolName));
            sortedResults.Sort();

            for (var i = 0; i < results.Count; i++)
            {
                Assert.Equal(sortedResults[i], results[i].SchoolName);
            }
        }


        [Fact]
        public async Task Read_schools_pages_results()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var schoolDataSource = new SqlServerSchoolDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

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
