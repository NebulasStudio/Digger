# Nakama runtime module builder

This image compiles `Backend/src` with the locked Node dependencies and exports only
`sandsunder.js`. Compose runs the final stage as a one-shot service and writes the module into a named,
read-only-at-runtime volume mounted at `/nakama/data/modules` by Nakama.

Build from the repository root so `.dockerignore` protects the context:

```sh
docker build --target module-export -f Infra/nakama-runtime/Dockerfile .
```

The runtime module contains no secret. `SANDSUNDER_MATCH_HMAC_SECRET` is injected into the Nakama service
from `Infra/.env` and must be replaced by a provider secret outside local development.

