using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using Stoolball.Web.Caching;
using Xunit;

namespace Stoolball.Web.UnitTests.Caching
{
    public class FeedOutputCachingTests
    {
        [Theory]
        [InlineData("/matches/rss", true)]
        [InlineData("/matches/ics/", true)]
        [InlineData("/competitions/example-league/matches/ICS", true)]
        [InlineData("/teams/example-team/matches/rss", true)]
        [InlineData("/tournaments/all/calendar.ics", true)]
        [InlineData("/matches", false)]
        [InlineData("/matches/example-match", false)]
        [InlineData("/players/example-player", false)]
        [InlineData("/news/rss-feeds-explained", false)]
        [InlineData("", false)]
        public void IsFeed_recognises_RSS_and_ICS_paths(string path, bool expected)
        {
            Assert.Equal(expected, FeedPaths.IsFeed(new PathString(path)));
        }

        private static RequestDelegate CreatePipeline(RequestDelegate endpoint)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOutputCache(options => options.AddBasePolicy(new FeedOutputCachePolicy()));
            var app = new ApplicationBuilder(services.BuildServiceProvider());
            app.UseFeedOutputCaching();
            app.Run(endpoint);
            return app.Build();
        }

        private static async Task<HttpContext> SendRequest(RequestDelegate pipeline, string path, string method = "GET", string? query = null)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("www.stoolball.org.uk");
            context.Request.Path = path;
            context.Request.QueryString = new QueryString(query);
            context.Response.Body = new MemoryStream();
            await pipeline(context);
            return context;
        }

        [Fact]
        public async Task Successful_feed_is_cached()
        {
            var calls = 0;
            var pipeline = CreatePipeline(async context => { calls++; await context.Response.WriteAsync("feed"); });

            await SendRequest(pipeline, "/matches/rss");
            var second = await SendRequest(pipeline, "/matches/rss");

            Assert.Equal(1, calls);
            Assert.Equal(StatusCodes.Status200OK, second.Response.StatusCode);
        }

        [Fact]
        public async Task Failing_feed_returns_503_and_is_cached()
        {
            var calls = 0;
            var pipeline = CreatePipeline(context => { calls++; throw new TimeoutException(); });

            var first = await SendRequest(pipeline, "/competitions/example/matches/ics");
            var second = await SendRequest(pipeline, "/competitions/example/matches/ics");

            Assert.Equal(1, calls);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, first.Response.StatusCode);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, second.Response.StatusCode);
            Assert.Equal(((int)FeedOutputCachePolicy.FailureExpiration.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture), second.Response.Headers.RetryAfter.ToString());
        }

        [Fact]
        public async Task Failing_feed_is_cached_for_failure_expiration()
        {
            OutputCacheContext? cacheContext = null;
            var pipeline = CreatePipeline(context => { cacheContext = context.Features.Get<IOutputCacheFeature>()!.Context; throw new TimeoutException(); });

            await SendRequest(pipeline, "/matches/rss");

            Assert.Equal(FeedOutputCachePolicy.FailureExpiration, cacheContext!.ResponseExpirationTimeSpan);
        }

        [Fact]
        public async Task Feeds_with_different_query_strings_are_cached_separately()
        {
            var calls = 0;
            var pipeline = CreatePipeline(async context => { calls++; await context.Response.WriteAsync("feed"); });

            await SendRequest(pipeline, "/matches/rss");
            await SendRequest(pipeline, "/matches/rss", query: "?format=tweet");

            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task Not_found_feed_is_not_cached()
        {
            var calls = 0;
            var pipeline = CreatePipeline(context => { calls++; context.Response.StatusCode = StatusCodes.Status404NotFound; return Task.CompletedTask; });

            await SendRequest(pipeline, "/teams/missing/matches/rss");
            await SendRequest(pipeline, "/teams/missing/matches/rss");

            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task Response_setting_a_cookie_is_not_cached()
        {
            var calls = 0;
            var pipeline = CreatePipeline(async context => { calls++; context.Response.Cookies.Append("example", "value"); await context.Response.WriteAsync("feed"); });

            await SendRequest(pipeline, "/matches/rss");
            await SendRequest(pipeline, "/matches/rss");

            Assert.Equal(2, calls);
        }

        [Theory]
        [InlineData("/players/example-player", "GET")]
        [InlineData("/matches/rss", "POST")]
        public async Task Other_requests_are_not_cached(string path, string method)
        {
            var calls = 0;
            var pipeline = CreatePipeline(async context => { calls++; await context.Response.WriteAsync("page"); });

            await SendRequest(pipeline, path, method);
            await SendRequest(pipeline, path, method);

            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task Exception_from_other_request_is_not_handled()
        {
            var pipeline = CreatePipeline(context => throw new TimeoutException());

            await Assert.ThrowsAsync<TimeoutException>(async () => await SendRequest(pipeline, "/players/example-player"));
        }
    }
}
