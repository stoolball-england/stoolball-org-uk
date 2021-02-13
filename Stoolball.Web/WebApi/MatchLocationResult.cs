using System.Collections.Generic;

namespace Stoolball.Web.WebApi
{
    public class MatchLocationResult
    {
        public string name { get; set; }
        public double? latitude { get; set; }
        public double? longitude { get; set; }

        public IEnumerable<TeamResult> teams { get; internal set; }
    }
}