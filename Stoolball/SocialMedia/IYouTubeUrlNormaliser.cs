using System;

namespace Stoolball.SocialMedia
{
    public interface IYouTubeUrlNormaliser
    {
        bool TryNormaliseUrl(Uri urlToParse, out Uri normalisedUrl);
        bool TryNormaliseUrl(string urlToParse, out Uri normalisedUrl);
    }
}