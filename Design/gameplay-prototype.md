# Sandsunder local gameplay slice

Status: implementation target for `local-slice-0.1.0`; values marked provisional do not alter the `mvp-0.1.0` balance contract.

## Play loop

1. Move with WASD or the left stick and aim with the mouse or right stick.
2. Fire the projectile-pistol proxy with left mouse or right trigger.
3. Swing the shovel with Space or the gamepad south button. A swing damages one actor or advances one dig node.
4. Dodge with Left Shift, right mouse, or the gamepad east button. The short invulnerability window is smaller than the full roll.
5. Break a sand node with exactly three valid shovel strikes, then collect its newly revealed pickup by contact.
6. Defeat the three Dune Spitter proxies while avoiding their telegraphed projectiles.

## Frozen foundation values reused by the slice

| Rule | Value |
|---|---:|
| Player speed | 5.2 m/s |
| Player collision radius | 0.38 m |
| Player health | 100 |
| Shovel reach | 1.4 m |
| Shovel damage | 12 |
| Shovel cadence | 0.55 s |
| Normal dig effort | 3 strikes |
| Twin Fangs proxy damage | 6 |
| Twin Fangs proxy cadence | 5 shots/s |
| Twin Fangs proxy speed | 24 m/s |
| Twin Fangs proxy range | 11 m |
| Dune Spitter health | 55 |
| Dune Spitter speed | 3.2 m/s |
| Dune Spitter damage | 12 |
| Dune Spitter attack interval | 1.8 s |

## Provisional local-only values

| Rule | Value |
|---|---:|
| Roll distance / duration | 2.4 m / 0.30 s |
| Roll invulnerability | first 0.20 s |
| Roll cooldown | 1.25 s |
| Shovel arc | 90 degrees |
| Spitter telegraph | 0.35 s |
| Spitter projectile speed / lifetime | 7 m/s / 1.0 s |

These defaults require a later playtest decision and ADR before they become product rules.

## Architecture boundary

- `Simulation` owns versioned rules, cooldowns, damage, health, roll state, projectile ownership, dig progress, reveal and pickup idempotency without Unity or vendor dependencies.
- `Gameplay` converts local input into commands and projects simulation state, hitboxes and temporary proxy visuals.
- `Editor` only builds the repeatable Gameplay Lab scene and generated placeholder assets.
- Photon, matchmaking, PvP, inventory, ammunition, reload, loot reservation and account rewards remain out of this local slice.

The prototype must never reveal buried item identity before the third valid strike, and a pickup must be consumable at most once.
