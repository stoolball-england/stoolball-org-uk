using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class ResultsTableRow
    {
        public Team Team { get; set; }
        public int Played { get; set; } = 0;
        public int Won { get; set; } = 0;
        public int Lost { get; set; } = 0;
        public int Tied { get; set; } = 0;
        public int NoResult { get; set; } = 0;
        public int RunsScored { get; set; } = 0;
        public int RunsConceded { get; set; } = 0;
        public int Points { get; set; } = 0;
    }
}