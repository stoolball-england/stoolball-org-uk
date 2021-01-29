using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Stoolball.Logging;

namespace Stoolball.Schools
{
    public class School : IAuditable
    {
        public Guid? SchoolId { get; set; }
        public string SchoolName { get; set; }

        /// <summary>
        /// Gets the version of the school's name used to sort
        /// </summary>
        /// <returns></returns>
        public string ComparableName()
        {
            var comparable = SchoolName?.ToUpperInvariant() ?? string.Empty;
            if (comparable.StartsWith("THE ", StringComparison.Ordinal)) { comparable = comparable.Substring(4); }
            return (Regex.Replace(comparable, "[^A-Z0-9]", string.Empty));
        }
        public string Website { get; set; }
        public string Twitter { get; set; }
        public string Facebook { get; set; }
        public string Instagram { get; set; }
        public string YouTube { get; set; }
        public Guid? MemberGroupKey { get; set; }
        public string MemberGroupName { get; set; }
        public string SchoolRoute { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri {
            get { return new Uri($"https://www.stoolball.org.uk/id/school/{SchoolId}"); }
        }
    }
}