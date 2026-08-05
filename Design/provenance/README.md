# Asset provenance workflow

`assets.csv` is the production manifest; each row links to one provenance record through `provenance_id`. Copy `provenance.template.json` to `<provenance_id>.json` only when work on that asset begins, then validate it against `provenance.schema.json`.

Rules:

1. Keep prompt, model, job ID, seed, generation timestamp, reference ownership/license, and original job metadata before editing.
2. Record each material transformation and hash every stored original, intermediate, and runtime artifact with SHA-256.
3. Store originals and metadata privately; only an approved runtime derivative may set `published: true`.
4. `approved` requires named human review, all three review gates passing, rights checks complete, and a resolved Steam AI disclosure value.
5. Rejected outputs remain recorded for audit but never enter runtime bundles.
6. Never use living-artist imitation, third-party game IP, or unlicensed reference media in prompts.

The sandbox sand, ruin, hero, and Spitter proxies currently have generated media attached and remain non-publishable `in_review` assets. Every other row remains a non-publishable `planned_placeholder` until its own provenance record is created.
