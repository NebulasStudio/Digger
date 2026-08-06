# 🧠 SANDSUNDER — KNOWLEDGE BASE COMPLETA: TUTTO CIO' CHE ABBIAMO IMPARATO

> Documento di memoria permanente creato da Higgsfield AI al termine della sessione di sviluppo del gioco **Sandsunder** (2D top-down PvPvE pixel-art, Unity 6.3 LTS, repo `NebulasStudio/Digger`).
> **Scopo:** consolidare in formato testuale tutto ciò che abbiamo imparato su game design, generazione asset, build Unity e comunicazione con l'agente di build (Antigravity), con esempi concreti e un playbook per ottimizzare le sessioni future.

---

## PARTE 1 — IL PROGETTO: CONTESTO E OBIETTIVI

### 1.1 Che cos'è Sandsunder

Sandsunder è un gioco 2D top-down PvPvE (player vs player vs environment) in pixel-art 16-bit, ambientato in un'arena desertica. Il giocatore entra con una sola pala, scava per trovare equipaggiamento, sopravvive a creature di sabbia in escalation e gareggia verso condizioni di vittoria multiple non equivalenti. Il progetto punta a Windows/Steam come target commerciale primario, con controller-first e Steam Deck considerato.

L'architettura è rigorosa: **server autoritativo dedicato** (multiplayer), `Domain`/`Simulation` mai dipendenti da Unity, e gli asset generati su Higgsfield sono considerati "concept/source" finché un umano non ne valida silhouette, leggibilità, animazione, coerenza e licenza.

### 1.2 Perché abbiamo creato questa knowledge base

Durante lo sviluppo abbiamo attraversato **tre fasi di frustrazione** che hanno rivelato problemi di processo, non di talento o di codice:

1. **Asset generati ma non visibili nel gioco** — abbiamo generato decine di sprite e animazioni che "non si vedevano in Play Mode".
2. **Report di build troppo ottimistici** — l'agente dichiarava "OK" sulla base di verifica statica (file presenti, compilazione riuscita) senza guardare il gioco in movimento.
3. **Animazioni schedeno a personaggi sbagliati** — fogli generati per "wanderer/explorer/scout/rogue" invece che per il Nomad, creando personaggi fantasma.

Questa knowledge base è la medicina per evitare di ripetere questi errori. Ogni sezione contiene: cosa abbiamo imparato, l'esempio concreto che lo dimostra, e la regola operativa per il futuro.

---

## PARTE 2 — GAME DESIGN (LEZIONI PRINCIPALI)

### 2.1 La lezione più importante: la SPEC prima del codice

Il problema più grave è stato **non avere una spec di gameplay condivisa**. Ogni agente applicava fix a pezzi senza una visione coerente del risultato finale. La lezione: **prima di scritto qualsiasi codice o generazione, bisogna avere un documento di riferimento che descrive COME deve essere il gameplay finale.**

**Esempio concreto:** l'utente voleva "scavo a 3 stati" (intatta → crepata → cratere) e "transizione sotterranea visibile". Ma il codice implementava un overlay minuscolo e un cambio di colore del personaggio. Non era un problema di codice: era che **nessuno aveva scritto "la cella deve deformarsi su tutta la superficie" e "scendendo sottoterra il mondo deve cambiare palette"** prima di implementare.

**Regola operativa:** prima di generare o implementare, scrivi `docs/gameplay-final-spec.md` con: mondo, personaggio, armi, combat, scavo, tunnel, mob, UI. Questo documento è il "contratto" con cui ogni agente verifica il proprio lavoro.

### 2.2 Architettura del terreno: Tilemap a celle, non sprite ripetuto

Abbiamo scoperto che la mappa era costruita con `CreateTiledSprite` = un singolo `SpriteRenderer` con tileMode ripetuto. **Non era una griglia di celle.** Questo è il motivo per cui scavo dinamico e tunnel non potevano essere reali.

**Lezione:** per meccaniche come scavo, deformazione del terreno e livelli sotterranei, il terreno DEVE essere una **Tilemap a celle** (Grid + Tilemap), dove ogni cella è una tile indipendente e scavabile. Un singolo sprite ripetuto non può deformarsi.

**Esempio concreto:** quando abbiamo provato ad aggiungere "scavo 3 stati", il sistema metteva un piccolo overlay sopra la cella ma la sabbia di sotto non si apriva. La soluzione architetturale corretta è linkare `DigTerrainView` alla Tilemap, non a sprite fuori griglia.

### 2.3 Separazione corpo / arma / proiettile

Il `SandboxActorVisual` ha **due renderer separati**: `bodyRoot` (il corpo del personaggio) e `weaponRoot` (l'arma, ruotata verso il mouse). Questa è una bella architettura, ma richiede una disciplina di generazione asset:

**Regola: le animazioni del corpo DEVONO essere senza arma in mano.** Se disegni la pala nel frame del corpo, in gioco vedi l'arma doppia (quella del foglio + quella del weaponRoot).

**Esempio concreto:** inizialmente il prompt di generazione includeva "nomad holding a shovel". Questo produceva fogli con la pala disegnata, che poi appariva doppia. La soluzione: generare il corpo con mani vuote, e animare le armi separatamente sul `WeaponAnimator`.

### 2.4 Mob e AI: comportamento chiaro, non solo sprite

Ogni mob deve avere un comportamento definito, non solo uno sprite:

- **Dune Spitter:** sprite a tartaruga/coleottero, animazioni idle + death burst, spara proiettile telegrafato.
- **Sandstorm Golem:** boss con stato Idle → Telegraph → Charge → Cooldown, nucleo runico ciano fluttuante.
- **Crystal Turtle:** patrol lento, si ritira nel guscio quando colpita (invulnerabilità breve), esce e attacca da vicino, ignora i giocatori sotterranei.

**Lezione:** l'AI va scritta seguendo un pattern a macchina a stati chiaro, e il comportamento va definito NEL design prima di implementare.

### 2.5 Multiplayer e autorità del server

Il progetto è rigoroso sull'autorità del server: il server possiede tempo, RNG, loot nascosto, combat, AI, respawn, obiettivi. Il client è solo presentazione. **Le feature visive (scavo, terrain deformation, stealth) devono essere "presentation layer" che leggono lo stato dal server, mai mutarlo.**

**Regola:** quando aggiungo un sistema visivo, deve essere un relay del server, non una regola gameplay. Es. `DigDepthSystem` è una "proiezione" della depth server-owned.

---

## PARTE 3 — GENERAZIONE ASSET SU HIGGSFIELD (LEZIONI PRINCIPALI)

### 3.1 Usare SEMPRE l'asset di riferimento nel prompt

La lezione più importante della generazione asset: **ogni animazione deve partire dall'asset base del personaggio come riferimento visivo, e va incluso nel prompt (e nel contesto).**

**Esempio concreto:** quando abbiamo generato le animazioni del Nomad senza riferimento, il modello creava personaggi diversi (con tunica gialla, turbante). Quando abbiamo incluso `nomad_32.png` (giacca blu #3466B8, cappuccio bianco, sciarpa turchese #26B8C6) come riferimento in `medias`, il risultato era coerente.

**Regola operativa:** per ogni animazione di un personaggio, passa l'immagine base del personaggio (e idealmente un foglio di animazione esistente) in `medias` con `role: "image"`. Il modello manterrà stile, palette e proporzioni.

### 3.2 Sfondo: magenta solido esplicito, non "trasparente" o checkerboard

I modelli di generazione **non producono trasparenza reale**. Chiedere "transparent background" produce spesso un **finto checkerboard grigio/bianco** (pattern disegnato), che è inutilizzabile perché non è un colore solido chiaveabile.

**Esempio concreto:** i primi fogli generati con "transparent background" avevano uno sfondo a scacchi grigio/bianco. Il keying era impossibile (pattern non uniforme) e i frame risultavano sporchi.

**Regola operativa:** il prompt DEVE chiedere esplicitamente **"Background is SOLID UNIFORM BRIGHT MAGENTA #FF00FF everywhere (no checkerboard, no gradients, no gray)"**. Poi in post-produzione si keya il magenta a trasparenza con flood-fill dai bordi.

### 3.3 Griglia esatta e dimensioni dichiarate

Il `SpriteSheetImporter` di Unity taglia il foglio con `cellW = width / columns` e `cellH = height / rows`. Quindi **il foglio deve avere dimensioni esattamente divise dalla griglia** e ogni cella deve essere pulita.

**Esempio concreto:** il modello genera a 1792×2400 o 2048×2048, non a 128×64. Se non riassembli noi il foglio nella griglia esatta, lo slicing di Unity produce frame tagliati male.

**Regola operativa:** il prompt dichiara "EXACTLY 4 columns x 2 rows = 8 equal square frames, 32x32 pixels per frame, entire sheet 128x64". Poi in post-produzione riassembliamo il foglio: chiavo magenta, ritaglio ogni cella, ridimensiono NEAREST a 32×32, e ricompongo in una griglia esatta.

### 3.4 Griglie reali da verificare, non da assumere

Il modello AI spesso non rispetta la griglia dichiarata. **Dopo la generazione bisogna verificare la griglia REALE** (numero di righe/colonne) e aggiornare il manifest con quella reale.

**Esempio concreto:** chiesti 8 frame (4×2), il modello ne generava 16 (4×4). Abbiamo dovuto riassemblare in 4×4 e registrare `Nomad_Walk` come 4×4=16 frame nel manifest.

### 3.5 Quantizzazione palette 16-bit

Per coerenza con lo stile retro, dopo il keying e il downscale a 32×32 conviene **quantizzare la palette a ≤14 colori** (MEDIANCUT, no dither) per avere un look 16-bit pulito.

### 3.6 Verifica della qualità di OGNI foglio prima del successivo

Il modello può produrre frame danneggiati (linee nere che tagliano il soggetto, artefatti, soggetto troppo piccolo). **Bisogna verificare visivamente ogni foglio prima di committare.**

**Esempio concreto:** dei 7 fogli iniziali del Nomad, solo walk/melee/hurt erano puliti. Run/dig/death/shoot erano danneggiati e li abbiamo rigenerati uno per volta.

**Regola operativa:** dopo ogni generazione, fai un `image_analyze` sul foglio riassemblato per verificare: soggetto corretto, sfondo pulito, frame leggibili. Se è danneggiato, rigenera con prompt più esplicito.

### 3.7 Naming standard (le regole del progetto)

Il progetto ha regole di naming precise (in `README_HIGGSFIELD_RULES.md`):

| Categoria | Prefisso | Esempio | PPU |
|---|---|---|---|
| Personaggio base | `nomad_` / `char_` | `nomad_32.png` | 32 |
| Animazione | `<char>_<azione>` | `nomad_walk.png` | 32 |
| Mob | `mob_` | `mob_dune_spitter_32.png` | 32 |
| Arma impugnabile | `<arma>_32` | `sword_scimitar_32.png` | 32 |
| Ambiente | `env_` | `env_palm_tree_32.png` | 32 |
| Terreno | `sand_` / `ruin_` | `sand_basecolor.png` | 256 |
| UI | `ui_` | `ui_glass_panel.png` | 100 |
| Proiettile | `proj_` | `proj_sentinel_cyan_rune_32.png` | 32 |

**Cartelle:** `Characters/`, `Mobs/`, `Weapons/`, `Projectiles/`, `Environment/`, `Terrain/`, `Animations/`, `UI/` — tutte sotto `Art/Runtime/`.

**Lezione:** il naming standard è essenziale perché l'`SandboxArtAssetFactory` e l'importer li riconoscano automaticamente.

---

## PARTE 4 — BUILD UNITY (LEZIONI PRINCIPALI)

### 4.1 La verifica statica NON basta: serve il video

Il problema più costoso è stato che **l'agente dichiarava "OK" sulla base di verifica statica** (file `.anim` non vuoti, compilazione riuscita, gerarchia corretta) **senza guardare il gioco in movimento**.

**Esempio concreto:** l'agente riportava "personaggio = nomad reale, arma ancorata, animazioni collegate" ma il video mostrava il personaggio che scivola senza animazione, l'arma fluttuante accanto al corpo, e i nemici che attraversavano i muri.

**Regola operativa:** **l'unica prova valida è il VIDEO in Play Mode**, non la compilazione, non la gerarchia, non i byte dei file. Prima di dichiarare "fatto", registra un video del personaggio che cammina, attacca, scava, va sottoterra.

### 4.2 Il mapping animazioni → stati è 1:1 e fisso

Ogni personaggio/mob deve avere SOLO le proprie animazioni. Proibito applicare a un personaggio una clip appartenente a un altro.

**Esempio concreto:** il `NomadAnimatorController` aveva lo stato Idle che puntava a `Shovel_Idle` (la clip della pala) invece che a `Nomad_Idle`. Per questo il personaggio a volte mostrava frame della pala.

**Regola operativa:** verifica che ogni stato dell'Animator punti alla clip giusta (controlla i GUID), e che le clip delle armi vadano solo sul `WeaponAnimator`, non sul corpo.

### 4.3 Il build della scena va rigenerato con il vero art

Il vero art si carica SOLO quando la scena viene rigenerata con `Sandsunder > Gameplay > Build Gameplay Lab`. Se apri una scena salvata in precedenza, vedi i fallback procedurali.

**Esempio concreto:** l'utente giocava ancora la scena vecchia con lo sprite blu procedurale, perché la scena non era stata rigenerata dopo l'import degli asset.

**Regola operativa:** dopo l'import di nuovi asset, rigenera SEMPRE la scena col builder, poi verifica in Play.

### 4.4 RequireComponent prima di AddComponent

Quando crei un GameObject con `AddComponent`, Unity richiede che i componenti obbligatori (`RequireComponent`) vengano aggiunti PRIMA. Es. `PrototypeDestructibleVase` richiede `Collider2D` + `SpriteRenderer`; `SandstormGolemAI` richiede `PrototypeHealth` + `Rigidbody2D`.

**Esempio concreto:** il builder aggiungeva `PrototypeDestructibleVase` senza collider → errore "Adding component failed. Add required component...". Fix: aggiungere `SpriteRenderer` + `BoxCollider2D` prima.

### 4.5 CS0234: niente References Editor nell'assembly runtime

L'assembly `Sandsunder.Gameplay` NON deve riferire `Sandsunder.Editor` (sarebbe dipendenza circolare). Per caricare sprite in editor dal runtime, usa `UnityEditor.AssetDatabase` SOLO dentro `#if UNITY_EDITOR`.

**Esempio concreto:** `PrototypeInventoryHUD`, `DigTerrainView`, `SandstormGolemAI` riferivano `Sandsunder.Editor.SandboxArtAssetFactory` dall'assembly runtime → errore CS0234. Fix: usare `AssetDatabase.LoadAssetAtPath` dentro `#if UNITY_EDITOR`.

---

## PARTE 5 — COMUNICAZIONE CON ANTIGRAVITY (LEZIONI PRINCIPALI)

### 5.1 Come funziona il flusso

Antigravity è l'agente che opera DENTRO Unity (via Unity MCP Server / relay). Higgsfield AI (io) genera gli asset e scrive codice. Il flusso corretto:

1. **Io** leggo le regole (`README_HIGGSFIELD_RULES.md`, `.agents/rules/higgsfield_asset_rules.md`, `HIGGSFIELD_INSTRUCTION_PROTOCOL.md`).
2. **Io** genero gli asset su Higgsfield, li processo (keying, griglia), li salvo nelle cartelle giuste, li registro nel manifest.
3. **Io** notifico in chat ad Antigravity i percorsi PNG + le voci manifest.
4. **Antigravity** lancia `Unity_RunCommand` (Build Animation Clips From Manifest, Build Gameplay Lab) e verifica in Play.
5. **Antigravity** deve mostrare VIDEO di verifica, non solo report.

### 5.2 Il problema: "antigravity dice OK ma non è vero"

Il problema ricorrente: Antigravity verifica la compilazione e la gerarchia, ma NON il gameplay reale. I suoi screenshot sono "fermi" e non rivelano animazioni, rotazioni, collisioni.

**Regola operativa:** ogni prompt deve includere **"PROVA A VIDEO"** come criterio di accettazione, e **"NON dichiarare fatto senza video"**. Il video è l'unica prova che il gameplay funziona in movimento.

### 5.3 Il prompt per Antigravity deve essere SPEC-LED e con mapping esatto

Il prompt per Antigravity deve:
- Partire dalla **diagnosi** (cosa è sbagliato e perché).
- Richiedere la **spec** del gameplay finale.
- Dare il **mapping esatto** di ogni animazione allo stato.
- Richiedere la **verifica a video**.
- Vietare la generazione di nuovi asset (sono già pronti).

**Esempio concreto:** il prompt più efficace iniziava con "⚠️ BUG CONFERMATO: il NomadAnimatorController ha lo stato Idle che punta a Shovel_Idle" e poi dava la tabella di mapping 1:1.

---

## PARTE 6 — PLAYBOOK PER LE SESSIONI FUTURE (OTTIMIZZAZIONE)

### 6.1 Sequenza ottimale per generare un blocco di animazioni

1. **Leggi le regole** (README_HIGGSFIELD_RULES, higgsfield_asset_rules, protocol).
2. **Individua l'asset base** del personaggio da animare (es. `nomad_32.png`).
3. **Genera UN foglio alla volta** (o in batch, ma verifica ognuno) con:
   - Riferimento del personaggio in `medias`.
   - Prompt con "SOLID UNIFORM BRIGHT MAGENTA #FF00FF, EXACTLY NxM grid, 32x32 per frame, no checkerboard, no gray".
   - Corpo SENZA arma (le armi si animano a parte).
4. **Processa**: chiavo magenta (flood-fill), ritaglio celle, ridimensiono NEAREST a 32×32, riassemblo in griglia esatta, quantizzo palette a ≤14 colori.
5. **Verifica** con `image_analyze`: soggetto corretto, sfondo pulito, frame leggibili.
6. **Salva** in `Animations/` con nome `nomad_<azione>.png` + `.meta` (PPU 32, alphaIsTransparency).
7. **Registra** nel `AnimationBuildManifest.asset` con griglia REALE.
8. **Notifica** Antigravity con percorsi + mapping esatto.

### 6.2 Checklist di verifica per ogni asset generato

- [ ] Nome nel formato standard (`nomad_<azione>.png`, in `Animations/`)
- [ ] Sfondo trasparente (magenta keyato) o magenta solido chiaveabile
- [ ] Griglia esatta divisibile (128×64, 128×32, ecc.)
- [ ] PPU corretto (32 per personaggio/arma, 256 per terreno)
- [ ] `.meta` presente con alphaIsTransparency
- [ ] Voce nel `AnimationBuildManifest.asset` con griglia reale
- [ ] Verifica visiva (image_analyze) della qualità
- [ ] Fatto il keying (flood-fill dai bordi per magenta/checkerboard)

### 6.3 Come evitare i 3 errori più costosi

1. **Asset non visibili in gioco** → rigenera SEMPRE la scena col builder (`Build Gameplay Lab`) dopo l'import, e verifica in Play.
2. **Report "OK" falsi** → imponi "PROVA A VIDEO in Play Mode" come unico criterio; mai fidarsi di compilazione/gerarchia/byte.
3. **Animazioni su personaggi sbagliati** → usa SEMPRE l'asset base come riferimento nel prompt, e verifica il mapping stato→clip.

### 6.4 Buone pratiche tecniche

- **Keying magenta**: flood-fill BFS dai 4 angoli con tolleranza, poi clear dei pixel magenta interni non connessi.
- **Downscale**: sempre NEAREST per pixel-art (LANCZOS smuere).
- **Palette**: quantizzazione MEDIANCUT a ≤14 colori, senza dither, per look 16-bit.
- **Griglia**: rileva la griglia REALE dopo la generazione, aggiorna il manifest di conseguenza.
- **Corpo senza arma**: separa corpo e arma; le armi vanno sul WeaponAnimator.
- **CS0234**: mai `Sandsunder.Editor` nel runtime; usa `AssetDatabase` in `#if UNITY_EDITOR`.
- **RequireComponent**: aggiungi i componenti obbligatori PRIMA di `AddComponent`.

---

## PARTE 7 — MEMORIA PERSISTENTE (PROFILO UTENTE E PROGETTO)

Per le sessioni future, queste sono le informazioni chiave da ricordare:

- **Progetto:** Sandsunder, 2D top-down PvPvE pixel-art, Unity 6.3 LTS, repo `NebulasStudio/Digger`.
- **Stile:** gioca blu #3466B8, cappuccio bianco #FFFFFF, sciarpa turchese #26B8C6 per il Nomad; palette scuro #1A1410, oro #D6B336, ciano #26B8C6.
- **Struttura asset:** 8 cartelle in `Art/Runtime/` (Characters, Mobs, Weapons, Projectiles, Environment, Terrain, Animations, UI).
- **Regole:** `README_HIGGSFIELD_RULES.md`, `.agents/rules/higgsfield_asset_rules.md`, `HIGGSFIELD_INSTRUCTION_PROTOCOL.md`.
- **Flusso:** io genero → processo → notifico ad Antigravity → Antigravity builda e verifica A VIDEO.
- **Lezione chiave:** la verifica a video è l'unica prova valida; la spec prima del codice; l'asset base come riferimento nel prompt.

---

## CONCLUSIONE

Questa sessione ci ha insegnato che il successo nello sviluppo di un gioco con AI generativa dipende da **processo**, non da talento. I tre pilastri sono:

1. **SPEC condivisa** del gameplay finale (prima di generare/implementare).
2. **Riferimento visivo** in ogni generazione (l'asset base nel prompt).
3. **Verifica a video** come unica prova (mai fidarsi di compilazione/gerarchia).

Con questi tre principi, le sessioni future saranno molto più efficienti: meno asset scartati, meno "OK falsi", meno animazioni su personaggi sbagliati, e un gioco che finalmente corrisponde alla visione.

**Prossimi passi suggeriti:** generare il blocco sabbia 3 stati (basato sulle texture esistenti), il Desert Sorcerer (nuovo personaggio), e i proiettili per-arma, seguendo questo playbook. Poi dare ad Antigravity il prompt spec-led con mapping esatto e prova a video.