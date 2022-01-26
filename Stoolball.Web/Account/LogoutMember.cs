using System.Collections.Generic;
using Stoolball.Metadata;
using Stoolball.Navigation;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Stoolball.Web.Models
{
    public partial class LogoutMember : IHasViewMetadata
    {
        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        public ViewMetadata Metadata { get; set; } = new ViewMetadata();

        /// <summary>
        /// Gets the photo that appears in the header of the site
        /// </summary>
        public IPublishedContent HeaderPhotoWithInheritance() => (IPublishedContent)this.Value("headerPhoto", fallback: Fallback.ToAncestors);

        /// <inheritdoc/>
        public List<Breadcrumb> Breadcrumbs { get; } = new List<Breadcrumb>();
    }
}