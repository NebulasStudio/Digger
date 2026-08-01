# Design source of truth

- `GDD.md`: MVP experience, rules, session flow, victory resolution, content boundary, and acceptance criteria.
- `style-bible.md`: frozen visual formula, palette, pixel/import rules, prompt assembly, and human review gates.
- `assets.csv`: complete planned visual/audio manifest. All rows are placeholders; no media has been generated or published.
- `balance/*.csv`: ruleset-versioned foundation values for six characters, ten weapons, four utilities, and five enemy tiers.
- `provenance/`: JSON Schema and copyable record template for asset history, rights, transformations, review, and publication state.
- `telemetry-events.md`: event contract, privacy rules, derived metrics, and quality checks.
- `market-naming.md`: market references, positioning hypothesis, working-title rationale, and clearance gate.

Run `powershell -ExecutionPolicy Bypass -File Design/validate.ps1` from the repository root to validate local design data. Balance values are starting hypotheses, not promises; tune one variable per versioned experiment.
