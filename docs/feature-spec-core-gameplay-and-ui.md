# Sandsunder — Specifica Tecnica & Grafica: 4 Feature Core (Gameplay + UI)

> **Ruolo:** Lead Technical Game Designer & UI/UX Architect
> **Progetto:** Sandsunder (PvPvE Top-Down 2D Roguelike, Unity 6 LTS + URP 2D, Steam/Windows)
> **Stato base:** repo `NebulasStudio/Digger` @ `b0f54f2` (ultimo push: batch 12 asset Higgsfield + NomadAnimatorController + keying magenta)
> **Convenzioni rispettate:** `AGENTS.md` (Dominio/Simulazione mai dipendenti da Unity/Photon; test con cambi comportamento), `docs/adr/0001` (autorità server, `map_seed` server-only), `Design/style-bible.md`, `Design/assets.csv` (manifest), pipeline `Tools/art/key_magenta_sprite.py`.

Questa specifica è **grounded sul codice esistente** (riferimenti precisi ai file e alle classi attuali) e copre, per ogni feature: obiettivo, architettura, componenti C# (struttura tecnica), integrazione con l'esistente, e istruzioni esatte per la generazione asset/frame UI su **Higgsfield**.

---

## Riferimento allo stato attuale (baseline)

| Area | File esistente | Cosa c'è già |
|---|---|---|
| Scavo | `Gameplay/PrototypeDigging.cs` | `DigGrid` 64×64, `CombatDigNodeState`, `PrototypeDigGridAuthority.TryDigAtWorldPosition`, `SandboxPitDecal.SpawnAt(depth)`, `SandboxVisualEffects.SpawnDust` |
| Terreno 3 stadi | `Editor/SandboxArtAssetFactory.cs` | `DigIntact` / `DigCracked` / `DigOpened` (procedurali 32×24) |
| Input scavo | `Gameplay/PrototypeCombat.cs` | `shovelAction` = `<Mouse>/rightButton` (+`F`, `<Gamepad>/buttonSouth`); channeling 3.0s con `IsDiggingChanneling` |
| Tunnel/Profondità | `Gameplay/PrototypeTunnelSystem.cs` | enum `MatrixLayerDepth` (Surface_L0 / Subterranean_L1 / RuneVault_L2), transizione colore bg/ambient |
| Profondità vis | `Gameplay/TopDownPlayerController.cs` | `CurrentDepth` (0/1/2) + `UpdateSubterraneanVisuals` (depth==2 → ciano 0.60 alpha) |
| Animator Nomad | `Art/Generated/NomadAnimatorController.controller` | Stati `Idle/Walk/Run/Roll/Dig`; param `IsMoving/IsRolling/IsDigging/Speed` |
| Pipeline art | `Tools/art/key_magenta_sprite.py` | Keying magenta `#FF00FF`, resample NEAREST, target-height, padding |
| Piazza armi | `Art/Source/Higgsfield/*`, `hf_asset_N.png` | 12 asset HD trasparenti (Pistol, Shovel, Scimitar, Shotgun, Blaster, Relic, …) |
| Inventario/HUD | `Gameplay/PrototypeInventoryHUD.cs`, `SandboxInventoryWindow.cs`, `SandboxReloadBar.cs` | Hotbar 5 slot, selezione 1–5/scroll, sprite procedurali 16px + fallback HD |

Regole di progetto che **vincolano** ogni feature: il **server dedicato** è autoritativo su scavo/loot/profondità/combattimento; gli asset generati da Higgsfield sono **concept/source** finché un umano non valida silhoutte, leggibilità, coerenza e licenza (AGENTS.md); `map_seed` mai esposto al client.

---

# FEATURE 1 — DINAMICA DI SCAVO & TERRENO DINAMICO
## (Dynamic Sand Excavation & Crepe Cracks)

### A. Obiettivo e contratto di autorità
Il terreno di sabbia **non scompare** ma **deforma in 3 stadi progressivi** per cella: `Intact → Cracked → Opened(Pit)`. Lo scavo "channeling" (Tasto Destro con `shovel.default`) genera in tempo reale **fratture a stella (starburst crepe cracks)** e **pulviscolo di sabbia**. La deformazione è un **overlay procedurale a maschera** (32×32 per cella) integrato con `sand_basecolor.png`.

**Ciclo di autorità (da preservare):** il server decide la `depth` di ogni cella (`DigGrid` già esiste). Il client renderizza solo l'overlay della `depth` ricevuta. La deformazione è **puramente presentazionale** → resta fuori da `Sandsunder.Domain`/`Simulation` (nessuna regola gameplay dentro i visual).

### B. Architettura dei 3 stadi per cella
| Stage | `depth` cella | Overlay renderizzato | Asset |
|---|---|---|---|
| **Intact** | 0 | Nessun overlay (sabbia piena, `sand_basecolor`) | `DigIntact` |
| **Cracked** | 1 | 1–2 starburst crepe, minima depressione, ombra d'arenaria | `DigCracked` + `CrepeMask_01` (32×32) |
| **Opened/Pit** | 2 | Cratere scavato con bordo d'ombra, sabbia frastagliata, buca visibile | `DigOpened` + `PitMask_02` (32×32) |

### C. Componenti C# (struttura tecnica)

```
Sandsunder.Gameplay/
├── DigTerrainView.cs            // RENDERER per-cell overlay (Tilemap secondario)
├── SandCrepeCracksFX.cs         // Fratture a stella transitorie (top layer)
├── SandDustEmitter.cs           // Pulviscolo continuo durante channeling
├── PrototypeDigging.cs          // [ESTESO] hook del 3-stadi + trigger FX
└── (reuse) SandboxPitDecal.cs, SandboxVisualEffects.cs
```

**1) `DigTerrainView : MonoBehaviour`** — Tilemap di overlay sopra la `Tilemap` del terreno (RenderLayer dedicato, `sortingOrder` tra terreno e entità).
```csharp
[RequireComponent(typeof(TilemapRenderer))]
public sealed class DigTerrainView : MonoBehaviour
{
    [SerializeField] private Tilemap terrainTilemap;   // ground (sand_basecolor)
    [SerializeField] private Tilemap overlayTilemap;   // deformazioni per cella
    [SerializeField] private Tile intactTile, crackedTile, openedTile;

    // API: ascolta il server (o il grid locale in sandbox) e aggiorna la cella.
    public void SetCellDepth(GridCell cell, int depth) { /* scrubba la tile vecchia, piazza la nuova */ }

    // Maschera 32x32 per la depressione/ombra: incisa via Texture2D blittata su SpriteMask
    // (v. D. — Overlay & Masking) per evitare TileAdd on tile.
}
```
- **Convenzione:** `overlayTilemap` usa `TilemapRenderer.mode = Chunk` e `sortingOrder = 5` (sotto Pickup/entità, sopra sabbia). Le tile `cracked/opened` hanno `colliderType = Tile.ColliderType.None` (deformazione solo visiva; il collider di scavo resta al `DigGrid`).

**2) `SandCrepeCracksFX : MonoBehaviour`** — linee di frattura a stella generate proceduralmente, indipendenti dalla tile (effetto "live" mentre tieni premuto il destro).
```csharp
public sealed class SandCrepeCracksFX : MonoBehaviour
{
    [SerializeField] private int arms = 6;            // raggi della stella
    [SerializeField] private float crackLength = 0.35f;
    [SerializeField] private float life = 0.9f;
    [SerializeField] private Color sandShadow = new(0.42f, 0.30f, 0.16f, 0.85f);

    public void SpawnStarburst(Vector2 worldPoint, int depthDelta);
    // disegna linee (Bresenham) + ramificazioni secondarie, fade-out alpha,
    // poi fade-in verso il "Cracked" persistente della cella.
}
```
- Usa una `LineRenderer` o un `Mesh` a strisce; **nessuna allocazione per-frame** (quality gate: zero per-frame alloc).

**3) `SandDustEmitter : MonoBehaviour`** — pulviscolo durante `IsDiggingChanneling`.
```csharp
public sealed class SandDustEmitter : MonoBehaviour
{
    [SerializeField] private ParticleSystem dust;   // sub-emitter: burst + soft billboard
    [SerializeField] private Color sandTint = new(0.86f, 0.70f, 0.43f);
    public void SetChanneling(bool active, Vector2 digCenter);
}
```
- Config consigliata: `ParticleSystem` con `main.startLifetime=0.5–0.9s`, `startSize=0.05–0.18`, `gravity=negativa` (ricaduta soffice), `emission` a 20–40/s durante channeling, `renderMode=Billboard`, sortingOrder alto ma sotto le entità.

**4) `PrototypeDigging.cs` (estensione)** — nel punto in cui `TryDigAtWorldPosition` riceve `DigResult result`:
```csharp
if (result.Changed)
{
    DigTerrainView.Instance?.SetCellDepth(cell, result.NewDepth);     // 3-stadi
    SandCrepeCracksFX.Instance?.SpawnStarburst(worldPos, result.NewDepth);
    SandDustEmitter.Instance?.SetChanneling(true, worldPos);          // durante channeling
    SandboxPitDecal.SpawnAt(cellCenter, result.NewDepth);             // già esistente
}
```
- **Trigger stella:** emessa a ogni *strike* (40 Hz channeling → ~2.5 colpi/s di `PlayMelee`), non a ogni frame.

### D. Texture Map & Masking (overlay 32×32)
L'overlay usa **due tecniche combinate**:
1. **Tilemap overlay** per gli stadi persistenti (cracked/opened) — tile 32×32 con pivot in basso-sinistra, `filterMode=Point`, `compression=Uncompressed`.
2. **SpriteMask + mask texture 32×32** per ombra d'arenaria e bordi del cratere: una `Texture2D` RGBA 32×32 (canale alpha = profondità della depressione) usata come `Sprite` di uno `SpriteMask`, con `SpriteRenderer` "masked" sopra il terreno. Questo dà l'**avvallamento** (sabbia più scura al centro) senza toccare la tilemap.

**Integrazione con `sand_basecolor.png`:** le texture di overlay (crepe/ombra/pit) sono `Additive`/`Multiply` rispetto al basecolor: il pit "scava" alzando il contrasto verso `Umber (43,33,30)` (palette già usata nelle `Draw*` della factory), le crepe si scuriscono lungo il bordo illuminato.

### E. Istruzioni esatte per generare gli asset su Higgsfield
Genera **4 texture di overlay** (tileable, teste/tagliate a 32×32 in Unity) + **1 particle texture**:

1. **`CrepeMask_01` (cracked overlay, 32×32 tileable)**
   > "Top-down 2D pixel art sand-texture mask, 32×32, tileable seamless. A single central starburst of thin dark sand cracks (5–6 radial arms) with small secondary branches, on a plain transparent background. Colors: deep sandstone brown (#2B211E) cracks with a subtle darker inner shadow for the depression. No characters, no text, no border, flat orthographic, muted desert palette (umber, tan #D6B336, pale sand #E4C98A). Pixel-perfect, crisp 32px grid, no anti-aliasing."

2. **`PitMask_02` (opened pit overlay, 32×32 tileable)**
   > "Top-down 2D pixel art excavated sand crater tile, 32×32, tileable seamless. A dark oval pit depression in the center with jagged broken sand edges, falling sand crumbs along the rim, and a soft dark sandstone shadow gradient inside (color #2B211E to #1A120E). Dune ripples around the rim. Transparent background, no characters, no text, flat orthographic, muted desert palette, pixel-perfect 32px grid, no anti-aliasing."

3. **`SandDust_Particle` (256×256, sprite)**
   > "2D game particle sprite sheet, soft sand dust puffs, 256×256, 4×4 grid of 16 square puffs, each a soft blurred cluster of tiny tan sand grains (#D6B336, #C9A05A) fading to transparent. No text, no characters, subtle warm desert lighting, grayscale-tinted fine grain, soft edges, transparent background."

4. **`SandShadowCrack` (mask, 32×32)** — per lo SpriteMask dell'ombra d'arenaria
   > "2D pixel art soft radial shadow mask, 32×32, transparent center with a feathered dark sandstone vignette (#2B211E) fading outward, soft gradient, no hard edges, no characters, no text, flat orthographic, pixel-perfect 32px grid."

**Post-generazione (pipeline):** scarica PNG → keying con `python3 Tools/art/key_magenta_sprite.py <src> <out> --target-height 32 --padding 2` (solo se con fondo magenta; altrimenti fondo trasparente già ok) → import in `Assets/Sandsunder/Art/Source/Higgsfield/` → registra in `SandboxArtAssetFactory` (aggiungi `Sprite CrepeMask`/`PitMask` a `SandboxArtSet`) → applica `filterMode=Point`, `compression=Uncompressed`, `spritePixelsPerUnit=32`.

---

# FEATURE 2 — SISTEMA TUNNEL LIVELLO -1 & FURTIVITÀ SOTTERRANEA
## (Subterranean Depth System)

### A. Obiettivo e regole di interazione
Raggiungendo **Depth ≥ 2** (scavi cumulativi), il Nomad entra nel **livello sotterraneo (-1)**:
- **Invisibile e inattaccabile** da proiettili e nemici in superficie (Dune Spitter).
- I proiettili di superficie **sorvolano** il giocatore (overflight).
- **Non interagisce** con casse/oggetti di superficie finché non risale al Livello 0.
- Rendering: **silhouette traslucida ciano `#00F0E6`**, opacità **65%**, `sortingOrder = -10`, visibile attraverso la sabbia scavata (galleggiamento/slittamento).

### B. Architettura (deep tunnel)
- **Autorità:** il server possiede la `depth` del giocatore (via `DigGrid`/`MatchSimulation`). Il client applica solo effetto visivo e regole di collisione/overflight.
- **Stato corrente da estendere:** `PrototypeTunnelSystem` ha già l'enum `MatrixLayerDepth` e `TopDownPlayerController.CurrentDepth` (0/1/2). Manca: il **binding depth→layer**, la **stealth (invisibilità ai nemici/proiettili)**, l'**overflight dei proiettili**, e il **blocco interazione con oggetti di superficie**.

### C. Componenti C# (struttura tecnica)

```
Sandsunder.Gameplay/
├── DigDepthSystem.cs            // [NUOVO] autorità depth → layer, eventi
├── SubterraneanStealth.cs       // [NUOVO] invisibilità + invulnerabilità + overflight
├── PrototypeTunnelSystem.cs     // [ESTESO] usa DigDepthSystem, non più solo toggle
├── PrototypeProjectile.cs       // [ESTESO] overflight sopra player sotterraneo
├── PrototypeDuneSpitter*.cs     // [ESTESO] aggro bloccato su target sotterraneo
├── PrototypePickup.cs / PrototypeDesertRuinDoor.cs  // [ESTESO] interazione gated da depth
└── TopDownPlayerController.cs   // [ESTESO] silhouette ciano #00F0E6 @65%
```

**1) `DigDepthSystem : MonoBehaviour`** — unico punto di verità runtime per la profondità.
```csharp
public sealed class DigDepthSystem : MonoBehaviour
{
    public static DigDepthSystem Instance { get; private set; }
    public int CurrentDepth { get; private set; }              // 0 superficie, >=1 tunnel
    public bool IsSubterranean => CurrentDepth >= 1;

    public event Action<int> DepthChanged;

    public void RaiseDepth(int by /* +=1 per scavo profondo, tipicamente 2 */);
    public void SetDepth(int depth);
}
```
- `CompleteDigChanneling` (in `PrototypeCombat.cs`) chiamerà `DigDepthSystem.Instance?.RaiseDepth(2)` invece di `PrototypeTunnelSystem.ToggleNextLayer()`. Il `DougDepthSystem` decide `MatrixLayerDepth` (depth≥2 → `Subterranean_L1`) e notifica `PrototypeTunnelSystem` per la transizione colore già esistente.

**2) `SubterraneanStealth : MonoBehaviour`** — applica le regole di sistema.
```csharp
public sealed class SubterraneanStealth : MonoBehaviour
{
    [SerializeField] private float silhouetteOpacity = 0.65f;
    [SerializeField] private Color silhouetteColor = new(0.0f, 0.94f, 0.90f); // #00F0E6
    [SerializeField] private int silhouetteSortingOrder = -10;

    public bool IsStealthed { get; private set; }

    private void OnDepthChanged(int depth)
    {
        IsStealthed = depth >= 1;
        // 1) silhouette: color=#00F0E6, alpha=0.65, sortingOrder=-10 sul SpriteRenderer del Nomad
        // 2) invulnerabilità ai proiettili di superficie: skip in OnTriggerEnter2D
        // 3) interdizione interazione: dispatcher oggetti (below) se depth>=1
    }
}
```
- **Rendering silhouette:** applica `renderer.color = new Color(0f, 0.94f, 0.90f, 0.65f)` e `renderer.sortingOrder = -10` (attraverso la sabbia scavata). In risalita (`depth==0`) ripristina `Color.white` e l'ordine normale. Nota: `TopDownPlayerController.UpdateSubterraneanVisuals` attuale usa `(0.15,0.55,0.65,0.60)` per depth 2 — **sostituire** con il ciano `#00F0E6` @65% per coerenza con la specifica.

**3) Overflight proiettili** — in `PrototypeProjectile` (o nel sistema proiettili esistente):
```csharp
// Nel callback di collisione con il player:
if (target.TryGetComponent(out SubterraneanStealth stealth) && stealth.IsStealthed)
    return; // il proiettile sorvola: nessun danno, nessun hit
// (opzionale dedicato) il proiettile mantiene la direzione e NON sparisce.
```

**4) Aggro nemico bloccato** — nell'AI del Dune Spitter: se il target ha `SubterraneanStealth.IsStealthed == true`, il nemico torna in patrol e non spara.

**5) Interazione gated** — in `PrototypePickup.TryCollect` e `PrototypeDesertRuinDoor`:
```csharp
if (DigDepthSystem.Instance != null && DigDepthSystem.Instance.IsSubterranean)
    return false; // non puoi interagire con casse/oggetti di superficie da sotto terra
```

### D. Higgsfield (asset minimali per questa feature)
La feature è quasi tutta codice/effetto shader. Asset consigliati (opzionali, per il "feel" di profondità):
1. **Ambient subterranean tint** (per il layer tunnel): usa la transizione colore già in `PrototypeTunnelSystem`; nessun asset necessario.
2. **Silhouette shader** in Unity (non Higgsfield): `Unlit`/`ShaderGraph` con `color = lerp(base, #00F0E6, 0.9)` e `alpha = 0.65`, `Queue=Transparent`. Se preferisci un effetto "flow" del mantello, generare una texture alfa animata del nomad (v. Feature 3) e riusarla qui.

---

# FEATURE 3 — PIPELINE ANIMAZIONI COMPLETE
## (Nomad Player & Desert Mobs Spritesheets)

### A. Obiettivo
Spritesheet pixel-art completi per **Nomad** (IDLE, WALK/RUN, STEALTH CROUCH, ROLL 360°, DIG CHANNELING) e per i **mob** (Dune Spitter: Idle/Patrol/Acid Spit/Death Burst; Sandstorm Golem Boss: charge + nucleo runico). Tutto su fondo **Magenta `#FF00FF`**, 32×32 (Nomad) / 64×64 (mob/boss), direzionati in prospettiva **Top-Down 3/4**.

### B. Stato attuale da estendere
- `NomadAnimatorController.controller` ha già stati `Idle/Walk/Run/Roll/Dig` e param `IsMoving/IsRolling/IsDigging/Speed`.
- `SandboxArtAssetFactory` importa `nomad_32.png` e `spitter_32.png` (singoli sprite) e crea `NomadAnimatorController` via editor.
- Visione target: **spritesheet multi-frame** → slicing → clip animati → Animator (già esistente) + nuovi stati (StealthCrouch, DigChannel, DeathBurst).

### C. Architettura asset→animazione
1. **Generazione** spritesheet su Higgsfield (fondo magenta, griglia N×M).
2. **Keying** con `Tools/art/key_magenta_sprite.py` (o `SandboxArtAssetFactory.CreateKeyedSprite` inline) per rimuovere il magenta.
3. **Slicing** in Unity: `SpriteEditor` o tool editor batch → `Sprite[] frames` (TextureImporter `SpriteImportMode.Multiple`, `spritePixelsPerUnit=32/64`).
4. **Clip animati** generati da codice (editor tool) con `AnimationClip.SetCurve` su `SpriteRenderer.sprite` (sample rate 12 fps, loop).
5. **Animator** esteso: nuove transizioni + param `IsStealthed`, `IsDigChanneling`, `IsDying`.

### D. Componenti C# (struttura tecnica)

```
Sandsunder.Editor/                      (editor-only)
├── SpriteSheetImporter.cs              // [NUOVO] batch: import + slicing + clip
└── SandboxArtAssetFactory.cs           // [ESTESO] registra i nuovi sheet
Sandsunder.Gameplay/
├── NomadAnimator.cs                    // [NUOVO] guida i param dallo stato gameplay
├── SandboxActorVisual.cs               // [ESTESO] PlayIdle/Walk/Run/Stealth/Roll/DigChannel
└── SandstormGolemAI.cs                 // [NUOVO] boss: idle/charge/death
```

**1) `SpriteSheetImporter : EditorWindow`** — tool editor per convertire uno spritesheet in clip:
```csharp
public static AnimationClip BuildClip(Sprite[] frames, string clipName, float fps = 12f)
{
    var clip = new AnimationClip { frameRate = fps, wrapMode = WrapMode.Loop };
    var curve = new AnimationCurve();
    for (int i = 0; i < frames.Length; i++)
        curve.AddKey(new Keyframe(i / fps, 0f)); // ObjectReferenceKeyframe per sprite
    // Imposta ObjectReferenceCurve: clip.SetCurve("", typeof(SpriteRenderer), "m_Sprite", keys);
    return clip;
}
```
- Slicing: `TextureImporter.spritesheet = frames[]` con `SpriteRect` da griglia (righe/colonne in ingresso), `filterMode=Point`, pivot `(0.5,0.08)` per player.

**2) `NomadAnimator : MonoBehaviour`** — come conduca l'Animator esistente:
```csharp
public sealed class NomadAnimator : MonoBehaviour
{
    private Animator animator;
    public void SetMoving(float speed)   { animator.SetFloat("Speed", speed); animator.SetBool("IsMoving", speed > 0.01f); }
    public void SetRolling(bool active)  { animator.SetBool("IsRolling", active); }
    public void SetDigging(bool active)  { animator.SetBool("IsDigging", active); }
    public void SetStealthed(bool active){ animator.SetBool("IsStealthed", active); } // → StealthCrouch
}
```
- Collegato in `SandboxActorVisual` (che già chiama `PlayMelee/PlayRoll/PlayStrike`) e a `PrototypeCombat` (channeling → `SetDigging(true)`).

**3) `SandstormGolemAI : MonoBehaviour`** — boss: macchina a stati `Idle → Telegraph → Charge → Cooldown` + `DeathBurst`; nucleo runico ciano fluttuante (Sprite `CyanRune` già in factory) con `Sine` bob e `Additive` glow.

### E. Istruzioni esatte per generare gli spritesheet su Higgsfield

**Nomad (32×32/cell, fondo `#FF00FF`):**
1. **IDLE (4 frame)**
   > "2D top-down pixel art game sprite sheet, 128×128, 4×1 grid of 32×32 cells, background solid magenta #FF00FF. Nomad desert wanderer seen from top-down 3/4 angle: tan hooded cloak (#D6B336, #C9A05A) with a flowing cloth trailing behind, subtle idle breathing (chest rise/fall) and the cloak swaying gently in the desert wind across the 4 frames. No text, no border, flat orthographic, muted sand palette, crisp pixel-perfect 32px, no anti-aliasing."

2. **WALK / RUN (8 frame)**
   > "…128×256, 4×2 grid of 32×32 cells… Nomad desert wanderer top-down 3/4, full run cycle of 8 frames: legs striding forward, arms pumping, hood cloak flowing and rippling behind, sand dust puffs underfoot. Background solid magenta #FF00FF. Muted desert palette (tan #D6B336, brown #6A4A2E, pale #E4C98A), flat orthographic, crisp 32px pixel art, no anti-aliasing."

3. **STEALTH CROUCH (6 frame)**
   > "…128×192, 4×1.5 → 4×2 grid of 32×32… Nomad in low crouch stealth walk, top-down 3/4, 6 frames: hunched low, slow creeping steps, cloak close to body, subtle forward lean. Background solid magenta #FF00FF, flat orthographic, muted sand palette, pixel-perfect 32px, no anti-aliasing."

4. **ROLL 360° (8 frame)**
   > "…128×256, 4×2 grid of 32×32… Nomad performing a full 360° combat roll evasion, 8 frames: body tucking and spinning end-over-end, sand dust trail and a soft golden motion arc behind the roll. Background solid magenta #FF00FF, flat orthographic, muted desert palette, pixel-perfect 32px, no anti-aliasing."

5. **DIG CHANNELING (6 frame)**
   > "…128×192, 4×2 grid… Nomad digging with a wooden shovel, 6-frame loop: raising the shovel overhead, driving the blade down into the sand, scooping and tossing a spray of sand particles to the side. Background solid magenta #FF00FF, flat orthographic, muted desert palette, pixel-perfect 32px, no anti-aliasing."

**Dune Spitter (64×64/cell, fondo `#FF00FF`):**
1. **IDLE/PATROL (6 frame)**
   > "…256×192, 4×2 grid of 64×64… insectoid desert creature 'Dune Spitter', top-down 3/4, 6-frame idle/patrol: leggy segmented body, small mandibles, sand-crusted carapace (#A97B3E, #6A4A2E), subtle bobbing and antenna twitch. Background solid magenta #FF00FF, flat orthographic, pixel-perfect 64px, no anti-aliasing."
2. **ACID SPIT SPRAY (6 frame)**
   > "… insectoid Dune Spitter rearing up and spraying a wide fan of glowing acid-green projectiles from the mandibles, 6 frames, projectile splash with toxic green (#7FFF4D) globs. Background solid magenta #FF00FF, pixel-perfect 64px."
3. **DEATH BURST (6 frame)**
   > "… Dune Spitter bursting apart into a cloud of sand and chitin shards, 6 frames, dissolving with a green-pink effect flash. Background solid magenta #FF00FF, pixel-perfect 64px."

**Sandstorm Golem Boss (64×64/cell, fondo `#FF00FF`):**
> "…256×256, 4×4 grid of 64×64… colossal sandstone golem boss, 16 frames: 8-frame charge (lurching forward, fists raised, dust cloud) + 8-frame wind-up/telegraph with a floating cyan rune core (#00F0E6) glowing in its chest, sandstorm swirls. Top-down 3/4, background solid magenta #FF00FF, high-contrast silhouette, pixel-perfect 64px, no anti-aliasing."

**Post-generazione (ogni sheet):** scarica PNG → `python3 Tools/art/key_magenta_sprite.py <sheet> <out> --target-height 128|192|256 --padding 0` (keying magenta; il NEAREST preserva i pixel) → import → slicing 32/64px → clip a 12 fps → registra in `SandboxArtAssetFactory` → aggiorna `Design/assets.csv`.

---

# FEATURE 4 — INTERFACCIA UI / HUD & INVENTARIO TAB
## (Sandsunder Modern UI — Glassmorphism)

### A. Obiettivo
UI **glassmorphism scura premium**: pannelli d'arenaria scura **trasparenza 90%**, dettagli dorati + neon ciano `#00F0E6`. Elementi:
- **TAB = Inventario**: Paper-doll del Nomad centrale, barre HP (verde neon 100/100) e Stamina (giallo sabbia 100/100), indicatore Furtività.
- **Hotbar compatta 5 slot** in basso con icone HD trasparenti Higgsfield (Pala, Fucile, Scimitarra, Shotgun, Blaster).
- **Barra ricarica overhead** (40×6px) sopra la testa, **solo durante ricarica** armi da fuoco.
- **Card statistiche arma** a destra nell'inventario TAB: danno/portata/cadenza comparative.

### B. Stato attuale da estendere
- `PrototypeInventoryHUD` costruisce hotbar 5 slot (36×36), selezione 1–5/scroll, sprite procedurali 16px + **fallback HD Higgsfield** già presenti (`art.Shovel/Pistol/Scimitar/Shotgun/Blaster/Relic`).
- `SandboxReloadBar` esiste (barra overhead ricarica).
- `SandboxInventoryWindow` esiste (finestra inventario).
- **Da aggiungere:** glass panel estetico, paper-doll centrale, barre HP/Stamina, card statistiche, indicatore furtività, modalità TAB.

### C. Componenti C# (struttura tecnica)

```
Sandsunder.Gameplay/UI/
├── SandboxModernHUD.cs          // [NUOVO] orchestratore: hotbar + barre + paper-doll
├── GlassPanel.cs                // [NUOVO] pannello vetro (sfondo 90% + bordo + blur)
├── StatBarWidget.cs             // [NUOVO] barra HP/Stamina animata
├── WeaponStatCard.cs            // [NUOVO] card comparativa danno/portata/cadenza
├── StealthIndicator.cs          // [NUOVO] indicatore furtività (ciano)
├── TabInventoryController.cs    // [NUOVO] apertura/chiusura TAB + layout
├── SandboxReloadBar.cs          // [ESTESO] 40×6px, solo durante ricarica
└── PrototypeInventoryHUD.cs     // [ESTESO/REFACTOR] alimenta ModernHUD
```

**1) `GlassPanel : MonoBehaviour`** — materiale vetro: `Image` con `material` ShaderGraph `UI/GlassDark`: colore `#1A1410` con alpha 0.90, bordo dorato 1px (`#D6B336`), riflesso diagonale sottile, `sprite` con corners (9-slice). Se serve blur reale: camera secondaria a bassa risoluzione → `RenderTexture` → `RawImage` sfocato (opzionale, costoso; per MVP va l'alpha 0.90 + bordo).

**2) `StatBarWidget : MonoBehaviour`** — barra reattiva:
```csharp
public sealed class StatBarWidget : MonoBehaviour
{
    [SerializeField] private Image fill;                 // fill amount animato
    [SerializeField] private Color fillColor;            // HP=verde neon #00FF7A, Stamina=giallo #D6B336
    [SerializeField] private Text label;                 // "100/100"
    public void SetValue(float current, float max, float smooth = 6f);
    // lerp fill.fillAmount + label; flash color on gain/loss.
}
```

**3) `WeaponStatCard : MonoBehaviour`** — card a destra:
```csharp
[System.Serializable] public struct WeaponStats { public float Damage, Range, FireRate; }
public sealed class WeaponStatCard : MonoBehaviour
{
    public void Show(string itemId, WeaponStats stats, WeaponStats equipped);
    // 3 barre comparative (danno, portata, cadenza) con fill vs arma equipaggiata.
}
```
- Dati armi da `Design/balance/weapons.csv` (già esistente) → config `ScriptableObject` `WeaponCatalog` (versioned data, non costanti).

**4) `TabInventoryController : MonoBehaviour`** — TAB:
```csharp
public sealed class TabInventoryController : MonoBehaviour
{
    [SerializeField] private GameObject inventoryRoot;   // canvas modal
    public void Toggle() { /* pausa input gameplay, mostra inventoryRoot, popola paper-doll + card */ }
    // Input: KeyCode.Tab (o <Keyboard>/tab) → toggle.
}
```

**5) Layout TAB (1280×720 reference):**
```
┌──────────────────────────── Glass Panel (90%) ────────────────────────────┐
│  [Nome/Logo dorato]                          [Stealth Indicator ● ciano]  │
│  ┌──────────────┬──────────────────────┬──────────────────────────────┐  │
│  │  HOTBAR 5    │   PAPER-DOLL NOMAD   │  WEAPON STAT CARD (destra)   │  │
│  │  slot 64×64  │   (avatar centrale)  │  Danno ▓▓▓▓░░  Range ▓▓░░░░  │  │
│  │  icone HD    │   • HP 100/100 verde │  Cadenza ▓▓▓▓▓░  + equip     │  │
│  │              │   • Stam 100/100 sab │                              │  │
│  └──────────────┴──────────────────────┴──────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────┘
```

### D. Istruzioni esatte per generare i frame UI su Higgsfield
Genera **3 asset** (icone + paper-doll + frame vetro), tutti con fondo trasparente o magenta:

1. **Icone armi HD (5 icone, singole 256×256 trasparenti)** — per l'hotbar (sostituiscono i fallback):
   - **Shovel:** `"HD game item icon, 256×256, transparent background, pixel-art rounded icon of a wooden D-handle shovel with a steel spade blade, subtle gold rim (#D6B336) and soft drop shadow, centered, no text, no background, crisp, game UI icon."`
   - **Rifle (brass):** `"…icon of a brass rifle with a wooden stock and gold barrel highlights, 256×256, transparent background, game UI icon, no text."`
   - **Scimitar:** `"…icon of a curved golden-hilted scimitar with a steel curved blade, 256×256, transparent, game UI icon."`
   - **Shotgun (heavy):** `"…icon of a heavy side-by-side double-barrel shotgun with wooden stock and twin steel barrels, 256×256, transparent, game UI icon."`
   - **Blaster (rune):** `"…icon of a sci-fantasy runic blaster with a glowing cyan rune core (#00F0E6) and dark steel body, 256×256, transparent, game UI icon."`

2. **Paper-doll Nomad (1024×1024, corpo intero, fondo `#FF00FF`)**
   > "HD top-down 3/4 character portrait of the Nomad desert wanderer, full body from head to boots, standing pose with the hooded tan cloak (#D6B336) flowing, holding a wooden shovel, 1024×1024, background solid magenta #FF00FF, premium game UI avatar, soft rim light, no text, no UI elements."

3. **Frame vetro / pannello (512×512, corner 9-slice, fondo trasparente)**
   > "Game UI glass panel frame, 512×512, transparent background, rounded-rect dark sandstone glass panel (#1A1410 at 90% opacity) with a thin gold border (#D6B336) and a subtle diagonal light reflection streak, soft outer glow, premium glassmorphism, no text, 9-slice safe corners."

**Post-generazione:** icone → keying magenta se necessario → `ImportTile(..., 32f)` in `SandboxArtAssetFactory` (aggiungi `IconShovel/IconRifle/IconScimitar/IconShotgun/IconBlaster`, `NomadPortrait`, `GlassFrame`) → usa in `PrototypeInventoryHUD`/`SandboxModernHUD` al posto dei colori procedurali. Aggiorna `Design/assets.csv`.

---

## Piano di implementazione suggerito (ordine di dipendenza)

1. **Feature 1** (base visiva dello scavo) — `DigTerrainView` + `SandCrepeCracksFX` + `SandDustEmitter`; test EditMode su `SandboxVisualEffects` (zero-per-frame alloc).
2. **Feature 2** (profondità) — `DigDepthSystem` + `SubterraneanStealth`; estendi `PrototypeProjectile`/Spitter/Pickup/Door; test PlayMode (overflight + interazione gated + silhouette alfa).
3. **Feature 3** (animazioni) — `SpriteSheetImporter` + `NomadAnimator` + sheet; test visivi in PlayMode (stati).
4. **Feature 4** (UI) — `GlassPanel`/`StatBarWidget`/`WeaponStatCard`/`TabInventoryController`; test EditMode layout (risoluzioni supportate, mouse+controller).

**Quality gates da rispettare (AGENTS.md):** determinismo simulazione (hashing stato) invariato da questi visual; networking/economia non toccati; UI legibile mouse+controller e alle risoluzioni supportate; zero per-frame alloc nei sistemi FX; asset Higgsfield **concept fino a review umana** (silhouette/leggibilità/coerenza/licenza) e registrati in `Design/provenance/*.json`.

---

## Domande di conferma (prima di implementare)
1. **Scope:** vuoi che proceda a **implementare il codice** Unity di queste 4 feature (partendo da Feature 1), o solo la specifica?
2. **Assett:** procedo a **generare su Higgsfield** gli asset (3 mask, particle, 5 sheet Nomad, 3 sheet spitter, 1 sheet golem, 5 icone, 1 paper-doll, 1 frame) — o genero solo una selezione per validare lo stile?
3. **Depth binding:** confermi che "Depth ≥ 2" = entrare in sotterraneo (-1) e che il vecchio `UpdateSubterraneanVisuals` (ciano 0.60) va sostituito con `#00F0E6` @65% `sortingOrder=-10`?