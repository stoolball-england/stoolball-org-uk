using Stoolball.Metadata;

namespace Umbraco.Web.PublishedModels
{
    public partial class Home : IHasViewMetadata
    {
        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        public ViewMetadata Metadata { get; set; } = new ViewMetadata();
    }
}