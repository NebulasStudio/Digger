# SANDSUNDER — Manuale di consegna per l'Agente Compilatore Unity

> Stato: repo `NebulasStudio/Digger` branch `main`, sync con Unity 6.3 LTS (6000.3.21f1).
> Ultimo commit: `06c28f7`. Tutto pushato, working tree pulito.

---

## 1. Riepilogo tecnico delle modifiche (cosa è stato integrato)

### A. Asset grafici 32×32 (nuovi, in `Art/Runtime/Processed/`)
11 sprite già importati con `.meta` Unity (sprite, pivot 0.5/0.5, pixelsPerUnit 32, Point filter, trasparenza):
- `shovel_default_32.png`, `rifle_brass_32.png`, `sword_scimitar_32.png`, `shotgun_heavy_32.png`, `blaster_rune_32.png`
- `env_ruin_door_32.png`, `env_vase_destructible_32.png`, `env_relic_chest_32.png`, `mob_dune_spitter_32.png`
- `icon_mortar_sandstorm_32.png`, `proj_sentinel_cyan_rune_32.png`

### B. Fogli di animazione (13, in `Art/Runtime/Processed/Anims/`)
già keyed (magenta rimosso), griglia nota:
| Sheet | grid | loop |
|---|---|---|
| shovel_idle / shovel_swing | 4×1 | sì / no |
| rifle_idle / rifle_fire / rifle_reload | 4×1 / 4×1 / **3×3** | sì / no / no |
| shotgun_idle | 4×1 | sì |
| blaster_idle / blaster_fire | 4×1 | sì / no |
| scimitar_swing | 4×1 | no |
| nomad_stealth_crouch | 4×2 | sì |
| spitter_idle / spitter_death_burst | 4×2 | sì / no |
| golem_idle_charge | 4×4 | no |

### C. Pipeline animazioni (editor, in `Sandsunder.Editor`)
- `SpriteSheetImporter.cs` — slice griglia → `Sprite[]` (pivot 0.5/0.08), `filterMode=Point`, `Uncompressed`
- `AnimationClipBuilder.cs` — consuma un `AnimationBuildManifest` e produce `.anim` in `Art/Generated/`
- `AnimationBuildManifest.cs` + **asset `Art/Generated/AnimationBuildManifest.asset`** già creato (13 voci)

### D. Runtime gameplay (nuovi, in `Sandsunder.Gameplay`)
- `WeaponAnimator.cs` — frame-player arma (idle/fire/reload/swing), auto-creato sul weaponRoot di `SandboxActorVisual`
- `NomadAnimator.cs` — guida l'AnimatorController (Speed/IsMoving/IsRolling/IsStealthed), agganciato al bodyRoot
- `SandstormGolemAI.cs` — boss a stati (Idle→Telegraph→Charge→Cooldown→Dying), nucleo runico ciano; **spawnato dal builder** e dallo initializer
- `DigDepthSystem.cs` — owner depth (0 superficie, ≥1 sotterraneo), eventi `DepthChanged`/`SubterraneanChanged`
- `SubterraneanStealth.cs` — silhouette ciano #00F0E6 @65% sortingOrder -10, invulnerabile ai proiettili di superficie
- `DigTerrainView.cs` — overlay 3 stadi per cella (pool 256)
- `SandCrepeCracksFX.cs` — fratture a stella transitorie (zero alloc per-frame)
- `SandDustEmitter.cs` — polvere continua durante channeling
- `SandboxModernHUD.cs` — orchestratore UI inventario TAB (glass panel, barre HP/Stamina, weapon stat card, stealth indicator)

### E. File modificati
- `GameplayLabBuilder.cs` — fix RequireComponent vasi/obeliscchi + spawn golem baked
- `SandboxSceneInitializer.cs` — fix collider vasi + auto-istanzia i 4 sistemi runtime
- `PrototypeCombat.cs` — hook `SandDustEmitter` nel dig channeling; stealth via `SubterraneanStealth`
- `PrototypeDigging.cs` — hook `DigTerrainView` + `SandCrepeCracksFX` + gating interazione in sotterraneo
- `PrototypeInventoryHUD.cs` — carica sprite reali 32×32 via `AssetDatabase` (fix CS0234)
- `TopDownPlayerController.cs` — silhouette ciano quando depth≥1
- `SandboxActorVisual.cs` — crea/guida `WeaponAnimator` + `NomadAnimator`
- `SandboxArtAssetFactory.cs` — carica gli 11 sprite reali (fallback procedurale sotto)

---

## 2. FIX DI COMPILAZIONE NOTI (verifica prioritaria)
- **CS0234**: qualsiasi riferimento a `Sandsunder.Editor` dall'assembly `Sandsunder.Gameplay` è **vietato** (l'asmdef Gameplay NON referenzia Editor, sarebbe dipendenza circolare). `PrototypeInventoryHUD`, `DigTerrainView`, `SandstormGolemAI` usano `UnityEditor.AssetDatabase` dentro `#if UNITY_EDITOR` — pattern corretto.
- **RequireComponent da rispettare** quando si spawna via `AddComponent`:
  - `PrototypeDestructibleVase` → `SpriteRenderer` + `Collider2D` (Builder e Initializer già corretti)
  - `PrototypeAncientRuneObelisk` → `SpriteRenderer` (Builder già corretto)
  - `SandstormGolemAI` → `PrototypeHealth` + `Rigidbody2D` (già gestito)

---

## 3. COSA FARE IN UNITY (ordine esatto)

### Step 1 — Aggiornare il progetto
Già fatto dal team (git pull). Verificare con `Assets > Refresh` / Ctrl+R che la console non abbia errori di compilazione.

### Step 2 — Generare i clip di animazione
Menu: **`Sandsunder > Art > Build Animation Clips From Manifest`**
- Legge `Art/Generated/AnimationBuildManifest.asset` (13 voci)
- Per ogni voce: slice + build `.anim` in `Art/Generated/`
- Requisito: i PNG in `Anims/` devono essere importati come `SpriteImportMode.Multiple` (lo fa `SpriteSheetImporter.SliceSheet`)
- Output atteso: `Shovel_Idle.anim`, `Rifle_Fire.anim`, `Golem_Charge.anim`, ecc.
- Se il menu Art non compare: `Assets > Refresh`, o errore di compilazione → leggere Console

### Step 3 — Rigenerare la scena Gameplay Lab
Menu: **`Sandsunder > Gameplay > Build Gameplay Lab`**
- Ricostruisce `Assets/Scenes/GameplayLab.unity` da zero
- Conterrà: player + nomad, 3 dune spitter, dig nodes, vasi (con collider), obelischi (con renderer), ruin doors, **golem boss**, e i 4 sistemi runtime
- Se Unity chiede di salvare modifiche alla scena: confermare (la scena verrà comunque ricostruita)

### Step 4 — Play & verifica visuale
Premere Play nella scena rigenerata. Verificare:
1. **Console pulita** (zero errori) dopo il build
2. **Golem** a nord (0,9): colosso arenaria, nucleo runico ciano fluttuante, carica il player
3. **Inventario TAB** (tasto Tab): pannello vetro scuro, 11 icone 32×32, barre HP/Stamina, card arma, indicatore furtività
4. **Scavo** (tasto destro con pala): fratture a stella + polvere continua + terreno 3 stadi (intatto→crepato→cratere)
5. **Stealth**: dopo 2 scavi profondi → il Nomad diventa silhouette ciano traslucida; proiettili/Spitter lo sorvolano
6. **Animazioni arma**: sparare/colpire con le armi → i frame del WeaponAnimator (se i clip sono costruiti e assegnati)

---

## 4. NOTE / LIMITI
- I clip `.anim` costruiti allo Step 2 NON sono ancora **assegnati** ai prefab/WeaponAnimator in scena: il wiring visuale delle animazioni arma richiede aggancio manuale (assegnare gli array `idleFrames`/`fireFrames`/`reloadFrames`/`swingFrames` sul componente `WeaponAnimator` del weaponRoot) — task di rifinitura visiva.
- `DigTerrainView`/`SandstormGolemAI` caricano sprite stage/rune via `AssetDatabase` SOLO in editor; a runtime i colori/fallback procedurali restano.
- I fogli `shovel_swing`, `rifle_reload`, `blaster_fire` erano segnalati come "da review" (soggetto mescolato / glow) nell'animation-pipeline doc — da validazione umana prima dello shipping (AGENTS.md).
- Nessun commit di provenienza (`Design/provenance/*.json`) è stato ancora aggiornato per gli 11 nuovi sprite: da compilare per rispettare le regole repo.

## 5. DOMANDE DI VERIFICA PER L'AGENTE
- Dopo `Build Animation Clips From Manifest`: quanti `.anim` generati? (attesi 13)
- Dopo `Build Gameplay Lab`: ci sono errori in console? (attesi 0)
- In Play: il golem esiste? le icone inventario sono gli sprite 32×32 reali o i quadratini procedurali?