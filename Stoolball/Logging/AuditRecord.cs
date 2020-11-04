using System;

namespace Stoolball.Logging
{
    /// <summary>
    /// An audited change to an entity
    /// </summary>
    public class AuditRecord
    {
        /// <summary>
        /// Gets or sets the GUID identifier for the Umbraco Member taking the action
        /// </summary>
        public Guid? MemberKey { get; set; }

        /// <summary>
        /// The name of the Umbraco Member or automated process taking the action
        /// </summary>
        public string ActorName { get; set; }

        /// <summary>
        /// The action being taken on the audited entity
        /// </summary>
        public AuditAction Action { get; set; }

        /// <summary>
        /// The unique URI for the entity to which the audited change applies
        /// </summary>
        public Uri EntityUri { get; set; }

        /// <summary>
        /// The new state of the entity after the <see cref="Action"/> has been taken
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// The date the action was taken
        /// </summary>
        public DateTimeOffset AuditDate { get; set; }
    }
}
