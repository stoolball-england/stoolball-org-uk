﻿using System;
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

            if (before.Any(x => x.OverNumber < 1))
            {
                throw new ArgumentException("Over numbers must be 1 or greater", nameof(before));
            }

            if (after.Any(x => x.OverNumber < 1))
            {
                throw new ArgumentException("Over numbers must be 1 or greater", nameof(after));
            }

            var comparison = new BowlingScorecardComparison();

            var identitiesBefore = before.Select(x => x.Bowler.PlayerIdentityName).Distinct();
            var identitiesAfter = after.Select(x => x.Bowler.PlayerIdentityName).Distinct();
            comparison.PlayerIdentitiesAdded.AddRange(identitiesAfter.Where(x => !identitiesBefore.Contains(x)));
            comparison.PlayerIdentitiesRemoved.AddRange(identitiesBefore.Where(x => !identitiesAfter.Contains(x)));

            var overNumber = 1;
            Over overBefore = null, overAfter = null;
            do
            {
                try
                {
                    overBefore = before.SingleOrDefault(x => x.OverNumber == overNumber);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ArgumentException($"{nameof(before)} has multiple overs with OverNumber={overNumber}", nameof(before), ex);
                }

                try
                {
                    overAfter = after.SingleOrDefault(x => x.OverNumber == overNumber);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ArgumentException($"{nameof(after)} has multiple overs with OverNumber={overNumber}", nameof(after), ex);
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
                    if (overBefore.Bowler.ComparableName() != overAfter.Bowler.ComparableName() ||
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

                overNumber++;
            }
            while (overBefore != null || overAfter != null);

            return comparison;
        }
    }
}
