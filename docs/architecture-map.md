# Sandsunder — Mappa dell'architettura

> Map generata dal repository al commit `1edd514`. Stato: fase **foundation** (MVP).
> Progetto: Sandsunder — PvPvE competitivo per 6 giocatori, top-down, pixel-art moderna.
> Fonte normativa: `docs/adr/0001-runtime-and-service-boundaries.md` e gli `.asmdef` sotto `Game/Assets/Sandsunder/*`.

---

## 1. Panoramica a strati

Sandsunder separa in modo rigido il **dominio** e la **simulazione deterministica** (puro C#, zero dipendenze) da tutto il resto: Unity, Photon Fusion, Nakama, Cloudflare. La regola architetturale centrale è che **Domino e Simulation non possono riferire alcun SDK vendor**; ogni provider è nascosto dietro interfacce (port).

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLIENT (Unity)                               │
│   Presentation (IMatchView)  ─  Gameplay (input, movimento, HUD)    │
│        └───────────────┬───────────────────────────────┘            │
│                        │ view + comandi (immutabili)                │
├────────────────────────▼────────────────────────────────────────────┤
│        DOMAIN  +  SIMULATION  (core puro, deterministico)           │
│        MatchRules · MatchSimulation · CombatRules · DigGrid · RNG   │
│        (nessuna dipendenza da Unity/Photon/vendor)                  │
├────────────────────────▼────────────────────────────────────────────┤
│   Networking.Fusion (IAuthoritativeSession)  ·  Platform (port)     │
│   ── Photon Fusion 2 in Server Mode ────────────  Edgegap           │
├────────────────────────▼────────────────────────────────────────────┤
│   BACKEND  Nakama + PostgreSQL  (bootstrap, ticket, settlement)     │
│   Cloudflare (DNS/WAF/CDN/object storage) · Contracts (OpenAPI)     │
└─────────────────────────────────────────────────────────────────────┘
```

**Flusso di autorità (match):** il **server dedicato** (Unity headless) possiede tempo, RNG seed, esiti di scavo, loot nascosto, combattimento, IA, respawn, obiettivi e vincitori. Il client riceve solo coordinate di connessione + un **ticket firmato monouso** (`ServerConnection`). A fine partita il server sottopone un risultato **firmato e idempotente** al backend Nakama (`match_id + account_id`), che gestisce progressione e warehouse. Il client non può mai assegnare progressione.

---

## 2. Dipendenze tra assembly (da `.asmdef`)

| Assembly | Dipende da | `noEngineReferences` | Include/Exclude | Ruolo |
|---|---|---|---|---|
| `Sandsunder.Domain` | — (nessuna) | `true` | tutte | Tipi puri, regole, identity |
| `Sandsunder.Simulation` | `Sandsunder.Domain` | `true` | tutte | Stato di match deterministico |
| `Sandsunder.Networking.Fusion` | `Domain`, `Simulation` | `true` | tutte | Port fotone-indipendente |
| `Sandsunder.Platform` | `Domain` | `true` | tutte | Port backend/crash (provider) |
| `Sandsunder.Presentation` | `Domain`, `Simulation` | `false` | tutte | Vista client (consuma viste immutabili) |
| `Sandsunder.Gameplay` | `Domain`, `Simulation`, `Unity.InputSystem` | `false` | tutte | Gameplay client su Unity |
| `Sandsunder.Server` | `Domain`, `Simulation`, `Networking.Fusion`, `Platform` | `true` | constraint `UNITY_SERVER` | Composition root del server headless |
| `Sandsunder.Gameplay.Editor` | `Gameplay` | `false` | solo `Editor` | Tooling editor (lab, asset factory) |

**Grafo di dipendenza (diretto):**

```
Domain ──► Simulation ──► Networking.Fusion ──► Server
   │          │                                   ▲
   │          └──────────── Presentation ─────────┘
   └──────────────► Platform ─────────────────────┘
   └──────────────► Gameplay (ref anche Simulation) ──► Gameplay.Editor
```

Osservazioni chiave:
- **Domain** alla radice: nessun riferimento (nemmeno a Unity → `noEngineReferences: true`). È il contratto più basso.
- **Simulation** dipende solo da Domain ed è `noEngineReferences: true` → gira identica su client, server headless e test.
- **Server** è l'unico assembly che compone insieme Fusion + Platform + Simulation, ed è attivo solo con la define `UNITY_SERVER`.
- Gameplay e Presentation sono gli unici layer Unity-dipendenti (`noEngineReferences: false`).

---

## 3. Layer DOMAIN (`Game/Assets/Sandsunder/Domain/`)

Assembly `Sandsunder.Domain` — **puro C#, zero riferimenti**. Nessun codice qui può toccare Unity, Photon o vendor.

| File | Contenuto |
|---|---|
| `MatchTypes.cs` | `MatchPhase` (Preparation/CenterOpen/SuddenDeath/Completed), `WinCondition` (None/RitualRace/RelicExtraction/LastSurvivor/ObjectiveTimeout), struct value `PlayerId`, `GridCell`, `MatchIdentity` (match_id + build_id + ruleset_version), `AuthoritativeMatchIdentity` (**internal**, progetto server-only con `map_seed` che non entra mai nei contratti client), `MatchOutcome` (winner + condition + tick) |
| `MatchRules.cs` | Regole configurabili del match (es. max players, tempi, budget) — dati versionati |
| `PlayerState.cs` | Stato per giocatore: seat, alive/eliminato, respawn, `ObjectiveMilestones`, `LastMilestoneTick` |

**Dipendenze:** nessuna (radice del grafo).

---

## 4. Layer SIMULATION (`Game/Assets/Sandsunder/Simulation/`)

Assembly `Sandsunder.Simulation` — dipende solo da Domain, `noEngineReferences: true`. È il **cuore autoritativo e deterministico** del gioco.

| File | Contenuto |
|---|---|
| `MatchSimulation.cs` | Orchestratore del match: aggiunta giocatori (0..max), avanzamento a tick espliciti, gestione fasi (`Preparation` → … → `Completed`), valutazione condizioni di vittoria, eliminazioni. Espone `Tick`, `Phase`, `Outcome`, `Players` |
| `CombatRules.cs` | Regole di combattimento (danno, validazioni) |
| `CombatState.cs` | Stato di combattimento per entità |
| `CombatDigging.cs` | Meccanica di scavo legata al combattimento |
| `CombatRollMotion.cs` | Rotolamento/schivata (roll) |
| `PlayerKinematics.cs` | Cinematica del giocatore (movimento puro) |
| `DigGrid.cs` | Griglia di scavo del terreno (cellule) |
| `DeterministicRng.cs` | PRNG deterministico e seed-cato per simulazione riproducibile |
| `StableHash.cs` / `SimulationStateHasher.cs` | Hashing stabile dello stato → test di determinismo (quality gate: hash di stato da seed+input identici) |
| `RitualRaceState.cs` | Condizione di vittoria "Ritual Race" |
| `RelicExtractionState.cs` | Condizione di vittoria "Relic Extraction" |
| `LastSurvivorState.cs` | Condizione di vittoria "Last Survivor" |
| `EliminationResult.cs` | Esito di un'eliminazione |
| `AssemblyInfo.cs` | Metadata assembly |

**Dipendenze:** `Sandsunder.Domain`. Nessuna dipendenza Unity/vendor → testabile con edit-mode test puro.

---

## 5. Layer UNITY — Client

Cinque assembly lato Unity; solo Presentation e Gameplay sono `noEngineReferences: false`.

### 5.1 Presentation (`.../Presentation/`)
Assembly `Sandsunder.Presentation` → ref `Domain`, `Simulation`; Unity-dipendente.

| File | Contenuto |
|---|---|
| `IMatchView.cs` | **Port** di presentazione: `ShowPhase(MatchPhase, authoritativeTick)` e `ShowOutcome(MatchOutcome)`. I MonoBehaviours concreti consumano **viste immutabili** della simulazione |

### 5.2 Gameplay (`.../Gameplay/`)
Assembly `Sandsunder.Gameplay` → ref `Domain`, `Simulation`, `Unity.InputSystem`. Contiene sia il gameplay "top-down" sia il sandbox/prototipo.

**Movimento & input (produzione):**
| File | Contenuto |
|---|---|
| `TopDownPlayerController.cs` | Controller top-down del giocatore |
| `TopDownMovementMath.cs` | Matematica del movimento (puro, testabile) |
| `TopDownMovementProfile.cs` | Profilo di movimento configurabile |
| `AimInputArbiter.cs` | Arbitraggio dell'input di mira (mouse/touch/controller) |
| `OrthographicCameraFollow.cs` | Camera ortografica che segue il giocatore |

**Sandbox scene (playground di atterraggio):**
| File | Contenuto |
|---|---|
| `SandboxSceneInitializer.cs` | Inizializzatore della scena sandbox |
| `SandboxActorVisual.cs`, `SandboxFootprint.cs`, `SandboxPitDecal.cs`, `SandboxVisualEffects.cs` | Visual/feedback (impronte, decal di fossa, VFX) |
| `SandboxInventoryWindow.cs`, `SandboxReloadBar.cs`, `SandboxMinimap.cs` | UI: inventario, barra ricarica, minimappa |

**Prototipi gameplay (da consolidare in produzione):**
| File | Contenuto |
|---|---|
| `PrototypeDigging.cs`, `PrototypeDigNode.cs`, `PrototypeDigGridAuthority.cs` | Scavo e autorità griglia |
| `PrototypeCombat.cs`, `PrototypePlayerCombat.cs`, `PrototypeHealth.cs`, `PrototypeProjectile.cs` | Combattimento, HP, proiettili |
| `PrototypeDuneSpitter.cs`, `PrototypeMobSpawnerToggle.cs` | Nemico spitter e spawner mob |
| `PrototypeTunnelSystem.cs` | Sistema gallerie sotterranee (stealth) |
| `PrototypeAncientRuneObelisk.cs`, `PrototypeDesertRuinDoor.cs`, `PrototypeDestructibleVase.cs` | Oggetti di scena (reliquie, porta, vaso) |
| `PrototypePixelArt.cs` | Supporto pixel-art |
| `PrototypeInventoryHUD.cs`, `PrototypePlayerStatusHUD.cs` | HUD inventario e stato giocatore |
| `PrototypePickup.cs` | Oggetti raccoglibili |

### 5.3 Editor (`.../Editor/`)
Assembly `Sandsunder.Gameplay.Editor` (solo Editor) → ref `Sandsunder.Gameplay`.
| File | Contenuto |
|---|---|
| `GameplayLabBuilder.cs` | Builder del "gameplay lab" (scena di test) |
| `SandboxArtAssetFactory.cs` | Factory programmatica per asset pixel-art generati (sprites) |

**Dipendenze client:** Gameplay → {Domain, Simulation, InputSystem}; Presentation → {Domain, Simulation}. Nessun riferimento diretto a Photon/Nakama dal client.

---

## 6. Layer PHOTON FUSION (rete autoritativa)

Assembly `Sandsunder.Networking.Fusion` → ref `Domain`, `Simulation`; `noEngineReferences: true`. **Nota importante dallo stato attuale:** questo layer oggi contiene **solo le porte (port)** indipendenti da Photon; l'implementazione concreta di Fusion 2 in Server Mode è dichiarata come "futura" nel codice (`NetworkPorts.cs`).

| File | Contenuto |
|---|---|
| `NetworkPorts.cs` | `ServerConnection` (endpoint + transport + single-use ticket firmato); interfaccia **`IAuthoritativeSession`** (`IsServer`, `Tick`, `StartServer`, `Stop`); `IPlayerInputSource<TInput>` (lettura input per giocatore/tick) |

**Photocode (previsto, da implementare):** l'adapter che realizzerà `IAuthoritativeSession` con `GameMode.Server` di Fusion 2. Il server dedicato possiede tick, RNG, scavo/loot, combattimento, IA, respawn, obiettivi e vincitori. `ServerConnection` fornisce al client solo endpoint + ticket: nessun seed di mappa/state esposto.

**Dipendenza:** → {Domain, Simulation} (mai Photon nei layer sotto).

---

## 7. Layer SERVER (composition root dedicato)

Assembly `Sandsunder.Server` → ref {Domain, Simulation, Networking.Fusion, Platform}; `noEngineReferences: true`; **`defineConstraints: ["UNITY_SERVER"]`**.

| File | Contenuto |
|---|---|
| `ServerCompositionRoot.cs` | **Composition root headless**: richiede una sessione autoritativa (`IAuthoritativeSession`), verifica `IsServer`, avvia `StartServer(identity, connection)` e costruisce `MatchSimulation` con `AuthoritativeMatchIdentity` (che porta il `map_seed` server-only) |

**Container:** il server Unity dedicato è impacchettato via Docker (`Infra/unity-server/Dockerfile`) e distribuito su **Edgegap** dietro l'interfaccia `IServerAllocator` (ADR-0001). Il compose locale (`Infra/compose.yaml`) non avvia ancora il server Unity: oggi copre backend Nakama+Postgres.

---

## 8. Layer PLATFORM (adapter provider)

Assembly `Sandsunder.Platform` → ref `Domain`; `noEngineReferences: true`. Raccoglie le **porte verso servizi esterni** (backend, crash reporting).

| File | Contenuto |
|---|---|
| `PlatformPorts.cs` | `ResultSubmission` (identity + outcome + signedPayload); **`IMatchResultSink.SubmitAsync`** (idempotente per `match_id + account_id`); `ICrashReporter.Capture` |

**Nota:** la porta `IServerAllocator` citata in ADR-0001/AGENTS.md (allocazione server Edgegap) è un contratto architetturale previsto; l'implementazione concreta è in fase di scaffolding.

---

## 9. Layer BACKEND — Nakama + PostgreSQL (`Backend/`)

Modulo Nakama (TypeScript → `dist/sandsunder.js`) per bootstrap match, ticket monouso, settlement firmato e progressione orizzontale. **Solo server-a-server** (eccetto la progressione utente).

### 9.1 Sorgenti (`Backend/src/`)
| File | Contenuto |
|---|---|
| `runtime.ts` | Registrazione RPC e logica di esposizione: `sandsunder_match_bootstrap_v1`, `sandsunder_match_ticket_consume_v1`, `sandsunder_match_result_submit_v1` (server-only, rifiutano ctx con `userId`), `sandsunder_progression_get_v1` (unico player-facing, deriva `account_id` dal ctx autenticato) |
| `domain.ts` | Payload tipizzati: `MatchResultPayload`, `MatchBootstrapPayload` (include `map_seed`), `MatchTicketPayload` (esclude `map_seed`), `ProgressionView`, `SettlementReceipt`, `SignedEnvelope<T>` |
| `security.ts` | Envelope **canonical-JSON HMAC-SHA256** (`v1=<hex>`), firma/verifica, anti-skew (max 300s) |
| `validation.ts` | Validatori payload (UUID, ID, range interi, ecc.) |
| `repository.ts` | `PostgresPersistence`: `settle`, `bootstrap`, `consumeTicket`, `getProgression`. Il settlement è **una singola statement PostgreSQL** (nonce, risultato, progressione, ledger e outbox atomici) |
| `types/nakama.d.ts` | Type declarations dei soli API Nakama usati |
| `test-exports.ts` | Export per i test |

### 9.2 Migrazioni (`Backend/migrations/`)
| File | Contenuto |
|---|---|
| `001_foundation.sql` | Schema base: `account_progression`, `character_mastery`, `account_unlocks`, `match_sessions`, `match_roster`, `match_results` (PK `match_id+account_id`), `progression_ledger`, `request_nonces`, `outbox` |
| `002_operational_guards.sql` | Guardie/indici operativi |
| `003_settlement_roster_guards.sql` | Guardie sul roster di settlement |

### 9.3 Sicurezza e trust boundary
- RPC server-only rifiutano ogni contesto con `userId` (eccetto `progression_get`).
- Segreto `SANDSUNDER_MATCH_HMAC_SECRET` (≥32 caratteri) presente solo su Nakama e server di controllo/match. Mai esposto al client Unity.
- Ticket bootstrap sta dietro il piano di controllo Edgegap/matchmaking.
- Idempotenza e resistenza alla duplicazione: PK `(match_id, account_id)` + nonce; nessun client assegna progressione.

**Dipendenza:** Modulo Nakama → PostgreSQL (schema `sandsunder`). Client Unity **non** parla direttamente a Nakama per il match.

---

## 10. Layer CONTRACTS (`Contracts/`)

Contratti versionati condivisi da client Unity, server dedicato, control plane e adapter Nakama. Provider-neutral.

| File | Contenuto |
|---|---|
| `openapi/sandsunder-backend.v1.yaml` | OpenAPI 3.1 del backend; `x-nakama-rpc-id` mappa le operazioni logiche all'RPC runtime |
| `schemas/match-bootstrap.schema.json` | Messaggio firmato server-to-server (contiene `map_seed`) |
| `schemas/match-ticket.schema.json` | Ticket client: **esclude `map_seed`** (evita ricostruzione anticipata del loot) |
| `schemas/match-result.schema.json` | Risultato match firmato |
| `schemas/progression.schema.json` | Account XP, mastery character, unlock cosmetici/sidegrade. **Niente modificatori permanenti di potenza** |
| `schemas/ticket-consume.schema.json` | Consumo ticket con nonce |
| `schemas/common.schema.json` | Tipi comuni |
| `examples/match-result.json`, `examples/match-ticket.json` | Esempi |

**Regole:** HMAC = JSON ricorsivamente key-sorted senza spazi della `payload`; firma `v1=<hex>`. Timestamp UTC ISO-8601, skew ≤ 5 min. La generazione del client C# è prevista in CI (non committata in foundation).

---

## 11. Layer INFRA (`Infra/`)

| File/Path | Contenuto |
|---|---|
| `compose.yaml` | Stack locale: `postgres` (16.8-alpine), `backend-build` (Docker multistage → `nakama-modules`), `nakama-schema-migrate`, `sandsunder-schema-migrate` (applica `Backend/migrations/*.sql`), `nakama` (3.37.0, monta `/nakama/data/modules`) |
| `nakama-runtime/Dockerfile` | Build del modulo Nakama (target `module-export`) |
| `unity-server/Dockerfile` | Container del server Unity dedicato (target pack Edgegap) |
| `edgegap/` | Template `application-version.template.json` + schema, `.env.example` (allocazione server) |
| `cloudflare/` | Worker (`src/index.ts`), `wrangler.jsonc`, bindings (DNS/WAF/CDN/object storage) |
| `providers/` | `.env.example` server-runtime, documentazione provider |
| `.env.example` | Variabili locali (DB, Nakama, HMAC secret) |
| `README.md` | Documentazione infra |

**Orchestrazione locale:** compose gestisce backend+DB; il server Unity è un target Docker separato per Edgegap.

---

## 12. Test e CI

**Test Unity (edit/play mode):**
| File | Contenuto |
|---|---|
| `Game/Assets/Tests/EditMode/SimulationTests.cs` | Test di determinismo della simulazione |
| `Game/Assets/Tests/Gameplay/EditMode/CombatPrototypeTests.cs` | Prototipo combattimento |
| `Game/Assets/Tests/Gameplay/EditMode/SandboxPresentationTests.cs` | Presentazione sandbox |
| `Game/Assets/Tests/Gameplay/EditMode/TopDownGameplayTests.cs` | Gameplay top-down |
| `Game/Assets/Tests/Gameplay/PlayMode/CombatPrototypePlayModeTests.cs` | Play-mode combattimento |
| `Game/Assets/Tests/Gameplay/PlayMode/TopDownPlayerPlayModeTests.cs` | Play-mode giocatore |

**Backend/Contracts:** test Node (`Backend/tests/*.test.cjs`, `Contracts/tests/contracts.test.cjs`) con test runner nativo + PGlite per le migrazioni.

**CI (`.github/workflows/ci.yml`):** job `infrastructure` (compose config + build modulo + parse JSON + type-check Worker), `contracts-backend` (lint + test Node + .NET), `unity-tests` (gated da `RUN_UNITY_TESTS`/workflow_dispatch, licensed Unity via `game-ci/unity-test-runner`).

---

## 13. Quality gates e invarianti architetturali (da AGENTS.md)

- `Domain`/`Simulation` **mai** dipendenti da Unity/Photon/Steam/Nakama/Edgegap/Cloudflare/analytics.
- Server autoritativo dedicato obbligatorio per match competitivi (mai host/shared authority).
- Server possiede: tempo, RNG seed, esiti di scavo, loot non scoperto, validazione combattimento, IA, respawn, obiettivi, vincitore.
- `MatchResult` server→backend firmato, idempotente, deduplicato da `match_id + account_id`. I client non assegnano progressione.
- Catalogo/balance/armi/personaggi/loot come **dati versionati**, non costanti sparse.
- Progressione **orizzontale**: niente bonus permanenti a danno/vita/ricompense.
- Carries `match_id`, `build_id`, `ruleset_version`, `map_seed` (server-only) per tutto il ciclo di vita.
- Test: determinismo (hash di stato da seed+input identici), networking (latenza/jitter/perdita/reconnect/abuso), economy (idempotenza/duplicazione).
- Niente deploy/publish/push/credenziali/stato di produzione senza **approvazione esplicita dell'utente al momento dell'azione**.

---

## 14. Legenda dei livelli di maturità

- **Implementato e testato** — Domain, Simulation, port (Networking/Platform), Backend+Contracts+Infra, test Unity edit-mode, CI.
- **Port dichiarate, implementazione Fusion/Provider futura** — `IAuthoritativeSession` (Fusion 2 Server Mode), `IServerAllocator`/Edgegap, adapter Cloudflare per il match.
- **Prototipo/sandbox** — `Prototype*` in Gameplay (da consolidare in produzione), scena sandbox.