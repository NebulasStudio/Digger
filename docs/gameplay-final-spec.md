# SANDSUNDER — GAMEPLAY FINAL SPECIFICATION (CONTRACT)

**Version:** 1.0.0 MVP  
**Target Platform:** Windows / Steam (Unity 6.3 LTS 6000.3.21f1)  
**Repository:** `NebulasStudio/Digger` (Branch: `main`)

---

## 1.1 WORLD & TERRAIN ARCHITECTURE

1. **Cell Grid / Tilemap Foundation:**
   - The world is constructed as a 48×32 meter Grid of distinct 1×1m Cells (or Unity Tilemap).
   - A single stretched `SpriteRenderer` with repeated `tileMode` (`CreateTiledSprite`) is strictly forbidden for gameplay surfaces.
2. **Biome & Surface Variety:**
   - **Base Terrain:** Desert Sand with 4 HD texture variations (`sand_basecolor`, `sand_feather_basecolor`, `sand_final_basecolor`, `sand_rolled`).
   - **Transitions & Boundaries:** Smooth transitional tile borders between Sand, Dunes, and Ancient Stone Ruins (no hard unblended vertical cuts).
   - **Interactive Props & Structures:** Destructible Vases (`vase_break_32.png`), Treasure Chests (`chest_open_32.png`), Desert Palms, Cactus, Ancient Stone Pillars, and Ruin Walls with physical colliders (`BoxCollider2D`).
3. **Subterranean Cavern Layer (Depth Level -1):**
   - Positioned beneath the surface grid (`sortingOrder = -1500`) with dark bronze cavern tiles (`#2E241A`).

---

## 1.2 PLAYER CHARACTER (NOMAD)

1. **Visual Model:**
   - Uses the real HD Nomad sprite `nomad_32.png` (white hood, cyan/teal scarf, blue tunic, belt, brown boots). Procedural blue/colored fallback sprites (`GetCachedSprite`) are strictly prohibited.
2. **Animator State Machine (`NomadAnimatorController`):**
   - States: `Idle`, `Walk`, `Run`, `Roll`, `Dig`, `StealthCrouch`.
   - Driven by parameters: `Speed` (`Float`), `IsMoving` (`Bool`), `IsRolling` (`Bool`), `IsDigging` (`Bool`), `IsStealthed` (`Bool`).
   - Spritesheet animation clips: `Nomad_WalkNew` (4f), `Nomad_RunNew` (8f), `Nomad_RollNew` (8f), `Nomad_DigNew` (8f), `Nomad_StealthCrouch` (6f).
3. **Weapon Anchoring & Pointers:**
   - The weapon pivot (`weaponRoot`) is anchored directly to the Nomad's hand (`x = +0.25f` when aiming right, `x = -0.25f` when aiming left).
   - The weapon rotates continuously towards the mouse cursor direction vector in `LateUpdate()`.

---

## 1.3 WEAPON ARSENAL & BALANCE PROFILE

| Weapon ID | Visual Sprite | Attack Type | Projectile Visual | Range / Speed |
| :--- | :--- | :--- | :--- | :--- |
| **`shovel`** | `starter_shovel_32.png` | Melee Arc / Dig | Ground Crack FX | 1.8m / Melee |
| **`rifle.brass`** | `rifle_brass_32.png` | Ranged Single Shot | Brass Bullet (`proj_sentinel_cyan_rune`) | 18m / 16m/s |
| **`shotgun.heavy`** | `shotgun_heavy_32.png` | Ranged Spread (5 Pellets) | Heavy Lead Pellets | 12m / 14m/s |
| **`blaster.rune`** | `blaster_rune_32.png` | Energy Rapid Fire | Glowing Cyan Rune Beam | 22m / 24m/s |
| **`sword.scimitar`** | `scimitar_32.png` | Melee Arc Swing | Crescent Slash Arc FX | 2.2m / Melee |
| **`icon.mortar_sandstorm`** | `mortar_32.png` | Arc Lob / Area Blast | Sandstorm Shell | 16m / Lobbed |

- **Right Click Action:** Right-click initiates Digging **ONLY** when the `shovel` item is selected in the active hotbar slot.

---

## 1.4 COMBAT & PROJECTILE VISUALS

1. **Aiming Vector:** Mouse position determines player aim direction; weapons and projectiles launch towards the exact world cursor position.
2. **Projectile Readability:**
   - Core Sprite: `proj_sentinel_cyan_rune_32.png` (or per-weapon sprite) scaled to **`1.20 × 0.80`** world units.
   - Glow & Trail: Glowing aura + `TrailRenderer` width **`0.30m`**, time `0.20s`.
   - Muzzle Flash: Instant directional flash particle at barrel tip upon firing.

---

## 1.5 DYNAMIC CELL DIGGING

1. **3-Stage Cell Deformation:**
   - **Stage 0 (Intact):** Full solid sand tile.
   - **Stage 1 (Cracked):** High-contrast fissure overlay (`DigCrackedSprite`, `sortingOrder = 10`, scaled 1.25×1.25).
   - **Stage 2 (Crater Pit):** Deep excavated crater (`DigOpenedSprite`) exposing subterranean entrance underneath.
2. **Excavation FX:** Radial dust particles + rock debris pop upon shovel impact.

---

## 1.6 SUBTERRANEAN STEALTH & TUNNEL SYSTEM

1. **Transition Flow:**
   - Entering tunnel (`Depth >= 2` or `Shift` held): Smooth screen fade-out (0.2s) -> surface grid opacity reduces to 35% -> subterranean cavern grid illuminates.
2. **Player Underground State:**
   - Nomad converts to a translucent cyan silhouette (`#00F0E6` @ 65% opacity, `sortingOrder = -10`).
   - Surface enemies (Spitters, Golem, Turtle) lose sight and target tracking of subterranean player.

---

## 1.7 MOBS & BOSS COLLISION INVARIANTS

1. **Dune Spitter:** Ranged enemy with `Spitter_Idle` (4f) and `Spitter_DeathBurst` (5f) animations. Fires telegraphed spit projectiles.
2. **Sandstorm Golem Boss:** Heavy boss with charge attack and glowing rune core.
3. **Crystal Turtle:** Patrols, retracts into shell upon hit (invulnerable), lunges forward.
4. **Collision Invariant:** Mobs have `BoxCollider2D` / `CircleCollider2D` + `Physics2D.OverlapCircle` obstacle detection. Mobs **NEVER** clip or walk through ruin walls.

---

## 1.8 USER INTERFACE & MINIMAP

1. **TAB Inventory Window:** Sliced glassmorphism panel (`ui_glass_panel.png`), 32×32 HD item icons, health/stamina bars, weapon stat card.
2. **Minimap:** Golden decorative frame, real-time indicators for player, mobs, and chests.
3. **Clean HUD Rule:** Zero debug grey text panels, zero `[M]` debug overlays, zero raw system popups inside the gameplay viewport.

---

## 2. AUDIT COMPLETO DEI FILE C# (COERENZA CON LA SPEC)

| File C# | Coerente con Spec? | Comportamento Attuale | Discrepanza / Problema | Fix Applicato / Necessario |
| :--- | :--- | :--- | :--- | :--- |
| **`GameplayLabBuilder.cs`** | **COERENTE** | Costruisce la scena 48×32m come griglia di celle 1×1m con 4 varianti di texture sabbia. | Precedentemente usava `CreateTiledSprite` (singolo sprite teso). | **Rifondato:** Costruisce celle individuali 1×1m per superficie e layer sotterraneo. |
| **`SandboxActorVisual.cs`** | **COERENTE** | Gestisce lo `SpriteRenderer` di corpo, ombra ed arma, controllando `NomadAnimator` ed `ApplyFacing`. | In precedenza conteneva ricorsione `Configure()` ed un early return che saltava il binding del controller. | **Risolto:** Assegna `NomadAnimatorController` a `bodyRoot` e mantiene l'arma ancorata alla mano. |
| **`NomadAnimator.cs`** | **COERENTE** | Mappa i parametri `Speed`, `IsMoving`, `IsRolling`, `IsDigging`, `IsStealthed` sull'Animator. | In precedenza le transizioni del controller caricavano le clip vuote a 0 keyframes. | **Risolto:** Cablate le clip reali `Nomad_WalkNew` (4f), `Nomad_RunNew` (8f), `Nomad_RollNew` (8f), `Nomad_DigNew` (8f). |
| **`WeaponAnimator.cs`** | **COERENTE** | Esegue le animazioni per-arma (idle, fire, reload, swing). | Frame popolati dinamicamente da manifest e sprite processate. | Verificato runtime. |
| **`SandboxVisualEffects.cs`** | **COERENTE** | Gestisce proiettili, muzzles, scie ed effetti particellari di scavo. | Proiettili con scala `1.20×0.80` e scia `0.30m` per visibilità ottimale. | Nessun fallback procedurale blu attivo. |
| **`DigTerrainView.cs`** | **COERENTE** | Gestisce la deformazione delle celle in 3 stadi (Intatta → Crepata → Cratere). | Rescalato l'overlay a 1.25×1.25m con `sortingOrder = 10` per visibilità netta. | Sovrapposizione perfetta alle celle 1×1m. |
| **`PrototypeCombat.cs`** | **COERENTE** | Gestisce la logica dei proiettili, Dune Spitter e salute. | I Dune Spitter difettavano di un controllo di collisione con le pareti delle rovine. | **Risolto:** Aggiunta la verifica `Physics2D.OverlapCircle` per impedire il clipping. |
| **`SandstormTurtleAI.cs`** | **COERENTE** | AI della tartaruga di cristallo (patrol, ritirata nel guscio, lunge). | Mancava il controllo di collisione circolare nei muri durante il patrol. | **Risolto:** Aggiunto il controllo ostacoli per impedire alla tartaruga di attraversare le rovine. |
| **`SandstormGolemAI.cs`** | **COERENTE** | AI del Golem della tempesta di sabbia (carica, invocazione rune). | Verificati i limiti di movement e collisione. | Invariante rispettata. |
| **`TopDownPlayerController.cs`** | **COERENTE** | Gestisce il movimento WASD, il roll e l'input del giocatore. | Sovrascriveva forzatamente il colore a ciano in superficie quando depth >= 1. | **Risolto:** Delegata la gestione del colore esclusivamente a `SubterraneanStealth.cs`. |
| **`SubterraneanStealth.cs`** | **COERENTE** | Gestisce lo stato stealth sotterraneo del Nomad. | Imposta il colore a `Color.white` in superficie e silhouette ciano `#00F0E6` a profondità 2. | Transizione visiva pulita. |
| **`PrototypeTunnelSystem.cs`** | **COERENTE** | Gestisce le transizioni di layer del terreno tra superficie e cavern sottostante. | Applica la dissolvenza della griglia di superficie (alpha 35%) al raggiungimento di Subterranean L1. | Effetto cambio mondo. |
| **`PrototypeInventoryHUD.cs`** | **COERENTE** | Gestisce l'inventario del giocatore e le 6 armi equipaggiabili. | Pannello con texture di vetro `ui_glass_panel.png`. | Grafica pulita senza overlay di debug. |

