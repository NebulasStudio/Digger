# Sandsunder — Stato del Progetto e Cache Persistente

**Ultimo aggiornamento:** 2026-08-03  
**Stato Fase:** Foundation / Baseline Architecture (Foundation 0.1)  
**Ruleset Core:** `mvp-0.1.0`  

---

## 1. Sintesi del Progetto e Target

- **Nome Progetto:** Sandsunder
- **Genere & Modello:** Top-down desert arena PvPvE expedition game (6 giocatori FFA).
- **Piattaforma Target:** Windows / Steam (controller-first), con compatibilità futura Steam Deck.
- **Visual Design:** 32x32 pixel art (URP 2D), palette calda desertica (ochre/sand/clay) con accenti acid-cyan per gli obiettivi e coral/red per i pericoli.
- **Condizioni di Vittoria:** Tre vie indipendenti e simultanee (Ritual Race, Relic Extraction, Last Survivor) + risoluzione a punti/milestone al minuto 15:00.
- **Progressione Account:** Orizzontale (XP, cosmetici, maestria personaggi, sblocchi sidegrade); zero vantaggi statistici o boost competitivi a pagamento.

---

## 2. Decisioni Architetturali Consolidate (ADR & Invarianti)

- **ADR-0001 (Runtime & Service Boundaries):**
  - **Client/Server Game Loop:** Unity 6 LTS (URP 2D) per client Windows e server dedicato Linux headless.
  - **Simulazione & Network:** Photon Fusion 2 in Server Mode dedicato. Il server gestisce tempo di simulazione, RNG, visibilità e apertura loot, combattimento, AI, respawn e proclamazione del vincitore.
  - **Isolamento Domain/Simulation:** I moduli `Domain` e `Simulation` rimangono codice C# puro senza dipendenze da Unity presentation o SDK vendor (Photon, Nakama, Edgegap, Cloudflare).
  - **Allocazione Server:** Edgegap interfacciato tramite l'astrazione `IServerAllocator`. Ai client vengono fornite solo le coordinate di rete e un ticket firmato monouso.
  - **Backend & Persistence:** Nakama Cloud + PostgreSQL. I risultati del match vengono inviati esclusivamente server-to-server con firma HMAC-SHA256 e gestiti in modo idempotente deduplicato per `(match_id, account_id)`.
  - **Edge & Content:** Cloudflare per DNS/WAF, API perimetrali e R2 storage. La simulazione di combattimento NON gira su Worker/Cloudflare.
  - **Sicurezza del Seed:** `map_seed` è generato e mantenuto esclusivamente dal server; mai inviato al client nel ticket di matchmaking.

---

## 3. Struttura del Workspace e Moduli

| Directory | Responsabilità e Componenti | Stato Test |
| --- | --- | --- |
| `Game/` | C# assemblies (`Sandsunder.Domain`, `.Simulation`, `.Gameplay`, `.Networking.Fusion`, `.Platform`, `.Presentation`, `.Server`) + Test EditMode | Unit test compilano e superano la validazione `dotnet test` |
| `Backend/` | Moduli TypeScript Nakama (`src/`), Migrazioni PostgreSQL (`migrations/*.sql`), RPC firmate server-to-server | Test Node superati (13/13 test `npm test`) |
| `Contracts/` | Schema OpenAPI 3.1 (`openapi/sandsunder-backend.v1.yaml`) e schemi JSON (`schemas/`) | Schema e contratti validati (4/4 test `npm test`) |
| `Infra/` | Docker Compose (`compose.yaml`), Nakama + PG setup, edgegap specs, cloudflare workers, linux dedicated server container setup (`unity-server/`) | Configurazione Compose e Worker sintatticamente corretti |
| `Design/` | GDD (`GDD.md`), Style Bible (`style-bible.md`), Manifest Asset (`assets.csv`), Bilanciamento CSV (`characters.csv`, `weapons.csv`, etc.) | Script PowerShell validato (`validate.ps1` superato) |
| `docs/adr/` | Architectural Decision Records (`0001-runtime-and-service-boundaries.md`) | Consolidato |

---

## 4. Cronologia Verifiche e Baseline

- **Validazione Design Manifests:** `powershell -ExecutionPolicy Bypass -File Design/validate.ps1` → **OK** (76 asset pianificati, 6 personaggi, 10 armi, 4 utility, 5 nemici).
- **Validazione Service Contracts:** `npm test` in `Contracts/` → **PASS** (4/4 test).
- **Validazione Backend Runtime & Migrazioni SQL:** `npm test` in `Backend/` → **PASS** (13/13 test in-memory PGlite & HMAC signature verification).
- **Validazione Game C# Projects:** `dotnet test Game/Game.sln` → **OK** (Compilazione e restore puliti).
- **Integrazione MCP Unity:** Registrato `unity-mcp` (`relay_win.exe --mcp`) in [`C:\Users\leopo\.gemini\config\mcp_config.json`](file:///C:/Users/leopo/.gemini/config/mcp_config.json) ed esposto in [`Game/UserSettings/mcp.json`](file:///c:/Users/leopo/Vault_LP/Vault/Progetti_Sviluppo/VideoGame/Game/UserSettings/mcp.json) per il collegamento bidirezionale.
- **Distinzione Rigida Armi, Furtività Tunnel, Crepe Scavo & Inventario TAB Minecraft:** Riconfigurato `PrototypeCombat.cs` per limitare lo scavo alla sola pala ed il fuoco con proiettili alle armi da fuoco. Integrata l'invisibilità totale ai nemici di superficie nei tunnel sotterranei (`CurrentDepth >= 2`), l'effetto crepe radiale sugli avvallamenti della sabbia in `SandboxPitDecal.cs` e l'inventario TAB stile Minecraft con avatar preview ed indicatori HP/Stamina in `SandboxInventoryWindow.cs`. Compilazione C# 100% pulita, tutti i test superati.
