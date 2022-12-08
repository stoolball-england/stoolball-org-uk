using System.Collections.Generic;
using Stoolball.Metadata;
using Stoolball.Web.Navigation;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Stoolball.Web.Models
{
    public partial class ConfirmEmailAddress : IHasViewMetadata
    {
        /// <summary>
        /// Gets or sets whether a token was recognised, validated and matched to a request for a new email address
        /// </summary>
        public bool TokenValid { get; set; }

        /// <summary>
        /// Gets or sets the name of the member requesting the change
        /// </summary>
        public string MemberName { get; set; }

        /// <summary>
        /// Gets the email address which has been confirmed
        /// </summary>
        public string EmailAddress { get; set; }

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