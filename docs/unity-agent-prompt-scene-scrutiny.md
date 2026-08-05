# SANDSUNDER — PROMPT CRITICO: METTERE GLI ASSET IN SCENA (verifica folder Unity)

> Repo: `NebulasStudio/Digger` — branch `main` — Unity 6.3 LTS (6000.3.21f1)
> Stato: TUTTI gli asset e le animazioni SONO nel progetto (verificati dagli screenshot dell'utente). Ma la SCENA NON li usa — sono "morti" nelle folder. Il gameplay giocato è ancora il vecchio prototipo.

## ⚠️ DIAGNOSI CONFERMATA (leggi prima)
Gli screenshot dell'utente mostrano che in `Assets/Sandsunder/Art/` CI SONO: animazioni (Blaster_Fire, Golem_Charge, Nomad_*, Chest_Open...), sprite (DigCracked/DigOpened, nomad, spitter, sand_tile, ruin_tile), e UI (GlassPanel, StatBarWidget, TabInventory, WeaponStatCard). **Il problema è che la scena di gioco NON li utilizza.**

**CRITICO — verifica la scena giusta:** il titolo della finestra Unity dell'utente è "PlayLab", ma la scena corretta è `GameplayLab.unity`. **Assicurati di lavorare/verificare su `Assets/Scenes/GameplayLab.unity`** (quella in `EditorBuildSettings`), NON su un'altra scena di test. Se esistono più scene, chiarisci quale è quella di gioco.

**Obiettivo:** far sì che il gioco GIOCATO mostri gli asset e le animazioni presenti nelle folder. Questo NON è un problema di asset mancanti — è un problema di MESSA IN SCENA (wiring).

---

## FASE 0 — VERIFICA FOLDER (conferma che stai guardando il progetto giusto)
Verifica in Unity Project che esistano e APRILI per conferma:
1. `Assets/Sandsunder/Art/Generated/` → clip `.anim` (Blaster_Fire, Golem_Charge, Nomad_Walk/Run/Roll/Dig/Stealth, Chest_Open, Vase_Break, Pickup_Bob, Shovel_*, Rifle_*, Scimitar_Swing, Shotgun_Idle, Spitter_*) + sprite `.asset` (DigIntact/Cracked/Opened, nomad, spitter)
2. `Assets/Sandsunder/Art/Runtime/Processed/` → i PNG 32×32 reali (nomad_32, spitter_32, shovel_default, rifle_brass, ecc.)
3. `Assets/Sandsunder/Art/Source/Higgsfield/` → gli `hf_asset_*` (sorgenti)
4. `Assets/Sandsunder/Gameplay/UI/` → GlassPanel, StatBarWidget, StealthIndicator, TabInventoryController, WeaponStatCard
5. **`Assets/Scenes/GameplayLab.unity`** → la scena di gioco corretta

**Riporta alla fine:** quali folder esistono, quali no, e in quale scena stai lavorando.

---

## FASE 1 — RIGENERA LA SCENA CORRETTA (PUO risolvere tutto)
1. Verifica che la scena attiva/apribile sia `Assets/Scenes/GameplayLab.unity`.
2. Esegui `Sandsunder > Gameplay > Build Gameplay Lab` per RIGENERARLA da zero con gli asset reali.
3. Esegui `Sandsunder > Art > Build Animation Clips From Manifest` (21 clip già nel manifest).
4. **Play** e verifica: il personaggio deve essere il nomad reale (NON il quadrato/fALLO blu procedurale).

## FASE 2 — SE ANCORA VEDI IL FALLBACK (debug factory)
Se dopo il rebuild il personaggio è ancora il fallback blu:
- Debug `SandboxArtAssetFactory.ImportSpriteOptional(string path, ...)`: stampa se `AssetImporter.GetAtPath(path)` ritorna null (file non trovato) o se `LoadAssetAtPath` ritorna null.
- Verifica i PERCORSI esatti: devono essere `Assets/Sandsunder/Art/Runtime/Processed/nomad_32.png` ecc. (NON `Art/Source/Higgsfield/hf_asset_*`).
- Se il nome file non corrisponde a quello su disco, CORREGGI il path nel factory.

---

## FASE 3 — ASSOCIA LE ANIMAZIONI (ora sono "vuote" nelle folder)
Le clip `.anim` esistono in `Art/Generated` ma NON sono collegate agli oggetti in scena. Collega:
1. **NomadAnimatorController** (in `Art/Generated`) → assegna: Walk→`Nomad_Walk`, Run→`Nomad_Run`, Roll→`Nomad_Roll`, Dig→`Nomad_Dig`, StealthCrouch→`Nomad_StealthCrouch`. Param: Speed/IsMoving/IsRolling/IsDigging/IsStealthed.
2. **WeaponAnimator** sul weaponRoot del player → popola i frame PER OGNI arma: `Shovel_Idle/Swing`, `Rifle_Idle/Fire/Reload`, `Rifle_Reload_V2`, `Scimitar_Swing`, `Shotgun_Idle`, `Blaster_Idle/Fire`.
3. **Mob**: spitter → `Spitter_Idle` + `Spitter_DeathBurst`; golem → `Golem_Charge`.
4. **Mondo**: chest → `Chest_Open`; vaso → `Vase_Break`; pickup → `Pickup_Bob`.

**Nota:** se un clip appare "vuoto" o con frame sbagliati, NON rigenerarlo — segnalalo (verrà rifatto su Higgsfield).

---

## FASE 4 — MESSA IN SCENA DELLE MECCANICHE (gioco giocato)
Il gioco deve essere INTERATTIVO e COMPRENSIBILE. Verifica/attua:

1. **Scavo 3 stati (sabbia):** quando scavi con la pala, la cella deve mostrare chiaramente 3 stadi (intatta → crepata → cratere) usando `DigIntact/DigCracked/DigOpened`. Se ora vedi "dig crack strano che non si capisce", RIFAI l'overlay: più grande, più leggibile, sulla cella giusta. `DigTerrainView` deve essere istanziato in scena.
2. **Tunnel:** scendendo (depth≥2) il world deve cambiare visivamente (ambient/palette sotterranea) + il player diventa silhouette ciano traslucida visibile attraverso la sabbia. NON solo un piccolo cambio colore.
3. **Proiettili distinti per arma:** rifle=giallo, blaster=ciano runico, shotgun=arancione/spread, mortaio=deriva sabbiosa. NON tutti uguali.
4. **Arma orientata al mouse:** l'arma in mano ruota verso il cursore; i proiettili partono nella direzione del mouse.
5. **Melee:** scimitarra/pala usano `Scimitar_Swing`/`Shovel_Swing` (arco pulito), NON l'animazione di scavo.
6. **Mappa interattiva e leggibile:** palme, colonne, cactus, chest, vasi, rovine sparati e visibili; terreno variegato.

---

## FASE 5 — UI E MINIMAPPA
- **TAB inventory:** usa `ui_glass_panel.png` come pannello (vetro+oro+ciano), icone 32×32 reali. `TabInventoryController` + `WeaponStatCard` + `StatBarWidget` + `StealthIndicator` (in `Gameplay/UI`) devono essere wired.
- **Minimappa** (`SandboxMinimap`): bordo dorato, icone player/nemici/chest, sfondo semitrasparente.

---

## CRITERI DI ACCETTAZIONE (Play Mode, scena GameplayLab)
- [ ] Il personaggio è il nomad reale (NON blu procedurale)
- [ ] Il golem esiste a nord, la tartaruga a sud, 3 spitter
- [ ] Run/walk/roll/dig/stealth animati correttamente
- [ ] Armi animate (idle/fire/reload/swing) e orientate al mouse
- [ ] Proiettili distinguibili per arma
- [ ] Scavo mostra 3 stati della sabbia leggibili
- [ ] Transizione tunnel visibile con palette diversa
- [ ] Mappa popolata e variegata
- [ ] TAB con pannello vetro; minimappa rifatta
- [ ] Console 0 errori

## COSA SEGNALARE
- Clip "vuota"/sbagliata → segnala quale (rifatta su Higgsfield)
- Personaggio ancora blu dopo rebuild → riporta il Debug.Log della factory
- Folder mancanti → elenco preciso
- Lavoro manuale (assegnare clip in Inspector) → elenca con percorso esatto

## REGOLE
- Niente push senza conferma. Niente `Sandsunder.Editor` in `Sandsunder.Gameplay` (CS0234). `RequireComponent` prima di `AddComponent`. Committa e pusha ogni parte completata.