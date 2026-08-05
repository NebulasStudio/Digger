# SANDSUNDER — PROMPT COMPLETO PER L'AGENTE COMPILATORE UNITY

> Repo: `NebulasStudio/Digger` — branch `main` — Unity 6.3 LTS (6000.3.21f1)
> Ultimo commit: `d47effd` (tutto pushato). Leggi AGENTS.md prima di modificare.

---

## 1. RIASSUNTO DEI CAMBIAMENTI GIÀ FATTI E PUSHATI

**Asset grafici importati (in `Assets/Sandsunder/Art/Runtime/Processed/`):**
- 17 sprite 32×32 con `.meta`: shovel_default, rifle_brass, sword_scimitar, shotgun_heavy, blaster_rune, env_ruin_door, env_vase_destructible, env_relic_chest, mob_dune_spitter, icon_mortar_sandstorm, proj_sentinel_cyan_rune, nomad, spitter, env_palm_tree, env_ruin_pillar, env_cactus, env_chest_runed
- 1 pannello HUD glassmorphism: `ui_glass_panel.png` (16:9)
- Texture sabbia/rovine: sand_*, ruin_* (già presenti)

**Fogli animazione (in `Anims/`), 21 totali:**
- Armi: shovel_idle/swing, rifle_idle/fire/reload + reload_v2, shotgun_idle, blaster_idle/fire, scimitar_swing
- Player: nomad_run, nomad_walk, nomad_roll, nomad_dig, nomad_stealth_crouch
- Mob/Boss: spitter_idle, spitter_death_burst, golem_idle_charge
- Mondo: chest_open, vase_break, pickup_bob

**Manifest animazioni:** `Art/Generated/AnimationBuildManifest.asset` — 21 voci (griglie: armi 4×1, rifle_reload 3×3, nomad_run/roll/dig 4×2, nomad_walk 4×1, stealth 4×2, spitter 4×2, golem 4×4, chest/vase/reload_v2/pickup 4×1).

**Codice gameplay (modificato):**
- `SandboxArtAssetFactory.cs`: carica i 21 sprite reali + props environment nuovi (PalmTree/RuinPillar/Cactus/RunedChest)
- `GameplayLabBuilder.cs`: build scena con vasi/obelishi validi (collider+renderer), **golem baked**, e **palme/colonne/cactus** spawnati in world dressing
- `SandboxActorVisual.cs`: armi in mano **ingrandite** (0.62 vs 0.16), crea/guida WeaponAnimator + NomadAnimator
- `SandboxVisualEffects.cs`: proiettili **ingranditi** (0.55×0.32) e usano lo sprite rune ciano reale; effetto scavo attenuato (dust burst, non più swirl)
- `SandboxReloadBar.cs`: barra ricarica **ridisegnata** (bordo oro + label RELOADING)
- `NomadAnimator.cs`: guardia contro AnimatorController null (niente warning)
- `PrototypeCombat/Digging/Controller`: scavo FX, stealth, depth gating
- `SandboxModernHUD.cs`: orchestratore inventario TAB (glass panel, barre, stat card, stealth)

---

## 2. MAPPA ESATTA DEGLI ASSET (dove trovarli)

| Asset | Path in repo |
|---|---|
| Sprite armi/env/mob (17) | `Game/Assets/Sandsunder/Art/Runtime/Processed/*.png` |
| Fogli animazione (21) | `Game/Assets/Sandsunder/Art/Runtime/Processed/Anims/*.png` |
| Pannello HUD | `Game/Assets/Sandsunder/Art/Runtime/Processed/ui_glass_panel.png` |
| Manifest animazioni | `Game/Assets/Sandsunder/Art/Generated/AnimationBuildManifest.asset` |
| Texture sabbia/rovine | `Game/Assets/Sandsunder/Art/Runtime/Processed/{sand,ruin}_*.png` |
| Builder scena | `Game/Assets/Sandsunder/Editor/GameplayLabBuilder.cs` |
| Asset factory | `Game/Assets/Sandsunder/Editor/SandboxArtAssetFactory.cs` |
| Clip esistenti | `Game/Assets/Sandsunder/Art/Generated/*.anim` |

---

## 3. COSA DEVI FARE (in ordine)

### FASE A — Ricostruisci e COMMITTA i clip animazione
1. Esegui `Sandsunder > Art > Build Animation Clips From Manifest` (genera i 21 `.anim` da `AnimationBuildManifest.asset`).
2. **COMMITTALI e PUSHALI obbligatoriamente** — i clip precedentemente annunciati NON erano nel repo (causa animazioni non visibili).
3. Verifica su disco che i file `.anim` esistano in `Art/Generated/`.

### FASE B — Collega il vero art al personaggio e ai nemici
4. Il player deve usare `nomad_32.png` (non il fallback blu procedurale). Nemici → `spitter_32.png`/`mob_dune_spitter_32.png`.
5. Armi in mano e inventario → 6 sprite reali (shovel/rifle/scimitar/shotgun/blaster/mortar).
6. Proiettili → `proj_sentinel_cyan_rune_32.png` (già fatto in codice, verifica che si veda).

### FASE C — Implementa TUTTE le animazioni (incluse quelle già presenti)
7. Assigna i clip al `WeaponAnimator` del player (idle/fire/reload/swing per ogni arma).
8. Collega `NomadAnimatorController` a run/walk/roll/dig/stealth usando i nuovi fogli:
   - `Nomad_RunNew`/`WalkNew`/`RollNew`/`DigNew` (param Speed/IsMoving/IsRolling/IsDigging)
   - `Nomad_StealthCrouch` (param IsStealthed)
9. Anima i mob: `Spitter_Idle`, `Spitter_DeathBurst`; il boss: `Golem_Charge`.
10. Anima il mondo: `Chest_Open`, `Vase_Break`, `Pickup_Bob` (assegna agli script di chest/vaso/pickup).

### FASE D — Fix dei problemi di gameplay segnalati
11. **Scavo**: il click destro NON deve scavare quando si attacca; l'animazione di scavo deve usare `Nomad_DigNew` + effetto dust (già attenuato). Effetto "sabbia scavata" rifinito.
12. **Reload**: barra ridisegnata già in codice; collega l'animazione `Rifle_Reload_V2`. Ogni arma ha le sue feature (danno/portata/cadenza dal `Design/balance/weapons.csv`).
13. **Ingrandire proiettili** (già fatto 0.55) e verifica telegrafo visibile.
14. **Transizione sotterraneo**: usa `DigDepthSystem` + `SubterraneanStealth` per una transizione visiva chiara (fade + cambia layer + silhouette ciano #00F0E6), non solo cambio colore.
15. **Tunnel**: `PrototypeTunnelSystem` deve rispondere al layer (Surface/Subterranean) con feedback visivo.

### FASE E — UI e Minimappa (usa ui_glass_panel)
16. **TAB inventory**: usa `ui_glass_panel.png` come pannello; icone 32×32 reali ingrandite; layout pulito (pannello vetro + oro + ciano).
17. **Minimappa**: rifalla — rettangolo semitrasparente con bordo dorato, icone per player/nemici/chest (stile premium). Attualmente è un quadratino procedurale.
18. **Barra HP/Stamina**: usa `StatBarWidget` (già in codice) con colori neon.

### FASE F — MONDO
18. Verifica `Build Gameplay Lab` spawni: palme, colonne, cactus, chest runica, vasi, obelischi, golem.
19. Se manca qualcosa a runtime, aggiungi a `SandboxSceneInitializer`.

### FASE G — MOB E COMPORTAMENTI RICHIESTI IN PRECEDENZA (importante)
20. **TARTARUGA DEL DESERTO (mob richiesto dall'utente)** — non esiste ancora nel repo. Va:
    - Generata su Higgsfield come sprite 32×32 (o 64×64) con **animazioni** (idle, walk, attack, hurt, death) su fondo magenta, top-down 3/4, carapace di sabbia/arenaria con motivi runici.
    - Aggiunta a `Design/balance/enemies.csv` con una riga (tier, hp, speed, damage, behaviour).
    - Il comportamento richiesto: **si ritira nel guscio quando viene attaccata** (invulnerabile per qualche secondo, animazione di retrazione), **esce e attacca** a distanza ravvicinata, si muove lentamente in patrol. Se il player è sotterraneo (SubterraneanStealth) non la vede/aggro.
    - Integrare l'AI in un nuovo script `SandstormTurtleAI.cs` (o simile) seguendo il pattern di `SandstormGolemAI`, spawnata nel builder.
21. **Mob già pianificati ma NON ancora vivi/sprtate** (da `Design/balance/enemies.csv`) — implementare con sprite e AI:
    - `mob_sandling` (Sandling): chase semplice con pausa pre-lunge.
    - `mob_burrower` (Burrower): scia di scavo visibile + emergenza ritardata.
    - `mob_scorpion` (Glass Scorpion, elite): alterna artigli frontali a una coda a cono marcata + carica.
    - `mob_sun_maw` (Sun Maw, guardian): sputa, scava corsie, summon add.
    - Nota: `mob_spitter` (Dune Spitter) è già presente come sprite+sheet — va collegato e animato (Fase C punto 9).
22. Per ogni nuovo mob: registra sprite in `SandboxArtAssetFactory`, spawna nel builder, aggancia le animazioni, aggiorna `Design/balance/enemies.csv` e `Design/provenance/*.json`.

### FASE H — GAMEPLAY SOTTERRANEO (sopra/sotto)
23. Transizione completa: quando il player scende (depth>=2) → fade + switch layer + silhouette ciano `#00F0E6`. Quando risale → fade inverso.
24. Gli elementi di superficie (chest, vasi, door) non interagibili da sotterraneo (già parzialmente in codice).
25. Il golem e i mob di superficie non devono vedere/attaccare il player sotterraneo (già in codice per spitter/proiettili — estendi a tutti i mob).

---

## 4. CRITERI DI ACCETTAZIONE
- [ ] 21 clip `.anim` presenti nel repo e pushati
- [ ] Console 0 errori in Play Mode
- [ ] Player usa il vero art (non il fallback blu)
- [ ] Run/walk/roll/dig/stealth animati
- [ ] Mob e golem animati; chest/vaso/pickup animati
- [ ] Armi visibili (ingrandite) e animate; reload barra visibile
- [ ] Proiettili grandi, visibili, con sprite rune
- [ ] Scavo non conflittuale con l'attacco; effetto sabbia pulito
- [ ] Transizione sotterranea visibile (silhouette ciano)
- [ ] Minimappa e TAB rifatti con ui_glass_panel
- [ ] Mappa popolata con palme/colonne/cactus/chest

---

## 5. COSA SEGNALARE (se non puoi farlo tu)
- Se un asset è brutto/mancante: NON generarlo, segnalalo (verrà rifatto su Higgsfield).
- Se un'animazione non si aggancia a un oggetto in scena: dimmelo con il nome dell'oggetto.
- Se c'è lavoro manuale necessario (es. assegnare clip a prefab specifici): ELENCA in modo esplicito cosa va fatto a mano in Unity, con il percorso esatto.

---

## 6. REGOLE (AGENTS.md)
- Niente push senza conferma al momento dell'azione.
- Niente `Sandsunder.Editor` nell'assembly `Sandsunder.Gameplay` (CS0234) — usa `UnityEditor.AssetDatabase` solo dentro `#if UNITY_EDITOR`.
- `RequireComponent` prima di `AddComponent` (vasi, obelischi, golem già gestiti).
- Committa ogni fase completata e pusha.