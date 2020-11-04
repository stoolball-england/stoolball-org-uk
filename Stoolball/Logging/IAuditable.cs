using System;
using System.Collections.Generic;

namespace Stoolball.Logging
{
    public interface IAuditable
    {
        Uri EntityUri { get; }
        List<AuditRecord> History { get; }
    }
}