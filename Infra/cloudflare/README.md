# Public asset Worker

This peripheral Worker streams approved, immutable derivatives from the `PUBLIC_ASSETS` R2 binding. It has
no binding to the private source/provenance bucket, cannot mutate R2, and does not participate in match
simulation or result processing.

Objects must use this key shape:

```text
sha256/<64 lowercase hex characters>/<safe filename>
```

Requests use `GET` or `HEAD /v1/assets/<key>`. Content-addressed paths receive immutable cache headers.
`GET /health` is the only non-asset route.

## Local verification

```sh
npm ci
npm run cf-typegen
npm run check
npm run dev
```

The bucket names in `wrangler.jsonc` are non-secret placeholders. Create separate development, staging and
production buckets, then update the environment-specific configuration through a reviewed infrastructure
change. Use `wrangler secret put` for any future secret; never add values to this file or `.dev.vars`.

Deployment is intentionally absent from package scripts and CI.

