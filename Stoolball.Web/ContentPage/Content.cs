using Stoolball.Metadata;
using Stoolball.Web.Metadata;

namespace Umbraco.Web.PublishedModels
{
    public partial class Content : IHasViewMetadata
    {
        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        public ViewMetadata Metadata { get; set; } = new ViewMetadata();
    }
}