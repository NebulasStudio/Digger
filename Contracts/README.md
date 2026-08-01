# Sandsunder service contracts

Versioned OpenAPI 3.1 and JSON Schema contracts shared by the Unity client, dedicated server, control plane and Nakama adapter.

- `openapi/sandsunder-backend.v1.yaml` is provider-neutral. `x-nakama-rpc-id` maps logical operations to the current runtime.
- `schemas/match-bootstrap.schema.json`, `match-ticket.schema.json` and `match-result.schema.json` define server-only signed messages.
- `schemas/progression.schema.json` intentionally contains account XP, character mastery and cosmetic/sidegrade unlocks only. No permanent combat-stat modifiers are part of the contract.
- `map_seed` exists only in the signed, server-to-server bootstrap contract. It is deliberately absent from the match ticket returned to clients, preventing early reconstruction of buried loot.

HMAC input is the recursively key-sorted, whitespace-free JSON representation of `payload`; the transmitted signature is lowercase `v1=<sha256 hex>`. Timestamps use UTC ISO-8601 and internal signed requests allow at most five minutes of clock skew.

Run contract validation:

```powershell
$env:npm_config_cache = Join-Path (Get-Location) '.npm-cache'
npm install
npm test
```

Code generation is deliberately not checked in during foundation. CI should generate the C# transport client from the pinned OpenAPI file once the Unity assemblies exist, then fail on uncommitted contract drift.
