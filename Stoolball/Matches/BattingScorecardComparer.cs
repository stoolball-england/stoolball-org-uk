using System;
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

            var playerInningsBefore = new List<PlayerInnings>(before);
            var playerInningsAfter = new List<PlayerInnings>(after);

            if (playerInningsBefore.Any(x => x.BattingPosition < 1))
            {
                throw new ArgumentException("Batting position must be 1 or greater", nameof(before));
            }

            if (playerInningsAfter.Any(x => x.BattingPosition < 1))
            {
                throw new ArgumentException("Batting position must be 1 or greater", nameof(after));
            }

            var comparison = new BattingScorecardComparison();
            playerInningsBefore = playerInningsBefore.Where(x => x.Batter != null && !string.IsNullOrWhiteSpace(x.Batter.PlayerIdentityName)).ToList();
            playerInningsAfter = playerInningsAfter.Where(x => x.Batter != null && !string.IsNullOrWhiteSpace(x.Batter.PlayerIdentityName)).ToList();

            var battingIdentitiesBefore = playerInningsBefore.Select(x => x.Batter!.PlayerIdentityName!).Distinct();
            var bowlingIdentitiesBefore = playerInningsBefore.SelectMany(x => new[] { x.DismissedBy?.PlayerIdentityName, x.Bowler?.PlayerIdentityName }.Where(name => !string.IsNullOrWhiteSpace(name))).Select(name => name!).Distinct();
            var battingIdentitiesAfter = playerInningsAfter.Select(x => x.Batter!.PlayerIdentityName!).Distinct();
            var bowlingIdentitiesAfter = playerInningsAfter.SelectMany(x => new[] { x.DismissedBy?.PlayerIdentityName, x.Bowler?.PlayerIdentityName }.Where(name => !string.IsNullOrWhiteSpace(name))).Select(name => name!).Distinct();
            comparison.BattingPlayerIdentitiesAdded.AddRange(battingIdentitiesAfter.Where(x => !battingIdentitiesBefore.Contains(x)));
            comparison.BowlingPlayerIdentitiesAdded.AddRange(bowlingIdentitiesAfter.Where(x => !bowlingIdentitiesBefore.Contains(x)));
            comparison.BattingPlayerIdentitiesRemoved.AddRange(battingIdentitiesBefore.Where(x => !battingIdentitiesAfter.Contains(x)));
            comparison.BowlingPlayerIdentitiesRemoved.AddRange(bowlingIdentitiesBefore.Where(x => !bowlingIdentitiesAfter.Contains(x)));
            comparison.BattingPlayerIdentitiesAffected.AddRange(comparison.BattingPlayerIdentitiesAdded);
            comparison.BattingPlayerIdentitiesAffected.AddRange(comparison.BattingPlayerIdentitiesRemoved);
            comparison.BowlingPlayerIdentitiesAffected.AddRange(comparison.BowlingPlayerIdentitiesAdded);
            comparison.BowlingPlayerIdentitiesAffected.AddRange(comparison.BowlingPlayerIdentitiesRemoved);

            var index = 0;
            PlayerInnings? inningsBefore = null, inningsAfter = null;
            do
            {
                inningsBefore = playerInningsBefore.Count > index ? playerInningsBefore[index] : null;

                try
                {
                    inningsAfter = playerInningsAfter.SingleOrDefault(x => x.BattingPosition == index + 1);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ArgumentException($"{nameof(after)} has multiple innings with BattingPosition={index + 1}", nameof(after), ex);
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
                    if (inningsBefore.BattingPosition != inningsAfter.BattingPosition ||
                        inningsBefore.Batter!.ComparableName() != inningsAfter.Batter!.ComparableName() ||
                        inningsBefore.DismissalType != inningsAfter.DismissalType ||
                        inningsBefore.DismissedBy?.ComparableName() != inningsAfter.DismissedBy?.ComparableName() ||
                        inningsBefore.Bowler?.ComparableName() != inningsAfter.Bowler?.ComparableName() ||
                        inningsBefore.RunsScored != inningsAfter.RunsScored ||
                        inningsBefore.BallsFaced != inningsAfter.BallsFaced)
                    {
                        comparison.PlayerInningsChanged.Add((inningsBefore, inningsAfter));

                        if (!comparison.BattingPlayerIdentitiesAffected.Contains(inningsBefore.Batter!.PlayerIdentityName!))
                        {
                            comparison.BattingPlayerIdentitiesAffected.Add(inningsBefore.Batter.PlayerIdentityName!);
                        }

                        if (inningsBefore.DismissedBy != null && !comparison.BowlingPlayerIdentitiesAffected.Contains(inningsBefore.DismissedBy.PlayerIdentityName))
                        {
                            comparison.BowlingPlayerIdentitiesAffected.Add(inningsBefore.DismissedBy.PlayerIdentityName);
                        }

                        if (inningsBefore.Bowler != null && !comparison.BowlingPlayerIdentitiesAffected.Contains(inningsBefore.Bowler.PlayerIdentityName))
                        {
                            comparison.BowlingPlayerIdentitiesAffected.Add(inningsBefore.Bowler.PlayerIdentityName);
                        }

                        if (!comparison.BattingPlayerIdentitiesAffected.Contains(inningsAfter.Batter!.PlayerIdentityName!))
                        {
                            comparison.BattingPlayerIdentitiesAffected.Add(inningsAfter.Batter.PlayerIdentityName!);
                        }

                        if (inningsAfter.DismissedBy != null && !comparison.BowlingPlayerIdentitiesAffected.Contains(inningsAfter.DismissedBy.PlayerIdentityName))
                        {
                            comparison.BowlingPlayerIdentitiesAffected.Add(inningsAfter.DismissedBy.PlayerIdentityName);
                        }

                        if (inningsAfter.Bowler != null && !comparison.BowlingPlayerIdentitiesAffected.Contains(inningsAfter.Bowler.PlayerIdentityName))
                        {
                            comparison.BowlingPlayerIdentitiesAffected.Add(inningsAfter.Bowler.PlayerIdentityName);
                        }
                    }
                    else
                    {
                        comparison.PlayerInningsUnchanged.Add((inningsBefore, inningsAfter));
                    }
                }

                index++;
            }
            while (inningsBefore != null || inningsAfter != null);

            return comparison;
        }
    }
}
