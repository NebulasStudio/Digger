# SANDSUNDER — PROMPT DEFINITIVO: RIFONDAZIONE COMPLETA DEL GAMEPLAY (SPEC-LED, VERIFICA A VIDEO)

> Repo: `NebulasStudio/Digger` — branch `main` — Unity 6.3 LTS (6000.3.21f1)
> Stato: gli asset e le clip CI SONO, ma il GIOCO GIOCATO non corrisponde alla visione. I report precedenti "OK" erano basati su verifica statica (gerarchia/frame) NON su gameplay reale. **Questo prompt impone un metodo nuovo: prima la SPEC logica, poi verifica che OGNI file C# sia coerente con la spec, poi prova finale A VIDEO.**

## ⚠️ DIAGNOSI VERIFICATA (leggi e falla tua)
Il problema NON è la generazione asset. È che il codice non segue una **logica di gameplay finale coerente**:
1. **Il build usa `CreateTiledSprite`** = un SINGOLO SpriteRenderer con tileMode ripetuto. NON è una griglia di celle. Quindi scavo dinamico e tunnel non possono essere reali.
2. **`SandboxActorVisual` e `SandboxVisualEffects` caricano ancora sprite procedurali di fallback** (`PrototypePixelArt.GetCachedSprite`) in alcuni percorsi → il personaggio/armi/proiettili a volte mostrano il vecchio placeholder.
3. **Le animazioni esistono ma non sono collegate agli stati degli Animator in modo verificato runtime** → il personaggio scivola senza animazione.
4. **Ogni fix è stato applicato a pezzi senza una spec di riferimento** → il risultato è incoerente.

---

## PARTE 1 — COSTRUISCI TU LA SPEC LOGICA DEL GAMEPLAY FINALE (il tuo "contratto")
Prima di toccare codice, SCRIVI un documento `docs/gameplay-final-spec.md` in cui definisci, con precisione e coerenza, la logica di gioco finale. Questo documento è il tuo riferimento per ogni decisione. Deve contenere:

### 1.1 Il mondo
- Come è costruito il terreno: **griglia di celle (Tilemap)**, dimensione cella, tile di base.
- Varietà: zone di sabbia diversa, rocce, dune, palme, colonne, cactus, chest, vasi, rovine. Transizioni naturali tra zone (tile di raccordo).
- Non più un singolo sprite ripetuto.

### 1.2 Il personaggio (Nomad)
- Sprite reale `nomad_32.png` (mai fallback blu).
- Stati animati: Idle, Walk, Run, Roll, Dig, StealthCrouch — con le clip esatte e i parametri (`Speed/IsMoving/IsRolling/IsDigging/IsStealthed`).
- Arma in mano: ancorata al perno della mano, ruota verso il mouse in tempo reale.

### 1.3 Le armi (ognuna con le sue feature)
- shovel: scava (tasto destro) + melee.
- rifle.brass: spara proiettile giallo/ottone, allungato, velocità media.
- shotgun.heavy: spara 5 proiettili con spread, arancione/piombo.
- blaster.rune: spara proiettile ciano runico, veloce.
- sword.scimitar: melee (arco pulito con Scimitar_Swing).
- icon.mortar_sandstorm: proiettile ad arco/deriva sabbiosa.
- Ogni arma ha danno/portata/cadenza dal `Design/balance/weapons.csv`.

### 1.4 Combat
- Mira: il mouse definisce la direzione; arma e proiettili partono verso il cursore.
- Proiettili VISIBILI (grandi), distinti per arma, con telegrafo.
- Attacco vs scavo: il destro scava SOLO con shovel, non in attacco.
- Melee con animazione arco corretta.

### 1.5 Scavo
- 3 stati per cella: intatta → crepata → cratere, CHIARI e GRANDI (tutta la cella).
- Overlay legato alla Tilemap (non quadratini grezzi fuori griglia).
- FX: fratture stella + polvere durante il canale.

### 1.6 Tunnel / sotterraneo
- Transizione VISIBILE: fade-out → switch layer → fade-in con palette sotterranea distinta.
- Player = silhouette ciano #00F0E6 @65%, sortingOrder -10, visibile attraverso la sabbia.
- Elementi di superficie non interagibili da sotterraneo.

### 1.7 Mob e boss
- Dune Spitter (sprite + Idle/DeathBurst), con comportamento (spara proiettile telegrafato).
- Near il golem Sandstorm (charge, nucleo runico).
- Tartaruga Crystal (patrol, si ritira nel guscio, lunge).
- Nessun mob attraversa i muri (collisioni corrette).

### 1.8 UI e minimappa
- TAB inventory: pannello vetro (ui_glass_panel), icone 32×32 reali, barre HP/Stamina, card arma, stealth indicator.
- Minimappa: bordo dorato, icone player/nemici/chest.
- NESSUNA barra di debug, nessun log `[M]` in overlay, nessuna notifica di sistema nel gameplay.

### 1.9 Criteri di "finito"
- Console 0 errori. Nessun glitch di collisione. Le animazioni si vedono in movimento.

---

## PARTE 2 — AUDIT COMPLETO DEI FILE C# (coerenza con la spec)
Dopo aver scritto la spec, passa in rassegna OGNI file in `Assets/Sandsunder/Gameplay/` e `Assets/Sandsunder/Editor/`, e per ciascuno verifica che sia COERENTE con la spec. Produci una tabella:

| File C# | Coerente con spec? | Problema | Fix necessario |
|---|---|---|---|
| GameplayLabBuilder.cs | NO | usa CreateTiledSprite (single sprite) invece di Tilemap | rifare il terreno come Tilemap a celle |
| SandboxActorVisual.cs | PARZIALE | usa fallback GetCachedSprite in alcuni path | caricare SEMPRE nomad_32 |
| SandboxVisualEffects.cs | PARZIALE | proiettili/effetti usano fallback | usare sprite reali per proiettile |
| PrototypeCombat.cs | DA VERIFICARE | ... | ... |
| WeaponAnimator.cs | DA VERIFICARE | i frame non sono popolati a runtime | creare runtime loader |
| NomadAnimator.cs | ... | ... | ... |
| ... (ogni file) | ... | ... | ... |

**Per ogni file**, riporta: cosa fa, cosa dovrebbe fare secondo la spec, e se c'è discrepanza.

---

## PARTE 3 — FIX ARCHITETTURALI OBBLIGATORI (i 3 pilastri rotti)
### 3.1 RIFAI IL TERRENO COME TILEMAP A CELLE (prerequisito)
- Sostituisci `CreateTiledSprite` del floor con una **Tilemap** (Grid + Tilemap). Ogni cella è una tile.
- Varietà di tile, transizioni, props posizionati su celle.
- `DigTerrainView` deve agganciarsi alla Tilemap (cella = tile), NON sprite fuori griglia.

### 3.2 ELIMINA I FALLBACK PROCEDURALI
- `SandboxActorVisual`: il body DEVE essere sempre `nomad_32` (mai `GetCachedSprite(Player, ...)`).
- `SandboxVisualEffects`: i proiettili DEVONO usare `proj_sentinel_cyan_rune` (o sprite per-arma), mai il fallback.
- Rimuovi o rendi inerte ogni `PrototypePixelArt.GetCachedSprite` che produce placeholder nel gameplay reale.

### 3.3 COLLEGA LE ANIMAZIONI A RUNTIME (loader)
- Crea un `RuntimeAnimationLoader` (o estendi `WeaponAnimator`/`NomadAnimator`) che:
  - Carica i frame delle clip dall'AssetDatabase (editor) o da una lista serializzata (build).
  - Popola `WeaponAnimator.idle/fire/reload/swingFrames` per ogni arma.
  - Popola il `NomadAnimatorController` con le clip corrette.
- Verifica in Play Mode che gli stati si eseguano DAVVERO (personaggio che cammina anima le gambe).

---

## PARTE 4 — FIX GAMEPLAY VERIFICATI (da video reale)
1. **Movimento scivoloso**: il personaggio deve avere animazione camminata/corsa attive. Se scivola, il wiring Animator non gira.
2. **Arma fluttuante**: ancorare al perno mano, ruotare verso il mouse in LateUpdate (AimDirection aggiornata).
3. **Nemici attraverso i muri**: aggiungere collisioni corrette (Rigidbody2D + collider non-trigger per i muri).
4. **Scavo brutto**: overlay grandi e leggibili sulla Tilemap, 3 stadi.
5. **Transizione sotterranea**: fade + palette + silhouette ciano, non camminare su un'altra texture.
6. **Confine roccia/sabbia netto**: tile di raccordo.
7. **UI sporca**: rimuovere barre debug, log `[M]`, notifiche; rifare HP bar, minimappa.
8. **Proiettili invisibili**: ingrandire, render visibili, traiettoria chiara.
9. **Oggetti scavati che "pop"**: animazione di comparsa.

---

## PARTE 5 — VERIFICA OGGETTIVA E PROVA A VIDEO (obbligatoria)
Il criterio di accettazione è **il VIDEO in Play Mode**, NON la compilazione, NON la gerarchia, NON i byte dei file. Registra e mostra:

1. Video: il personaggio CAMMINA 3s (le gambe si animano, non scivola).
2. Video: muovi il MOUSE — l'arma ruota verso il cursore.
3. Video: ATTACCHI un nemico — proiettili visibili nella direzione del mouse.
4. Video: SCAVI — la cella si deforma in 3 stadi leggibili.
5. Video: SCENDI SOTTOTERRA — fade + palette + silhouette ciano.
6. Video: un nemico raggiunge un muro — SI FERMA, non lo attraversa.
7. Video: TAB — inventario con pannello vetro, icone reali, minimappa.

Per ogni video: se il comportamento non compare, NON dichiarare fatto. Riporta il Debug.Log e il problema.

---

## CRITERI DI ACCETTAZIONE FINALI
- [ ] `docs/gameplay-final-spec.md` scritto e coerente
- [ ] Tabella audit di tutti i file C# con discrepanze
- [ ] Terreno = Tilemap a celle (non sprite unico)
- [ ] Zero fallback procedurali nel gameplay reale
- [ ] Animazioni collegate e visibili in movimento
- [ ] Arma ancorata e ruota al mouse
- [ ] Proiettili grandi e distinti per arma
- [ ] Scavo 3 stadi leggibili sulla Tilemap
- [ ] Transizione sotterranea visibile
-- [ ] Nemici non attraversano i muri
- [ ] UI pulita; minimappa rifatta
- [ ] 7 video di verifica mostrati
- [ ] Console 0 errori

## COSA SEGNALARE
- Clip/asset brutto → segnala quale (verrà rifatto su Higgsfield).
- Se un file C# è incoerente con la spec → elenca la discrepanza.
- Lavoro manuale necessario (es. assign in Inspector) → elenca con percorso esatto.

## REGOLE
- Niente push senza conferma. Niente `Sandsunder.Editor` in `Sandsunder.Gameplay` (CS0234). `RequireComponent` prima di `AddComponent`. Committa e pusha ogni parte completata.