# Edgegap deployment template

This folder records the desired application-version settings without embedding an Edgegap token or relying
on an unstable API payload. Apply these values through the Edgegap dashboard/API only after the Unity
networking spike establishes its final port and readiness behavior.

## Application version

- Container image: immutable registry digest produced from `Infra/unity-server/Dockerfile`.
- CPU/memory: benchmark-derived; do not copy development workstation values into production.
- Port: internal `7777`, external dynamic, `UDP`.
- Public ports: gameplay only. Metrics, debug endpoints and Nakama are never public container ports.
- Environment: use `deployment.env.example` for non-secret names and Edgegap secrets for secret values.
- Command: keep the Docker image entrypoint; provider arguments may append match allocation metadata.
- Sessions: one six-player match per deployment for the MVP.

## Lifecycle gates

1. Allocation is idempotent on `match_id` and pins an immutable `build_id`.
2. The process starts with no players accepted until ticket verification and runtime config are ready.
3. Readiness is reported only when the UDP endpoint can accept the assigned match.
4. On match completion, signed results are retried idempotently before graceful shutdown.
5. A hard lifetime limit terminates orphaned sessions after the match timeout plus reconnect/result grace.
6. Logs contain `match_id`, `build_id` and allocation ID, but never tickets or credentials.

`application-version.template.json` is an internal, provider-neutral review artifact. The backend adapter or
deployment automation should translate it to the current Edgegap API schema rather than POST it verbatim.
This avoids silently treating documentation as executable deployment automation.

