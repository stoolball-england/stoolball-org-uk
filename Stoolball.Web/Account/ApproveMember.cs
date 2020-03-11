using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.PublishedModels;

namespace Umbraco.Web.PublishedModels
{
    public partial class ApproveMember
    {
        /// <summary>
        /// Gets or sets whether a token was recognised, validated and matched to a member
        /// </summary>
        public bool ApprovalTokenValid { get; set; }

        /// <summary>
        /// Gets or sets the name of the newly-approved member
        /// </summary>
        public string MemberName { get; set; }
    }
}