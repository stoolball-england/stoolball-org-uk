using Stoolball.Audit;
using Stoolball.Teams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stoolball.MatchLocations
{
    public class MatchLocation : IAuditable
    {
        public Guid? MatchLocationId { get; set; }

        public string SortName { get; set; }

        public string SecondaryAddressableObjectName { get; set; }

        public string PrimaryAddressableObjectName { get; set; }

        public string StreetDescription { get; set; }

        public string Locality { get; set; }

        public string Town { get; set; }

        public string AdministrativeArea { get; set; }

        public string Postcode { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public GeoPrecision? GeoPrecision { get; set; }

        public string MatchLocationNotes { get; set; }
        public List<Team> Teams { get; internal set; } = new List<Team>();

        public string MatchLocationRoute { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/match-location/{MatchLocationId}"); }
        }

        /// <summary>
        /// Gets the name of the ground and the town it's in
        /// </summary>
        public override string ToString()
        {
            var text = new StringBuilder(SecondaryAddressableObjectName);
            if (!string.IsNullOrWhiteSpace(PrimaryAddressableObjectName))
            {
                if (text.Length > 0) text.Append(", ");
                text.Append(PrimaryAddressableObjectName);
            }
            var placeName = LocalityOrTown();
            if (!string.IsNullOrWhiteSpace(placeName))
            {
                text.Append(", ").Append(placeName);
            }

            return text.ToString();
        }

        /// <summary>
        /// Get the most useful value of locality or town
        /// </summary>
        /// <returns></returns>
        public string LocalityOrTown()
        {
            if (!string.IsNullOrWhiteSpace(Town))
            {
                if (!string.IsNullOrWhiteSpace(Locality) && Town.StartsWith("Near ", StringComparison.OrdinalIgnoreCase))
                {
                    return Locality;
                }

                else
                {
                    return Town;
                }
            }
            else return Locality;
        }

        /// <summary>
        /// Gets a description of the match location suitable for including in metadata or search results
        /// </summary>
        public string Description()
        {
            var description = new StringBuilder(ToString());
            if (Teams?.Count > 0)
            {
                description.Append(" is home to ");
                for (var i = 0; i < Teams.Count; i++)
                {
                    description.Append(Teams[i].TeamName);
                    if (i < (Teams.Count - 2)) { description.Append(", "); };
                    if (i == (Teams.Count - 2)) { description.Append(" and "); };
                }
                description.Append(".");
            }
            else
            {
                description.Append(" is not currently home to any teams.");
            }
            return description.ToString();
        }
    }
}