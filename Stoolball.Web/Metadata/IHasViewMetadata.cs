using System.Collections.Generic;
using Stoolball.Metadata;
using Stoolball.Navigation;
using Umbraco.Cms.Core.Models.PublishedContent;

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
        IPublishedContent HeaderPhotoWithInheritance();

        /// <summary>
        /// Gets the custom stylesheet that should be applied to the page (minus the .css extension)
        /// </summary>
        string Stylesheet { get; }

        /// <summary>
        /// Gets the pages to display in a breadcrumb trail
        /// </summary>
        /// <returns></returns>
        List<Breadcrumb> Breadcrumbs { get; }
    }
}