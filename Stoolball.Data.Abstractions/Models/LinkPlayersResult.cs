using System;

namespace Stoolball.Data.Abstractions.Models
{
    public record LinkPlayersResult
    {
        public Guid PlayerIdForSourcePlayer { get; set; }
        public Guid? PreviousMemberKeyForSourcePlayer { get; set; }
        public required string PreviousRouteForSourcePlayer { get; set; }
        public Guid PlayerIdForTargetPlayer { get; set; }
        public Guid? PreviousMemberKeyForTargetPlayer { get; set; }
        public required string PreviousRouteForTargetPlayer { get; set; }
        public required string NewRouteForTargetPlayer { get; set; }
        public Guid? NewMemberKeyForTargetPlayer { get; set; }
    }
}
