using Stoolball.Metadata;
using Stoolball.Web.Metadata;

namespace Umbraco.Web.PublishedModels
{
    public partial class ApproveMember : IHasViewMetadata
    {
        /// <summary>
        /// Gets or sets whether a token was recognised, validated and matched to a member
        /// </summary>
        public bool ApprovalTokenValid { get; set; }

        /// <summary>
        /// Gets or sets the name of the newly-approved member
        /// </summary>
        public string MemberName { get; set; }

        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        public ViewMetadata Metadata { get; set; } = new ViewMetadata();
    }
}