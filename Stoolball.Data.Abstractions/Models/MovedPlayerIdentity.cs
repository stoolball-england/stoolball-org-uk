﻿using System;

namespace Stoolball.Data.Abstractions.Models
{
    public record MovedPlayerIdentity
    {
        public Guid PlayerIdentityId { get; set; }
        public Guid PlayerIdForSourcePlayer { get; set; }
        public Guid? MemberKeyForSourcePlayer { get; set; }
        public required string PreviousRouteForSourcePlayer { get; set; }
        public Guid PlayerIdForTargetPlayer { get; set; }
        public Guid? MemberKeyForTargetPlayer { get; set; }
        public required string PreviousRouteForTargetPlayer { get; set; }
        public required string NewRouteForTargetPlayer { get; set; }
    }
}