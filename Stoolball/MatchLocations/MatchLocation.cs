using Stoolball.Teams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stoolball.MatchLocations
{
    public class MatchLocation
    {
        public int? MatchLocationId { get; set; }

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
        public DateTimeOffset? DateCreated { get; set; }
        public DateTimeOffset? DateUpdated { get; set; }
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
            if (!string.IsNullOrWhiteSpace(Town))
            {
                text.Append(", ");
                if (!string.IsNullOrWhiteSpace(Locality) && Town.StartsWith("Near ", StringComparison.OrdinalIgnoreCase))
                {
                    text.Append(Locality);
                }

                else
                {
                    text.Append(Town);
                }
            }
            return text.ToString();
        }
    }
}