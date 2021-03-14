using CsvHelper.Configuration.Attributes;

namespace Stoolball.Web.Matches
{
    public class WomensSportsNetworkCsvRecord
    {
        [Name("Match id"), Index(1)]
        public int MatchId { get; set; }

        [Name("Title"), Index(2)]
        public string Title { get; set; }

        [Name("Start time"), Index(3)]
        public string StartTime { get; set; }

        [Name("Match type"), Index(4)]
        public string MatchType { get; set; }

        [Name("Overs"), Index(5)]
        public int? Overs { get; set; }

        [Name("Player type"), Index(6)]
        public string PlayerType { get; set; }

        [Name("Players per team"), Index(7)]
        public int? PlayersPerTeam { get; set; }

        [Name("Latitude"), Index(8)]
        public double? Latitude { get; set; }

        [Name("Longitude"), Index(9)]
        public double? Longitude { get; set; }

        [Name("SAON"), Index(10)]
        public string SecondaryAddressableObjectName { get; set; }

        [Name("PAON"), Index(11)]
        public string PrimaryAddressableObjectName { get; set; }

        [Name("Town"), Index(12)]
        public string Town { get; set; }

        [Name("Website"), Index(13)]
        public string Website { get; set; }

        [Name("Description"), Index(14)]
        public string Description { get; set; }
    }
}