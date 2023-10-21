using System;

namespace Stoolball.Awards
{
    public class AwardNotFoundException : Exception
    {
        public AwardNotFoundException(string awardName) : base($"Award {awardName} was not found")
        {
        }
    }
}
