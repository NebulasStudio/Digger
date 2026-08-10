# ADR-0002: Sandbox dungeon input and match treasure

- Status: Accepted for the local gameplay sandbox; production multiplayer contracts remain pending
- Date: 2026-08-10
- Owners: Game architecture and Unity gameplay

## Context

The gameplay milestone requires two-stage excavation, discovered dungeon entrances, a contextual
enter/exit action, an adapted inventory on `Tab`, oxygen underground, and a final chest containing
gold. The foundation GDD previously reserved `Tab` for a map and excluded an in-match spendable
currency. Treating the requested chest reward as account currency or wiring it into the prototype
shop would violate horizontal progression and server-owned result processing.

## Decision

- `E` / gamepad west is the only contextual world action for entrances, exits, pickups and chests.
- Excavation depth and player location are separate states. The second shovel interaction reveals
  an entrance; it never moves the player underground automatically.
- The local sandbox assigns `Tab` / gamepad start to the adapted inventory. The map remains unbound
  until a later input-map decision avoids overlap.
- The final dungeon chest grants `treasure.gold` as idempotent, match-local, non-spendable score.
  It does not grant account progression, shop currency, competitive power, or backend inventory.
- Dungeon topology and match treasure state remain Unity-free deterministic data. Unity components
  only project floors, walls, guidance, silhouette, HUD and contextual interactions.

## Consequences

The sandbox can validate the full dig-to-dungeon loop without prematurely defining monetization or
account-economy contracts. A future production implementation must move entrance, movement, oxygen,
chest claim and reconnect snapshots behind dedicated-server commands and include their state in the
authoritative replay/result contract.

## Validation and revisit triggers

- Verify that the second dig creates one entrance and leaves oxygen untouched until `E` is pressed.
- Verify path confinement, chest idempotency, oxygen depletion/refill and modal input blocking.
- Revisit the map binding before adding a public map screen.
- A spendable form of gold, shop integration or account reward requires a superseding economy ADR,
  transactional backend contract and independent duplication/security review.
