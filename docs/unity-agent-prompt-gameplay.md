# SANDSUNDER — PROMPT GAMEPLAY: attua e verifica tutte le modifiche discusse + associa le animazioni

> Repo: `NebulasStudio/Digger` — branch `main` — Unity 6.3 LTS (6000.3.21f1)
> Ultimo commit: `8a4604c`. Leggi AGENTS.md prima di modificare.

## OBIETTIVO
Attuare (o verificare che siano attuate) TUTTE le modifiche di gameplay discusse, e associare le animazioni corrette a ogni sistruppo del gioco. Questo è il checkpoint finale prima di considerare il gameplay "vivo".

---

## PARTE 1 — COSTRUISCI I CLIP ANIMAZIONE (obbligatorio, mancano)
1. Esegui `Sandsunder > Art > Build Animation Clips From Manifest`. Il manifest `Art/Generated/AnimationBuildManifest.asset` ha **21 voci**:
   `Shovel_Idle, Shovel_Swing, Rifle_Idle, Rifle_Fire, Rifle_Reload, Shotgun_Idle, Blaster_Idle, Blaster_Fire, Scimitar_Swing, Nomad_StealthCrouch, Spitter_Idle, Spitter_DeathBurst, Golem_Charge, Nomad_RunNew, Nomad_WalkNew, Nomad_RollNew, Nomad_DigNew, Chest_Open, Vase_Break, Pickup_Bob, Rifle_Reload_V2`.
2. **COMMITTA e PUSHA i `.anim` generati** — finora i clip nuovi NON risultano nel repo (su disco ci sono solo i 9 vecchi `Nomad_*/Player_*`). Questa è la causa principale delle animazioni "non visibili".
3. Verifica che i file `.anim` esistano in `Game/Assets/Sandsunder/Art/Generated/`.

---

## PARTE 2 — ASSOCIA LE ANIMAZIONI CORRETTE (wiring)
Il `WeaponAnimator` e il `NomadAnimator` sono GIÀ creati e guidati in `SandboxActorVisual.cs`. Il tuo compito è ASSEGNARE le clip/frame corretti.

### 2.1 Nomad / Player (personaggio)
- `NomadAnimatorController.controller` (in `Art/Generated/`) deve usare i clip nuovi:
  - Idle → `Nomad_Idle` (o Idle nuovo se generato)
  - Walk → `Nomad_WalkNew` (param `Speed`, `IsMoving`)
  - Run → `Nomad_RunNew`
  - Roll → `Nomad_RollNew` (param `IsRolling`)
  - Dig → `Nomad_DigNew` (param `IsDigging`)
  - StealthCrouch → `Nomad_StealthCrouch` (param `IsStealthed`)
- Verifica che i parametri del controller coincidano con quelli settati da `NomadAnimator.cs` (`Speed`, `IsMoving`, `IsRolling`, `IsDigging`, `IsStealthed`).

### 2.2 Armi (WeaponAnimator sul weaponRoot del player)
Assegna i frame per ogni arma dell'inventario (`PrototypeInventoryHUD.InventoryItems`):
- `shovel.default` → Idle: `Shovel_Idle`, Swing: `Shovel_Swing`
- `rifle.brass` → Idle: `Rifle_Idle`, Fire: `Rifle_Fire`, Reload: `Rifle_Reload` (o `Rifle_Reload_V2`)
- `shotgun.heavy` → Idle: `Shotgun_Idle`, Fire: (manca — usa fallback o idle)
- `blaster.rune` → Idle: `Blaster_Idle`, Fire: `Blaster_Fire`
- `sword.scimitar` → Swing: `Scimitar_Swing`
- `icon.mortar_sandstorm` → Idle/Fire se disponibili
Il `WeaponAnimator` ha 4 array: `idleFrames`, `fireFrames`, `reloadFrames`, `swingFrames`. Popolali dai clip generati (usa `AnimationClipBuilder.ClipFrames(clip)` per estrarre i frame).

### 2.3 Mob e boss
- `sandbox_actor` dei Dune Spitter → `Spitter_Idle` (loop), `Spitter_DeathBurst` (one-shot su morte)
- `SandstormGolemAI` → `Golem_Charge` per lo stato Charge/Telegraph
- (se esiste già) `SandstormTurtleAI` → animazioni della tartaruga quando aggiunta

### 2.4 Mondo / interattivi
- Chest / dig nodes → `Chest_Open` (one-shot quando si apre)
- `PrototypeDestructibleVase` → `Vase_Break` (one-shot su rottura)
- `PrototypePickup` → `Pickup_Bob` (loop, bob/glow)

---

## PARTE 3 — VERIFICA/ATTUA LE MODIFICHE DI GAMEPLAY DISCUSSE

### 3.1 Attacco vs scavo (bug segnalato)
- Il **click destro NON deve scavare quando si sta attaccando** con altra arma. Verifica la logica in `PrototypeCombat.cs` (handling `shovelAction`): lo scavo parte SOLO con `shovel.default` equipaggiata e non in attacco.
- L'animazione di scavo deve usare `Nomad_DigNew` + effetto dust (già attenuato in `SandboxVisualEffects.SpawnSandSpiral`). L'effetto "sabbia scavata" deve essere pulito e leggibile.

### 3.2 Ricarica
- Barra di ricarica RIDISEGNATA già in codice (`SandboxReloadBar.cs`: bordo oro + label RELOADING + 72×10). Verifica che appaia SOLO durante la ricarica e che sia visibile/leggibile.
- Collega l'animazione di reload (`Rifle_Reload`/`Rifle_Reload_V2`) quando un'arma da fuoco ricarica.
- Ogni arma ha le sue feature (danno/portata/cadenza da `Design/balance/weapons.csv`): verifica che switchare arma aggiorni il comportamento.

### 3.3 Proiettili
- Proiettili INGRANDITI (0.55×0.32) e con sprite reale `proj_sentinel_cyan_rune_32.png` (già in `SandboxProjectileVisual`). Verifica che si vedano chiaramente e abbiano il telegrafo visibile.
- Il proiettile runico del blaster/mortaio deve corrispondere allo sprite.

### 3.4 Stealth / sotterraneo
- `DigDepthSystem` + `SubterraneanStealth`: quando depth≥1 il player diventa silhouette ciano `#00F0E6` @65%, sortingOrder -10, invulnerabile a proiettili/nemici di superficie. **VERIFICA che la transizione sopra/sotto sia visibile** (non solo un cambio colore).
- `PrototypeTunnelSystem` deve rispondere al layer (Surface_L0 / Subterranean_L1) con feedback visivo (fade/ambient).
- Chest/vasi/door di superficie NON interagibili da sotterraneo (già gated in `PrototypeDigging`).

### 3.5 Mondo e mappa
- `Build Gameplay Lab` deve spawnare: palme, colonne rovine, cactus, chest runica, vasi (con collider), obelischi (con renderer), ruin doors, golem boss, 3 spitter.
- Se manca qualcosa a runtime, aggiungilo a `SandboxSceneInitializer.cs`.

### 3.6 UI / Minimappa
- TAB inventory: pannello `ui_glass_panel.png`, icone 32×32 reali ingrandite, barre HP/Stamina (`StatBarWidget`), card arma, indicatore furtività.
- Minimappa: rifatta (rettangolo semitrasparente, bordo dorato, icone player/nemici/chest).

### 3.7 Mob richiesti in precedenza
- **Tartaruga del deserto**: se lo sprite/animazioni sono stati aggiunti, integra AI (`SandstormTurtleAI.cs`) con comportamento "si ritira nel guscio quando attaccata, esce e attacca da vicino, patrol lento, ignora player sotterraneo". Se NON è ancora generata, segnalalo (verrà fatta su Higgsfield).
- Mob pianificati (`Design/balance/enemies.csv`): `mob_sandling`, `mob_burrower`, `mob_scorpion`, `mob_sun_maw` — se non implementati, segna come "da fare" senza bloccarti.

---

## CRITERI DI ACCETTAZIONE
- [ ] 21 clip `.anim` generati, committati e pushati
- [ ] Console 0 errori in Play Mode
- [ ] Player usa il vero art e ha run/walk/roll/dig/stealth animati
- [ ] Armi visibili (ingrandite) e con animazioni idle/fire/reload/swing
- [ ] Proiettili grandi con sprite rune
- [ ] Scavo NON conflittuale con l'attacco; reload barra visibile
- [ ] Transizione sotterranea con silhouette ciano visibile
- [ ] Minimappa e TAB rifatti; mappa popolata (palme, rovine, cactus, chest, golem)
- [ ] Spitter, golem animati; chest/vaso/pickup animati

## COSA SEGNALARE
- Asset brutto/mancante: NON generarlo, segnalalo (verrà rifatto su Higgsfield).
- Animazione non agganciabile: dimmi il nome dell'oggetto/prefab e il clip.
- Lavoro manuale necessario (es. assegnare clip a prefab specifici in Inspector): elenca esplicitamente cosa va fatto a mano in Unity con percorso esatto.

## REGOLE
- Niente push senza conferma. Niente `Sandsunder.Editor` in `Sandsunder.Gameplay` (CS0234). `RequireComponent` prima di `AddComponent`. Committa e pusha ogni parte completata.