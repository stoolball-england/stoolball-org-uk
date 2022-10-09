using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class ResultsTableRow
    {
        public Team? Team { get; set; }
        public int Played { get; set; }
        public int Won { get; set; }
        public int Lost { get; set; }
        public int Tied { get; set; }
        public int NoResult { get; set; }
        public int RunsScored { get; set; }
        public int RunsConceded { get; set; }
        public int Points { get; set; }
    }
}