# Players and player identities

A real-world player might play for several teams, and might play under different names (this commonly applies to women who marry), and two people with the same name may be the same person or they may not.

A player is therefore represented by one or more identities, so that we can manage the different names and teams but also recognise which ones belong to the same person.

## How identities are created and deleted

Identities are created automatically from scorecards entered for matches. When a scorecard is saved with a name we haven't seen before, a new player is created with a single identity, which is its default identity.

There is no method provided to create or delete an identity for a team except by adding them to a match, or removing them from all matches.

## Linking and unlinking identities

To link a second identity to one player, the source identity is attached to the target player, and the source player is deleted. Only the target player may be linked to a member account. Each player had a route which leads to their player profile page. The best route is chosen automatically and assigned to the target player, and the other one is redirected to the chosen route.

To unlink an identity from a player, a new player is created and the identity to unlink is attached to it. A new route will be generated for the new player.

## Players can link and unlink their own identities

Using the 'My statistics' feature, a registered member can link a player to their account. They can link identities to that player and unlink identities from it, subject to the following constraints:

- Linking identities from any team is allowed, so long as they're not already linked to another member's account. All identities are recorded as linked by the member.
- Unlinking identities is allowed, regardless of whether that link was added by the member themselves, their team or Stoolball England.
- Linking and unlinking identities for another member is not allowed.

### Link an identity to the authenticated member's account

| Player A before | Player A after |
|-|-|
| No `MemberKey` and a single `DefaultIdentity`. | Has a `MemberKey` and a single identity linked by `Member`. |
| No `MemberKey` and two identities linked by `Team`. | Has a `MemberKey` and two identities linked by `Member`. |

### Link another identity to the authenticated member's account

| Player A before | Player B before | Player A after | Player B after |
|-|-|-|-|
| Has `MemberKey` of the authenticated member, and a single identity linked by `Member`. | Has a single `DefaultIdentity`. | Has two identities, both linked by `Member`. `MemberKey` unchanged. | Identity moved to Player A. Player B deleted. |
| Has `MemberKey` __not__ matching the authenticated member, and a single identity linked by `Member`. | Has a single `DefaultIdentity`. | _Not allowed._ | _Not allowed._ |
| Has `MemberKey` of the authenticated member, and a single identity linked by `Member`. | Has two identities on the same team, both linked by `Team`. May be the same team as Identity A, or not.  | Has three identities, all linked by `Member`. `MemberKey` unchanged. | Both identities moved to Player A. Player B deleted.|
| Has `MemberKey` of the authenticated member, and a single identity linked by `Member`. | Has a `MemberKey` __not__ matching the authenticated member, and a single identity linked by `Member`. | _Not allowed._ | _Not allowed._ |

### Unlink an identity from the authenticated member's account

| Player A before | Player A after | Player B after |
|-|-|-|
| Has `MemberKey` of the authenticated member, and two identities linked by `Member`. | `MemberKey` unchanged. Has a single identity linked by `Member`. | `MemberKey` is `NULL`. Has a single `DefaultIdentity`. |
| Has `MemberKey` of the authenticated member, and a single identity linked by any value. | `MemberKey` is `NULL`. Has a single `DefaultIdentity`. | _Not applicable._
| Has `MemberKey` __not__ matching the authenticated member, and any number of identities linked by any value. | _Not allowed._ | _Not allowed._ |

## Team owners can link and unlink identities

If a member is in the member group for a team, they can manage that team including its players and identities. They can link and unlink identities, subject to the following constraints:

- Linking two players which are each linked to a member account is not allowed. This would break the 'My statistics' feature for one of the member accounts.
- The target player must have at least one identity on the same team as the source identity. This is because the authenticated member  does not have permissions to manage other teams. __(This is an assumption. A possible enhancement would be to test for permission. See [#674](https://github.com/stoolball-england/stoolball-org-uk/issues/674).)__
- If the target player is linked to a member account, ~~linking an identity is allowed provided that the other constraints are met.~~ __Linking to a player with a member account is not allowed unless the member account is the authenticated member. See [#675](https://github.com/stoolball-england/stoolball-org-uk/issues/675).__
- If the source identity is linked to a member account, ~~linking to a target player is allowed by swapping the roles of source and target,  provided that the other constraints are met.~~ __Linking to a player with a member account is not allowed unless the member account is the authenticated member. See [#675](https://github.com/stoolball-england/stoolball-org-uk/issues/675).__
- If the source identity belongs to a player with other identities, ~~linking is allowed provided that the other constraints are met.~~ __Linking to players with multiple identities is not allowed. See [#673](https://github.com/stoolball-england/stoolball-org-uk/issues/673).__
- If the target player is not linked to a member account, linking an identity is allowed provided that the other constraints are met.
- Identities which were linked by the team or by Stoolball England may be unlinked. Identities which were linked by the member may not.
- Unlinking identities is allowed, unless the identity was linked by a member using the 'My statistics' feature.
- The last identity for a player cannot be unlinked.

### Stoolball England administrators can link and unlink identities

Stoolball England administrators can link and unlink identities, subject to the same constraints as team owners.

### Link identities as a team owner

| Player A before | Player B before | Limits | Player A after | Player B after |
|-|-|-|-|-|
| Has a single `DefaultIdentity` on the owned team. | Has a single `DefaultIdentity` on the owned team. || Moved to Player B. Player A  deleted. | Has two identities, both linked by `Team`. `MemberKey` is `NULL`. |
| Has a single `DefaultIdentity` on the owned team. | Has a `MemberKey` and two identities linked by a `Member`. At least one identity is on the owned team. | ~~Member can cancel the operation.~~ __Not allowed. See [#675](https://github.com/stoolball-england/stoolball-org-uk/issues/675).__ | ~~Moved to Player B. Player A deleted.~~ __Not allowed.__ | ~~Has three identities, two linked by `Member`. The moved identity is linked by `Member` if they accepted, `Team` otherwise. `MemberKey` unchanged.~~ __Not allowed.__ |
| Has a single `DefaultIdentity` on the owned team. | Has two identities on the owned team, linked by `Team`. || Moved to Player B. Player A deleted. | Has three identities, all linked by `Team`. `MemberKey` is `NULL`. |
| Has a `MemberKey` and a single identity on the owned team, linked by `Member`. | Has a single `DefaultIdentity` on the owned team. | ~~Member can cancel the operation.~~ __Not allowed. See [#675](https://github.com/stoolball-england/stoolball-org-uk/issues/675).__ | ~~Player A has two identities, one linked by `Member`. The moved identity is linked by `Member` if they accepted, `Team` otherwise. `MemberKey` unchanged.~~ __Not allowed.__| ~~Moved to Player A. Player B deleted.~~ __Not allowed.__ |
| Has a `MemberKey` and a single identity on the owned team, linked by `Member`. | Has a `MemberKey` and a single identity on the owned team, linked by `Member`. | _Not allowed._ |||
| Has a `MemberKey` and a single identity on the owned team, linked by `Member`. | Has two identities on the owned team, both linked by `Team`. | ~~Member can cancel the operation.~~ __Not allowed. See [#675](https://github.com/stoolball-england/stoolball-org-uk/issues/675).__ | ~~Has three identities, one linked by `Member`. The moved identities are linked by `Member` if they accepted, `Team` otherwise. `MemberKey` unchanged.~~ __Not allowed.__ | ~~Moved to Player A. Player B deleted.~~ __Not allowed.__ |
| Has two identities on the owned team, both linked by `Team`. | On the owned team, with a single `DefaultIdentity`. || Has three identities, all linked by `Team`. | Moved to Player A. Player B deleted. |
| Has two identities on the owned team, both linked by `Team`. | Has a `MemberKey` and two identities. One identity is on the owned team and one on a different team, both linked by a `Member`. |~~Member can cancel the operation.~~ __Not allowed. See [#675](https://github.com/stoolball-england/stoolball-org-uk/issues/675).__ | ~~Moved to Player B. Player A deleted.~~ __Not allowed.__ | ~~Has four identities, two linked by `Member`. The moved identities are linked by `Member` if they accepted, `Team` otherwise. `MemberKey` unchanged.~~ __Not allowed.__ |
| Has two identities on the owned team, both linked by `Team`. | Has two identities on the owned team, both linked by `Team`. | __Not allowed. See [#673](https://github.com/stoolball-england/stoolball-org-uk/issues/673).__ | ~~Moved to Player B. Player A deleted.~~ __Not allowed.__ | ~~Has four identities, all linked by `Team`. `MemberKey` is `NULL`.~~  __Not allowed.__ |

### Unlink an identity as a team owner

| Player A before | Limits | Player A after | Player B after |
|-|-|-|-|
| Has `MemberKey` and two identities linked by `Member`. | _Not allowed._ |||
| Has `MemberKey`, one identity linked by `Member` and one identity linked by `Team`. The first one is unlinked. | ~~Member can cancel the operation.~~ __Not allowed. See [#675](https://github.com/stoolball-england/stoolball-org-uk/issues/675).__ | ~~`MemberKey` is `NULL`. Has a single `DefaultIdentity`.~~ __Not allowed.__ | ~~`MemberKey` is `NULL`. Has a single `DefaultIdentity`.~~ __Not allowed.__ |
| Has `MemberKey`, one identity linked by `Member` and one identity linked by `Team`. The second one is unlinked. || `MemberKey` unchanged. Has a single identity linked by `Member`. | `MemberKey` is `NULL`. Has a single `DefaultIdentity`. |
| Has three identities linked by `Team`. || Has two identities linked by `Team`. | Has a single `DefaultIdentity`. |
| Has two identities linked by `Team`. || Has a single `DefaultIdentity`. | Has a single `DefaultIdentity`. |
| Has a single `DefaultIdentity`. | _Not allowed._ |||

## Data structures

| Player.MemberKey | PlayerIdentity[0].LinkedBy | PlayerIdentity[n].LinkedBy | Meaning |
|-|-|-|-|
| `NULL` | `DefaultIdentity` | No second identity. | No linking has occurred. |
| `NOT NULL` | `Member` | `Member` | Member linked identities using 'My statistics'. |
| `NULL` | `Team` | `Team` | Team owners linked the identities. |
| `NULL` | `StoolballEngland` | `StoolballEngland` | Stoolball England administrators linked the identities. |
| `NULL` | `Team` | `StoolballEngland` | Team owners linked identities and Stoolball England administrators added more (or vice versa). |
| `NOT NULL` | `Member` | `Team` | Member linked identities using 'My statistics' and a team owner added more, which the member did not accept or reject when prompted. |
| `NOT NULL` | `Member` | `StoolballEngland` | Member linked identities using 'My statistics' and Stoolball England administrators added more, which the member did not accept or reject when prompted. |
