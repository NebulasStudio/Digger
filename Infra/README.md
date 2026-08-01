# Sandsunder infrastructure

This directory contains local development and provider templates. Nothing here deploys automatically.

## Local backend

1. Copy `.env.example` to `.env` and replace every local-only value.
2. Build the locked backend module, apply Nakama plus Sandsunder migrations, and start the services:

   ```sh
   docker compose --env-file Infra/.env -f Infra/compose.yaml up --wait
   ```

3. Nakama's HTTP API is available at `http://127.0.0.1:7350`; the local console is at
   `http://127.0.0.1:7351`.

Compose runs three one-shot gates before Nakama becomes healthy:

- `backend-build` compiles `Backend/src` and exports `sandsunder.js` into the `nakama-modules` volume;
- `nakama-schema-migrate` applies Nakama's own database migrations;
- `sandsunder-schema-migrate` applies every `Backend/migrations/*.sql` file in numeric filename order with
  `ON_ERROR_STOP=1`.

Nakama mounts the compiled module read-only at `/nakama/data/modules/sandsunder.js`. Any build or migration
failure prevents it from starting. Add new migrations with zero-padded numeric names and keep them safe to
rerun in local recovery scenarios.

The Compose project is deliberately for developer workstations only. Its credentials, open host ports,
and debug logging are not production defaults. The named PostgreSQL volume is persistent; removing it
destroys local account and progression data.

## Provider boundaries

- `providers/` documents the stable configuration boundary expected by the future `IServerAllocator`.
- `edgegap/` contains a reviewed deployment checklist and non-secret environment template.
- `cloudflare/` is a small peripheral Worker for immutable public derivatives in R2. It does not host the
  authoritative match loop and it cannot access the private provenance bucket.
- `unity-server/` contains the Linux headless runtime image. A Unity build must exist before building it.

Provider credentials belong in the provider secret manager or GitHub environment secrets. Do not create
committed `.env` files, Wrangler `.dev.vars`, Edgegap tokens, Photon keys, or Nakama server keys.

## Validation

The repository CI parses Compose, validates provider JSON, and type-checks the Worker. Locally, when the
required tools are installed:

```sh
docker compose --env-file Infra/.env.example -f Infra/compose.yaml config --quiet
docker compose --env-file Infra/.env.example -f Infra/compose.yaml build backend-build
cd Infra/cloudflare && npm ci && npm run check
```
