# 🏜️ SANDSUNDER - CODEX UNITY MASTER HANDOFF & TECHNICAL SPECIFICATION
> **Document Version**: 2.0.0 (Authoritative Handoff Specification)  
> **Target Subsystem**: Unity Client, Art Pipeline, Sandbox Gameplay, UI & Animation Rigging  
> **Repository Path**: `Game/Assets/Sandsunder/Art/CODEX_UNITY_MASTER_HANDOFF.md`  
> **Target Agent**: CODEX (Unity Builder Agent with direct Unity CLI & MCP capabilities)

---

## 📋 1. EXECUTIVE SUMMARY & MISSION DIRECTIVE

Welcome, Agent CODEX. This document serves as your **master sitemap, asset catalog, game database, and step-by-step execution protocol** for Sandsunder—a 2D Top-Down PvPvE excavation game built in Unity 6 LTS.

### Primary Goal
Reduce speculative breadth and transform the current project into a **rock-solid, 100% verified Sandbox MVP**. Instead of managing dozens of partially broken weapons and broken sprite animations, we are scoping down to a tight, perfectly polished vertical slice containing:
1. **The Nomad Player Character**: Official blue coat, white hood, turquoise scarf (`nomad_32.png`), perfectly proportioned, 0 sliced sprite artifacts, with working Idle, Walk, Run, Dig, Stealth Crouch, Roll, Hurt, and Death states.
2. **1 Hostile Mob**: The **Spitter** (Crystal Turtle mob using `spitter_32.png` / `spitter_source.png`), fully animated with `Spitter_Idle`, `Spitter_Charge`, and `Spitter_DeathBurst`.
3. **1 Starter Shovel**: Digs sand pits + performs melee swing attack.
4. **1 Melee Weapon**: The **Desert Scimitar** (`sword_scimitar_32.png`), swinging in a 90° arc.
5. **1 Ranged Weapon**: The **Brass Rifle** (`rifle_brass_32.png`), anchored to player hands, tracking mouse aim in 360°, firing straight projectiles.
6. **UI & HUD Overhaul**: Ancient desert ruin / temple brochure aesthetic, glassmorphic HUD frames, Health Bar, **O2 (Oxygen) Subterranean Level Bar**, and TAB Inventory modal with Minecraft-style paper-doll player preview.

---

## 📁 2. FILE SITEMAP & FOLDER CONVENTIONS

All graphical assets and tools MUST reside strictly under `Game/Assets/Sandsunder/Art/` adhering to the official hierarchy:

```
Game/Assets/Sandsunder/Art/
├── Source/Higgsfield/         # Raw generated textures, concept sheets, and master prompt source material
│   ├── nomad_source.png       # Master 1024x1024 source art for Nomad
│   ├── spitter_source.png     # Master source art for Crystal Turtle Spitter
│   ├── ruin_wall_source.png   # Master source art for ruin walls
│   └── sand_source.png        # Master source art for sand terrain
│
├── Runtime/                   # Production-ready game assets used by Unity at runtime
│   ├── Characters/            # 32x32 Base character body sprites (32 PPU, Point Filter, Uncompressed)
│   │   └── nomad_32.png       # Base Nomad sprite (Blue coat #3466B8, white hood, turquoise scarf #26B8C6)
│   ├── Mobs/                  # Enemy and neutral creature sprites (32 PPU)
│   │   ├── spitter_32.png     # Crystal Turtle Spitter mob
│   │   └── mob_worm_32.png    # Golden Dune Beetle Worm mob
│   ├── Weapons/               # Equippable 32x32 item icons (32 PPU)
│   │   ├── shovel_default_32.png
│   │   ├── rifle_brass_32.png
│   │   ├── sword_scimitar_32.png
│   │   ├── shotgun_heavy_32.png
│   │   └── blaster_rune_32.png
│   ├── Environment/           # Interactive props and destructibles (32 PPU)
│   │   ├── env_palm_tree_32.png
│   │   ├── env_ruin_pillar_32.png
│   │   ├── env_chest_runed_32.png
│   │   └── env_relic_chest_32.png
│   ├── Terrain/               # Seamless ground & wall tilemaps (256 PPU)
│   │   ├── sand_basecolor.png
│   │   └── ruin_basecolor.png
│   └── Animations/            # Multi-frame PNG animation sheets (32 PPU)
│       ├── nomad_walk.png, nomad_run.png, nomad_dig.png, nomad_stealth_crouch.png
│       ├── nomad_melee_scimitar.png, nomad_shoot_recoil.png, nomad_hurt.png, nomad_death.png
│       └── spitter_idle.png, spitter_charge.png, spitter_death_burst.png
│
├── Generated/                 # Generated .anim clips and .controller assets
│   ├── NomadAnimatorController.controller
│   ├── SpitterAnimatorController.controller
│   └── *.anim clips
│
└── CODEX_UNITY_MASTER_HANDOFF.md # THIS HANDOFF SPECIFICATION FILE
```

---

## 📊 3. COMPLETE ASSET INVENTORY & GAME STATS DATABASE

### A. Asset Catalog Inventory

| Asset Name | Relative Path | Type | PPU | Purpose / Description |
| :--- | :--- | :--- | :--- | :--- |
| `nomad_32.png` | `Runtime/Characters/nomad_32.png` | Sprite | 32 | Base player body sprite (Blue coat, white hood) |
| `spitter_32.png` | `Runtime/Mobs/spitter_32.png` | Sprite | 32 | Crystal turtle hostile mob base sprite |
| `mob_worm_32.png` | `Runtime/Mobs/mob_worm_32.png` | Sprite | 32 | Golden Dune Worm secondary mob base sprite |
| `shovel_default_32.png` | `Runtime/Weapons/shovel_default_32.png` | Sprite | 32 | Starter excavation tool & melee weapon |
| `rifle_brass_32.png` | `Runtime/Weapons/rifle_brass_32.png` | Sprite | 32 | Primary ranged rifle weapon |
| `sword_scimitar_32.png` | `Runtime/Weapons/sword_scimitar_32.png` | Sprite | 32 | Desert scimitar melee weapon |
| `shotgun_heavy_32.png` | `Runtime/Weapons/shotgun_heavy_32.png` | Sprite | 32 | Heavy spread shotgun weapon |
| `blaster_rune_32.png` | `Runtime/Weapons/blaster_rune_32.png` | Sprite | 32 | Ancient cyan rune energy blaster |
| `sand_basecolor.png` | `Runtime/Terrain/sand_basecolor.png` | Tile | 256 | Seamless desert sand ground tile |
| `ruin_basecolor.png` | `Runtime/Terrain/ruin_basecolor.png` | Tile | 256 | Ancient sandstone ruin floor & wall tile |

### B. Game Balance & Simulation Database

| Entity / Weapon | Max HP | Damage | Speed / Rate | Range / Reach | Cooldown / Interval | Hitbox Radius |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Nomad Player** | 100 HP | N/A | 4.2 m/s | N/A | Roll: 1.25s (75 ticks) | 0.35m Circle |
| **Spitter Mob** | 55 HP | 4 HP | 0.8 m/s | 6.0 m | 2.66s (160 ticks) | 0.42m Circle |
| **Starter Shovel** | N/A | 12 HP | 1.82 Swings/s | 1.4 m | 0.55s (33 ticks) | 90° Arc, 1.4m |
| **Brass Rifle** | N/A | 6 HP | 300 RPM (5/s) | 11.0 m | 0.20s (12 ticks) | Projectile: 0.1m |
| **Desert Scimitar** | N/A | 18 HP | 2.50 Swings/s | 1.6 m | 0.40s (24 ticks) | 110° Arc, 1.6m |
| **Heavy Shotgun** | N/A | 5x22 HP | 33 RPM | 10.0 m | 1.80s (108 ticks) | 5x Pellets |
| **Rune Blaster** | N/A | 38 HP | 66 RPM | 16.0 m | 0.90s (54 ticks) | Projectile: 0.2m |
| **O2 Subterranean**| 100% | Depletes -1%/s | Refills +5%/s | N/A | Max 100 Seconds | N/A |

---

## 🎨 4. HIGGSFIELD AI INTEGRATION & ANIMATION DIAGNOSIS

### How Higgsfield AI Was Used
1. Higgsfield AI generates 2D pixel art concept sheets and sprite PNGs based on standard prompts (blue coat `#3466B8`, white hood, turquoise scarf `#26B8C6`, transparent/magenta key background, 32 PPU).
2. PNG files were pushed into `Game/Assets/Sandsunder/Art/Runtime/Animations/`.
3. An automated editor tool (`SpriteSheetImporter.cs` + `AnimationClipBuilder.cs`) sliced textures into uniform grids (4x4, 4x1, 4x2) and created `.anim` clips.

### Root Cause of Previous Animation Glitches & Video Errors
- **Uneven Cell Bounding Boxes**: AI-generated sheets often have slight sprite offsets within grid cells. When auto-sliced into fixed rectangular grids (e.g. 32x32 per frame), the sprite center jumps, causing body parts to be cut off and creating floating blue coat rectangles on screen.
- **Animator Component Overriding Base Renderer**: In Unity, binding `NomadAnimatorController` to the player's `BodyRenderer` caused Unity's Animator to swap `bodyRenderer.sprite` to broken sliced frames every frame.
- **Solution for CODEX**:
  - Keep **Nomad Invariance**: Player always renders `nomad_32.png` as its base solid body sprite!
  - For multi-frame animations (Walk, Dig, Roll), use **Manual Unity Animation Window editing** or precise keyframe bounding rects. Guide the user step-by-step to bind clips manually in Unity Editor, or write custom C# animation scripts (`SpriteFramePlayer`) that use exact pixel rect offsets.

---

## 🖥️ 5. UI & HUD OVERHAUL SPECIFICATION

The game UI must be completely redesigned to feel like an **ancient desert ruin / dungeon brochure** with premium glassmorphism.

### Key UI Components
1. **Health Bar (Red/Gold)**: Located top-left, featuring a glass panel frame (`ui_glass_panel.png`) with a smooth fill bar and current/max HP text (`100 / 100`).
2. **O2 (Oxygen) Subterranean Level Bar (Blue/Cyan)**: Located directly below the Health Bar.
   - When the player is on the surface (Level 0), O2 remains at 100% (hidden or full).
   - When the player digs subterranean (Level -1), the O2 bar activates, smoothly depleting over 100 seconds.
   - If O2 reaches 0%, subterranean suffocation damage occurs (5 HP/sec) until surfacing.
3. **Minecraft-Style TAB Inventory Modal**:
   - Toggles open/closed on **TAB** keypress (or **Escape** to close).
   - **Left Panel**: 3D / Paper-doll preview of the Nomad character (`nomad_32.png`), displaying current armor, held weapon, and level stats.
   - **Center Grid**: 5-slot item hotbar and 15-slot desert backpack grid for collected loot, relics, and weapons.
   - **Right Panel**: Weapon Stat Card displaying Damage, Fire Rate, Reach, and Ammo type.

---

## 🎯 6. PHYSICS, SHADOWS, AIMING & SHOOTING

1. **Blob Shadows**:
   - Shadow renderer (`shadowRenderer`) anchored at feet (`X = 0m`, `Y = -0.15m`, `Z = 0m`).
   - Sprite: `BlobShadow` (32x16 oval, semi-transparent black `Color(0.08, 0.06, 0.05, 0.48)`). Prevents the character from appearing to float.
2. **360° Weapon Aiming**:
   - Hand Anchor (`weaponRoot`): Positioned tightly at `X = ±0.08m`, `Y = 0.05m`.
   - Rotation: `Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg`.
   - Facing Left (`explicitAim.x < 0`): `flipY = true`, `localRotation = Quaternion.Euler(0, 0, angle + 180f)` to keep weapon upright and pointing directly at the cursor.
3. **Straight Shooting Trajectory**:
   - Bullet spawn position: `transform.position + (aimDirection * 0.42f)`.
   - Trajectory: Linear velocity vector `aimDirection * speed` without curve distortion or mouse offset errors.

---

## 🤖 7. DIRECTIVES FOR AGENT CODEX (CLI & MCP EXECUTION)

When Agent CODEX receives this handoff prompt, it MUST follow these instructions:

1. **Initial Protocol**: Start in **PLAN MODE**. Do NOT edit codebase files blindly.
2. **Reconnaissance Steps**:
   - Inspect `Game/Assets/Sandsunder/Art/` and `Game/Assets/Sandsunder/Editor/`.
   - Check `SandboxActorVisual.cs`, `GameplayLabBuilder.cs`, and `SandboxArtAssetFactory.cs`.
   - Run `dotnet test Game.sln` to verify C# build status.
3. **Unity MCP Execution**:
   - Call `Unity_RunCommand` to verify Unity Editor status, refresh asset database, and rebuild `GameplayLab.unity`.
4. **User Guidance**:
   - If manual Unity Animation Window clip creation is needed, provide step-by-step instructions to the user on how to open `Window > Animation > Animation`, select clips, and assign keyframe sprite rects.

---

## 📌 8. VERIFICATION CHECKLIST FOR CODEX

- [ ] `dotnet test Game.sln` compiles with 0 errors.
- [ ] Unity Editor scene `Assets/Scenes/GameplayLab.unity` builds cleanly via `GameplayLabBuilder.BuildFromCommandLine()`.
- [ ] Nomad character renders `nomad_32.png` with 0 sliced floating coat artifacts.
- [ ] Spitter mob executes `Spitter_Idle.anim` in loop continuously.
- [ ] Left-Click attacks enemies only (no ground digging or chest spawning).
- [ ] Right-Click / Space / Shift digs sand pits (`DigIntact`, `DigCracked`, `DigOpened`).
- [ ] TAB key toggles glassmorphic inventory modal open and closed cleanly.
- [ ] All changes committed and pushed to GitHub `origin/main`.
