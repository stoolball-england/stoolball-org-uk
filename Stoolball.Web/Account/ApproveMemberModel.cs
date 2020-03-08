using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.Account
{
    public class ApproveMemberModel : ApproveMember
    {
        public ApproveMemberModel(IPublishedContent content) : base(content)
        {
        }

        /// <summary>
        /// Gets or sets whether a token was recognised, validated and resulted in approval of the member
        /// </summary>
        public bool MemberWasApproved { get; set; }

        /// <summary>
        /// Gets or sets the name of the newly-approved member
        /// </summary>
        public string MemberName { get; set; }
    }
}