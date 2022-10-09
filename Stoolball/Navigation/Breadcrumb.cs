using System;
using System.Diagnostics.CodeAnalysis;

namespace Stoolball.Navigation
{
    [ExcludeFromCodeCoverage]
    public class Breadcrumb
    {
        public string? Name { get; set; }
        public Uri? Url { get; set; }
    }
}