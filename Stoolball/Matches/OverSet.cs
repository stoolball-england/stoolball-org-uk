using System;
using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class OverSet
    {
        public Guid? OverSetId { get; set; }
        public int OverSetNumber { get; set; }
        public int? Overs { get; set; }
        public int? BallsPerOver { get; set; } = 8;

        public static OverSet ForOver(IEnumerable<OverSet> overSets, int overNumber)
        {
            if (overSets is null)
            {
                return null;
            }

            var over = 1;
            foreach (var set in overSets)
            {
                var totalOvers = over + set.Overs;
                do
                {
                    if (over == overNumber)
                    {
                        return set;
                    }
                    over++;
                } while (over < totalOvers);
            }

            return null;
        }
    }
}
