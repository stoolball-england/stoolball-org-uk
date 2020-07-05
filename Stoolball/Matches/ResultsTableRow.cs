using Stoolball.Teams;
using System;

namespace Stoolball.Matches
{
    public class ResultsTableRow : IComparable<ResultsTableRow>
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

        public int CompareTo(ResultsTableRow other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (Points != other.Points)
            {
                return Points.CompareTo(other.Points) * -1;
            }
            else
            {
                return Played.CompareTo(other.Played) * -1;
            }
        }
    }
}