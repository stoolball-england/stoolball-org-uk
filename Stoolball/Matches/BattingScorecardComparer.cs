﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Stoolball.Matches
{
    public class BattingScorecardComparer : IBattingScorecardComparer
    {
        public BattingScorecardComparison CompareScorecards(IEnumerable<PlayerInnings> before, IEnumerable<PlayerInnings> after)
        {
            if (before is null)
            {
                throw new ArgumentNullException(nameof(before));
            }

            if (after is null)
            {
                throw new ArgumentNullException(nameof(after));
            }

            if (before.Any(x => x.BattingPosition < 1))
            {
                throw new ArgumentException("Batting position must be 1 or greater", nameof(before));
            }

            if (after.Any(x => x.BattingPosition < 1))
            {
                throw new ArgumentException("Batting position must be 1 or greater", nameof(after));
            }

            var comparison = new BattingScorecardComparison();

            var battingIdentitiesBefore = before.Where(x => !string.IsNullOrWhiteSpace(x.Batter.PlayerIdentityName)).Select(x => x.Batter.PlayerIdentityName).Distinct();
            var bowlingIdentitiesBefore = before.Where(x => !string.IsNullOrWhiteSpace(x.Batter.PlayerIdentityName)).SelectMany(x => new[] { x.DismissedBy?.PlayerIdentityName, x.Bowler?.PlayerIdentityName }.Where(name => !string.IsNullOrWhiteSpace(name))).Distinct();
            var battingIdentitiesAfter = after.Where(x => !string.IsNullOrWhiteSpace(x.Batter.PlayerIdentityName)).Select(x => x.Batter.PlayerIdentityName).Distinct();
            var bowlingIdentitiesAfter = after.Where(x => !string.IsNullOrWhiteSpace(x.Batter.PlayerIdentityName)).SelectMany(x => new[] { x.DismissedBy?.PlayerIdentityName, x.Bowler?.PlayerIdentityName }.Where(name => !string.IsNullOrWhiteSpace(name))).Distinct();
            comparison.BattingPlayerIdentitiesAdded.AddRange(battingIdentitiesAfter.Where(x => !battingIdentitiesBefore.Contains(x)));
            comparison.BowlingPlayerIdentitiesAdded.AddRange(bowlingIdentitiesAfter.Where(x => !bowlingIdentitiesBefore.Contains(x)));
            comparison.BattingPlayerIdentitiesRemoved.AddRange(battingIdentitiesBefore.Where(x => !battingIdentitiesAfter.Contains(x)));
            comparison.BowlingPlayerIdentitiesRemoved.AddRange(bowlingIdentitiesBefore.Where(x => !bowlingIdentitiesAfter.Contains(x)));

            var battingPosition = 1;
            PlayerInnings inningsBefore = null, inningsAfter = null;
            do
            {
                try
                {
                    inningsBefore = before.Where(x => !string.IsNullOrWhiteSpace(x.Batter.PlayerIdentityName)).SingleOrDefault(x => x.BattingPosition == battingPosition);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ArgumentException($"{nameof(before)} has multiple innings with BattingPosition={battingPosition}", nameof(before), ex);
                }

                try
                {
                    inningsAfter = after.Where(x => !string.IsNullOrWhiteSpace(x.Batter.PlayerIdentityName)).SingleOrDefault(x => x.BattingPosition == battingPosition);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ArgumentException($"{nameof(after)} has multiple innings with BattingPosition={battingPosition}", nameof(after), ex);
                }

                if (inningsBefore != null && inningsAfter == null)
                {
                    comparison.PlayerInningsRemoved.Add(inningsBefore);
                }
                else if (inningsBefore == null && inningsAfter != null)
                {
                    comparison.PlayerInningsAdded.Add(inningsAfter);
                }
                else if (inningsBefore != null && inningsAfter != null)
                {
                    if (inningsBefore.Batter.ComparableName() != inningsAfter.Batter.ComparableName() ||
                        inningsBefore.DismissalType != inningsAfter.DismissalType ||
                        inningsBefore.DismissedBy?.ComparableName() != inningsAfter.DismissedBy?.ComparableName() ||
                        inningsBefore.Bowler?.ComparableName() != inningsAfter.Bowler?.ComparableName() ||
                        inningsBefore.RunsScored != inningsAfter.RunsScored ||
                        inningsBefore.BallsFaced != inningsAfter.BallsFaced)
                    {
                        comparison.PlayerInningsChanged.Add((inningsBefore, inningsAfter));

                        if (!comparison.BattingPlayerIdentitiesRemoved.Contains(inningsBefore.Batter.PlayerIdentityName) && !comparison.BattingPlayerIdentitiesAffected.Contains(inningsBefore.Batter.PlayerIdentityName))
                        {
                            comparison.BattingPlayerIdentitiesAffected.Add(inningsBefore.Batter.PlayerIdentityName);
                        }

                        if (inningsBefore.DismissedBy != null && !comparison.BowlingPlayerIdentitiesRemoved.Contains(inningsBefore.DismissedBy.PlayerIdentityName) && !comparison.BowlingPlayerIdentitiesAffected.Contains(inningsBefore.DismissedBy.PlayerIdentityName))
                        {
                            comparison.BowlingPlayerIdentitiesAffected.Add(inningsBefore.DismissedBy.PlayerIdentityName);
                        }

                        if (inningsBefore.Bowler != null && !comparison.BowlingPlayerIdentitiesRemoved.Contains(inningsBefore.Bowler.PlayerIdentityName) && !comparison.BowlingPlayerIdentitiesAffected.Contains(inningsBefore.Bowler.PlayerIdentityName))
                        {
                            comparison.BowlingPlayerIdentitiesAffected.Add(inningsBefore.Bowler.PlayerIdentityName);
                        }

                        if (!comparison.BattingPlayerIdentitiesAdded.Contains(inningsAfter.Batter.PlayerIdentityName) && !comparison.BattingPlayerIdentitiesAffected.Contains(inningsAfter.Batter.PlayerIdentityName))
                        {
                            comparison.BattingPlayerIdentitiesAffected.Add(inningsAfter.Batter.PlayerIdentityName);
                        }

                        if (inningsAfter.DismissedBy != null && !comparison.BowlingPlayerIdentitiesRemoved.Contains(inningsAfter.DismissedBy.PlayerIdentityName) && !comparison.BowlingPlayerIdentitiesAffected.Contains(inningsAfter.DismissedBy.PlayerIdentityName))
                        {
                            comparison.BowlingPlayerIdentitiesAffected.Add(inningsAfter.DismissedBy.PlayerIdentityName);
                        }

                        if (inningsAfter.Bowler != null && !comparison.BowlingPlayerIdentitiesRemoved.Contains(inningsAfter.Bowler.PlayerIdentityName) && !comparison.BowlingPlayerIdentitiesAffected.Contains(inningsAfter.Bowler.PlayerIdentityName))
                        {
                            comparison.BowlingPlayerIdentitiesAffected.Add(inningsAfter.Bowler.PlayerIdentityName);
                        }
                    }
                    else
                    {
                        comparison.PlayerInningsUnchanged.Add(inningsBefore);
                    }
                }

                battingPosition++;
            }
            while (inningsBefore != null || inningsAfter != null);

            return comparison;
        }
    }
}
