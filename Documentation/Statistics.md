# Statistics

Stoolball is a fast-moving sport where even experienced scorers can miss a moment, so working out statistics from stoolball scorebooks inevitably means working with partial data and even data which does not add up.

## Most runs

The 'total innings' figure for most runs is calculated using player innings where runs scored were recorded. Innings are not counted where the player is recorded as playing (by a dismissal type other than `DidNotBat`) but no runs are recorded. However, those innings are included in the 'total matches' count.

## Batting average

Your batting average is the number of runs you've scored divided by the number of times you've been dismissed.

- You don't have an average until you've been dismissed at least once.
- Innings are not counted where you were dismissed but your runs were not recorded.
- An unknown dismissal method is counted as a dismissal.
- 'Best batting average' tables only show players with a minimum number of qualifying innings, to avoid skewing the tables with outlier results from players who have hardly played.

Batting average is calculated in three places:

- in `SqlServerPlayerSummaryStatisticsDataSource` for the summary statistics on a player's individual batting statistics.
- in `SqlServerBestPlayerAverageStatisticsDataSource` for the 'best batting average' tables.
- in `SqlServerBestPlayerTotalStatisticsDataSource` for the 'most runs scored' tables.

## Bowling average

Your bowling average is the number of wickets you've taken divided by the number of runs conceded from your overs, including extras.

- You don't have an average until you've taken at least one wicket.
- Innings are not counted where a bowler's wickets are recorded but their runs conceded are not.
- 'Best bowling average' tables only show players with a minimum number of qualifying innings, to avoid skewing the tables with outlier results from players who have hardly played.

It's calculated in three places:

- in `SqlServerPlayerSummaryStatisticsDataSource` for the summary statistics on a player's individual bowling statistics.
- in `SqlServerBestPlayerAverageStatisticsDataSource` for the 'best bowling average' tables.
- in `BowlingFiguresCalculator` for bowling figures in a match.
