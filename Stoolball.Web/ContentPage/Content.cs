﻿using Stoolball.Metadata;
using Stoolball.Web.Metadata;
using Umbraco.Core.Models.PublishedContent;

namespace Umbraco.Web.PublishedModels
{
    public partial class Content : IHasViewMetadata
    {
        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        public ViewMetadata Metadata { get; set; } = new ViewMetadata();

        /// <summary>
        /// Gets the photo that appears in the header of the site
        /// </summary>
        public IPublishedContent HeaderPhotoWithInheritance() => this.Value("headerPhoto", fallback: Fallback.ToAncestors) as IPublishedContent;
    }
}