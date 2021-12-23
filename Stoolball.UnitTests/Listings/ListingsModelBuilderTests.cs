using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Stoolball.Listings;
using Stoolball.Metadata;
using Stoolball.Navigation;
using Xunit;

namespace Stoolball.UnitTests.Listings
{
    public class ListingsModelBuilderTests
    {
        private const string PAGE_TITLE = "Page title";
        private readonly Uri PAGE_URL = new Uri("https://example.org/page");

        private class StubModel { public string Name { get; set; } }
        private class StubFilter : IListingsFilter
        {
            public string Query { get; set; }
            public Paging Paging { get; set; } = new Paging();
        }
        private class StubViewModel : IListingsModel<StubModel, StubFilter>
        {
            public List<StubModel> Listings { get; private set; } = new List<StubModel>();
            public StubFilter Filter { get; set; }
            public ViewMetadata Metadata { get; set; } = new ViewMetadata();
        }

        [Fact]
        public async Task Throws_ArgumentNullException_if_buildInitialState_is_null()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await listingsBuilder.BuildModel(null,
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Throws_ArgumentException_if_buildInitialState_returns_null()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            await Assert.ThrowsAsync<ArgumentException>(async () => await listingsBuilder.BuildModel(() => null,
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Throws_ArgumentException_if_buildInitialState_returns_a_null_filter()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            await Assert.ThrowsAsync<ArgumentException>(async () => await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = null },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Throws_ArgumentNullException_if_totalListings_is_null()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                null,
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Throws_ArgumentNullException_if_listings_is_null()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                null,
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Throws_ArgumentException_if_pageTitle_is_null_or_empty(string pageTitle)
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            await Assert.ThrowsAsync<ArgumentException>(async () => await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                pageTitle,
                PAGE_URL,
                new NameValueCollection()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Throws_ArgumentNullException_if_pageUrl_is_null()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                null,
                new NameValueCollection()).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Throws_ArgumentNullException_if_queryParameters_is_null()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                null).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Returns_model_from_buildInitialState()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();
            var initialModel = new StubViewModel { Filter = new StubFilter() };

            var result = await listingsBuilder.BuildModel(
                () => initialModel,
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection()
                ).ConfigureAwait(false);

            Assert.Equal(initialModel, result);
        }

        [Fact]
        public async Task Parses_page_number_from_querystring()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            var result = await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection() { { "page", "5" } }
                ).ConfigureAwait(false);

            Assert.Equal(5, result.Filter.Paging.PageNumber);
        }

        [Fact]
        public async Task Missing_page_number_defaults_to_1()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            var result = await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection()
                ).ConfigureAwait(false);

            Assert.Equal(1, result.Filter.Paging.PageNumber);
        }

        [Fact]
        public async Task Page_0_defaults_to_1()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            var result = await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection() { { "page", "0" } }
                ).ConfigureAwait(false);

            Assert.Equal(1, result.Filter.Paging.PageNumber);
        }

        [Fact]
        public async Task Negative_page_defaults_to_1()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            var result = await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection() { { "page", "-1" } }
                ).ConfigureAwait(false);

            Assert.Equal(1, result.Filter.Paging.PageNumber);
        }

        [Fact]
        public async Task Invalid_page_defaults_to_1()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            var result = await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection() { { "page", "invalid" } }
                ).ConfigureAwait(false);

            Assert.Equal(1, result.Filter.Paging.PageNumber);
        }

        [Fact]
        public async Task Page_size_set_to_default()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            var result = await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection() { { "size", "15" }, { "pagesize", "15" } }
                ).ConfigureAwait(false);

            Assert.Equal(Constants.Defaults.PageSize, result.Filter.Paging.PageSize);
        }

        [Fact]
        public async Task Page_URL_set_to_pageUrl_parameter()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            var result = await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection() { { "size", "15" }, { "pagesize", "15" } }
                ).ConfigureAwait(false);

            Assert.Equal(PAGE_URL, result.Filter.Paging.PageUrl);
        }

        [Fact]
        public async Task Total_results_set_to_result_of_totalListings_func()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();
            const int EXPECTED = 20;

            var result = await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(EXPECTED),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection() { { "size", "15" }, { "pagesize", "15" } }
                ).ConfigureAwait(false);

            Assert.Equal(EXPECTED, result.Filter.Paging.Total);
        }


        [Fact]
        public async Task Listings_set_to_result_of_listings_func()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();
            var EXPECTED = new List<StubModel> {
                new StubModel{ Name = "Model 1" },
                new StubModel{ Name = "Model 2" },
                new StubModel{ Name = "Model 3" }
            };

            var result = await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(EXPECTED.Count),
                x => Task.FromResult(new List<StubModel> { EXPECTED[0], EXPECTED[1], EXPECTED[2] }),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection() { { "size", "15" }, { "pagesize", "15" } }
                ).ConfigureAwait(false);

            Assert.Equal(EXPECTED.Count, result.Listings.Count);
            foreach (var listing in EXPECTED)
            {
                Assert.True(result.Listings.IndexOf(listing) > -1);
            }
        }

        [Fact]
        public async Task Reads_query_from_querystring_into_view_model()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            var result = await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection() { { "q", "example" } }
                ).ConfigureAwait(false);

            Assert.Equal("example", result.Filter.Query);
        }

        [Fact]
        public async Task Page_title_equals_page_title_parameter_if_no_query()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            var result = await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection()
                ).ConfigureAwait(false);

            Assert.Equal(PAGE_TITLE, result.Metadata.PageTitle);
        }

        [Fact]
        public async Task Appends_query_from_querystring_onto_page_title()
        {
            var listingsBuilder = new ListingsModelBuilder<StubModel, StubFilter, StubViewModel>();

            var result = await listingsBuilder.BuildModel(
                () => new StubViewModel { Filter = new StubFilter() },
                x => Task.FromResult(0),
                x => Task.FromResult(new List<StubModel>()),
                PAGE_TITLE,
                PAGE_URL,
                new NameValueCollection() { { "q", "example" } }
                ).ConfigureAwait(false);

            Assert.StartsWith(PAGE_TITLE, result.Metadata.PageTitle);
            Assert.Contains("example", result.Metadata.PageTitle, StringComparison.Ordinal);
        }

    }
}
