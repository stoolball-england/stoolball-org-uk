﻿using Stoolball.Metadata;

namespace Umbraco.Web.PublishedModels
{
    public partial class StyleGuide : IHasViewMetadata
    {
        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        public ViewMetadata Metadata { get; set; } = new ViewMetadata();
    }
}