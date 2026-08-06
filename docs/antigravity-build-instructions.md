# 🎮 ISTRUZIONI DI BUILD PER ANTIGRAVITY AI — BLOCCO ANIMAZIONI NOMAD

> Repo: `NebulasStudio/Digger` — branch `main` — commit `aec5f22` + `7c6985b` (sync)
> Progetto: `Game/` — cartella asset: `Game/Assets/Sandsunder/Art/Runtime/Animations/`
> Mittente: Higgsfield AI. Destinatario: Antigravity AI (Unity builder).

---

## 1. COSA È STATO GENERATO E PUSHATO

Sono stati generati **7 sprite sheet di animazione del NOMAD ufficiale** (giacca blu `#3466B8`, cappuccio bianco, sciarpa turchese `#26B8C6`), tutti con sfondo trasparente (magenta keyato), 32 PPU, e registrati nel manifest.

### 📁 File PNG in `Game/Assets/Sandsunder/Art/Runtime/Animations/` (con `.meta`)

| File | Griglia | Frame | Clip nel manifest |
|---|---|---|---|
| `nomad_walk.png` | 4×4 | 16 | `Nomad_Walk` |
| `nomad_run.png` | 4×4 | 16 | `Nomad_Run` |
| `nomad_dig.png` | 4×4 | 16 | `Nomad_Dig` |
| `nomad_melee_scimitar.png` | 4×1 | 4 | `Nomad_Melee` |
| `nomad_shoot_recoil.png` | 4×1 | 4 | `Nomad_ShootRecoil` |
| `nomad_hurt.png` | 2×1 | 2 | `Nomad_Hurt` |
| `nomad_death.png` | 4×2 | 8 | `Nomad_Death` |

### ✅ Manifest aggiornato (`Game/Assets/Sandsunder/Art/Generated/AnimationBuildManifest.asset`)
Aggiunte 7 voci: `Nomad_Walk`, `Nomad_Run`, `Nomad_Dig`, `Nomad_Melee`, `Nomad_ShootRecoil`, `Nomad_Hurt`, `Nomad_Death` — `pixelsPerUnit: 32`, `fps: 12`, griglie esatte, `loop` corretto.

---

## 2. ⚠️ PERSONAGGI SBAGLIATI DA NON USARE PER IL NOMAD

Nella cartella `Animations/` ci sono ANCHE questi file che **NON sono il Nomad** e **NON devono** essere bindati al personaggio principale:
- `wanderer_walk.png` (pellegrino in tunica gialla) — VIETATO
- `explorer_dig.png` (esploratore) — VIETATO
- `scout_run.png` (scout) — VIETATO
- `rogue_roll.png` (rogue) — VIETATO

**Regola:** il Nomad usa SOLO `nomad_*.png`. Se nel controller vedi riferimenti a wanderer/explorer/scout/rogue, **sostituiscili con le clip del Nomad** (`Nomad_Walk`, `Nomad_Run`, `Nomad_Dig`, `Nomad_Roll`→se presente, ecc.). Questi file sbagliati si possono rimuovere o ignorare.

---

## 3. 🔗 A COSA BINDARE OGNI ANIMAZIONE (mapping esatto)

### 3.1 `NomadAnimatorController` (Animator del personaggio)
| Stato | Clip (dal manifest) | Parametro |
|---|---|---|
| Idle | `Nomad_Idle` (o `Nomad_Walk` frame stabile) | - |
| Walk | `Nomad_Walk` | `Speed`, `IsMoving` |
| Run | `Nomad_Run` | `Speed` (alto), `IsMoving` |
| Dig | `Nomad_Dig` | `IsDigging` |
| StealthCrouch | `Nomad_StealthCrouch` | `IsStealthed` |
| Melee | `Nomad_Melee` | (trigger attacco) |
| ShootRecoil | `Nomad_ShootRecoil` | (trigger sparo) |
| Hurt | `Nomad_Hurt` | (trigger danno) |
| Death | `Nomad_Death` | (trigger morte) |

### 3.2 `WeaponAnimator` (arma separata sul `weaponRoot`)
Le animazioni del corpo sono **SENZA arma in mano** (scelta concordata). Le armi si animano a parte sul `WeaponAnimator`:
- `shovel.default` → `Shovel_Idle` / `Shovel_Swing`
- `rifle.brass` → `Rifle_Idle` / `Rifle_Fire` / `Rifle_Reload` / `Rifle_Reload_V2`
- `shotgun.heavy` → `Shotgun_Idle`
- `blaster.rune` → `Blaster_Idle` / `Blaster_Fire`
- `sword.scimitar` → `Scimitar_Swing`
- `icon.mortar_sandstorm` → (se disponibile)

**NON** disegnare l'arma nei frame del corpo del Nomad.

---

## 4. 🔨 COSA FARE ORA (ordine esatto)

1. **`Sandsunder > Art > Build Animation Clips From Manifest`** → genera i `.anim` da tutte le voci del manifest (inclusi i 7 nuovi del Nomad). Verifica che NON ci siano errori di slicing.
2. **Apri `NomadAnimatorController`** e assicurati che gli stati usino le clip del Nomad (mapping §3.1), NON wanderer/explorer/scout/rogue.
3. **Apri la scena `GameplayLab.unity`**, rigenera con `Sandsunder > Gameplay > Build Gameplay Lab`.
4. **Play Mode** e verifica: il Nomad cammina/corre/scava con il suo corpo reale (giacca blu), senza arma doppia, con animazioni fluide.
5. **Screenshot/Video** di verifica: Nomad in movimento (walk/run), scavo, attacco, morte.

---

## 5. LOGICA DI GENERAZIONE (perché così)

- **Corpo senza arma:** il `weaponRoot` è un renderer separato che applica l'arma a parte. Se il corpo avesse l'arma disegnata, in gioco ci sarebbe l'arma doppia.
- **Griglia esatta:** ogni cella è 32×32, il `SpriteSheetImporter` taglia `width/columns` × `height/rows`. Fogli 128×64 (4×2) e 128×32 (4×1) sono uniformi e senza residui.
- **Sfondo trasparente:** il magenta `#FF00FF` è stato keyato a trasparenza (`alphaIsTransparency=true`), come da regole.
- **PPU 32:** tutte le animazioni a 32 PPU, coerenti con lo sprite base `nomad_32.png`.

---

## 6. CRITERI DI ACCETTAZIONE
- [ ] 7 clip `.anim` del Nomad generati e pushati
- [ ] Controller usa SOLO clip del Nomad (niente wanderer/explorer/scout/rogue)
- [ ] Il Nomad si anima in Play (walk/run/dig/melee/shoot/hurt/death)
- [ ] Nessuna arma doppia (corpo senza arma)
- [ ] Console 0 errori
- [ ] Screenshot/video di verifica mostrati

## 7. REGOLE
- Niente push senza conferma. Niente `Sandsunder.Editor` in `Sandsunder.Gameplay` (CS0234). Committa e pusha ogni parte completata.