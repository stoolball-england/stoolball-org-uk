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

## Batting strike rate

Your batting strike rate is the average number of runs you score from 100 legitimate balls faced.

- Only innings with both runs scored and balls faced recorded are counted.
- 'Best batting strike rate' tables only show players with a minimum number of qualifying innings, to avoid skewing the tables with outlier results from players who have hardly played.

Batting strike rate is calculated in two places:

- in `SqlServerPlayerSummaryStatisticsDataSource` for the summary statistics on a player's individual bowling statistics.
- in `SqlServerBestPlayerAverageStatisticsDataSource` for the 'best batting strike rate' tables.

## Bowling average

Your bowling average is the number of wickets you've taken divided by the number of runs conceded from your overs, including extras.

- You don't have an average until you've taken at least one wicket.
- Innings are not counted where a bowler's wickets are recorded but their runs conceded are not.
- 'Best bowling average' tables only show players with a minimum number of qualifying innings, to avoid skewing the tables with outlier results from players who have hardly played.

It's calculated in three places:

- in `SqlServerPlayerSummaryStatisticsDataSource` for the summary statistics on a player's individual bowling statistics.
- in `SqlServerBestPlayerAverageStatisticsDataSource` for the 'best bowling average' tables.
- in `BowlingFiguresCalculator` for bowling figures in a match.

## Economy rate

Your economy rate is the average number of runs conceded for each over you bowl, including extras.

- Completed overs are assumed to be 8 balls.
- If runs conceded are recorded but not the number is balls bowled, a complete over is assumed.
- Innings are not counted where overs are recorded runs conceded are not.
- 'Best economy rate' tables only show players with a minimum number of qualifying innings, to avoid skewing the tables with outlier results from players who have hardly played.

It's calculated in three places:

- in `SqlServerPlayerSummaryStatisticsDataSource` for the summary statistics on a player's individual bowling statistics.
- in `SqlServerBestPlayerAverageStatisticsDataSource` for the 'best economy rate' tables.
- in `BowlingFiguresCalculator` for bowling figures in a match.

## Bowling strike rate

Your bowling strike rate is the average number of balls you bowl to take a wicket.

- You don't have an bowling strike rate until you've taken at least one wicket.
- Only innings with both overs bowled and wickets taken are counted.
- Because balls bowled is calculated from overs bowled, it assumes 8 balls per over.
- 'Best bowling strike rate' tables only show players with a minimum number of qualifying innings, to avoid skewing the tables with outlier results from players who have hardly played.

Bowling strike rate is calculated in three places:

- in `SqlServerPlayerSummaryStatisticsDataSource` for the summary statistics on a player's individual bowling statistics.
- in `SqlServerBestPlayerAverageStatisticsDataSource` for the 'best bowling strike rate' tables.
- in `BowlingFiguresCalculator` for bowling figures in a match.
