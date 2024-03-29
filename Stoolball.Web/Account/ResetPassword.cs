﻿using System.Collections.Generic;
using Stoolball.Metadata;
using Stoolball.Web.Navigation;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Stoolball.Web.Models
{
    public partial class ResetPassword : IHasViewMetadata
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
        /// Gets or sets whether the request was received successfully and an email was sent
        /// </summary>
        public bool ShowPasswordResetRequested { get; set; }

        /// <summary>
        /// Gets or sets whether the password reset process has completed successfully
        /// </summary>
        public bool? ShowPasswordResetSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the email address the password reset is requested for
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        public ViewMetadata Metadata { get; set; } = new ViewMetadata();

        /// <summary>
        /// Gets the photo that appears in the header of the site
        /// </summary>
        public IPublishedContent HeaderPhotoWithInheritance() => (IPublishedContent)this.Value("headerPhoto", fallback: Fallback.ToAncestors);

        /// <inheritdoc/>
        public List<Breadcrumb> Breadcrumbs { get; } = new List<Breadcrumb>();
    }
}