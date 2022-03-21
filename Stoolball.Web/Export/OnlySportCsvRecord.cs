using CsvHelper.Configuration.Attributes;

namespace Stoolball.Web.Export
{
    public class OnlySportCsvRecord
    {
        [Name("Match id"), Index(1)]
        public int MatchId { get; set; }

        [Name("Title"), Index(2)]
        public string? Title { get; set; }

        [Name("Start time"), Index(3)]
        public long StartTime { get; set; }

        [Name("Latitude"), Index(4)]
        public double? Latitude { get; set; }

        [Name("Longitude"), Index(5)]
        public double? Longitude { get; set; }

        [Name("Website"), Index(6)]
        public string? Website { get; set; }

        [Name("Description"), Index(7)]
        public string? Description { get; set; }
    }
}