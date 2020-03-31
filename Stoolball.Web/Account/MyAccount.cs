using Stoolball.Metadata;

namespace Umbraco.Web.PublishedModels
{
    public partial class MyAccount : IHasViewMetadata
    {
        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        public ViewMetadata Metadata { get; set; } = new ViewMetadata();
    }
}