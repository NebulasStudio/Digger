# Sandsunder Agent Instructions

These rules apply to every agent working anywhere in this repository. More specific nested `AGENTS.md` files may add constraints but must not weaken these safety, ownership, or architecture rules.

## Mission and current phase

Build Sandsunder incrementally from a verifiable technical foundation into a six-player authoritative PvPvE MVP. Favor small vertical slices, deterministic behavior, explicit contracts, and replaceable vendor adapters over speculative breadth.

The initial commercial target is Windows/Steam. Do not let mobile constraints shape the MVP. Steam Deck and future consoles are compatibility considerations, not current release commitments.

## Required workflow

1. Read the nearest instructions, relevant ADRs, manifests, and existing implementation before proposing or editing.
2. Discover applicable skills, project tools, tests, and vendor documentation progressively. Prefer current primary documentation for version-sensitive APIs.
3. State the exact files or subsystem you own before parallel implementation. Never edit a file owned by another active agent without coordinating first.
4. Preserve unrelated and user-authored changes. Never reset, revert, delete, or broadly reformat work you did not create.
5. Make the smallest coherent change that satisfies the task. Record cross-cutting or difficult-to-reverse decisions as ADRs.
6. Run focused tests first, then the broadest affordable deterministic verification. Report commands, results, and anything not verified.
7. Require an independent read-only review for security-sensitive, networking, economy, progression, release, or infrastructure changes before merge.

## Delegation and ownership

- Delegate only bounded tasks that can proceed independently. Use read-only agents for reconnaissance and review whenever possible.
- Give every writing agent exclusive path or subsystem ownership and remind it that other agents share the worktree.
- Do not recursively fan out unless the parent request explicitly requires it and ownership remains unambiguous.
- Return concise evidence: changed files, decisions, tests, risks, and blockers. The parent agent integrates results.
- No agent may deploy, publish, push, merge, submit store content, create paid resources, change DNS, or mutate a production service without explicit user approval at the moment of action.

## Architectural invariants

- Production matches use a dedicated authoritative server. Never ship competitive gameplay using host/shared authority.
- `Domain` and deterministic `Simulation` rules must not depend on Unity presentation, Photon, Steam, Nakama, Edgegap, Cloudflare, analytics, or other vendor SDKs.
- Hide provider details behind interfaces. In particular, server placement stays behind `IServerAllocator`, and clients receive only connection coordinates plus a single-use signed ticket.
- The server owns time, RNG seeds, digging outcomes, undiscovered loot, combat validation, AI, respawns, objectives, and winner selection.
- Do not replicate buried loot before discovery. Treat reconnect, ticket replay, duplicate result submission, input spam, and reward duplication as first-class failure cases.
- `MatchResult` processing is server-to-backend, signed, idempotent, and deduplicated by `match_id + account_id`. Clients never award account progression.
- Catalogs, balance curves, weapons, characters, loot, and rulesets are versioned data. Avoid scattered gameplay constants.
- Account progression is horizontal. Do not add permanent bonuses to damage, health, loot probability, or other competitive power.
- Trading, gifting, and manual item dropping are outside the MVP unless a later accepted ADR explicitly introduces them.

## Data, contracts, and observability

- Carry `match_id`, `build_id`, `ruleset_version`, and server-only `map_seed` through the match lifecycle. Never expose sensitive seed-derived state early.
- Version public schemas and keep generated artifacts reproducible. Do not hand-edit generated clients.
- Use structured logs with correlation identifiers and no tokens, credentials, personal data, or raw platform authentication payloads.
- Analytics events must be pseudonymous and schema-versioned. Crash reporting and gameplay telemetry must fail safely without changing simulation outcomes.
- Migrations must be forward-safe, reviewed, and accompanied by rollback or recovery notes before production use.

## Art and generative asset rules

- Follow the style bible and `Design/assets.csv` manifest when those files exist.
- Higgsfield output is concept/source material until a human reviews silhouette, readability, animation, visual collision language, licensing, and consistency.
- Record model, prompt, date, job or seed reference, source references, transformations, reviewer, and status for every generative asset considered for shipping.
- Do not imitate a living artist or an existing game/IP directly. Do not publish unreviewed output.

## Official Art Pipeline & Asset Handling (`Game/Assets/Sandsunder/Art/`)

Every agent operating on graphics or gameplay visuals MUST comply with the following structure and conventions:

1. **Folder Hierarchy (`Game/Assets/Sandsunder/Art/Runtime/`)**:
   - `Characters/`: Base character sprites 32x32 (e.g. `nomad_32.png`, `sorcerer_32.png`).
   - `Mobs/`: Enemy and neutral creature sprites (e.g. `mob_dune_spitter_32.png`).
   - `Weapons/`: Equippable 32x32 weapon icons (e.g. `rifle_brass_32.png`, `sword_scimitar_32.png`).
   - `Projectiles/`: Spells, bullets, and attack FX (e.g. `proj_sentinel_cyan_rune_32.png`).
   - `Environment/`: Interactive and static props (e.g. `env_palm_tree_32.png`, `env_relic_chest_32.png`).
   - `Terrain/`: Tilemaps & seamless ground textures (e.g. `sand_basecolor.png`, 256 PPU).
   - `Animations/`: Multi-frame animation sheets (e.g. `nomad_walk.png`, `nomad_run.png`, `nomad_dig.png`).
   - `UI/`: HUD and glassmorphic modal frames (e.g. `ui_glass_panel.png`).

2. **Nomad Character Invariance**:
   - Nomad character MUST ALWAYS use `nomad_32.png` as its base body sprite.
   - Nomad animations MUST use `nomad_*.png` clips ONLY (`Nomad_Walk`, `Nomad_Run`, `Nomad_Dig`, `Nomad_StealthCrouch`, `Nomad_Melee`, `Nomad_ShootRecoil`, `Nomad_Hurt`, `Nomad_Death`).
   - NEVER bind `wanderer_walk`, `explorer_dig`, `scout_run`, or `rogue_roll` to the Nomad.

3. **Weapon Separation**:
   - Player body sprite sheets MUST NOT render weapons inside the body frames.
   - Weapons are rendered on the separate `weaponRoot` transform via `WeaponAnimator` anchored at `X = ±0.08m`, `Y = 0.05m`.

4. **Manifest Registration**:
   - Every new animation sheet added to `Runtime/Animations/` MUST be registered in `Assets/Sandsunder/Art/Generated/AnimationBuildManifest.asset`.
   - Run `AnimationClipBuilder.BuildAll()` (or `Sandsunder > Art > Build Animation Clips From Manifest`) to generate `.anim` files.

5. **Importer Settings**:
   - PPU: `32` for all character, mob, weapon, and environment sprites; `256` for seamless ground tiles.
   - Texture Filter: `Point (no filter)`. Compression: `Uncompressed` / `RGBA32`. `alphaIsTransparency = true`.

## Quality gates

- Deterministic simulation tests must compare state hashes from identical seeds and inputs.
- Networking changes must cover latency, jitter, packet loss, reconciliation, reconnect, and abuse cases proportionate to the change.
- Economy/progression changes must test idempotency and duplication resistance.
- UI changes must remain legible with mouse/keyboard and controller, and should be verified at supported resolutions.
- Performance claims require measurements. Initial hypotheses such as 30 Hz server tick are defaults to validate, not facts to code around permanently.

## Security and repository hygiene

- Never commit secrets, private keys, API tokens, connection strings, proprietary SDK binaries, build artifacts, raw replays, or private player data.
- Do not weaken authentication, authorization, ticket validation, TLS, anti-cheat, or audit logging to simplify local development.
- Avoid destructive Git or filesystem operations. Ask before removing material user data or changing external state.
- Keep dependency and engine upgrades isolated, pinned, documented, and independently tested.

## Role routing

- `game_architect`: product rules, GDD boundaries, ADRs, and roadmap analysis.
- `unity_gameplay`: Unity client, deterministic gameplay implementation, UI/input, and editor tooling.
- `multiplayer_server`: Photon Fusion integration, authoritative server, prediction/reconciliation, replay, and anti-cheat boundaries.
- `backend_platform`: Nakama/PostgreSQL, contracts, Edgegap/Cloudflare adapters, and local infrastructure.
- `art_pipeline`: style system, Higgsfield workflow, provenance, import settings, and asset QA.
- `balance_analytics`: simulations, balance/economy analysis, telemetry definitions, and acceptance metrics.
- `qa_security_release`: read-only validation of correctness, security, performance evidence, compliance, and release readiness.

