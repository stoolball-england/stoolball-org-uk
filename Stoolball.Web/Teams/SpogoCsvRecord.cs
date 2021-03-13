using CsvHelper.Configuration.Attributes;

namespace Stoolball.Web.Teams
{
    public class SpogoCsvRecord
    {

        [Name("Team id"), Index(1)]
        public int TeamId { get; set; }

        [Name("Team name"), Index(2)]
        public string TeamName { get; set; }

        [Name("Player type"), Index(3)]
        public string PlayerType { get; set; }

        [Name("Home ground name"), Index(4)]
        public string HomeGroundName { get; set; }

        [Name("Street name"), Index(5)]
        public string StreetName { get; set; }

        [Name("Locality"), Index(6)]
        public string Locality { get; set; }

        [Name("Town"), Index(7)]
        public string Town { get; set; }

        [Name("Administrative area"), Index(8)]
        public string AdministrativeArea { get; set; }

        [Name("Postcode"), Index(9)]
        public string Postcode { get; set; }

        [Name("Country"), Index(10)]
        public string Country { get; set; }

        [Name("Latitude"), Index(11)]
        public double? Latitude { get; set; }

        [Name("Longitude"), Index(12)]
        public double? Longitude { get; set; }

        [Name("Contact phone"), Index(13)]
        public string ContactPhone { get; set; }

        [Name("Contact email"), Index(14)]
        public string ContactEmail { get; set; }

        [Name("Website"), Index(15)]
        public string Website { get; set; }

        [Name("Description"), Index(16)]
        public string Description { get; set; }
    }
}