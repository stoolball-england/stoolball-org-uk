using System;
using Stoolball.SocialMedia;
using Xunit;

namespace Stoolball.Tests.SocialMedia
{
    public class YouTubeUrlNormaliserTests
    {
        [Theory]
        [InlineData("https://www.youtube.com/embed/ITqskvM7HAw")]
        [InlineData("https://www.youtube-nocookie.com/embed/ITqskvM7HAw")]
        [InlineData("https://www.youtube.com/watch?v=ITqskvM7HAw")]
        [InlineData("https://youtu.be/ITqskvM7HAw")]
        public void Normalise_valid_URL_returns_no_cookie_URL(string originalUrl)
        {
            var normaliser = new YouTubeUrlNormaliser();
            var result = normaliser.TryNormaliseUrl(new Uri(originalUrl), out var normalisedUri);

            Assert.True(result);
            Assert.Equal("https://www.youtube-nocookie.com/embed/ITqskvM7HAw", normalisedUri.ToString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-url")]
        [InlineData("https://www.youtube.com")]
        [InlineData("https://example.org")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Testing for invalid URLs")]
        public void Invalid_URL_returns_false(string originalUrl)
        {
            var normaliser = new YouTubeUrlNormaliser();

            var result = normaliser.TryNormaliseUrl(originalUrl, out var normalisedUri);

            Assert.False(result);
        }
    }
}
