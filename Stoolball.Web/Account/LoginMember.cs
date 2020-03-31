using Stoolball.Metadata;

namespace Umbraco.Web.PublishedModels
{
    public partial class LoginMember : IHasViewMetadata
    {
        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        public ViewMetadata Metadata { get; set; } = new ViewMetadata();
    }
}