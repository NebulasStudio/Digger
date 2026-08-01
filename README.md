# Sandsunder

Sandsunder is a six-player competitive PvPvE expedition game. Players enter a top-down desert arena with only a shovel, dig for equipment, survive escalating sand creatures, and race toward one of several non-equivalent victory conditions.

The repository is currently in the **foundation phase**. The working title, service vendors, performance defaults, and content counts remain subject to validation before a public announcement or production commitment.

## Product target

- Windows and Steam first, controller-first, with Steam Deck and future consoles considered in architectural boundaries.
- One authoritative six-player FFA queue for the MVP.
- Target match median of 10–12 minutes, hard stop at 15 minutes.
- Horizontal account progression; permanent power advantages are out of scope.
- Modern pixel-art presentation, with human-reviewed generative concepts and recorded provenance.

## Intended repository layout

| Area | Responsibility |
| --- | --- |
| `Game/` | Unity client, shared domain/simulation assemblies, presentation, networking adapters, platform adapters, and Linux dedicated server composition root. |
| `Backend/` | Nakama modules, PostgreSQL migrations, account progression, inventory ledger, and idempotent result processing. |
| `Contracts/` | Versioned OpenAPI/JSON Schema contracts and generated clients. |
| `Infra/` | Local containers, Edgegap packaging, Cloudflare configuration, and CI/CD definitions. |
| `Design/` | GDD, balance data, art direction, asset manifest, and provenance records. |
| `docs/adr/` | Accepted and proposed architecture decision records. |

Not every directory is expected to exist until its owning implementation work begins.

## Architecture baseline

- Unity 6 LTS with URP 2D for client and Linux headless builds.
- Photon Fusion 2 in dedicated Server Mode; the server owns simulation, RNG, loot disclosure, combat, and victory decisions.
- Edgegap behind an `IServerAllocator` boundary for placement and server allocation.
- Nakama Cloud/PostgreSQL for canonical identity, progression, inventory, and deduplicated match results.
- Cloudflare for edge HTTP concerns and private/content-addressed object storage, never the initial combat loop.

These are recorded as an initial decision in [`docs/adr/0001-runtime-and-service-boundaries.md`](docs/adr/0001-runtime-and-service-boundaries.md).

## Project brochure

- Editable HTML: [`Design/Brochure/sandsunder-brochure.html`](Design/Brochure/sandsunder-brochure.html)
- Print-ready PDF: [`output/pdf/sandsunder-brochure.pdf`](output/pdf/sandsunder-brochure.pdf)

The brochure is a self-contained three-page A4 document built from the current GDD and visual style bible. It does not depend on external fonts or hosted media.

## Working rules

Read [`AGENTS.md`](AGENTS.md) before changing the repository. In short:

1. Discover the relevant code path and current ADRs before editing.
2. Assign exclusive ownership before parallel writes.
3. Keep core game rules independent of Unity, Photon, and hosted-service SDKs.
4. Add or update tests with behavior changes.
5. Never deploy, publish, rotate credentials, or alter production state without explicit user approval at action time.

Project-scoped Codex roles live in `.codex/agents/` and are configured for at most six concurrent subagent threads.

## Local prerequisites

Toolchain versions will be pinned when the respective project manifests are introduced. Do not install Unity packages or vendor SDKs ad hoc: record the decision, pin the version, and update through a dedicated change.

Secrets must remain outside Git. Commit only sanitized `.env.example` templates and use the secret store of the relevant development or hosting environment.
