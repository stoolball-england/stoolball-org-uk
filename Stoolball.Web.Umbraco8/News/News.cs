using System.Collections.Generic;
using Stoolball.Metadata;
using Stoolball.Navigation;
using Stoolball.Web.Metadata;
using Umbraco.Core.Models.PublishedContent;

namespace Umbraco.Web.PublishedModels
{
    public partial class News : IHasViewMetadata
    {
        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        public ViewMetadata Metadata { get; set; } = new ViewMetadata();

        /// <summary>
        /// Gets the photo that appears in the header of the site
        /// </summary>
        public IPublishedContent HeaderPhotoWithInheritance() => this.Value("headerPhoto", fallback: Fallback.ToAncestors) as IPublishedContent;

        /// <inheritdoc/>
        public List<Breadcrumb> Breadcrumbs { get; } = new List<Breadcrumb>();

        public List<NewsStory> Stories { get; private set; } = new List<NewsStory>();

        public int TotalStories { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}