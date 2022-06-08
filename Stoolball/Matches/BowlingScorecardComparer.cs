using System;
using System.Collections.Generic;
using System.Linq;

namespace Stoolball.Matches
{
    public class BowlingScorecardComparer : IBowlingScorecardComparer
    {
        public BowlingScorecardComparison CompareScorecards(IEnumerable<Over> before, IEnumerable<Over> after)
        {
            if (before is null)
            {
                throw new ArgumentNullException(nameof(before));
            }

            if (after is null)
            {
                throw new ArgumentNullException(nameof(after));
            }

            var oversBefore = new List<Over>(before);
            var oversAfter = new List<Over>(after);

            if (oversBefore.Any(x => x.OverNumber < 1))
            {
                throw new ArgumentException("Over numbers must be 1 or greater", nameof(before));
            }

            if (oversAfter.Any(x => x.OverNumber < 1))
            {
                throw new ArgumentException("Over numbers must be 1 or greater", nameof(after));
            }

            var comparison = new BowlingScorecardComparison();

            var identitiesBefore = oversBefore.Select(x => x.Bowler.PlayerIdentityName).Distinct();
            var identitiesAfter = oversAfter.Select(x => x.Bowler.PlayerIdentityName).Distinct();
            comparison.PlayerIdentitiesAdded.AddRange(identitiesAfter.Where(x => !identitiesBefore.Contains(x)));
            comparison.PlayerIdentitiesRemoved.AddRange(identitiesBefore.Where(x => !identitiesAfter.Contains(x)));

            var index = 0;
            Over overBefore = null, overAfter = null;
            do
            {
                overBefore = oversBefore.Count > index ? oversBefore[index] : null;

                try
                {
                    overAfter = oversAfter.SingleOrDefault(x => x.OverNumber == index + 1);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ArgumentException($"{nameof(after)} has multiple overs with OverNumber={index + 1}", nameof(after), ex);
                }

                if (overBefore != null && overAfter == null)
                {
                    comparison.OversRemoved.Add(overBefore);
                }
                else if (overBefore == null && overAfter != null)
                {
                    comparison.OversAdded.Add(overAfter);
                }
                else if (overBefore != null && overAfter != null)
                {
                    if (overBefore.OverNumber != overAfter.OverNumber ||
                        overBefore.Bowler.ComparableName() != overAfter.Bowler.ComparableName() ||
                        overBefore.BallsBowled != overAfter.BallsBowled ||
                        overBefore.NoBalls != overAfter.NoBalls ||
                        overBefore.Wides != overAfter.Wides ||
                        overBefore.RunsConceded != overAfter.RunsConceded)
                    {
                        comparison.OversChanged.Add((overBefore, overAfter));

                        if (!comparison.PlayerIdentitiesRemoved.Contains(overBefore.Bowler.PlayerIdentityName) && !comparison.PlayerIdentitiesAffected.Contains(overBefore.Bowler.PlayerIdentityName))
                        {
                            comparison.PlayerIdentitiesAffected.Add(overBefore.Bowler.PlayerIdentityName);
                        }
                        if (!comparison.PlayerIdentitiesAdded.Contains(overAfter.Bowler.PlayerIdentityName) && !comparison.PlayerIdentitiesAffected.Contains(overAfter.Bowler.PlayerIdentityName))
                        {
                            comparison.PlayerIdentitiesAffected.Add(overAfter.Bowler.PlayerIdentityName);
                        }
                    }
                    else
                    {
                        comparison.OversUnchanged.Add(overBefore);
                    }
                }

                index++;
            }
            while (overBefore != null || overAfter != null);

            return comparison;
        }
    }
}
