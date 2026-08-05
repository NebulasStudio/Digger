# SANDSUNDER — PROMPT DEFINITIVO TOTALE: RIPRENDITI TUTTI GLI ASSET, RIFAI UI & ANIMAZIONI, SCALA COERENTE, TUTTO PERFETTO

> Repo: `NebulasStudio/Digger` — branch `main` — Unity 6.3 LTS (6000.3.21f1)
> Stato: TUTTI gli asset e le animazioni SONO già generati e disponibili (in questa chat e nel repo). Il tuo compito è RIPRENDERLI, INTEGRARLI TUTTI, e rifare completamente UI + animazioni con scala coerente. Il gameplay giocato deve essere PERFETTO e corrispondere alla visione. Leggi AGENTS.md prima di modificare.

## ⚠️ REGOLA PRIMA DI TUTTO: NON GENERARE NULLA DI NUOVO
Tutti gli asset esistono già. RIPRENDITELI dal repo. Se un asset manca o è brutto, SEGNALALO (verrà rifatto su Higgsfield) — NON generarlo tu.

---

## PARTE 0 — INVENTARIO ASSET GIÀ ESISTENTI (riprendili TUTTI)

### Sprite 32×32 (in `Assets/Sandsunder/Art/Runtime/Processed/`)
- Armi: `shovel_default_32`, `rifle_brass_32`, `sword_scimitar_32`, `shotgun_heavy_32`, `blaster_rune_32`, `icon_mortar_sandstorm_32`
- Mondo: `env_palm_tree_32`, `env_ruin_pillar_32`, `env_cactus_32`, `env_chest_runed_32`, `env_relic_chest_32`, `env_vase_destructible_32`
- Proiettile: `proj_sentinel_cyan_rune_32`
- Mob: `nomad_32`, `spitter_32`, `mob_crystal_turtle_64`
- UI: `ui_glass_panel`

### Fogli animazione (in `Art/Runtime/Processed/Anims/`) — 21 totali
- Personaggio: `nomad_run`, `nomad_walk`, `nomad_roll`, `nomad_dig`, `nomad_stealth_crouch`
- Armi: `shovel_idle`, `shovel_swing`, `rifle_idle`, `rifle_fire`, `rifle_reload`, `rifle_reload_v2`, `shotgun_idle`, `blaster_idle`, `blaster_fire`, `scimitar_swing`
- Mob/Boss: `spitter_idle`, `spitter_death_burst`, `golem_idle_charge`
- Mondo: `chest_open`, `vase_break`, `pickup_bob`

### Clip .anim (in `Art/Generated/`) — 16+ già generate
`Shovel_Idle, Shovel_Swing, Rifle_Idle, Rifle_Fire, Rifle_Reload, Rifle_Reload_V2, Shotgun_Idle, Blaster_Idle, Blaster_Fire, Scimitar_Swing, Spitter_Idle, Spitter_DeathBurst, Golem_Charge, Chest_Open, Vase_Break, Pickup_Bob` (+ Nomad_* e Player_*).

### Mob pianificati (in `Design/balance/enemies.csv`)
`mob_sandling, mob_spitter, mob_burrower, mob_scorpion, mob_sun_maw, mob_crystal_turtle`.

---

## PARTE 1 — PERSONAGGIO NOMAD DEFINITIVO (il più importante)
**Il personaggio giocabile DEVE essere UNO SOLO: il Nomad del deserto.**
- Veste: color sabbia/beige, cappuccio che lascia solo gli occhi, mantello ocra, stivali scuri.
- Sprite: SOLO `nomad_32.png`. NESSUNA altra versione (es. tunica blu) deve comparire.
- **Se in scena/movimento appare un personaggio diverso (tunica blu, mantello diverso, "tizio a caso"), è un ERRORER: elimina il GameObject/il riferimento sbagliato.**
- Il `bodyRenderer` del player punta SEMPRE a `nomad_32`. Il `NomadAnimatorController` usa SOLO gli sprite del Nomad reale.

### Animazioni personaggio (già create, COLLEGALE)
- Idle → `Nomad_Idle`
- Walk → `Nomad_WalkNew` (param `Speed`, `IsMoving`)
- Run → `Nomad_RunNew`
- Roll → `Nomad_RollNew` (param `IsRolling`)
- Dig → `Nomad_DigNew` (param `IsDigging`)
- StealthCrouch → `Nomad_StealthCrouch` (param `IsStealthed`)
- **Il personaggio DEVE animarsi quando cammina/corre (gambe che si muovono), NON scivolare statico.**

### Arma (scala COERENTE)
- L'arma è ancorata al perno della mano e RUOTA verso il mouse in tempo reale.
- **SCALA: la pala/fucile NON devono essere giganteschi** (più grandi del personaggio). Scala proporzionata (~0.5-0.7). Debug: stampa la `localScale` del weaponRoot e correggila se è enorme.
- Ogni arma con la sua animazione: pala=Shovel_Idle/Swing, fucile=Rifle_Idle/Fire/Reload, ecc.

---

## PARTE 2 — TUTTI I NUOVI MOB E LE LORO ANIMAZIONI
Riprendi e integra TUTTI i mob con le animazioni già create:

1. **Dune Spitter** (`spitter_32` / `mob_dune_spitter_32`): `Spitter_Idle` (loop) + `Spitter_DeathBurst` (one-shot su morte). Comportamento: spara proiettile telegrafato.
2. **Sandstorm Golem** (boss): `Golem_Charge` per stato Charge/Telegraph + nucleo runico ciano che fluttua. Spawn a (0,9).
3. **Crystal Turtle** (`mob_crystal_turtle_64`): sprite reale + `SandstormTurtleAI` (patrol lento, si ritira nel guscio quando colpita, lunge da vicino, ignora player sotterraneo). Spawn a (3.5,-7.5).
4. **Mob pianificati** (sandling, burrower, scorpion, sun_maw): se non hanno sprite/animazioni, SEGNALALO (verranno fatti su Higgsfield). Spitter e golem e tartaruga devono funzionare.

**REGOLA: nessun mob deve attraversare i muri** — collisioni corrette (Rigidbody2D + collider non-trigger sui muri).

---

## PARTE 3 — COMBAT E PROIETTILI (scala coerente, visibili)
- Mira: il mouse definisce la direzione; arma e proiettili partono verso il cursore.
- Proiettili VISIBILI e GRANDI, distinti per arma:
  - rifle → giallo/ottone, allungato
  - blaster → ciano runico (`proj_sentinel_cyan_rune_32`)
  - shotgun → arancione/piombo, 5 con spread
  - mortar → deriva sabbiosa
- Ogni proiettile ha telegrafo visibile prima del colpo.
- Attacco vs scavo: il click destro scava SOLO con `shovel.default` equipaggiata e NON in attacco.

---

## PARTE 4 — SCAVO 3 STATI (perfetto, sulla Tilemap)
- Terreno = **Tilemap a celle** (Grid + Tilemap), NON singolo sprite ripetuto.
- Scavo: cella deforma in 3 stadi CHIARI e GRANDI: intatta → crepata (`DigCracked`) → cratere (`DigOpened`).
- Overlay legato alla Tilemap, non quadratini grezzi.
- FX: fratture stella (`SandCrepeCracksFX`) + polvere (`SandDustEmitter`) durante il canale.

---

## PARTE 5 — TRANSIZIONE SOTTERRANEA (visibile)
- Scendendo (depth≥2): fade-out → switch layer → fade-in con palette sotterranea distinta.
- Player = silhouette ciano `#00F0E6` @65%, sortingOrder -10, visibile attraverso la sabbia.
- Elementi di superficie (chest, vasi, door) non interagibili da sotterraneo.
- NON è solo "camminare su un'altra texture": deve esserci una transizione visiva chiara.

---

## PARTE 6 — RIFAI COMPLETAMENTE LA UI (da zero, premium)
Usa `ui_glass_panel.png` come base. Rifai TUTTO:
- **TAB inventory**: pannello vetro (oro+ciano), icone 32×32 reali INGRANDITE, barre HP/Stamina (`StatBarWidget`), card arma (`WeaponStatCard`), indicatore furtività (`StealthIndicator`), `TabInventoryController`.
- **Minimappa**: bordo dorato, icone player/nemici/chest, sfondo semitrasparente, riflette la mappa a celle.
- **Rimuovi TUTTO ciò che è sporco**: barre di debug, log `[M]` in overlay, notifiche di sistema, quadrati di selezione neri/ciano/verdi, rettangoli verdi senza testo, icone con fondo non trasparente (alpha rotto).
- **HP bar** ancorata correttamente sopra la testa, non fluttuante.
- Icone con **alpha trasparente corretto** (niente fondo bianco).

---

## PARTE 7 — MONDO E MAPPA (variegato, non piatto)
- Tilemap con varietà: sabbie diverse, rocce, dune, palme, colonne, cactus, chest, vasi, rovine.
- Transizioni naturali tra zone (tile di raccordo) — NON tagli netti roccia/sabbia.
- Oggetti posizionati su celle, prospettiva coerente (niente oggetti che fluttuano).

---

## PARTE 8 — VERIFICA FINALE A VIDEO (unica prova valida)
Registra e mostra:
1. Video: personaggio CAMMINA 3s (gambe animate, Nomad reale, NON scivola).
2. Video: muovi MOUSE — arma ruota verso cursore, scala proporzionata.
3. Video: ATTACCHI nemico — proiettili visibili nella direzione del mouse.
4. Video: SCAVI — cella deforma in 3 stadi leggibili.
5. Video: SCENDI SOTTOTERRA — fade+palette+silhouette ciano.
6. Video: nemico raggiunge muro — SI FERMA.
7. Video: TAB — inventario vetro, icone reali, minimappa.
8. Video: golem + tartaruga + spitter in scena, animati.

Se il comportamento non compare, NON dichiarare fatto. Riporta Debug.Log e problema.

---

## CRITERI DI ACCETTAZIONE FINALI
- [ ] Personaggio = Nomad reale UNO SOLO (niente tunica blu / tizio a caso)
- [ ] Animazioni personaggio collegate e visibili in movimento
- [ ] Arma scala coerente, ancorata, ruota al mouse
- [ ] Proiettili grandi e distinti per arma
- [ ] Scavo 3 stadi leggibili sulla Tilemap
- [ ] Transizione sotterranea visibile
- [ ] Spitter + golem + tartaruga presenti e animati
- [ ] Nemici non attraversano muri
- [ ] UI rifatta da zero con ui_glass_panel, senza debug/sporcizia
- [ ] Minimappa rifatta
- [ ] Mappa variegata con transizioni
- [ ] 8 video di verifica mostrati
- [ ] Console 0 errori

## COSA SEGNALARE
- Asset/mob senza sprite o animazione → elenca (verrà fatto su Higgsfield). NON generare tu.
- Unicode personaggio diverso trovato → riporta il GameObject e il riferimento.
- Lavoro manuale necessario → elenca con percorso esatto.

## REGOLE
- Niente push senza conferma. Niente `Sandsunder.Editor` in `Sandsunder.Gameplay` (CS0234). `RequireComponent` prima di `AddComponent`. Committa e pusha ogni parte completata.