# Sandsunder Nakama runtime

Authoritative backend module for match bootstrap, single-use tickets, signed match settlement and horizontal account progression.

## Trust boundaries

- `sandsunder_match_bootstrap_v1`, `sandsunder_match_ticket_consume_v1` and `sandsunder_match_result_submit_v1` are server-to-server RPCs. They reject any Nakama context containing `userId`.
- Internal payloads use a canonical-JSON HMAC-SHA256 envelope (`v1=<hex>`). Configure `SANDSUNDER_MATCH_HMAC_SECRET` with at least 32 random characters only on Nakama and trusted match/control servers.
- `sandsunder_progression_get_v1` is the only player-facing RPC and always derives `account_id` from the authenticated Nakama context.
- Match settlement is one PostgreSQL statement: nonce, result, progression, ledger and outbox are committed atomically. The `(match_id, account_id)` primary key makes retries safe.

Do not expose Nakama's server HTTP key or the HMAC secret to the Unity client. Ticket bootstrap belongs behind the Edgegap/matchmaking control plane.

## Build and test

```powershell
$env:npm_config_cache = Join-Path (Get-Location) '.npm-cache'
npm install
npm test
```

Copy `dist/sandsunder.js` into the Nakama JavaScript modules directory. Apply migrations in numeric order before loading the module. The declarations under `src/types` intentionally cover only APIs used here; validate them against the Nakama server version pinned by infrastructure before deployment.

### Compose integration contract

- Build artifact: `Backend/dist/sandsunder.js`; copy or mount it as `/nakama/data/modules/sandsunder.js`.
- Database migrations: apply `Backend/migrations/*.sql` in lexical/numeric order to the same PostgreSQL database used by Nakama, before Nakama starts.
- Required runtime environment: `SANDSUNDER_MATCH_HMAC_SECRET`, minimum 32 random characters, supplied only to Nakama and trusted match/control servers.
- Public schemas remain under `Contracts/schemas/`; the client ticket is `match-ticket.schema.json` and intentionally excludes `map_seed`.

Local unit tests use Node's built-in test runner and mocked Nakama ports. The default command also applies every migration to an embedded PGlite/PostgreSQL-compatible database and verifies uniqueness and monotonic-progression guards without secrets or Docker. A final integration run against the exact PostgreSQL version bundled with the chosen Nakama release is still required before deployment.
