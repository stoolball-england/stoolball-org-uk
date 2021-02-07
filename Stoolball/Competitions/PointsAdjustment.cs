using System;
using Stoolball.Teams;

namespace Stoolball.Competitions
{
    public class PointsAdjustment
    {
        public Guid? PointsAdjustmentId { get; set; }
        public Team Team { get; set; }
        public int Points { get; set; }
        public string Reason { get; set; }
    }
}