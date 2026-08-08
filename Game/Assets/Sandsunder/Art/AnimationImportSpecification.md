# Sandsunder Animation Import Specification

## Runtime contract

- All character, mob, weapon, environment, projectile, and animation-sheet frames use 32 PPU, Point filtering, Clamp wrapping, no mipmaps, Uncompressed texture compression, and alpha transparency.
- Ground tiles remain the only exception: 256 PPU and Repeat wrapping.
- Sheets must divide evenly by their declared column and row count. The importer rejects partial cells rather than silently cropping them.
- Frames are named and sorted in top-row-first, left-to-right order (`<sheet>_<row>_<column>`). Pivot is bottom-centre `(0.5, 0.08)` to preserve readable foot contact and visual collision language.
- Re-running the builder updates the existing generated clip in place. It does not create duplicate clips or replace a referenced controller asset.

## Visual language and readability

- The Nomad base body is always `Runtime/Characters/nomad_32.png`: blue coat `#3466B8`, white hood, and turquoise scarf `#26B8C6`. Presentation states may tilt, bob, recoil, tint, or add afterimages, but must never substitute a legacy or concept-sheet body frame.
- `nomad_*.png` is the only allowed source family for Nomad clips. `wanderer_walk`, `explorer_dig`, `scout_run`, and `rogue_roll` are retained as legacy sources only and cannot be bound to the Nomad controller.
- The Spitter uses `Runtime/Mobs/spitter_32.png` as its neutral readable silhouette. `Spitter_Idle` loops; Charge and DeathBurst are one-shots. Charge must expose a coral/ember telegraph before damage, while death must never alter collision or health authority.
- Body sheets never contain held weapons. A weapon is an independent child at `weaponRoot = (plus or minus 0.08m, 0.05m)` and can recoil or swing only through its own child offset.
- The visual-only BlobShadow is rooted at local `(0m, -0.15m)`, beneath the body. It communicates grounding and must not move the actor collider or Rigidbody.

## Review gate

Generated or concept-derived sheets are source material until a human reviews silhouette separation, controller-distance readability, frame alignment, visual collision language, colour consistency, animation looping, license/provenance, and the lack of direct imitation of a living artist or existing game/IP. No automated builder run is approval to ship.
