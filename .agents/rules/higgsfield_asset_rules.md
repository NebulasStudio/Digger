# Higgsfield Asset & Animation Classification Rules

## Asset Classification Hierarchy (`Assets/Sandsunder/Art/Runtime/`)
- `Characters/`: Player character base sprites (e.g. `nomad_32.png`).
- `Mobs/`: Enemy & neutral creature sprites (e.g. `mob_dune_spitter_32.png`).
- `Weapons/`: Equippable 32x32 weapon icons (e.g. `rifle_brass_32.png`).
- `Projectiles/`: Spells, bullets, and attack FX (e.g. `proj_sentinel_cyan_rune_32.png`).
- `Environment/`: Interactive and static props (e.g. `env_palm_tree_32.png`, `env_relic_chest_32.png`).
- `Terrain/`: Tilemaps & seamless ground textures (e.g. `sand_basecolor.png`).
- `Animations/`: Multi-frame animation sheets (e.g. `nomad_walk.png`, `spitter_charge.png`).
- `UI/`: HUD and modal frames (e.g. `ui_glass_panel.png`).

## Naming Constraints & Importer Auto-Rules
- Character base: `nomad_32.png` (32x32, 32 PPU, point filter).
- Weapon item: `<category>_<name>_32.png` (32x32, 32 PPU, pivot center 0.5, 0.5).
- Props: `env_<name>_32.png` (32x32, 32 PPU, alpha transparency).
- Mobs: `mob_<name>_32.png` (32x32, 32 PPU).
- Animation Sheets: Registered in `Assets/Sandsunder/Art/Generated/AnimationBuildManifest.asset`.
