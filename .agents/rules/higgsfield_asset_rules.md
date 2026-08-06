# Higgsfield Asset & Character System Rules (Sandsunder)

## Existing Character Reference Mapping
- **Nomad**: `Assets/Sandsunder/Art/Runtime/Characters/nomad_32.png` (Blue coat, white hood, teal scarf).
- **Dune Spitter**: `Assets/Sandsunder/Art/Runtime/Mobs/mob_dune_spitter_32.png` (Gold turtle bug, cyan eye).

## New Character: Desert Sorcerer (Ruin Mystic)
- **Base Sprite**: `Assets/Sandsunder/Art/Runtime/Characters/sorcerer_32.png` (Crimson red robes, gold trim, cyan rune staff).
- **Animation Sheets**:
  - `Assets/Sandsunder/Art/Runtime/Animations/sorcerer_walk.png` (4-direction walk)
  - `Assets/Sandsunder/Art/Runtime/Animations/sorcerer_cast.png` (Rune spellcast)
  - `Assets/Sandsunder/Art/Runtime/Animations/sorcerer_dash.png` (Teleport dash)

## Generation & Classification Guidelines
1. Always cross-reference uploaded chat gallery images and memory before generating new character sheets.
2. Maintain 32x32 pixel frames, 32 PPU, point filter, and transparent backgrounds (`alphaIsTransparency = true`).
3. Register all generated animation sheets in `Assets/Sandsunder/Art/Generated/AnimationBuildManifest.asset`.
