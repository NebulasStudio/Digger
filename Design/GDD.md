# Sandsunder — MVP Game Design Document

Status: `foundation-0.1`  
Ruleset: `mvp-0.1.0`  
Target: Windows/Steam, premium Early Access, controller-first  
Mode: one public six-player FFA competitive expedition PvPvE queue

## Product promise

**Experience formula:** The player feels like a daring desert opportunist because every dig changes what they know, what they can risk, and how close they are to stealing a non-lethal victory from five rivals.

Sandsunder is not marketed as a battle royale. A match is a short competitive expedition: players begin equal, excavate a build, survive escalating creatures, read rival intent, and converge on objectives that can be won without killing another player.

Design pillars:

1. **Every dig is a decision.** Digging costs time and exposes position, but reveals resources and routes.
2. **Danger and value share a map.** Inner rings pay better and punish earlier entry.
3. **Three credible victories.** Ritual, extraction, and survival remain live until one resolves.
4. **Readable improvisation.** Builds are shallow enough to parse at a glance and varied enough to change the next fight.

Primary engagement sources are execution and discovery. Social tension is produced by shared space, interruptions, visible objectives, and temporary non-binding coexistence; the MVP has no teams or item trading.

## Match contract

- Six seats, one winner, real-time top-down twin-stick play.
- Target median duration: 10–12 minutes. Sudden death begins at `14:00`; hard resolution occurs at `15:00`.
- All players start at 100 HP with the same basic shovel, no weapon, no utility, and one pre-centre respawn token.
- Account progression never changes match-start HP, damage, movement, drop odds, inventory size, or respawn count.
- The server owns simulation, map seed, hidden loot, hits, inventory, AI, objective progress, respawns, winner resolution, and rewards.
- A contender is alive or waiting for an available respawn. `Last Survivor` resolves only when exactly one contender remains.

### Timeline

| Time | State | Pressure and access |
|---|---|---|
| `00:00–00:30` | Deployment | Six symmetric outer spawns; PvP damage disabled for 10 seconds. |
| `00:30–03:00` | Scavenge | Outer and middle ring accessible; starter-cache protection and light base mobs. |
| `03:00–06:00` | Escalation I | Elite events and Sigil sources activate; spawn pressure increases. |
| `06:00–10:00` | Centre open | Centre gates and guardian activate; unused respawn tokens expire. |
| `10:00–14:00` | Escalation II | Faster spawns and stronger mobs; all victory routes fully active. |
| `14:00–15:00` | Sudden death | One extraction remains, channels shorten, no respawns, maximum mob pressure. |
| `15:00` | Hard resolution | Objective-milestone fallback selects exactly one winner. |

The first four eligible digs in each spawn sector use a server-side shuffled starter deck containing exactly one weapon. If a player still owns no weapon at `01:15`, their next valid dig becomes a basic weapon cache. This targets first weapon by 90 seconds in at least 95% of sessions without revealing buried contents to clients.

### Map

- Circular arena, approximately 160 metres in diameter, divided into six 60-degree sectors.
- Three concentric risk rings: outer `0.75x` loot value, middle `1.0x`, inner `1.25x`; rarity is capped by phase so an opening dig cannot roll endgame power.
- Each sector has equivalent path length, dig-node budget, cover budget, and one rotated prefab variant. Rotation changes routes, not expected value.
- Centre has four readable entrances, one guardian arena, two ritual stations, three extraction paths, and clear sightline blockers.
- Digging acts on one-metre logical cells. Three default shovel strikes excavate a normal cell. Visual sand deformation may interpolate, but collision and rewards change only on authoritative cell state transitions.
- Buried content is derived from the server seed and replicated only after reveal. Before reveal, clients receive diggability and visible surface tells, never item identity or rarity.

## Player verbs and agency metrics

Core verbs are move, aim, fire, shovel-strike, interact, use active ability, use utility, swap weapon, and inspect map/objective state.

| Metric | Foundation value |
|---|---:|
| Base movement | `5.2 m/s` |
| Player collision radius | `0.38 m` |
| Shovel reach | `1.4 m` |
| Basic shovel damage | `12` |
| Basic shovel cadence | `0.55 s` |
| Normal cell effort | `3 strikes` |
| Weapon slots | `2` |
| Utility slots | `2` |
| Interaction hold | `0.6 s` |
| Target client frame rate | `60 FPS` |
| Authoritative simulation | `30 Hz` |

These metrics are frozen before mass map or animation production. A proposed change requires an ADR plus revalidation of every dependent map clearance, animation, combat range, and time budget.

### Input

- Gamepad: left stick move, right stick aim, right trigger fire, left trigger active, south button shovel, west button interact/reload, shoulder buttons swap/use utility, view button map.
- Keyboard/mouse: physical `WASD` movement, pointer aim, left click fire, right click active, `Space` shovel, `E` interact/reload, number keys/scroll swap, `Q` utility, `Tab` map.
- All bindings are remappable. Simultaneous opposite movement axes resolve to zero; the most recently active aiming device owns aim until another device crosses its dead zone.
- Focus loss does not pause a network match. The local player stops sending held actions; the server times out stale input safely.

## Combat, loot, and death

- Damage is authoritative and never client-declared. Friendly-fire is irrelevant because the MVP has no teams.
- Base weapon outputs cluster around 24–32 sustained DPS before rarity and mechanical trade-offs. Projectiles, reloads, range, precision, telegraphs, and mobility create the option differences.
- Rarity multipliers are Common `1.00`, Refined `1.08`, Relic `1.16` to damage or the weapon's primary scalar; rarity never removes a weapon's counterplay.
- Loot uses finite server-side decks per sector and phase, not independent unconstrained rolls. Duplicate streaks are softened before content selection.
- Revealed pickups are public after a five-second revealer reservation. Sigils are personal, non-droppable, and non-tradable.
- A pre-`06:00` first death consumes the respawn token and returns the player after eight seconds at the safest valid outer spawn with shovel, 60 HP, and no duplicated inventory. Dropped inventory remains in the world.
- A death after token expiry is elimination. Disconnect keeps the avatar vulnerable for 20 seconds; reconnect resumes that state. After grace, the server eliminates the player and drops eligible inventory.

## Simultaneous victory routes

The match ends atomically on the first server tick that commits a valid victory. If multiple routes complete on the same tick, the lower deterministic match-seat priority established from the secret map seed wins; kill count is never a tie-breaker.

### Ritual Race

1. Earn three personal Sigils from three distinct PvE sources: buried shrine, ruin encounter, and elite contribution.
2. Activate both ritual stations with a three-second interruptible interaction; each activation persists for that player.
3. Complete an eight-second central channel. Player damage, displacement, leaving the ring, downing, or disconnect interrupts it without consuming Sigils.
4. In sudden death, station interactions take two seconds and the central channel takes five seconds.

### Relic Extraction

1. Defeat the centre guardian. Contribution is tracked for fallback scoring, but the relic is a single public pickup.
2. The carrier is revealed on map and world compass, moves 12% slower, cannot use movement actives, and drops the relic when eliminated or disconnected beyond grace.
3. Reach any declared extraction and channel for five seconds; damage, displacement, leaving, or dropping the relic interrupts it.
4. At `14:00`, two exits seal using a seeded selection announced 15 seconds beforehand; the remaining channel takes three seconds.

### Last Survivor

When only one contender remains, that player wins immediately. A player waiting on an available respawn token still counts as a contender.

### Hard-cap resolution

At `15:00`, rank players by:

1. unique objective milestones completed: three distinct Sigils, two distinct ritual stations, central-channel start, guardian contribution threshold, relic pickup, active-exit arrival;
2. earliest server tick of the player's most recent counted milestone;
3. deterministic seeded seat priority.

Eliminated players remain eligible for this fallback, preventing a final simultaneous PvE wipe from producing no result. Kills, damage to players, account level, and loot rarity are not tie-breakers.

## PvE escalation

- Base mobs teach contact avoidance, telegraphed ranged attacks, and burrow timing. The elite combines those patterns; the guardian examines movement, interruption, and add control.
- Active mob cap is 50. The director chooses only valid nav cells outside immediate player view and never spawns inside extraction or ritual channel zones.
- Director stages: light from `00:30`; standard from `03:00`; `+20% HP/+15% damage` from `06:00`; `+45% HP/+35% damage` from `10:00`; `+70% HP/+55% damage` from `14:00`.
- Guardian HP scales as `base_hp × (0.70 + 0.30 × active_contenders_at_spawn)` and then locks, so leaving or dying cannot heal or instantly collapse it.

Canonical foundation values live in `balance/*.csv`. Code must reference stable IDs and a `ruleset_version`; it must not duplicate balance constants.

## Characters and account progression

The MVP has six characters. Each has one passive, one active, and one shovel trait. All share 100 HP, 5.2 m/s base movement, identical inventory, and the same rarity curve. Character differences are sidegrades with explicit trade-offs; see `balance/characters.csv`.

Account persistence includes account XP, character mastery, cosmetics, challenges, and sidegrade unlock access. Matchmaking never considers paid ownership as power. No purchasable stat boosts, loot odds, respawns, inventory slots, or in-match currency enter the MVP.

Rewards are granted only from a signed, idempotent server result. Leaving early grants already-earned non-exploitable participation XP only after the match closes; reconnect never creates a second reward path.

## Information, interface, and accessibility

- Always visible: HP, active cooldown, current weapon/ammo, two utility slots, match clock, respawn availability, and concise objective state.
- Contextual: interaction progress, dig resistance, revealed loot reservation, carrier direction, station status, and sudden-death exit.
- Hidden until earned: buried item identity/rarity, unopened chest contents, other inventories, exact opponent cooldowns, and secret seed.
- Critical states use shape, motion, text/icon, and sound; colour is never the sole carrier.
- Required settings before play: remapping, text scale, colour-vision presets, screen shake, flash intensity, aim assist, vibration, subtitles, master/category audio, and hold/toggle alternatives.
- All player-visible strings are externalised and layout-tested for 30% expansion. No gameplay instruction depends on hover.

## Content boundary

MVP includes one modular map, six characters, ten weapons, four utilities, three base mobs, one elite, one guardian, one queue, a tutorial with bots, and internal custom lobbies for testing.

Explicitly excluded: item trading, gifting, manual drop, teams, ranked ladder, battle pass, mobile client, console certification, crossplay, user-generated content, voice chat, marketplace publication, and the remaining 50 roadmap characters.

## Outcome and acceptance

The foundation becomes an MVP candidate only when telemetry supports:

- first weapon by 90 seconds in at least 95% of matches;
- match duration p50 10–12 minutes and p90 at or below 15 minutes;
- at least 85% match completion and 98% sessions without crash or recorded desync;
- at least 25% of winners with zero player kills and kill leader winning under 50% of matches;
- each character's observed win rate within ±5 percentage points of expected after the sample-size gate is met;
- at least 70% of moderated testers choosing an immediate second match.

Balance changes alter one independent variable per experiment, increment `ruleset_version`, and retain the prior table snapshot for replay interpretation.
