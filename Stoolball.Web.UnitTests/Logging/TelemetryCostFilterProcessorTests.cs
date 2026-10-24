using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Moq;
using Stoolball.Web.Logging;
using Xunit;

namespace Stoolball.Web.UnitTests.Logging
{
    public class TelemetryCostFilterProcessorTests
    {
        private readonly Mock<ITelemetryProcessor> _next = new();

        private static RequestTelemetry CreateRequest(string url, bool success)
        {
            return new RequestTelemetry { Url = new Uri(url), Success = success };
        }

        [Theory]
        [InlineData("https://www.stoolball.org.uk/.env")]
        [InlineData("https://www.stoolball.org.uk/.env.local?import&raw")]
        [InlineData("https://www.stoolball.org.uk/api/.env")]
        [InlineData("https://www.stoolball.org.uk/media../.env")]
        [InlineData("https://www.stoolball.org.uk/.git/config")]
        [InlineData("https://www.stoolball.org.uk/wp-content/uploads/2020/10/official-logo-512x512.png")]
        [InlineData("https://www.stoolball.org.uk/wp-admin/")]
        [InlineData("https://www.stoolball.org.uk/wp-includes/wlwmanifest.xml")]
        [InlineData("https://www.stoolball.org.uk/wp-json/batch/v1")]
        [InlineData("https://www.stoolball.org.uk/@fs/.env?raw")]
        [InlineData("https://www.stoolball.org.uk/__vite_rsc_findSourceMapURL")]
        [InlineData("https://www.stoolball.org.uk/ipfs/bafkreicyqcbhpicbos7ev4mrxofwqx6hvvge7pahpta6xuspr44crai5by")]
        [InlineData("https://www.stoolball.org.uk/.aws/credentials")]
        [InlineData("https://www.stoolball.org.uk/feed/")]
        [InlineData("https://www.stoolball.org.uk/wp-login.php")]
        [InlineData("https://www.stoolball.org.uk/Index.PHP?option=com_breezingforms")]
        public void Failed_request_for_scanner_path_is_dropped(string url)
        {
            var processor = new TelemetryCostFilterProcessor(_next.Object);

            processor.Process(CreateRequest(url, false));

            _next.Verify(x => x.Process(It.IsAny<ITelemetry>()), Times.Never);
        }

        [Theory]
        [InlineData("https://stoolball.org.uk/teams/bolney", true)]
        [InlineData("https://STOOLBALL.CO.UK/wp-json/batch/v1", true)]
        [InlineData("https://www.stoolball.co.uk/wp-content/themes/about.php", false)]
        public void Request_to_alternate_domain_is_dropped(string url, bool success)
        {
            var processor = new TelemetryCostFilterProcessor(_next.Object);

            processor.Process(CreateRequest(url, success));

            _next.Verify(x => x.Process(It.IsAny<ITelemetry>()), Times.Never);
        }

        [Theory]
        [InlineData("https://www.stoolball.org.uk/teams/bolney")]
        [InlineData("https://stoolball.euwest01.umbraco.io/teams/bolney")]
        public void Successful_request_to_other_domain_is_kept(string url)
        {
            var processor = new TelemetryCostFilterProcessor(_next.Object);
            var request = CreateRequest(url, true);

            processor.Process(request);

            _next.Verify(x => x.Process(request), Times.Once);
        }

        [Fact]
        public void Successful_request_for_scanner_path_is_kept()
        {
            var processor = new TelemetryCostFilterProcessor(_next.Object);
            var request = CreateRequest("https://www.stoolball.org.uk/.env", true);

            processor.Process(request);

            _next.Verify(x => x.Process(request), Times.Once);
        }

        [Theory]
        [InlineData("https://www.stoolball.org.uk/players/laurs")]
        [InlineData("https://www.stoolball.org.uk/admin")]
        [InlineData("https://www.stoolball.org.uk/environment")]
        [InlineData("https://www.stoolball.org.uk/feedback")]
        [InlineData("https://www.stoolball.org.uk/teams/php-stoolball-club")]
        public void Failed_request_for_other_path_is_kept(string url)
        {
            var processor = new TelemetryCostFilterProcessor(_next.Object);
            var request = CreateRequest(url, false);

            processor.Process(request);

            _next.Verify(x => x.Process(request), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Error_page_for_scanner_path_is_dropped(bool success)
        {
            var processor = new TelemetryCostFilterProcessor(_next.Object);

            processor.Process(CreateRequest("https://83204419-7b64-43ae-b47b-bc4f6d81ef41-1.azurewebsites.net/status/404.html?404;https://83204419-7b64-43ae-b47b-bc4f6d81ef41-1:80/wp-login.php", success));

            _next.Verify(x => x.Process(It.IsAny<ITelemetry>()), Times.Never);
        }

        [Fact]
        public void Error_page_for_wp_json_is_dropped()
        {
            var processor = new TelemetryCostFilterProcessor(_next.Object);

            processor.Process(CreateRequest("https://83204419-7b64-43ae-b47b-bc4f6d81ef41-1.azurewebsites.net/status/404.html?404;https://83204419-7b64-43ae-b47b-bc4f6d81ef41-1:80/wp-json/batch/v1", true));

            _next.Verify(x => x.Process(It.IsAny<ITelemetry>()), Times.Never);
        }

        [Theory]
        [InlineData("https://www.stoolball.org.uk/status/404.html?404;https://www.stoolball.org.uk:80/sitemap.xml")]
        [InlineData("https://www.stoolball.org.uk/status/404.html")]
        [InlineData("https://www.stoolball.org.uk/status/404.html?404;not-a-url")]
        public void Error_page_for_other_path_is_kept(string url)
        {
            var processor = new TelemetryCostFilterProcessor(_next.Object);
            var request = CreateRequest(url, true);

            processor.Process(request);

            _next.Verify(x => x.Process(request), Times.Once);
        }

        [Fact]
        public void Request_without_url_is_kept()
        {
            var processor = new TelemetryCostFilterProcessor(_next.Object);
            var request = new RequestTelemetry { Success = false };

            processor.Process(request);

            _next.Verify(x => x.Process(request), Times.Once);
        }

        [Theory]
        [InlineData("SQL")]
        [InlineData("sql")]
        public void Successful_SQL_dependency_is_dropped(string type)
        {
            var processor = new TelemetryCostFilterProcessor(_next.Object);

            processor.Process(new DependencyTelemetry { Type = type, Success = true });

            _next.Verify(x => x.Process(It.IsAny<ITelemetry>()), Times.Never);
        }

        [Fact]
        public void Failed_SQL_dependency_is_kept()
        {
            var processor = new TelemetryCostFilterProcessor(_next.Object);
            var dependency = new DependencyTelemetry { Type = "SQL", Success = false };

            processor.Process(dependency);

            _next.Verify(x => x.Process(dependency), Times.Once);
        }

        [Theory]
        [InlineData("HTTP")]
        [InlineData("Azure blob")]
        public void Successful_other_dependency_is_kept(string type)
        {
            var processor = new TelemetryCostFilterProcessor(_next.Object);
            var dependency = new DependencyTelemetry { Type = type, Success = true };

            processor.Process(dependency);

            _next.Verify(x => x.Process(dependency), Times.Once);
        }

        [Fact]
        public void Other_telemetry_is_kept()
        {
            var processor = new TelemetryCostFilterProcessor(_next.Object);
            var exception = new ExceptionTelemetry(new InvalidOperationException());

            processor.Process(exception);

            _next.Verify(x => x.Process(exception), Times.Once);
        }
    }
}
