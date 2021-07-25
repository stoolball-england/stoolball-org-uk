namespace Stoolball.Html
{
    public interface IHtmlSanitizer
    {
        /// <summary>
        /// Sanitizes the specified HTML
        /// </summary>
        /// <returns>The sanitized HTML</returns>
        string Sanitize(string html);
    }
}
