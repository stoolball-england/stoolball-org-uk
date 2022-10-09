using System;
using System.Text.RegularExpressions;

namespace Stoolball.SocialMedia
{
    public class YouTubeUrlNormaliser : IYouTubeUrlNormaliser
    {
        public bool TryNormaliseUrl(Uri urlToParse, out Uri? normalisedUrl)
        {
            if (urlToParse is null)
            {
                normalisedUrl = null;
                return false;
            }

            return TryNormaliseUrl(urlToParse.ToString(), out normalisedUrl);
        }

        public bool TryNormaliseUrl(string urlToParse, out Uri? normalisedUrl)
        {
            if (string.IsNullOrWhiteSpace(urlToParse))
            {
                normalisedUrl = null;
                return false;
            }

            var match = Regex.Match(urlToParse, @"(\/embed\/|\/watch\?v=|youtu.be\/)(?<VideoId>[A-Z0-9_]+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                normalisedUrl = null;
                return false;
            }

            normalisedUrl = new Uri("https://www.youtube-nocookie.com/embed/" + match.Groups["VideoId"].Value);
            return true;
        }
    }
}
