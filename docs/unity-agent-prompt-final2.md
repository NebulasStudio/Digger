# SANDSUNDER — PROMPT DEFINITIVO: RIFONDARE IL GAMEPLAY (scena + art + animazioni + meccaniche)

> Repo: `NebulasStudio/Digger` — branch `main` — Unity 6.3 LTS (6000.3.21f1)
> Stato: tutto pushato. **Il gioco NON mostra ancora le nuove feature.** Leggi AGENTS.md prima di modificare.

## ⚠️ DIAGNOSI PRINCIPALE (perché l'utente "non vede niente")
L'utente gioca ancora la **scena vecchia con sprite procedurali** (personaggio blu con cappuccio = fallback, NON nomad_32). Il vero art si carica SOLO quando la scena viene RIGENERATA. **Prima di tutto:**
1. Esegui `Sandsunder > Gameplay > Build Gameplay Lab` per rigenerare `Assets/Scenes/GameplayLab.unity` da zero.
2. Poi esegui `Sandsunder > Art > Build Animation Clips From Manifest` (i 21 clip, già nel manifest).
3. Verifica in Play che il personaggio sia il nomad reale (cappuccio/veste color sabbia, NON blu) e che il golem esista a nord.
4. Se dopo il rebuild vedi ancora il fallback blu: l'asset factory non carica `nomad_32.png` — DEBUG: controlla `SandboxArtAssetFactory.ImportSpriteOptional` e che il file esista in `Art/Runtime/Processed/nomad_32.png`.

---

## PARTE 1 — ORIENTAMENTO ARMA E MIRA (il problema più urgente)
**Sintomo:** "l'arma non è orientata come il mouse; i proiettili vanno a cazzo".
- Il `SandboxActorVisual.ApplyFacing()` ruota già `weaponRoot` sull'angolo di `explicitAim`. **Ma** se l'arma è uno sprite quadrato/simple essa non "legge" come direzionata.
- **Fix richiesto:** assicurarsi che l'arma in mano sia leggibile e punti nella direzione del mouse:
  - `weaponRoot.localRotation = Quaternion.Euler(0,0, angle)` — verifica che `angle` usi l'aim WORLD (mouse) e non un default.
  - Verify che `AimDirection` venga aggiornato ogni frame dall'input mouse (`TopDownPlayerController.OnPointerInput` → `aimArbiter.SubmitMouseWorldDirection`).
  - Se possibile, ruota l'INTERO visualRoot (body+arma) verso l'aim, non solo l'arma, per coerenza visiva.
- **Proiettili:** già usano `movement.AimDirection` — se "vanno a cazzo" è perché l'aim non è aggiornato. Debug: stampa `AimDirection` in `PrototypeCombat` al momento del fuoco.

---

## PARTE 2 — PROIETTILI DISTINTI PER ARMA (ora tutti uguali)
**Sintomo:** "i proiettili di ogni arma sono uguali".
- `SandboxProjectileVisual.Configure` ora carica SEMPRE `proj_sentinel_cyan_rune_32.png` (aggiunto in `#if UNITY_EDITOR`). Questo li rende tutti ciano.
- **Fix richiesto:** ogni arma deve avere il SUO proiettile:
  - Rifle brass → giallo/ottone, sprite allungato (proiettile di fucile)
  - Blaster rune → ciano runico (proj_sentinel_cyan_rune)
  - Shotgun → piombo/arancione, più proiettili con spread
  - Mortaio → proiettile ad arco/deriva sabbiosa
  - (le scimitarra/pala non sparano)
- Per armi prive di sprite dedicato, genera un proiettile procedurale GIÀ differenziato per colore+forma (usa `PrototypePixelArt.GetCachedSprite(Projectile, <colore>)` con forme diverse per arma), NON lo sprite ciano unico.

---

## PARTE 3 — ANIMAZIONI (attacco, melee, scavo, roll, corsa, stealth)
**Sintomi:** "melee fanno animazioni strane, pala sbagliata, roll non mi piace, niente animazioni nuove".
- **Wiring animazioni NON completo.** Il `WeaponAnimator` e `NomadAnimator` sono creati, ma i FRAME non sono assegnati in scena.
- **Fix richiesto (obbligatorio):**
  1. `NomadAnimatorController.controller` → assegna i clip nuovi: Walk→`Nomad_WalkNew`, Run→`Nomad_RunNew`, Roll→`Nomad_RollNew`, Dig→`Nomad_DigNew`, StealthCrouch→`Nomad_StealthCrouch`. Param: `Speed/IsMoving/IsRolling/IsDigging/IsStealthed`.
  2. `WeaponAnimator` sul weaponRoot → popola `idleFrames/fireFrames/reloadFrames/swingFrames` PER OGNI arma dai clip (`Shovel_Idle/Swing`, `Rifle_Idle/Fire/Reload/V2`, `Scimitar_Swing`, `Shotgun_Idle`, `Blaster_Idle/Fire`).
  3. **Melee (scimitarra/pala):** usa `Scimitar_Swing`/`Shovel_Swing` — NON l'animazione di scavo. Il taglio deve essere un arco pulito, non un movimento strano.
  4. **Scavo:** usa `Nomad_DigNew` SOLO quando si scava col destro e con shovel equipaggiata. Deve essere un loop di scavo chiaro (alza pala → colpisci → butta sabbia).
  5. **Roll:** migliora `Nomad_RollNew` — rotazione pulita del personaggio, non un tilt strano.
  6. **Corsa/stealth:** verifica che Walk/Run e StealthCrouch si attivino con i parametri giusti.
- Se un clip è "strano" (frame confusi), NON rigenerarlo: segnala quale, verrà rifatto su Higgsfield.

---

## PARTE 4 — MECCANICA SABBIA 3 STATI (quasi assente)
**Sintomo:** "non hai implementato la sabbia che scavo con 3 stati".
- `DigTerrainView` (3 stadi: Intact→Cracked→Opened) ESISTE ed è chiamato in `PrototypeDigging` (`SetCellDepth`). Ma potrebbe non essere visibile perché:
  - gli sprite stage (`DigIntactSprite.asset` ecc.) sono procedurali e piccoli, oppure
  - `DigTerrainView` non è istanziato in scena (dovrebbe esserlo via `SandboxSceneInitializer.EnsureRuntimeSystems`).
- **Fix richiesto:**
  1. Verifica che `DigTerrainView` esista in scena a runtime (Debug.Log in Awake).
  2. Migliora la VISIBILITÀ dei 3 stati: overlay più grande, più contrasto, texture di sabbia che si deforma (usa `sand_rolled`/`sand_final`).
  3. Lo scavo deve mostrare chiaramente: cella intatta → crepe → cratere aperto.

---

## PARTE 5 — MECCANICA TUNNEL (entrare e vedere in modo diverso)
**Sintomo:** "non hai implementato il tunnel: entri dentro e vedi il gioco diverso".
- `DigDepthSystem` + `SubterraneanStealth` esistono (silhouette ciano a depth≥1). Ma la transizione "world visivamente diverso sottoterra" NON è implementata.
- **Fix richiesto:**
  1. Quando il player scende (depth≥2): transizione visiva (fade-out + switch layer + fade-in) con palette sotterranea distinta (ambient scuro, sabbia scura, tunnel visibile).
  2. `PrototypeTunnelSystem` deve rispondere al layer (Surface_L0 / Subterranean_L1) cambiando il rendering del mondo (colore ambient, overlay tunnel), non solo un piccolo cambio di tinta.
  3. Il player sottoterra deve essere una silhouette ciano traslucida chiaramente visibile attraverso la sabbia.

---

## PARTE 6 — MAPPA E MONDO (diversificare)
**Sintomo:** "la mappa è sostanzialmente uguale".
- Il builder spawna già palme/colonne/cactus/chest runica/golem. **Ma** se la scena non è rigenerata (Parte 0), non si vedono.
- **Fix richiesto:** dopo il rebuild, verifica che la mappa abbia: palme, colonne, cactus, chest, vasi, obelischi, rovine, golem, 3 spitter, tartaruga. Se è ancora piatta, aggiungi varietà di terreno (zone di sabbia diversa con `sand_feather_*`/`sand_final_*`), più rovine e resti architettonici.

---

## PARTE 7 — UI E MINIMAPPA
**Sintomo:** "non vedo la nuova UI; il menu tab fa schifo; la minimappa fa schifo".
- Il TAB inventory FUNZIONA (screenshot 3 lo mostra) ma è testuale/procedurale. `SandboxModernHUD` e `ui_glass_panel.png` esistono.
- **Fix richiesto:**
  1. Usa `ui_glass_panel.png` come sfondo del pannello TAB (vetro + bordo oro + ciano), non un pannello piatto.
  2. Minimappa: `SandboxMinimap` — rifalla con bordo dorato, icone per player/nemici/chest, sfondo semitrasparente.
  3. Sostituisci le barre/icone procedurali con quelle reali dove possibile.

---

## PARTE 8 — CRITERI DI ACCETTAZIONE (verifica finale in Play)
- [ ] Dopo `Build Gameplay Lab`: personaggio = nomad reale (NON blu), golem a nord, tartaruga a sud, mondo popolato
- [ ] Console 0 errori
- [ ] Arma in mano orientata verso il mouse; rotazione fluida
- [ ] Proiettili distinguibili per arma (colore/forma/velocità)
- [ ] Melee/scavo/roll/corsa/stealth con le animazioni corrette
- [ ] Scavo mostra 3 stati della sabbia chiaramente
- [ ] Transizione tunnel visibile (palette sotterranea diversa + silhouette ciano)
- [ ] Mappa variegata (palme, rovine, cactus, chest, golem)
- [ ] TAB con pannello vetro; minimappa rifatta

## COSA SEGNALARE
- Clip/animazione "strana" o frame confusi → NON rigenerare, segnala quale (verrà rifatto su Higgsfield).
- Se dopo il rebuild il personaggio è ancora blu → bug factory, riporta il Debug.Log di `SandboxArtAssetFactory`.
- Lavoro manuale necessario (es. assegnare clip a prefab in Inspector) → elenca esplicitamente con percorso esatto.

## REGOLE
- Niente push senza conferma. Niente `Sandsunder.Editor` in `Sandsunder.Gameplay` (CS0234). `RequireComponent` prima di `AddComponent`. Committa e pusha ogni parte completata.