# 🎨 REGOLE DI CLASSIFICAZIONE E NAMING ASSET HIGGSFIELD → UNITY (SANDSUNDER)

Questo documento definisce lo standard di convenzione dei nomi, la struttura delle cartelle e le regole di rendering per l'integrazione nativa tra **Higgsfield AI**, gli agenti di sviluppo ed il motore di gioco **Unity 6**.

---

## 📁 1. STRUTTURA DELLE CARTELLE ED ORGANIZZAZIONE DEGLI ASSET

Tutti gli asset generati ed elaborati devono risiedere nella cartella `Assets/Sandsunder/Art/Runtime/` suddivisi rigorosamente in 8 sottocartelle tematiche:

```text
Assets/Sandsunder/Art/Runtime/
├── Characters/   -> Sprite base dei personaggi giocabili (es. nomad_32.png)
├── Mobs/         -> Sprite nemici e creature (es. mob_dune_spitter_32.png)
├── Weapons/      -> Icone ed armi impugnabili 32x32 (es. rifle_brass_32.png)
├── Projectiles/  -> Proiettili ed effetti d'attacco (es. proj_sentinel_cyan_rune_32.png)
├── Environment/  -> Oggetti decorativi ed interattivi (es. env_palm_tree_32.png)
├── Terrain/      -> Tile map seamless del terreno e rovine (es. sand_basecolor.png)
├── Animations/   -> Sprite Sheet di animazione (es. nomad_walk.png, spitter_charge.png)
└── UI/           -> Elementi di interfaccia grafica glassmorphic (es. ui_glass_panel.png)
```

---

## 🏷️ 2. CONVENZIONI DI NAMING ED ESTENSIONI (CLASSIFICAZIONE AUTOMATICA)

Gli asset devono rispettare i seguenti prefissi in modo che lo script di automazione C# (`SandboxArtAssetFactory.cs`) e l'importer di Unity li riconoscano senza configurazione manuale:

| Categoria Asset | Prefisso File | Esempio Nome File | PPU (Pixels Per Unit) | Formato Texture Importer |
| :--- | :--- | :--- | :--- | :--- |
| **Personaggio Base** | `nomad_` / `char_` | `nomad_32.png` | `32` | Sprite Single, Point Filter, Clamp |
| **Animazione Personaggio** | `<char>_<azione>.png` | `nomad_walk.png` | `32` | Sprite Multiple, Grid Slicing |
| **Mob / Nemico** | `mob_` | `mob_dune_spitter_32.png` | `32` | Sprite Single / Multiple, Point Filter |
| **Arma Impugnabile** | `<arma>_32.png` | `sword_scimitar_32.png` | `32` | Sprite Single, Pivot Center (0.5, 0.5) |
| **Oggetto Ambiente** | `env_` | `env_relic_chest_32.png` | `32` | Sprite Single, Alpha Transparency |
| **Tile Terreno** | `sand_` / `ruin_` | `sand_basecolor.png` | `256` | Sprite Single, Wrap Mode: Repeat |
| **Pannello UI** | `ui_` | `ui_glass_panel.png` | `100` | Sprite Single, 9-Slice Sliced |

---

## ⚙️ 3. REGOLE GENERALI DI SCRITTURA NELL'ENVIRONMENT (UNITY & AGENTI)

1. **Invarianza dello Sprite Base Nomad:**
   - Lo sprite del personaggio principale Nomad (`nomad_32.png`) ha giacca blu, cappuccio bianco e sciarpa verde acqua.
   - Non sostituire mai lo sprite base con altri personaggi (es. pellegrini o esploratori in tunica gialla).

2. **Gestione Trasparenza & Chroma Key (Sfondo):**
   - Tutti gli Sprite Sheet e le immagini devono avere **sfondo trasparente** (`alphaIsTransparency = true`) o colore magenta solido `#FF00FF` da convertire in trasparente tramite lo script d'importazione.

3. **Ancoraggio Armi in Mano:**
   - Le armi impugnate vengono posizionate sull'ancora `weaponRoot` a `X = ±0.08m`, `Y = 0.05m` rispetto al centro del personaggio per allineare l'impugnatura esattamente alle mani.

4. **Registro nel Manifest Animazioni:**
   - Ogni nuovo Sprite Sheet inserito in `Runtime/Animations/` deve essere registrato in `Assets/Sandsunder/Art/Generated/AnimationBuildManifest.asset` indicando `columns`, `rows`, `fps` e `pixelsPerUnit: 32`.
