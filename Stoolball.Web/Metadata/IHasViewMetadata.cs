using Stoolball.Metadata;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Metadata
{
    public interface IHasViewMetadata
    {
        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        ViewMetadata Metadata { get; }

        /// <summary>
        /// Gets the photo that appears in the header of the site
        /// </summary>
        IPublishedContent HeaderPhoto { get; }
    }
}