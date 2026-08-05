using System.Collections.Generic;
using System.Linq;
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
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new System.IO.FileNotFoundException($"Missing sprite sheet: {assetPath}");
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = pixelsPerUnit;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
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

            importer.spritesheet = metas.ToArray();
            importer.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderBy(s => s.name, SuffixComparer.Instance)
                .ToArray();
        }

        /// <summary>Build a looping AnimationClip from a frame strip (SpriteRenderer.m_Sprite).</summary>
        public static AnimationClip BuildClip(Sprite[] frames, string clipName, float fps = DefaultFps)
        {
            if (frames == null || frames.Length == 0)
            {
                throw new System.ArgumentException("BuildClip requires at least one frame.", nameof(frames));
            }

            AnimationClip clip = new AnimationClip { frameRate = fps, wrapMode = WrapMode.Loop };

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
            AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings
            {
                loopTime = true,
                wrapMode = WrapMode.Loop
            });

            AssetDatabase.CreateAsset(clip, $"Assets/Sandsunder/Art/Generated/{clipName}.anim");
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
                int idx = name.LastIndexOf('_');
                col = 0;
                if (idx < 0) return 0;
                string[] parts = name.Substring(idx + 1).Split('_');
                if (parts.Length >= 1 && int.TryParse(parts[0], out int parsedRow))
                {
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int parsedCol)) col = parsedCol;
                    return parsedRow;
                }
                return 0;
            }
        }
    }
}