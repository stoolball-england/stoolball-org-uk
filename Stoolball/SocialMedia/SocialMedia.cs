namespace Stoolball.SocialMedia
{
    public class SocialMedia
    {
        public string DisplayName { get; set; } = "us";
        public string? Twitter { get; set; }

        public string? Facebook { get; set; }

        public string? Instagram { get; set; }

        public string? YouTube { get; set; }

        public bool HasSocialMedia()
        {
            return !string.IsNullOrWhiteSpace(Twitter) || !string.IsNullOrWhiteSpace(Facebook) || !string.IsNullOrWhiteSpace(Instagram) || !string.IsNullOrWhiteSpace(YouTube);
        }
    }
}
