# Players and player identities

A real-world player might play for several teams, and might play under different names (this commonly applies to women who marry), and two people with the same name may be the same person or they may not.

A player is therefore represented by one or more identities, so that we can manage the different names and teams but also recognise which ones belong to the same person.

## How identities are created

Identities are created automatically from scorecards entered for matches. When a scorecard is saved with a name we haven't seen before, a new player is created with a single identity, which is its default identity.

There is no method provided to create or delete an identity for a team except by adding them to a match, or removing them from all matches.

## Linking and unlinking identities

To link a second identity to one player, the source identity is attached to the target player, and the source player is deleted. Each player had a route which leads to their player profile page. The best route is chosen automatically and assigned to the target player, and the other one is redirected to the chosen route.

To unlink an identity from a player, a new player is created and the identity to unlink is attached to it. A new route will be generated for the new player.

### Players can link and unlink their own identities

Using the 'My statistics' feature, a registered member can link a player to their account. They can link identities to that player and unlink identities from it, subject to the following constraints:

- Linking identities from any team is allowed, so long as they're not already linked to another member's account.
- Unlinking identities is allowed, regardless of whether that link was added by the member themselves, their team or Stoolball England.

### Team owners can link and unlink identities

If a member is in the member group for a team, they can manage that team including its players and identities. They can link and unlink identities, subject to the following constraints:

- Linking two players which are each linked to a member account is not allowed. This would break the 'My statistics' feature for one of the member accounts.
- The target player must have at least one identity on the same team as the source identity. This is because the member creating the link does not have permissions to manage other teams. (This is an assumption. A possible enhancement would be to test for permission.)
- If the target player is linked to a member account, ~~linking an identity is allowed provided that the other constraints are met.~~ __linking an identity is not allowed unless the member account is the current member, because the feature is not implemented.__
- If the source identity is linked to a member account, ~~linking to a target player is allowed provided that the other constraints are met.~~ __linking to a target player is not allowed unless the source identity's member account is the current member, because the feature is not implemented.__
- If the source identity belongs to a player with other identities, ~~linking is allowed provided that the other constraints are met.~~ __linking is not allowed because the feature is not implemented.__
- If the target player is not linked to a member account, linking an identity is allowed provided that the other constraints are met.
- Identities which were linked by the team or by Stoolball England may be unlinked. Identities which were linked by the member may not.
- Unlinking identities is allowed, unless the identity was linked by a member using the 'My statistics' feature.
- The last identity for a player cannot be unlinked.

### Stoolball England administrators can link and unlink identities

Stoolball England administrators can link and unlink identities, subject to the same constraints as team owners.

## Data structures

| Player.MemberKey | PlayerIdentity[0].LinkedBy | PlayerIdentity[n].LinkedBy | Meaning |
|-|-|-|-|
| `NULL` | `DefaultIdentity` | No second identity | No linking has occurred |
| `NOT NULL` | `Member` | `Member` | Member linked identities using 'My statistics' |
| `NULL` | `ClubOrTeam` | `ClubOrTeam` | Team owners linked the identities |
| `NULL` | `StoolballEngland` | `StoolballEngland` | Stoolball England administrators linked the identities |
| `NOT NULL` | `Member` | `ClubOrTeam` | Member linked identities using 'My statistics' and a team owner added more (or vice versa) |
| `NOT NULL` | `Member` | `StoolballEngland` | Member linked identities using 'My statistics' and Stoolball England administrators added more (or vice versa) |

## Scenarios

When linking a source identity to a target player, the source identity may be in the following states:

- on their own, the one default identity for their player
- linked by a member to other identities on the same team
- linked by a member to other identities on other teams
- linked by the team (or Stoolball England) to other identities on the same team

The target player may be in the following states:

- on their own, with one default identity
- have multiple identities on the same team, linked by a member
- have multiple identities on multiple teams, linked by a member
- have multiple identities on the same team, linked by the team or Stoolball England
- have multiple identities on multiple teams, some linked by a member and others by the team or Stoolball England

| Who makes the change | Identity A before | Player B before | Identity A  after | Player B after |
|-|-|-|-|-|
|`ClubOrTeam`| Player with a single `DefaultIdentity`. | Player with a single `DefaultIdentity`. | Moved to Player B as `ClubOrTeam`. Player A  deleted. | Has two identities, `MemberKey` is `NULL`. |
|`ClubOrTeam` | Player with a single `DefaultIdentity`. | Player with a `MemberKey` and two identities on the same team, linked by a `Member`. | Moved to Player B as `ClubOrTeam`, Player A deleted. | Has three identities, two still linked by `Member`. `MemberKey` unchanged. |
|`ClubOrTeam`|Player with a single `DefaultIdentity`. |Player with a `MemberKey` and two identities on different teams, linked by a `Member`.| Moved to player B as `ClubOrTeam`. Player A deleted. | Has three identities,  two still linked by `Member`. `MemberKey` unchanged.|
|`ClubOrTeam` | Player with a single `DefaultIdentity`. | Player with two identities on the same team, linked by `ClubOrTeam`. | Moved to Player B as `ClubOrTeam`. Player A deleted. | Has three identities, all linked by `ClubOrTeam`. `MemberKey` is `NULL`. |
|`ClubOrTeam` | Player with a `MemberKey` and a single identity linked by `Member`. | Player with a single `DefaultIdentity`. | Player A has two identities, one linked by `Member` and one by `ClubOrTeam`. `MemberKey` unchanged.| Identity moved to Player A as `ClubOrTeam`. Player B deleted. |
|`ClubOrTeam`| Player with a `MemberKey` and a single identity linked by `Member`. |Player with a `MemberKey` and a single identity linked by `Member`. | _Not allowed_ | _Not allowed_ |
|`ClubOrTeam` | Player with a `MemberKey` and a single identity linked by `Member`. | Player with two identities on the same team as Player A, both linked by `ClubOrTeam`. | Has three identities, one linked by `Member` and two by `ClubOrTeam`. `MemberKey` unchanged. | Both identities moved to Player A as `ClubOrTeam`. Player B deleted. |
|`ClubOrTeam` |  Player with two identities on the same team, both linked by `ClubOrTeam`. | Player on the same team, with a single `DefaultIdentity`. | Has three identities, all linked by `ClubOrTeam`. | Identity moved to Player A as `ClubOrTeam`, Player B deleted. |
|`ClubOrTeam` | Player with two identities on the same team, both linked by `ClubOrTeam`. | Player with a `MemberKey` and two identities on the same team, linked by a `Member`. | Both identities move to Player B, still linked by `ClubOrTeam`. Player A deleted. | Has four identities, two linked by `Member` and two by `ClubOrTeam`. `MemberKey` unchanged.|
|`ClubOrTeam` | Player with two identities on the same team, both linked by `ClubOrTeam`. | Player with a `MemberKey` and two identities. One identity is on the same team as Player A and one on a different team, both linked by a `Member`. | Both identities move to Player B as `ClubOrTeam`. Player A deleted. | Has four identities, two linked by `Member` and two by `ClubOrTeam`. `MemberKey` unchanged.|
|`ClubOrTeam` | Player with two identities on the same team, both linked by `ClubOrTeam`. | Player with two identities on the same team, both linked by `ClubOrTeam`. | Both identities move to Player B as `ClubOrTeam`, Player A deleted. | Has four identities, all linked by `ClubOrTeam`. |
|`Member` | Player with `MemberKey` of the member making the change, and a single identity linked by `Member`. | Player with a single `DefaultIdentity`. | Has two identities, both linked by `Member`. `MemberKey` unchanged. | Identity moved to Player A as `Member`. Player B deleted. |
|`Member` | Player with `MemberKey` of the member making the change, and a single identity linked by `Member`. | Player with two identities on the same team, both linked by `ClubOrTeam`. | Has three identities, one linked by `Member` and two by `ClubOrTeam`. `MemberKey` unchanged. | Both identities moved to Player A. Player B deleted.|
|`Member` | Player with `MemberKey` of the member making the change, and a single identity linked by `Member`. | Player with a `MemberKey`, and a single identity linked by `Member`. | _Not allowed_ | _Not allowed_ |
