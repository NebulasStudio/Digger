# Sandsunder — Animation Production Pipeline

> Pipeline per trasformare gli sheet animati (generati su Higgsfield, fondo magenta `#FF00FF`) in
> clip Unity implementati nel gameplay. Costruita partendo dagli asset già generati (Batch 1: armi).

## 1. Pipeline (3 step)

1. **Keying** — rimuove il magenta preservando la griglia frame:
   `python3 Tools/art/key_sheet.py <src> <out> --fill-glitch`
   (a differenza di `key_magenta_sprite.py`, NON croppa/resize: mantiene le dimensioni per lo slicing).
2. **Slicing + clip** — in Unity:
   `Sandsunder > Art > Build Animation Clips From Manifest`
   che legge un `AnimationBuildManifest` (SO), usa `SpriteSheetImporter` per tagliare la griglia e
   `AnimationClipBuilder` per creare i `.anim` in `Assets/Sandsunder/Art/Generated/`.
3. **Riproduzione runtime** — `WeaponAnimator` (frame-player) su un `SpriteRenderer` consuma gli
   array di frame per stato (Idle/Fire/Reload/Swing).

## 2. Manifest consigliato (Batch 1 — armi)

Crea in Unity: `Assets > Create > Sandsunder > Animation Build Manifest`, poi compila le voci:

| sourcePath (Assets/…) | clipName | cols | rows | ppu | fps | loop |
|---|---|---|---|---|---|---|
| Sandsunder/Art/Runtime/Processed/Anims/shovel_idle.png | Shovel_Idle | 4 | 1 | 64 | 12 | yes |
| Sandsunder/Art/Runtime/Processed/Anims/shovel_swing.png | Shovel_Swing | 4 | 1 | 64 | 12 | no |
| Sandsunder/Art/Runtime/Processed/Anims/rifle_idle.png | Rifle_Idle | 4 | 1 | 64 | 12 | yes |
| Sandsunder/Art/Runtime/Processed/Anims/rifle_fire.png | Rifle_Fire | 4 | 1 | 64 | 12 | no |
| Sandsunder/Art/Runtime/Processed/Anims/rifle_reload.png | Rifle_Reload | 3 | 3 | 64 | 12 | no |
| Sandsunder/Art/Runtime/Processed/Anims/shotgun_idle.png | Shotgun_Idle | 4 | 1 | 64 | 12 | yes |
| Sandsunder/Art/Runtime/Processed/Anims/blaster_idle.png | Blaster_Idle | 4 | 1 | 64 | 12 | yes |
| Sandsunder/Art/Runtime/Processed/Anims/blaster_fire.png | Blaster_Fire | 4 | 1 | 64 | 12 | no |
| Sandsunder/Art/Runtime/Processed/Anims/scimitar_swing.png | Scimitar_Swing | 4 | 1 | 64 | 12 | no |

Le clip vanno assegnate agli array del `WeaponAnimator` (idle/fire/reload/swing) sul prefab arma.

## 3. Valutazione qualità sheet (Batch 1) — da review umana (AGENTS.md)

| Sheet | Fondo magenta | Griglia | Qualità / Note |
|---|---|---|---|
| shovel_idle | sì | 4×1 | OK |
| shovel_swing | sì | 4×1 | ⚠ **Il soggetto è un personaggio che scava** (non la pala isolata) |
| rifle_idle | sì | 4×1 | OK (linee guida viola sottili sovrapposte) |
| rifle_fire | sì | 4×1 | OK |
| rifle_reload | sì | **3×3** | ⚠ griglia diversa + mano che aziona il fucile |
| shotgun_idle | sì | 4×1 | OK |
| shotgun_fire | — | — | ❌ **generazione fallita** (da rigenerare) |
| blaster_idle | sì | 4×1 | OK |
| blaster_fire | sì | 4×1 | ⚠ **glow ciano che sfuoca nel magenta** → masking difficile |
| scimitar_swing | sì | 4×1 | OK |

**Verdetto:** 5 fogli utilizzabili direttamente; 4 (shovel_swing, rifle_reload, blaster_fire, shotgun_fire)
richiedono rigenerazione con prompt corretti (palas isolata top-down, griglia 1×N, no glow sul fondo).

## 4. Prossimi lotti (per ogni asset del repo)

- **Nomad:** + STEALTH CROUCH
- **SandstormMortar:** Idle + Fire volley
- **Dune Spitter:** Idle · Patrol · Acid Spit · Death Burst
- **Sandstorm Golem:** Idle · Telegraph · Charge · Death Burst
- **Chest (dig nodes):** Open 3 stadi
- **Ruin Door:** Open (inserimento chiave)
- **Destructible Vase:** Break
- **Ancient Rune Obelisk:** Activation / idle glow
- **Key / Pickups (heal, relic):** Idle bob · Collect
- **SandTuft / DesertBone / CyanRune:** Idle sway / glow