# ADR-0001: Runtime and service boundaries

- Status: Accepted as an MVP baseline; vendor versions and performance defaults remain validation targets
- Date: 2026-08-01
- Owners: Game architecture and platform architecture

## Context

Sandsunder needs a competitive six-player PvPvE loop that is secure against client manipulation, testable by a very small team, portable beyond Windows/Steam later, and inexpensive to operate before demand is known. Combat, digging, hidden loot, AI escalation, reconnect, and three simultaneous victory conditions make client or peer authority unsuitable.

The project also needs to avoid coupling core rules to any hosting, networking, identity, analytics, or storage vendor. Managed services reduce initial operational load, but only if their responsibilities and exit boundaries are explicit.

## Decision

- Use Unity 6 LTS with URP 2D for the Windows client and Linux headless dedicated server. Pin an exact verified patch in Unity manifests once the project is created.
- Keep pure domain types and deterministic simulation rules independent of Unity presentation and vendor SDKs.
- Use Photon Fusion 2 in Server Mode for production matches. The dedicated server owns simulation time, RNG, digging and loot disclosure, combat, AI, respawns, objectives, and winners.
- Place Linux server containers through Edgegap for the MVP, behind an `IServerAllocator` interface. A client receives only endpoint, transport, and a signed single-use ticket.
- Use Nakama Cloud backed by PostgreSQL as the canonical account/progression service. Only a trusted match server submits signed results; processing is idempotent and deduplicated by `match_id + account_id`.
- Use Cloudflare for DNS/WAF, peripheral HTTP services, CDN, and private/content-addressed object storage. Do not run the initial combat loop in Workers, Durable Objects, or Containers.
- Treat 30 Hz server/input ticks, 15–20 Hz snapshots, roughly 100 ms interpolation, and 150–200 ms rewind as spike hypotheses, not permanent contract values.
- Use versioned contracts and data catalogs; carry `match_id`, `build_id`, `ruleset_version`, and a server-only `map_seed` across the match lifecycle.

## Consequences

The project gains an authoritative trust boundary, managed operational starting point, testable simulation core, and explicit replacement seams for networking/hosting/platform integrations. It also accepts vendor costs, dedicated-server packaging work, multiple service integrations, and the need for robust local fakes and contract tests.

Provider-specific types must not leak into Domain or Simulation. A proposal that changes authority, exposes hidden state, lets clients award progression, or binds callers directly to Edgegap/Nakama/Cloudflare requires a superseding ADR and security review.

## Validation and revisit triggers

- Validate simulation determinism, six-client behavior under latency/jitter/loss, reconnect, hidden-loot secrecy, result idempotency, and server tick budget during the technical spike.
- Benchmark operational cost and placement quality before closed alpha.
- Revisit a vendor when measured reliability, geographic coverage, feature gaps, licensing, or projected cost fails an agreed threshold.
- Revisit cross-platform identity and anti-cheat before console beta; they are intentionally outside the vertical slice.

