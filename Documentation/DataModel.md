# Stoolball data model

Stoolball data is represented by the following entities:

## Teams

A `Team` is the central entity used to represent what is generally thought of as a club or team. However a stoolball club isn't the only kind of `Team`. A `Team` can also be a school team, a county team, the England team or some friends or colleagues who just played a one-off match.

A team is based at one or more `MatchLocations` (its home ground or sports centre), and has many `Players`. It can be involved in one or more `Seasons` and `Matches`.

One special type of `Team` is a `TransientTeam`, which exists only for one `Match`. For example, a group of friends who enter a `Tournament` but never play again.

## Clubs

A `Club` represents a real-world stoolball club. A stoolball club might have several teams (for example, ladies, mixed and junior teams).

There are many things that happen in at club level in a stoolball club, but in the data model these are recorded at `Team` level. This is because the difference between `Clubs` and `Teams` is not easy to explain in the user interface, so `Clubs` are deliberately de-emphasised and are only a way to group `Teams` together.

## Schools

Similar to a `Club`, a `School` is a group of `Teams`. The teams belonging to a school might be year groups who play in class, a team representing the school at the School Games, or an after-school club.

## MatchLocations

A `MatchLocation` is anywhere where a stoolball match is played, which typically means a playing field or sports centre. A team is based at one or more `MatchLocations`, and a `Match` or `Tournament` takes place at a location.

## Matches

A `Match` takes place between _up to_ two `Teams`. (A practice `Match` might be just one team, and the final of a knockout competition might not have any teams, until they qualify for it.) It happens at a `MatchLocation` at a specific date and time, and it has a series of `MatchInnings` - usually only one per side, but there can be more, like a Test match in cricket.

A `Match` might stand alone, it might be part of a league or other `Competition`, or it might be part of a `Tournament`.

## Tournaments

A `Tournament` happens at a `MatchLocation` at a specific date and time, involves more than two `Teams` and is a container for several `Matches`.

## Competitions

A `Competition` is a group of teams that play together in a league, a knockout competition, or just in friendly matches. Despite the name, the teams can play just for fun rather than competitively.

A `Competition` typically goes on for many years, each represented by a `Season`. Things can change between `Seasons`, so the `Teams` and `Matches` that might be thought of as part of a league or competition actually belong to a `Season`.

## Seasons, SeasonPointsRules and SeasonPointsAdjustments

A `Season` is one year of a `Competition`. It is a group of `Teams` that play in a list of `Matches`. It has `SeasonPointsRules` determining how points are awarded, and `SeasonPointsAdjustments` to award bonus or deduct penalty points.

## Players and PlayerIdentities

A real-world player might play for several teams, and might play under different names (this commonly applies to women who marry), and two people with the same name may be the same person or they may not.

A `Player` is therefore represented by one or more `PlayerIdentities`, so that we can manage the different names and teams but also recognise which ones belong to the same person.

`PlayerIdentities` are created automatically from scorecards entered for `Matches`.

## PlayerInnings and Overs

`PlayerIdentities` are created automatically from scorecards entered for `Matches`. Scorecards are linked to `PlayerIdentities` in the following ways.

- A `PlayerInnings` is one player's (or rather, one `PlayerIdentity`'s) turn at the wicket when batting. It records details of that player's score and how they got out. This means it also records up to two other `PlayerIdentities` - the bowler and fielder credited with their dismissal.

  It might also record that they didn't bat, but this is important as it may be the only record of a player's involvement in a `Match`. There might also be more than one `PlayerInnings` for a `PlayerIdentity` in a single `MatchInnings` - in a friendly match, one person might be given two or more chances to bat.

- An `Over` records the bowling figures for a single over in a `MatchInnings` bowled by a `PlayerIdentity`. Together the `Overs` in a `MatchInnings` make up the bowling scorecard.

## Awards

`Awards` like player of the match or player of the season are awarded to `PlayerIdentities`. They might be common awards like player of the match, which we want to compare across all matches, or they might be scoped to a team or competition (for example, a team might present the Jane Smith award at the end of a season).

The scope of an `Award` is described by three things:

- `AwardForScope` describes when the award is presented, which must be at the end of a `Match`, `Tournament` or `Season`.
- `AwardByScope` describes which type of entity presents the award. This is `null` for common awards, and `Season`, `Club`, `Team` or `Tournament` for the more unique awards.
- `AwardBy` links the more unique `Awards` to the `Season`, `Club`, `Team` or `Tournament` that presents it.

When the award is presented to a `PlayerIdentity` it's treated as a `SeasonAward`, `TournamentAward` or `MatchAward` and its `AwardBy` scope is recorded along with the `PlayerIdentity` it is `AwardedTo`.
