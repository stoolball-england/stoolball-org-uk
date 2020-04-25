using System;
using System.Collections.Generic;

namespace Stoolball.Audit
{
    public interface IAuditable
    {
        Uri EntityUri { get; }
        List<AuditRecord> History { get; }
    }
}