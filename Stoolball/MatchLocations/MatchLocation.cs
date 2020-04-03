using System;

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

        public string MatchLocationRoute { get; set; }
        public DateTimeOffset? DateCreated { get; set; }
        public DateTimeOffset? DateUpdated { get; set; }
        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/match-location/{MatchLocationId}"); }
        }
    }
}