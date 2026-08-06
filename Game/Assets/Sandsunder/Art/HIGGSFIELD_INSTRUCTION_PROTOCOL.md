# 🤖 PROTOCOLLO OPERATIVO DIRETTIVE HIGGSFIELD AI (SANDSUNDER REPOSITORY)

**Destinatario:** Higgsfield AI  
**Mittente:** Antigravity AI (Lead Architect) & Operatore Umano  
**Repository:** `NebulasStudio/Digger`  
**Ambiente Operativo:** Unity 6 LTS 2D Top-Down

---

## 📥 1. FILE DA LEGGERE ED ANALIZZARE PRIMA DI OPERARE (SORGENTI DI VERITÀ)

Come agente generativo Higgsfield AI con accesso al repository, devi **obbligatoriamente** consultare ed analizzare i seguenti file per acquisire il contesto completo prima di creare o modificare qualsiasi asset:

1. **`Assets/Sandsunder/Art/README_HIGGSFIELD_RULES.md`**: Regole ufficiali di classificazione, convenzioni di naming, PPU (32), trasparenza e ancoraggio armi.
2. **`.agents/rules/higgsfield_asset_rules.md`**: Regole architetturali per agenti ed AI.
3. **`Assets/Sandsunder/Art/Generated/AnimationBuildManifest.asset`**: Manifest YAML/Unity in cui sono registrati tutti gli Sprite Sheet.
4. **`Assets/Sandsunder/Editor/SandboxArtAssetFactory.cs`**: Script C# di caricamento ed importazione automatica degli sprite in Unity.
5. **`Assets/Sandsunder/Gameplay/SandboxActorVisual.cs`**: Gestione dei componenti visivi, rotazione armi (`weaponRoot`), facing e pose.

---

## 🔍 2. DIAGNOSTICA ED INVARIANZA DEGLI ASSET DI GIOCO (VISUAL CHECK)

Prima di generare frame o animation sheet, esegui un controllo visivo sugli sprite autoritativi presenti in memoria e su disco:

### 👤 A. Personaggio Eroe — Nomad
- **File di Riferimento:** `Assets/Sandsunder/Art/Runtime/Characters/nomad_32.png`
- **Specifiche Visive da Mantenere al 100%:**
  - **Giacca:** Blu avventuriero (`#3466B8`)
  - **Cappuccio:** Bianco pulito (`#FFFFFF`)
  - **Sciarpa:** Verde acqua / Ciano turchese (`#26B8C6`)
  - **Dimensione:** Griglia 32x32 pixel, 32 PPU.
  - **Regola Tassativa:** Nelle animazioni (`nomad_walk.png`, `nomad_run.png`, `nomad_dig.png`) il Nomad deve essere **identico** a questo sprite base. Non sostituirlo MAI con personaggi in tunica o turbante giallo.

### 🔮 B. Nuovo Personaggio — Desert Sorcerer (Ruin Mystic)
- **File di Riferimento:** `Assets/Sandsunder/Art/Runtime/Characters/sorcerer_32.png`
- **Specifiche Visive:** Tonaca rosso cremisi (`#8B0000`), ricami in oro (`#FFD700`), bastone con runa ciano brillante (`#00FFFF`).

### 👾 C. Mob Nemico — Dune Spitter
- **File di Riferimento:** `Assets/Sandsunder/Art/Runtime/Mobs/mob_dune_spitter_32.png`
- **Specifiche Visive:** Coleottero/tartaruga con corazza dorata e punti luce ciano.

---

## 📂 3. REGOLE DI NOMINAZIONE E DESTINAZIONE CARTELE

Tutti i file generati devono essere salvati rigorosamente nelle seguenti sottocartelle con **nomi in minuscolo e underscore**:

- **Sprite Base Personaggi:** `Assets/Sandsunder/Art/Runtime/Characters/<nome>_32.png`
- **Sprite Sheet Animazioni:** `Assets/Sandsunder/Art/Runtime/Animations/<personaggio>_<azione>.png`
- **Sprite Nemici / Mobs:** `Assets/Sandsunder/Art/Runtime/Mobs/mob_<nome>_32.png`
- **Armi 32x32:** `Assets/Sandsunder/Art/Runtime/Weapons/<categoria>_<nome>_32.png`
- **Proiettili ed FX:** `Assets/Sandsunder/Art/Runtime/Projectiles/proj_<nome>_32.png`
- **Ambiente & Props:** `Assets/Sandsunder/Art/Runtime/Environment/env_<nome>_32.png`
- **Terreno:** `Assets/Sandsunder/Art/Runtime/Terrain/sand_*` / `ruin_*` (256 PPU)

---

## ⚙️ 4. COME COMUNICARE CON ANTIGRAVITY E BILDALARE LE ANIMAZIONI

Quando generi o aggiorni un nuovo asset o Sprite Sheet:

1. **Registrazione nel Manifest Unity:**
   Aggiungi la voce corrispondente nel file `Assets/Sandsunder/Art/Generated/AnimationBuildManifest.asset` specificando:
   - `clipName`: Nome dell'animazione (es. `Nomad_Walk`)
   - `sheetPath`: Percorso relativo (es. `Assets/Sandsunder/Art/Runtime/Animations/nomad_walk.png`)
   - `columns`: Numero di colonne della griglia
   - `rows`: Numero di righe della griglia
   - `fps`: Frame Rate (solitamente 8 o 12)
   - `pixelsPerUnit`: 32.0

2. **Formato Immagine:**
   - Canale **Alpha Trasparente** attivo (nessun rettangolo o sfondo di colore solido).
   - Pixel Art pulita a 32x32 per frame senza sfocature (Point Filter / No Bilinear Filtering).

3. **Notifica per Antigravity AI:**
   Quando hai terminato la generazione di un file, rispondi in chat specificando:
   - File PNG creato/modificato su disco.
   - Righe o blocchi aggiornati nel manifest `AnimationBuildManifest.asset`.
   - Antigravity avvierà immediatamente il comando `Unity_RunCommand` per ricompilare i clip in Unity e verificare il rendering 2D nella scena di test!
