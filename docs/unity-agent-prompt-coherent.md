# SANDSUNDER — PROMPT DI RIFONDAMENTO COERENTE (mappa a celle + meccaniche integrate)

> Repo: `NebulasStudio/Digger` — branch `main` — Unity 6.3 LTS (6000.3.21f1)
> Stato: TUTTI gli asset/animazioni sono nel progetto ma NON sono messi in scena. Il gameplay giocato non corrisponde alla visione. Leggi AGENTS.md prima di modificare.

## ⚠️ DIAGNOSI DI FONDO (verificata nel codice — leggi prima)
Il problema NON è la mancanza di asset. Il problema è **architettura del terreno**:
- La mappa è costruita con `CreateTiledSprite` = **un singolo SpriteRenderer con tileMode ripetuto** su una superficie piana. NON è una griglia di celle.
- `DigTerrainView` mette un piccolo overlay SpriteRenderer sopra la cella, ma la sabbia di sotto non si apre davvero → lo scavo "non si capisce".
- Il tunnel è un cambio di colore del personaggio, non un cambio di mondo → perché non ci sono layer separati.
- Le animazioni e l'art reale esistono ma NON sono assegnate alle entità in scena.

**Conclusione:** finché la mappa è un singolo sprite ripetuto, scavo dinamico e tunnel saranno sempre finti. La base va rifondata come **griglia di celle reali**.

---

## OBIETTIVO PRINCIPALE
Costruire un **mondo a griglia di celle reali** (Tilemap) su cui le meccaniche (scavo 3 stati, tunnel, mappa dinamica) funzionino davvero, e collegare TUTTI gli asset/animazioni esistenti alle entità. Risultato = gioco INTERATTIVO e COMPRENSIBILE, coerente con la visione.

---

## FASE 0 — VERIFICA SCENA E FOLDER (obbligatorio, prima di tutto)
1. Lavora SOLO su `Assets/Scenes/GameplayLab.unity` (l'unica in `EditorBuildSettings`, guid `acbdfe3e68494d145a4a1aec4b6819c2`). NON creare/verificare su altre scene.
2. Conferma che esistano queste folder (riporta cosa manca):
   - `Assets/Sandsunder/Art/Generated/` (clip .anim + sprite .asset)
   - `Assets/Sandsunder/Art/Runtime/Processed/` (PNG 32×32 reali)
   - `Assets/Sandsunder/Art/Source/Higgsfield/` (hf_asset_*)
   - `Assets/Sandsunder/Gameplay/UI/` (GlassPanel, StatBarWidget, StealthIndicator, TabInventoryController, WeaponStatCard)

---

## FASE 1 — RIFONDA LA MAPPA COME GRIGLIA DI CELLE (Tilemap) ⭐ prerequisito
Obiettivo: il terreno NON è più un singolo sprite ripetuto, ma una **griglia di celle sc3** ognuna indipendente e scavabile.

1. **Crea una Tilemap del terreno** (Grid + Tilemap) in `GameplayLab`, con celle quadrate (es. 1×1 unità). Usa `sand_tile`/`sand_basecolor` come tile di base.
2. **Sostituisci l'attuale `CreateTiledSprite` del floor** con la Tilemap: ogni cella è una tile. (Mantieni le texture sabbia già presenti.)
3. **Aggiungi varietà al terreno:** zone di sabbia diversa (usa `sand_feather_*`, `sand_final_*`, `sand_rolled`), macchie di rocce, dune. La mappa deve apparire "viva" e variegata, NON un rettangolo piatto.
4. **Popola la mappa** con gli oggetti già esistenti: palme (`env_palm_tree_32`), colonne (`env_ruin_pillar_32`), cactus (`env_cactus_32`), chest runica (`env_chest_runed_32`), vasi, obelischi, rovine. Posizionali su celle specifiche.

---

## FASE 2 — SCAVO A 3 STATI PER CELLA (interattivo e leggibile) ⭐
Obiettivo: quando scavi con la pala, la CELLA si deforma chiaramente in 3 stadi.

1. **Ancora `DigTerrainView` alla Tilemap**, non a sprite fuori griglia. Ogni cella ha uno stato `depth` (0=intatta, 1=crepata, 2=cratere aperto).
2. Gli **stadi devono essere CHIARI e grandi** (tutta la cella, NON un piccolo overlay):
   - Intatta → tile di sabbia normale
   - Crepata → `DigCracked` (crepe visibili su tutta la cella)
   - Aperta → `DigOpened` / `DigIntact`-scavato (cratere con bordo scuro, sabbia frastagliata)
3. Usa le sprite `DigIntact/DigCracked/DigOpened` (già in `Art/Generated`) come tile di overlay sopra la base, con contrasto alto. Deve essere OVVIO che la cella è stata scavata.
4. Il collider della cella scavata si apre (il player vi può entrare/proiettili vi cadono dentro) — comportamento coerente con `PrototypeDigGridAuthority`.
5. Mantieni il feedback FX: fratture a stella (`SandCrepeCracksFX`) + polvere (`SandDustEmitter`) durante il canale di scavo.

---

## FASE 3 — SISTEMA TUNNEL / SOTTERRANEO (cambio di mondo reale) ⭐
Obiettivo: scendendo sottoterra il MONDO cambia, non solo il colore del player.

1. **Crea un layer sotterraneo separato** (seconda Tilemap "Subterranean" sotto quella di superficie, o overlay a schermo/fade).
2. **Transizione:** quando il player raggiunge depth≥2:
   - Fade-out → switch layer → fade-in con **palette sotterranea distinta** (ambient scuro, sabbia scura, tunnel rock texture).
   - Il player diventa **silhouette ciano traslucida `#00F0E6` @65%**, sortingOrder -10, visibile attraverso la sabbia scavata.
3. `PrototypeTunnelSystem` deve rispondere al layer (`Surface_L0` / `Subterranean_L1`) cambiando il rendering del mondo (non solo un piccolo tint).
4. In sotterraneo: elementi di superficie (chest, vasi, door) NON interagibili (già gated in `PrototypeDigging`).
5. Riscendendo/risalendo: fade inverso.

---

## FASE 4 — COLLEGA ART E ANIMAZIONI (tutto quello che c'è nelle folder)
Le clip esistono in `Art/Generated`. Collega TUTTO:

1. **NomadAnimatorController** → Walk→`Nomad_Walk`, Run→`Nomad_Run`, Roll→`Nomad_Roll`, Dig→`Nomad_Dig`, StealthCrouch→`Nomad_StealthCrouch` (param Speed/IsMoving/IsRolling/IsDigging/IsStealthed). Il personaggio deve essere `nomad_32` reale (NON il fallback blu).
2. **WeaponAnimator** sul weaponRoot → frame per OGNI arma: Shovel_Idle/Swing, Rifle_Idle/Fire/Reload, Rifle_Reload_V2, Scimitar_Swing, Shotgun_Idle, Blaster_Idle/Fire.
3. **Mob**: spitter → Spitter_Idle + Spitter_DeathBurst; golem → Golem_Charge; tartaruga (se sprite presente).
4. **Mondo**: chest → Chest_Open; vaso → Vase_Break; pickup → Pickup_Bob.
5. Se il personaggio è ancora il fallback blu dopo il rebuild → DEBUG `SandboxArtAssetFactory.ImportSpriteOptional` (path deve essere `Art/Runtime/Processed/nomad_32.png`, NON `hf_asset_*`).

---

## FASE 5 — COMBAT COERENTE (proiettili, mira, melee)
1. **Arma orientata al mouse:** sandbox: l'arma in mano ruota verso il cursore; proiettili partono nella direzione del mouse (`AimDirection` aggiornata ogni frame).
2. **Proiettili distinti per arma** (NON tutti uguali ciano):
   - Rifle brass → giallo/ottone, allungato
   - Blaster → ciano runico (proj_sentinel_cyan_rune)
   - Shotgun → arancione/piombo, spread
   - Mortaio → proiettile ad arco/deriva sabbiosa
3. **Melee:** scimitarra/pala usano `Scimitar_Swing`/`Shovel_Swing` (arco pulito), NON l'animazione di scavo.
4. **Attacco vs scavo:** il click destro scava SOLO con `shovel.default` equipaggiata e non in attacco.

---

## FASE 6 — UI E MINIMAPPA COERENTI
1. **TAB inventory:** usa `ui_glass_panel.png` (vetro+oro+ciano), icone 32×32 reali. `TabInventoryController` + `WeaponStatCard` + `StatBarWidget` + `StealthIndicator` wired.
2. **Minimappa** (`SandboxMinimap`): bordo dorato, icone player/nemici/chest, sfondo semitrasparente. Deve riflettere la mappa a celle (mostra le zone scavate).

---

## CRITERI DI ACCETTAZIONE (Play, scena GameplayLab)
- [ ] Il terreno è una Tilemap a celle (NON un singolo sprite ripetuto)
- [ ] Scavando con la pala: la cella si deforma chiaramente in 3 stadi (intatta→crepata→cratere), leggibile e grande
- [ ] Scendendo sottoterra: il MONDO cambia visivamente (palette/layer sotterraneo) + player ciano traslucido
- [ ] Mappa variegata: palme, colonne, cactus, chest, vasi, rovine, golem, tartaruga, spitter
- [ ] Personaggio = nomad reale (NON blu); run/walk/roll/dig/stealth animati
- [ ] Armi animate e orientate al mouse; proiettili distinti per arma
- [ ] Melee corretto; scavo non conflittuale con attacco
- [ ] TAB con pannello vetro; minimappa che riflette la mappa
- [ ] Console 0 errori

## COSA SEGNALARE
- Clip "vuota"/sbagliata → segnala quale (rifatta su Higgsfield)
- Personaggio ancora blu → Debug.Log factory
- Folder mancanti → elenco preciso
- Lavoro manuale (assegnare clip in Inspector) → elenca con percorso esatto

## REGOLE
- Niente push senza conferma. Niente `Sandsunder.Editor` in `Sandsunder.Gameplay` (CS0234). `RequireComponent` prima di `AddComponent`. Committa e pusha ogni parte completata.