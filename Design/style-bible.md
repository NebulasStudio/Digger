# Sandsunder visual style bible

Status: foundation contract; generated sandbox media exists, but no production media is approved.  
Canonical reference ID: `SB-FORMULA-1`

## Frozen STYLE FORMULA

The paragraph below is inserted byte-for-byte into every visual generation prompt. It may change only after explicit art-direction approval; changing it invalidates the visual set and reopens review.

> Modern chunky pixel art with a crisp 32x32 grid feel, selective sub-pixel highlights, and no texture filtering. Use bold, readable silhouettes with two-pixel deep umber outlines, angular desert-worn shapes, and restrained interior detail. Environments use warm ochre, sand, clay, and charcoal shadows; playable characters use cool turquoise, cobalt, cream, and coral contrasts; hazards, rare loot, and objectives use luminous acid-cyan signals. Keep a tense yet adventurous desert mood under flat directional sunlight, with high gameplay contrast, clean readable silhouettes, and a consistent top-down three-quarter perspective across all assets.

STYLE TOKEN (`SB-TOKEN-1`):

> chunky pixel art, warm ochre desert, cool contrasting heroes, acid-cyan signals, deep umber outlines, adventurous mood

## Visual grammar

- Camera: fixed top-down three-quarter gameplay perspective. Character and mob sheets never use side view or frontal-only view.
- Shapes: playable silhouettes are compact and rounded-angular; enemies use low, wide or pointed profiles; interactable structures use clear vertical landmarks.
- Outlines: two logical pixels in deep umber `#2B211E`; one-pixel interior separations only where needed for pose readability.
- Detail: no more than three interior material clusters per small sprite. Avoid sand-coloured character torsos and decorative noise at gameplay scale.
- Light: flat sun from upper-left; one highlight ramp and one shadow ramp. No dynamic cast shadow is baked into keyed sprites.
- Shadows: runtime ellipse/drop shadows are separate assets or shaders and never part of a keyed sprite.
- Animation: strong contact poses, short anticipation, readable recovery; no smear may expand the gameplay hitbox.

## Palette by role

| Role | Canonical colours | Use |
|---|---|---|
| Sand/environment | `#D9A441`, `#B87333`, `#7A4E2D`, `#3B2C29` | Ground, ruins, depth, shadow. |
| Players | `#3BC7C4`, `#315C9B`, `#F3E4C2`, `#D9695F` | High-contrast costume blocks; each character uses two dominant colours. |
| Common loot | `#F3E4C2` | Neutral pickup readability. |
| Rare/objective signal | `#64F4E5` | Relic, Sigils, ritual, high-value edges; use sparingly. |
| Hostile warning | `#EF5B3F` | Attack telegraphs, damage, dangerous surfaces. |
| Healing/safe state | `#77C66E` | Healing and confirmed safe interactions only. |
| UI ink | `#211A19`, `#F7EBD1` | High-contrast panels and text. |

Colour is redundant with icon shape, animation, and audio. Objective cyan cannot be reused for ordinary decoration; warning coral cannot signal a beneficial pickup.

## Pixel and import contract

- Higgsfield concept inputs may be generated at 1k, but every shipped sprite is manually cleaned on the canonical logical pixel grid.
- Downsampling uses nearest-neighbour only. No Lanczos/bilinear resampling, mip-map blur, texture compression bleeding, or runtime filtering.
- Unity import defaults: Sprite (2D and UI), Filter Mode `Point`, Compression `None` during production, mipmaps off for sprites/UI, alpha-is-transparency on after key removal.
- Gameplay pixel density: 32 logical pixels per world metre. Sprite canvases can be padded powers of two, but `relative_scale` and collider dimensions come from data, not canvas bounds.
- Tiles use power-of-two masters and must wrap on all four edges. Pixel-perfect camera reference resolution starts at 640×360 and scales by integers where the display permits.
- Generated keyed subjects default to magenta `#FF00FF`; use green `#00FF00` when the subject contains magenta/purple, and blue `#0000FF` only when both conflict. Key removal must clear enclosed regions as well as borders.

## Asset prompt contract

Every visual prompt is assembled in this exact order:

`kind template + stable 3–4 word description + SB-FORMULA-1 byte-identical + kind suffix`

Do not insert character names, artist names, living-artist imitation, existing game IP, output dimensions, transparency claims, or URLs into the formula. Content-specific details belong only in the stable description. All six character sheets use the same animation vocabulary: idle, eight-direction movement, shovel strike, weapon fire, active, hit, downed.

## Readability budgets

- At gameplay zoom, identify player vs mob in 250 ms and rarity/objective state without reading text.
- No two critical telegraphs overlap in hue, silhouette, and timing. Telegraphs remain readable under the three common colour-vision simulations.
- Player body is visually larger than its collision radius; enemy hurtboxes never exceed the readable body without a telegraph.
- VFX may obscure at most 20% of the local decision area for 150 ms. Screen shake and flashes have accessible zero settings.
- Contact-sheet review places all sprites at manifest `relative_scale` on each sand ring before approval.

## Review and generation limits

The primary static generator is Higgsfield `nano_banana_2`; UI or tightly controlled sprite sheets may use `gpt_image_2` only when documented in provenance. Each asset has at most two regeneration attempts for style drift, keying, tiling, crop, or content failure. After that it is manually corrected or stays rejected—never silently shipped.

Human approval is mandatory for silhouette, role palette, top-down perspective, scale, animation readability, key removal, collision readability, license/provenance, and Steam AI disclosure. Generated output is a source, not a finished production asset.

Audio direction (`SB-AUDIO-1`): dry hand percussion, granular wind, wood/metal shovel transients, and short synthetic objective tones. Combat mixes reserve the 1–4 kHz band for threat and objective cues; music ducks under guardian, extraction, and ritual completion signals.
