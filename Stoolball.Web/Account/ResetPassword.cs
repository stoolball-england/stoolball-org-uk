using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Umbraco.Web.PublishedModels
{
	public partial class ResetPassword
	{
        /// <summary>
        /// Gets or sets a token presented in the URL
        /// </summary>
        public string PasswordResetToken { get; set; }

        /// <summary>
        /// Gets or sets whether a token was recognised, validated and matched to a member
        /// </summary>
        public bool PasswordResetTokenValid { get; set; }

        /// <summary>
        /// Gets or sets the name of the newly-approved member
        /// </summary>
        public string MemberName { get; set; }
    }
}