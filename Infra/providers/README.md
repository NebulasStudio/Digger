# Provider adapter contract

Infrastructure configuration is allowed to name providers; game and backend domain code should depend on
stable contracts instead. The future `IServerAllocator` implementation must translate the following
provider-neutral request and response without leaking Edgegap-specific fields to a client.

## Allocation request

Required fields:

- `match_id`: globally unique and immutable.
- `build_id`: immutable server build/version identifier.
- `ruleset_version`: immutable gameplay-data version.
- `region_hints`: ordered ISO region or provider-neutral metro hints from matchmaking.
- `max_players`: six for the MVP.
- `environment`: non-secret runtime configuration references; secrets are injected by the provider.

## Allocation response

The backend receives provider allocation ID, lifecycle status and diagnostic metadata. The client-facing
projection is limited to:

```json
{
  "endpoint": "game.example.invalid:7777",
  "transport": "udp",
  "ticket": "signed-single-use-ticket"
}
```

The signed ticket is issued by the trusted backend, never by infrastructure tooling. Allocation create and
terminate operations must be idempotent on `match_id`. Provider API failures should be classified as
retryable, capacity exhausted, invalid request, unauthorized or terminal.

`server-runtime.env.example` defines the names injected into the authoritative server. Values are examples,
not credentials.

