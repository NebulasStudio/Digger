using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sandsunder.Editor
{
    /// <summary>
    /// Editor-only batch pipeline for the Feature 3 animation sheets:
    /// 1. Slices a magenta-keyed sprite sheet into a grid of frames (SpriteImportMode.Multiple).
    /// 2. Builds one AnimationClip per state from those frames (SpriteRenderer.m_Sprite curves).
    /// 3. Optionally wires the clips into the NomadAnimatorController.
    ///
    /// Usage (menu): Sandsunder > Art > Build Sprite Sheet Animation...
    /// </summary>
    public static class SpriteSheetImporter
    {
        private const float DefaultFps = 12f;

        /// <summary>
        /// Slice a texture at assetPath into a uniform grid of frames and import it as a Multiple sprite.
        /// Returns the sliced Sprite[] in row-major order (top row first).
        /// </summary>
        public static Sprite[] SliceSheet(string assetPath, int columns, int rows, float pixelsPerUnit)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new System.ArgumentException("A sprite-sheet asset path is required.", nameof(assetPath));
            }

            if (columns <= 0 || rows <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(columns), "Sprite-sheet columns and rows must both be positive.");
            }

            if (pixelsPerUnit <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(pixelsPerUnit), "Pixels per unit must be positive.");
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException($"Missing sprite sheet: {assetPath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            // Reset stale slices first. Unity can otherwise retain orphaned sprite sub-assets when
            // a sheet changes from (for example) 4x4 to 4x2, producing the old frame count.
            importer.spriteImportMode = SpriteImportMode.Single;
#pragma warning disable CS0618
            importer.spritesheet = System.Array.Empty<SpriteMetaData>();
#pragma warning restore CS0618
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.isReadable = true;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();

            importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            importer.spriteImportMode = SpriteImportMode.Multiple;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                throw new FileNotFoundException($"Unable to load sprite sheet after import: {assetPath}");
            }

            if (texture.width % columns != 0 || texture.height % rows != 0)
            {
                throw new System.InvalidOperationException(
                    $"Sprite sheet '{assetPath}' is {texture.width}x{texture.height}, which is not evenly divisible by {columns}x{rows}.");
            }

            int cellW = texture.width / columns;
            int cellH = texture.height / rows;

            var metas = new List<SpriteMetaData>();
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    var meta = new SpriteMetaData
                    {
                        name = $"{System.IO.Path.GetFileNameWithoutExtension(assetPath)}_{row}_{col}",
                        rect = new Rect(col * cellW, (rows - 1 - row) * cellH, cellW, cellH),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, 0.08f)
                    };
                    metas.Add(meta);
                }
            }

#pragma warning disable CS0618
            importer.spritesheet = metas.ToArray();
#pragma warning restore CS0618
            importer.SaveAndReimport();

            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderBy(s => s.name, SuffixComparer.Instance)
                .ToArray();

            int expectedFrameCount = columns * rows;
            if (frames.Length != expectedFrameCount)
            {
                throw new System.InvalidOperationException(
                    $"Sprite sheet '{assetPath}' produced {frames.Length} frames; expected {expectedFrameCount}.");
            }

            return frames;
        }

        /// <summary>Build a looping AnimationClip from a frame strip (SpriteRenderer.m_Sprite).</summary>
        public static AnimationClip BuildClip(Sprite[] frames, string clipName, float fps = DefaultFps, bool loop = true)
        {
            if (frames == null || frames.Length == 0)
            {
                throw new System.ArgumentException("BuildClip requires at least one frame.", nameof(frames));
            }

            if (string.IsNullOrWhiteSpace(clipName))
            {
                throw new System.ArgumentException("An animation clip name is required.", nameof(clipName));
            }

            if (fps <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(fps), "Animation FPS must be positive.");
            }

            string outputPath = $"Assets/Sandsunder/Art/Generated/{clipName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = clipName };
                AssetDatabase.CreateAsset(clip, outputPath);
            }

            clip.frameRate = fps;
            clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;

            var keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i / fps,
                    value = frames[i]
                };
            }

            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        /// <summary>Frame names are "name_ROW_COL"; sort row-major (row, then col).</summary>
        private sealed class SuffixComparer : IComparer<string>
        {
            public static readonly SuffixComparer Instance = new();
            public int Compare(string a, string b)
            {
                int ar = ParseSuffix(a, out int ac);
                int br = ParseSuffix(b, out int bc);
                int row = ar.CompareTo(br);
                return row != 0 ? row : ac.CompareTo(bc);
            }

            private static int ParseSuffix(string name, out int col)
            {
                col = 0;
                string[] parts = name.Split('_');
                if (parts.Length >= 3
                    && int.TryParse(parts[parts.Length - 2], out int parsedRow)
                    && int.TryParse(parts[parts.Length - 1], out int parsedCol))
                {
                    col = parsedCol;
                    return parsedRow;
                }
                return 0;
            }
        }
    }
}
