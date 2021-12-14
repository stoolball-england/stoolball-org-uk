using System.Collections.Generic;
using Stoolball.Metadata;
using Stoolball.Navigation;
using Stoolball.Web.Metadata;
using Umbraco.Core.Models.PublishedContent;

namespace Umbraco.Web.PublishedModels
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
        public IPublishedContent HeaderPhotoWithInheritance() => this.Value("headerPhoto", fallback: Fallback.ToAncestors) as IPublishedContent;

        /// <inheritdoc/>
        public List<Breadcrumb> Breadcrumbs { get; } = new List<Breadcrumb>();
    }
}