using System;
using System.Collections.Generic;

namespace Stoolball.Web.Statistics.Models
{
    public class PlayerHeadingViewModel
    {
        public string? Heading { get; set; }
        public IEnumerable<string> AlternativeNames { get; set; } = Array.Empty<string>();
    }
}
