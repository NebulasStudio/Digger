using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sandsunder.Editor
{
    internal sealed class SandboxArtSet
    {
        public Sprite SandTile { get; set; }
        public Sprite SandFeather { get; set; }
        public Sprite SandFinal { get; set; }
        public Sprite SandRolled { get; set; }
        public Sprite RuinTile { get; set; }
        public Sprite Nomad { get; set; }
        public Sprite Spitter { get; set; }
        public Sprite Shadow { get; set; }
        public Sprite Pistol { get; set; }
        public Sprite Shovel { get; set; }
        public Sprite Scimitar { get; set; }
        public Sprite Shotgun { get; set; }
        public Sprite Blaster { get; set; }
        public Sprite Mortar { get; set; }
        public Sprite Relic { get; set; }
        public Sprite DigIntact { get; set; }
        public Sprite DigCracked { get; set; }
        public Sprite DigOpened { get; set; }
        public Sprite SandTuft { get; set; }
        public Sprite Bone { get; set; }
        public Sprite CyanRune { get; set; }
        public Sprite PalmTree { get; set; }
        public Sprite RuinPillar { get; set; }
        public Sprite Cactus { get; set; }
        public Sprite RunedChest { get; set; }
        public Sprite CrystalTurtle { get; set; }
        public RuntimeAnimatorController PlayerAnimator { get; set; }
    }

    internal static class SandboxArtAssetFactory
    {
        private const string RuntimeRoot = "Assets/Sandsunder/Art/Runtime";
        private const string GeneratedRoot = "Assets/Sandsunder/Art/Generated";

        public static SandboxArtSet LoadOrCreate()
        {
            EnsureAssetFolder(GeneratedRoot);

            Sprite sand = ImportTile($"{RuntimeRoot}/Processed/sand_basecolor.png", pixelsPerUnit: 256f);
            Sprite sandFeather = ImportTileOptional($"{RuntimeRoot}/Processed/sand_feather_basecolor.png", 256f) ?? sand;
            Sprite sandFinal = ImportTileOptional($"{RuntimeRoot}/Processed/sand_final_basecolor.png", 256f) ?? sand;
            Sprite sandRolled = ImportTileOptional($"{RuntimeRoot}/Processed/sand_rolled.png", 256f) ?? sand;
            Sprite ruin = ImportTile($"{RuntimeRoot}/Processed/ruin_basecolor.png", pixelsPerUnit: 256f);
            Sprite nomad = ImportSprite(
                $"{RuntimeRoot}/Processed/nomad_32.png",
                pixelsPerUnit: 32f,
                pivot: new Vector2(0.5f, 0.08f));
            Sprite spitter = ImportSprite(
                $"{RuntimeRoot}/Processed/spitter_32.png",
                pixelsPerUnit: 32f,
                pivot: new Vector2(0.5f, 0.08f));

            RuntimeAnimatorController animator = LoadOrCreatePlayerAnimator();

            return new SandboxArtSet
            {
                SandTile = sand,
                SandFeather = sandFeather,
                SandFinal = sandFinal,
                SandRolled = sandRolled,
                RuinTile = ruin,
                Nomad = nomad,
                Spitter = spitter,
                PlayerAnimator = animator,
                Shadow = CreateProceduralSprite("BlobShadow", 32, 16, 32f, DrawShadow),
                Pistol = ImportSpriteOptional($"{RuntimeRoot}/Processed/rifle_brass_32.png", 32f, new Vector2(0.5f, 0.5f))
                    ?? CreateProceduralSprite("BrassPistol", 20, 10, 32f, DrawPistol),
                Shovel = ImportSpriteOptional($"{RuntimeRoot}/Processed/shovel_default_32.png", 32f, new Vector2(0.5f, 0.5f))
                    ?? CreateProceduralSprite("StarterShovel", 24, 12, 32f, DrawShovel),
                Scimitar = ImportSpriteOptional($"{RuntimeRoot}/Processed/sword_scimitar_32.png", 32f, new Vector2(0.5f, 0.5f))
                    ?? CreateProceduralSprite("DesertScimitar", 28, 14, 32f, DrawScimitar),
                Shotgun = ImportSpriteOptional($"{RuntimeRoot}/Processed/shotgun_heavy_32.png", 32f, new Vector2(0.5f, 0.5f))
                    ?? CreateProceduralSprite("HeavyShotgun", 30, 12, 32f, DrawShotgun),
                Blaster = ImportSpriteOptional($"{RuntimeRoot}/Processed/blaster_rune_32.png", 32f, new Vector2(0.5f, 0.5f))
                    ?? CreateProceduralSprite("RuneBlaster", 26, 12, 32f, DrawBlaster),
                Mortar = ImportSpriteOptional($"{RuntimeRoot}/Processed/icon_mortar_sandstorm_32.png", 32f, new Vector2(0.5f, 0.5f))
                    ?? CreateProceduralSprite("SandstormMortar", 32, 14, 32f, DrawMortar),
                Relic = ImportSpriteOptional($"{RuntimeRoot}/Processed/env_relic_chest_32.png", 32f, new Vector2(0.5f, 0.5f))
                    ?? CreateProceduralSprite("CyanRelic", 24, 24, 32f, DrawRelic),
                DigIntact = CreateProceduralSprite("DigIntact", 32, 24, 32f,
                    (pixels, width, height) => DrawDigNode(pixels, width, height, 0)),
                DigCracked = CreateProceduralSprite("DigCracked", 32, 24, 32f,
                    (pixels, width, height) => DrawDigNode(pixels, width, height, 1)),
                DigOpened = CreateProceduralSprite("DigOpened", 32, 24, 32f,
                    (pixels, width, height) => DrawDigNode(pixels, width, height, 2)),
                SandTuft = CreateProceduralSprite("SandTuft", 20, 20, 32f, DrawSandTuft),
                Bone = CreateProceduralSprite("DesertBone", 24, 16, 32f, DrawBone),
                CyanRune = CreateProceduralSprite("CyanRune", 32, 32, 32f, DrawCyanRune),
                PalmTree = ImportSpriteOptional($"{RuntimeRoot}/Processed/env_palm_tree_32.png", 32f, new Vector2(0.5f, 0.5f)),
                RuinPillar = ImportSpriteOptional($"{RuntimeRoot}/Processed/env_ruin_pillar_32.png", 32f, new Vector2(0.5f, 0.5f)),
                Cactus = ImportSpriteOptional($"{RuntimeRoot}/Processed/env_cactus_32.png", 32f, new Vector2(0.5f, 0.5f)),
                RunedChest = ImportSpriteOptional($"{RuntimeRoot}/Processed/env_chest_runed_32.png", 32f, new Vector2(0.5f, 0.5f)),
                CrystalTurtle = ImportSpriteOptional($"{RuntimeRoot}/Processed/mob_crystal_turtle_64.png", 64f, new Vector2(0.5f, 0.5f)),
            };
        }

        private static Sprite ImportTile(string assetPath, float pixelsPerUnit)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException($"Missing sandbox art source: {assetPath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            TextureImporterSettings importerSettings = new();
            importer.ReadTextureSettings(importerSettings);
            importerSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(importerSettings);
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Unable to import tile sprite at {assetPath}.");
            }

            return sprite;
        }

        private static Sprite ImportTileOptional(string assetPath, float pixelsPerUnit)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return null;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            TextureImporterSettings importerSettings = new();
            importer.ReadTextureSettings(importerSettings);
            importerSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(importerSettings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static Sprite ImportSprite(string assetPath, float pixelsPerUnit, Vector2 pivot)
        {
            Sprite sprite = ImportSpriteOptional(assetPath, pixelsPerUnit, pivot);
            if (sprite == null)
            {
                throw new FileNotFoundException($"Missing sandbox runtime sprite: {assetPath}");
            }
            return sprite;
        }

        /// <summary>
        /// Like <see cref="ImportSprite"/> but returns null (instead of throwing) when the source
        /// asset is missing, so callers can fall back to a procedural sprite. Used for the generated
        /// 32x32 weapon / relic / mortar sprites that are imported from Runtime/Processed.
        /// </summary>
        private static Sprite ImportSpriteOptional(string assetPath, float pixelsPerUnit, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
            }
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;
            TextureImporterSettings importerSettings = new();
            importer.ReadTextureSettings(importerSettings);
            if (importer.spriteImportMode == SpriteImportMode.Single)
            {
                importerSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                importerSettings.spritePivot = pivot;
                importerSettings.spriteMeshType = SpriteMeshType.FullRect;
            }
            importer.SetTextureSettings(importerSettings);
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                var subSprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();
                if (subSprites.Length > 0) sprite = subSprites[0];
            }
            if (sprite == null)
            {
                throw new InvalidOperationException($"Unable to import runtime sprite at {assetPath}.");
            }

            return sprite;
        }

        private static Sprite CreateKeyedSprite(
            string sourcePath,
            string assetName,
            int targetHeightPixels,
            float pixelsPerUnit)
        {
            string texturePath = $"{GeneratedRoot}/{assetName}KeyedTexture.asset";
            string spritePath = $"{GeneratedRoot}/{assetName}KeyedSprite.asset";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existing != null)
            {
                return existing;
            }

            TextureImporter importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException($"Missing keyed sprite source: {sourcePath}");
            }

            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = true;
            importer.SaveAndReimport();

            Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            Color32[] sourcePixels = source.GetPixels32();
            int minimumX = source.width;
            int minimumY = source.height;
            int maximumX = -1;
            int maximumY = -1;

            for (int y = 0; y < source.height; y++)
            {
                for (int x = 0; x < source.width; x++)
                {
                    int index = (y * source.width) + x;
                    Color32 pixel = sourcePixels[index];
                    if (IsKeyMagenta(pixel))
                    {
                        pixel.a = 0;
                        sourcePixels[index] = pixel;
                        continue;
                    }

                    pixel.a = 255;
                    sourcePixels[index] = pixel;
                    minimumX = Math.Min(minimumX, x);
                    minimumY = Math.Min(minimumY, y);
                    maximumX = Math.Max(maximumX, x);
                    maximumY = Math.Max(maximumY, y);
                }
            }

            if (maximumX < minimumX || maximumY < minimumY)
            {
                throw new InvalidOperationException($"No opaque subject remained after keying {sourcePath}.");
            }

            const int sourcePadding = 8;
            minimumX = Math.Max(0, minimumX - sourcePadding);
            minimumY = Math.Max(0, minimumY - sourcePadding);
            maximumX = Math.Min(source.width - 1, maximumX + sourcePadding);
            maximumY = Math.Min(source.height - 1, maximumY + sourcePadding);
            int cropWidth = maximumX - minimumX + 1;
            int cropHeight = maximumY - minimumY + 1;
            float scale = Math.Min(1f, targetHeightPixels / (float)cropHeight);
            int targetWidth = Math.Max(1, Mathf.RoundToInt(cropWidth * scale));
            int targetHeight = Math.Max(1, Mathf.RoundToInt(cropHeight * scale));
            Color32[] targetPixels = new Color32[targetWidth * targetHeight];

            for (int y = 0; y < targetHeight; y++)
            {
                int sourceY = minimumY + Math.Min(cropHeight - 1, Mathf.FloorToInt(y / scale));
                for (int x = 0; x < targetWidth; x++)
                {
                    int sourceX = minimumX + Math.Min(cropWidth - 1, Mathf.FloorToInt(x / scale));
                    targetPixels[(y * targetWidth) + x] = sourcePixels[(sourceY * source.width) + sourceX];
                }
            }

            Texture2D texture = new(targetWidth, targetHeight, TextureFormat.RGBA32, false)
            {
                name = $"{assetName} Keyed Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(targetPixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            AssetDatabase.CreateAsset(texture, texturePath);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, targetWidth, targetHeight),
                new Vector2(0.5f, 0.08f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = $"{assetName} Keyed Sprite";
            AssetDatabase.CreateAsset(sprite, spritePath);
            return sprite;
        }

        private static bool IsKeyMagenta(Color32 pixel)
        {
            return pixel.r >= 145
                && pixel.b >= 145
                && pixel.g <= 145
                && pixel.r > pixel.g + 35
                && pixel.b > pixel.g + 35;
        }

        private static Sprite CreateProceduralSprite(
            string assetName,
            int width,
            int height,
            float pixelsPerUnit,
            Action<Color32[], int, int> draw)
        {
            string texturePath = $"{GeneratedRoot}/{assetName}Texture.asset";
            string spritePath = $"{GeneratedRoot}/{assetName}Sprite.asset";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existing != null)
            {
                return existing;
            }

            Color32[] pixels = new Color32[width * height];
            draw(pixels, width, height);
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                name = $"{assetName} Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            AssetDatabase.CreateAsset(texture, texturePath);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.12f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = $"{assetName} Sprite";
            AssetDatabase.CreateAsset(sprite, spritePath);
            return sprite;
        }

        private static void DrawShadow(Color32[] pixels, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - ((width - 1) * 0.5f)) / (width * 0.48f);
                    float dy = (y - ((height - 1) * 0.5f)) / (height * 0.42f);
                    if ((dx * dx) + (dy * dy) <= 1f)
                    {
                        pixels[(y * width) + x] = new Color32(43, 33, 30, 120);
                    }
                }
            }
        }

        private static void DrawPistol(Color32[] pixels, int width, int height)
        {
            Fill(pixels, width, 2, 4, 15, 7, new Color32(43, 33, 30, 255));
            Fill(pixels, width, 3, 5, 17, 6, new Color32(205, 139, 51, 255));
            Fill(pixels, width, 5, 2, 8, 5, new Color32(90, 54, 35, 255));
            Fill(pixels, width, 4, 7, 9, 8, new Color32(243, 228, 194, 255));
        }

        private static void DrawShovel(Color32[] pixels, int width, int height)
        {
            Color32 umber = new(43, 33, 30, 255);
            Color32 wood = new(135, 82, 45, 255);
            Color32 steelLight = new(220, 225, 235, 255);
            Color32 steelDark = new(100, 110, 125, 255);

            // T-Handle Top
            Fill(pixels, width, 2, 3, 4, 8, umber);
            Fill(pixels, width, 3, 4, 3, 7, wood);

            // Wooden Shaft
            for (int x = 4; x <= 14; x++)
            {
                Set(pixels, width, x, 5, umber);
                Set(pixels, width, x, 6, wood);
                Set(pixels, width, x, 7, umber);
            }

            // Spade Scoop Blade
            Fill(pixels, width, 14, 3, 21, 9, umber);
            Fill(pixels, width, 15, 4, 20, 8, steelDark);
            Fill(pixels, width, 16, 5, 22, 7, steelLight);
            Set(pixels, width, 23, 6, steelLight);
        }

        private static void DrawScimitar(Color32[] pixels, int width, int height)
        {
            Color32 steel = new(210, 215, 222, 255);
            Color32 gold = new(240, 192, 60, 255);
            Color32 wood = new(92, 57, 39, 255);

            Fill(pixels, width, 2, 4, 6, 8, wood);
            Fill(pixels, width, 6, 3, 8, 9, gold);
            for (int x = 8; x < width - 2; x++)
            {
                int y = 5 + (x > 18 ? (x - 18) / 2 : 0);
                Set(pixels, width, x, y, steel);
                Set(pixels, width, x, y + 1, steel);
            }
        }

        private static void DrawShotgun(Color32[] pixels, int width, int height)
        {
            Color32 steel = new(80, 85, 92, 255);
            Color32 wood = new(135, 82, 45, 255);

            Fill(pixels, width, 2, 3, 9, 7, wood);
            Fill(pixels, width, 9, 6, width - 2, 8, steel);
            Fill(pixels, width, 9, 3, width - 2, 5, steel);
        }

        private static void DrawBlaster(Color32[] pixels, int width, int height)
        {
            Color32 steel = new(60, 65, 75, 255);
            Color32 cyanEnergy = new(50, 240, 230, 255);

            Fill(pixels, width, 3, 3, 18, 8, steel);
            Fill(pixels, width, 8, 5, 14, 7, cyanEnergy);
            Fill(pixels, width, 18, 4, width - 2, 7, cyanEnergy);
        }

        private static void DrawDigNode(Color32[] pixels, int width, int height, int state)
        {
            Color32 umberOutline = new(43, 33, 30, 255);
            Color32 woodDark = new(92, 57, 39, 255);
            Color32 woodLight = new(154, 98, 54, 255);
            Color32 ironBand = new(72, 78, 85, 255);
            Color32 goldLatch = new(240, 192, 60, 255);

            if (state == 2)
            {
                // Opened chest / excavated loot pit
                Fill(pixels, width, 4, 3, 27, 20, umberOutline);
                Fill(pixels, width, 6, 5, 25, 18, woodDark);
                Fill(pixels, width, 8, 4, 23, 11, new Color32(20, 15, 12, 255));
                Fill(pixels, width, 10, 5, 21, 9, new Color32(220, 180, 50, 255));
            }
            else
            {
                // Intact or Cracked wooden chest
                Fill(pixels, width, 4, 3, 27, 20, umberOutline);
                Fill(pixels, width, 6, 5, 25, 18, woodLight);
                // Wood grain planks
                DrawLine(pixels, width, 6, 11, 25, 11, umberOutline);
                // Iron bands
                Fill(pixels, width, 9, 5, 11, 18, ironBand);
                Fill(pixels, width, 20, 5, 22, 18, ironBand);
                // Gold latch
                Fill(pixels, width, 14, 9, 17, 13, goldLatch);

                if (state == 1)
                {
                    // Crack lines
                    DrawLine(pixels, width, 12, 16, 15, 8, umberOutline);
                    DrawLine(pixels, width, 15, 8, 18, 14, umberOutline);
                }
            }
        }

        private static void DrawSandTuft(Color32[] pixels, int width, int height)
        {
            Color32 dark = new(111, 76, 41, 255);
            Color32 light = new(211, 149, 55, 255);
            DrawLine(pixels, width, 9, 2, 6, 16, dark);
            DrawLine(pixels, width, 10, 2, 10, 18, light);
            DrawLine(pixels, width, 11, 2, 15, 15, dark);
        }

        private static void DrawBone(Color32[] pixels, int width, int height)
        {
            Color32 outline = new(62, 44, 36, 255);
            Color32 bone = new(243, 228, 194, 255);
            Fill(pixels, width, 4, 6, 19, 9, outline);
            Fill(pixels, width, 5, 7, 18, 8, bone);
            Fill(pixels, width, 2, 4, 6, 11, outline);
            Fill(pixels, width, 17, 4, 21, 11, outline);
            Fill(pixels, width, 3, 5, 5, 10, bone);
            Fill(pixels, width, 18, 5, 20, 10, bone);
        }

        private static void DrawCyanRune(Color32[] pixels, int width, int height)
        {
            Color32 dark = new(43, 33, 30, 255);
            Color32 cyan = new(100, 244, 229, 230);
            for (int index = 5; index <= 26; index++)
            {
                Set(pixels, width, index, 5, dark);
                Set(pixels, width, index, 26, dark);
                Set(pixels, width, 5, index, dark);
                Set(pixels, width, 26, index, dark);
            }
            for (int index = 8; index <= 23; index++)
            {
                Set(pixels, width, index, 8, cyan);
                Set(pixels, width, index, 23, cyan);
                Set(pixels, width, 8, index, cyan);
                Set(pixels, width, 23, index, cyan);
            }
        }

        private static void DrawLine(Color32[] pixels, int width, int x0, int y0, int x1, int y1, Color32 color)
        {
            int dx = Math.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            while (true)
            {
                Set(pixels, width, x0, y0, color);
                if (x0 == x1 && y0 == y1) break;
                int doubled = 2 * error;
                if (doubled >= dy) { error += dy; x0 += sx; }
                if (doubled <= dx) { error += dx; y0 += sy; }
            }
        }

        private static void Fill(Color32[] pixels, int width, int minimumX, int minimumY, int maximumX, int maximumY, Color32 color)
        {
            for (int y = minimumY; y <= maximumY; y++)
            {
                for (int x = minimumX; x <= maximumX; x++)
                {
                    Set(pixels, width, x, y, color);
                }
            }
        }

        private static void Set(Color32[] pixels, int width, int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= width || y >= pixels.Length / width) return;
            pixels[(y * width) + x] = color;
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            string[] segments = assetPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }
                current = next;
            }
        }

        public static RuntimeAnimatorController LoadOrCreatePlayerAnimator()
        {
            string controllerPath = $"{GeneratedRoot}/NomadAnimatorController.controller";
            AssetDatabase.DeleteAsset(controllerPath); // Force regenerate to apply new transitions and states

            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsRolling", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsDigging", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

            AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{GeneratedRoot}/Shovel_Idle.anim") ?? CreateClip($"{GeneratedRoot}/Nomad_Idle.anim", "Nomad_Idle");
            AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{GeneratedRoot}/Nomad_WalkNew.anim") ?? CreateClip($"{GeneratedRoot}/Nomad_Walk.anim", "Nomad_Walk");
            AnimationClip runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{GeneratedRoot}/Nomad_RunNew.anim") ?? CreateClip($"{GeneratedRoot}/Nomad_Run.anim", "Nomad_Run");
            AnimationClip rollClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{GeneratedRoot}/Nomad_RollNew.anim") ?? CreateClip($"{GeneratedRoot}/Nomad_Roll.anim", "Nomad_Roll");
            AnimationClip digClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{GeneratedRoot}/Nomad_DigNew.anim") ?? CreateClip($"{GeneratedRoot}/Nomad_Dig.anim", "Nomad_Dig");

            var rootStateMach = controller.layers[0].stateMachine;
            var idleState = rootStateMach.AddState("Idle");
            idleState.motion = idleClip;

            var walkState = rootStateMach.AddState("Walk");
            walkState.motion = walkClip;

            var runState = rootStateMach.AddState("Run");
            runState.motion = runClip;

            var rollState = rootStateMach.AddState("Roll");
            rollState.motion = rollClip;

            var digState = rootStateMach.AddState("Dig");
            digState.motion = digClip;

            // Any State -> Roll (if IsRolling == true)
            var rollTransition = rootStateMach.AddAnyStateTransition(rollState);
            rollTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsRolling");
            rollTransition.duration = 0.05f;

            // Roll -> Idle (if IsRolling == false)
            var rollToIdle = rollState.AddTransition(idleState);
            rollToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsRolling");
            rollToIdle.duration = 0.1f;

            // Idle -> Walk (if IsMoving == true && Speed <= 0.75)
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsMoving");
            idleToWalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.75f, "Speed");
            idleToWalk.duration = 0.15f;

            // Walk -> Idle (if IsMoving == false)
            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsMoving");
            walkToIdle.duration = 0.15f;

            // Idle -> Run (if IsMoving == true && Speed > 0.75)
            var idleToRun = idleState.AddTransition(runState);
            idleToRun.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsMoving");
            idleToRun.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.75f, "Speed");
            idleToRun.duration = 0.15f;

            // Run -> Idle (if IsMoving == false)
            var runToIdle = runState.AddTransition(idleState);
            runToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsMoving");
            runToIdle.duration = 0.15f;

            // Walk -> Run (if Speed > 0.75)
            var walkToRun = walkState.AddTransition(runState);
            walkToRun.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.75f, "Speed");
            walkToRun.duration = 0.15f;

            // Run -> Walk (if Speed <= 0.75)
            var runToWalk = runState.AddTransition(walkState);
            runToWalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.75f, "Speed");
            runToWalk.duration = 0.15f;

            // Any State -> Dig (if IsDigging == true)
            var digTransition = rootStateMach.AddAnyStateTransition(digState);
            digTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsDigging");
            digTransition.duration = 0.05f;

            // Dig -> Idle (if IsDigging == false)
            var digToIdle = digState.AddTransition(idleState);
            digToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsDigging");
            digToIdle.duration = 0.1f;

            rootStateMach.defaultState = idleState;
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip CreateClip(string path, string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = name };
                AssetDatabase.CreateAsset(clip, path);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (name == "Nomad_Idle")
            {
                clip.ClearCurves();
                // Breathing: light rhythmic vertical scaling (bobbing)
                AnimationCurve scaleY = new(
                    new Keyframe(0f, 1.0f),
                    new Keyframe(0.5f, 1.03f),
                    new Keyframe(1.0f, 1.0f)
                );
                clip.SetCurve("", typeof(Transform), "m_LocalScale.y", scaleY);

                AnimationCurve posY = new(
                    new Keyframe(0f, 0.16f),
                    new Keyframe(0.5f, 0.175f),
                    new Keyframe(1.0f, 0.16f)
                );
                clip.SetCurve("", typeof(Transform), "m_LocalPosition.y", posY);
            }
            else if (name == "Nomad_Walk")
            {
                clip.ClearCurves();
                // Walk: alternating stride/tilt of 5 degrees (approx 0.043f in Quaternion.z)
                AnimationCurve rotZ = new(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.25f, 0.043f),
                    new Keyframe(0.5f, 0f),
                    new Keyframe(0.75f, -0.043f),
                    new Keyframe(1.0f, 0f)
                );
                clip.SetCurve("", typeof(Transform), "m_LocalRotation.z", rotZ);

                AnimationCurve rotW = new(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.25f, 0.999f),
                    new Keyframe(0.5f, 1f),
                    new Keyframe(0.75f, 0.999f),
                    new Keyframe(1.0f, 1f)
                );
                clip.SetCurve("", typeof(Transform), "m_LocalRotation.w", rotW);

                AnimationCurve posY = new(
                    new Keyframe(0f, 0.16f),
                    new Keyframe(0.25f, 0.18f),
                    new Keyframe(0.5f, 0.16f),
                    new Keyframe(0.75f, 0.18f),
                    new Keyframe(1.0f, 0.16f)
                );
                clip.SetCurve("", typeof(Transform), "m_LocalPosition.y", posY);
            }
            else if (name == "Nomad_Run")
            {
                clip.ClearCurves();
                // Run: lean forward/aerodynamic tilt at 12 degrees (approx 0.104f in Quaternion.z)
                AnimationCurve rotZ = new(
                    new Keyframe(0f, 0.104f),
                    new Keyframe(0.5f, 0.12f),
                    new Keyframe(1.0f, 0.104f)
                );
                clip.SetCurve("", typeof(Transform), "m_LocalRotation.z", rotZ);

                AnimationCurve rotW = new(
                    new Keyframe(0f, 0.994f),
                    new Keyframe(0.5f, 0.992f),
                    new Keyframe(1.0f, 0.994f)
                );
                clip.SetCurve("", typeof(Transform), "m_LocalRotation.w", rotW);

                AnimationCurve posY = new(
                    new Keyframe(0f, 0.14f),
                    new Keyframe(0.25f, 0.16f),
                    new Keyframe(0.5f, 0.14f),
                    new Keyframe(0.75f, 0.16f),
                    new Keyframe(1.0f, 0.14f)
                );
                clip.SetCurve("", typeof(Transform), "m_LocalPosition.y", posY);
            }
            else if (name == "Nomad_Roll")
            {
                clip.ClearCurves();
                // Roll: 360 rotation around z
                AnimationCurve rotZ = new(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.5f, 1.0f),
                    new Keyframe(1.0f, 0f)
                );
                clip.SetCurve("", typeof(Transform), "m_LocalRotation.z", rotZ);

                AnimationCurve rotW = new(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.5f, 0f),
                    new Keyframe(1.0f, -1f)
                );
                clip.SetCurve("", typeof(Transform), "m_LocalRotation.w", rotW);
            }
            else if (name == "Nomad_Dig")
            {
                clip.ClearCurves();
                // Dig: rapid back/forth shaking and downward scaling
                AnimationCurve scaleY = new(
                    new Keyframe(0f, 1.0f),
                    new Keyframe(0.25f, 0.85f),
                    new Keyframe(0.5f, 1.0f),
                    new Keyframe(0.75f, 0.85f),
                    new Keyframe(1.0f, 1.0f)
                );
                clip.SetCurve("", typeof(Transform), "m_LocalScale.y", scaleY);

                AnimationCurve rotZ = new(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.2f, 0.08f),
                    new Keyframe(0.4f, -0.08f),
                    new Keyframe(0.6f, 0.08f),
                    new Keyframe(0.8f, -0.08f),
                    new Keyframe(1.0f, 0f)
                );
                clip.SetCurve("", typeof(Transform), "m_LocalRotation.z", rotZ);

                AnimationCurve rotW = new(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.2f, 0.997f),
                    new Keyframe(0.4f, 0.997f),
                    new Keyframe(0.6f, 0.997f),
                    new Keyframe(0.8f, 0.997f),
                    new Keyframe(1.0f, 1f)
                );
                clip.SetCurve("", typeof(Transform), "m_LocalRotation.w", rotW);
            }

            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void DrawMortar(Color32[] pixels, int width, int height)
        {
            Color32 darkBrass = new(107, 81, 46, 255);
            Color32 cyanRune = new(51, 242, 230, 255);
            Color32 woodGrip = new(122, 76, 41, 255);
            Color32 clear = new(0, 0, 0, 0);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (y >= 4 && y <= 9 && x >= 2 && x <= 26)
                        pixels[y * width + x] = darkBrass;
                    else if (y >= 5 && y <= 8 && (x == 10 || x == 18))
                        pixels[y * width + x] = cyanRune;
                    else if (y >= 3 && y <= 10 && x >= 24 && x <= 30)
                        pixels[y * width + x] = woodGrip;
                    else
                        pixels[y * width + x] = clear;
                }
            }
        }

        private static void DrawRelic(Color32[] pixels, int width, int height)
        {
            Color32 goldRing = new(242, 204, 64, 255);
            Color32 cyanOrb = new(51, 242, 230, 255);
            Color32 clear = new(0, 0, 0, 0);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(11.5f, 11.5f));
                    if (dist <= 7f)
                        pixels[y * width + x] = cyanOrb;
                    else if (dist <= 10f && (x == y || x + y == width - 1))
                        pixels[y * width + x] = goldRing;
                    else
                        pixels[y * width + x] = clear;
                }
            }
        }
    }
}
