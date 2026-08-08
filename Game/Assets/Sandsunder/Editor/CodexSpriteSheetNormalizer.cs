using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sandsunder.Editor
{
    /// <summary>
    /// Converts large Codex ImageGen review sheets into deterministic 32 px strips. Outputs remain
    /// under Art/Review and are deliberately excluded from runtime manifests until a human approves
    /// identity, silhouette, frame order and licensing.
    /// </summary>
    public static class CodexSpriteSheetNormalizer
    {
        private const int FrameSize = 32;
        private const int ContentSize = 28;
        private const string SourceRoot = "Assets/Sandsunder/Art/Source/ImageGen";
        private const string ReviewRoot = "Assets/Sandsunder/Art/Review/CodexImage";

        private sealed class SheetPlan
        {
            public string Source;
            public int Columns;
            public string[] RowNames;
        }

        private static readonly SheetPlan[] Plans =
        {
            new()
            {
                Source = $"{SourceRoot}/nomad_locomotion_codex_master.png",
                Columns = 4,
                RowNames = new[] { "nomad_idle", "nomad_walk", "nomad_run", "nomad_roll" }
            },
            new()
            {
                Source = $"{SourceRoot}/nomad_actions_codex_master.png",
                Columns = 4,
                RowNames = new[] { "nomad_dig", "nomad_melee", "nomad_shoot_recoil", "nomad_hurt", "nomad_death" }
            },
            new()
            {
                Source = $"{SourceRoot}/spitter_actions_codex_master.png",
                Columns = 4,
                RowNames = new[] { "spitter_idle", "spitter_charge", "spitter_attack", "spitter_death_burst" }
            },
            new()
            {
                Source = $"{SourceRoot}/weapons_fx_codex_master.png",
                Columns = 4,
                RowNames = new[] { "shovel_swing", "scimitar_swing", "rifle_actions", "weapon_fx" }
            }
        };

        [MenuItem("Sandsunder/Art/Normalize Codex Image Candidates")]
        public static void NormalizeAll()
        {
            EnsureAssetFolder("Assets/Sandsunder/Art/Review");
            EnsureAssetFolder(ReviewRoot);

            foreach (SheetPlan plan in Plans)
            {
                Normalize(plan);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"[CodexSpriteSheetNormalizer] Review strips written to {ReviewRoot}. They are not runtime-approved assets.");
        }

        public static void NormalizeAllFromCommandLine()
        {
            NormalizeAll();
        }

        private static void Normalize(SheetPlan plan)
        {
            string absoluteSource = ToAbsolutePath(plan.Source);
            if (!File.Exists(absoluteSource))
            {
                Debug.LogWarning($"[CodexSpriteSheetNormalizer] Missing candidate: {plan.Source}");
                return;
            }

            Texture2D source = new(2, 2, TextureFormat.RGBA32, false, false);
            try
            {
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes(absoluteSource), markNonReadable: false))
                {
                    throw new InvalidDataException($"Unable to decode {plan.Source}.");
                }

                int rows = plan.RowNames.Length;
                for (int row = 0; row < rows; row++)
                {
                    Texture2D strip = new(plan.Columns * FrameSize, FrameSize, TextureFormat.RGBA32, false, false)
                    {
                        name = $"{plan.RowNames[row]}_codex_review",
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp
                    };
                    strip.SetPixels32(new Color32[strip.width * strip.height]);

                    for (int column = 0; column < plan.Columns; column++)
                    {
                        Color32[] frame = ExtractNormalizedFrame(source, plan.Columns, rows, column, row);
                        strip.SetPixels32(column * FrameSize, 0, FrameSize, FrameSize, frame);
                    }

                    strip.Apply(false, false);
                    string output = $"{ReviewRoot}/{plan.RowNames[row]}_codex_review.png";
                    File.WriteAllBytes(ToAbsolutePath(output), strip.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(strip);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static Color32[] ExtractNormalizedFrame(
            Texture2D source,
            int columns,
            int rows,
            int column,
            int topDownRow)
        {
            int xMin = Mathf.RoundToInt(column * source.width / (float)columns);
            int xMax = Mathf.RoundToInt((column + 1) * source.width / (float)columns);
            int bottomUpRow = rows - 1 - topDownRow;
            int yMin = Mathf.RoundToInt(bottomUpRow * source.height / (float)rows);
            int yMax = Mathf.RoundToInt((bottomUpRow + 1) * source.height / (float)rows);
            int width = Mathf.Max(1, xMax - xMin);
            int height = Mathf.Max(1, yMax - yMin);
            Color32[] cell = source.GetPixels32();
            bool[] background = FloodBackground(cell, source.width, source.height, xMin, yMin, width, height);

            int left = width;
            int right = -1;
            int bottom = height;
            int top = -1;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (background[y * width + x]) continue;
                    left = Mathf.Min(left, x);
                    right = Mathf.Max(right, x);
                    bottom = Mathf.Min(bottom, y);
                    top = Mathf.Max(top, y);
                }
            }

            Color32[] output = new Color32[FrameSize * FrameSize];
            if (right < left || top < bottom) return output;

            int subjectWidth = right - left + 1;
            int subjectHeight = top - bottom + 1;
            float scale = Mathf.Min(ContentSize / (float)subjectWidth, ContentSize / (float)subjectHeight);
            int targetWidth = Mathf.Max(1, Mathf.RoundToInt(subjectWidth * scale));
            int targetHeight = Mathf.Max(1, Mathf.RoundToInt(subjectHeight * scale));
            int targetX = (FrameSize - targetWidth) / 2;
            int targetY = 2;

            for (int y = 0; y < targetHeight; y++)
            {
                int sourceY = bottom + Mathf.Clamp(Mathf.FloorToInt(y / scale), 0, subjectHeight - 1);
                for (int x = 0; x < targetWidth; x++)
                {
                    int sourceX = left + Mathf.Clamp(Mathf.FloorToInt(x / scale), 0, subjectWidth - 1);
                    int localIndex = sourceY * width + sourceX;
                    if (background[localIndex]) continue;
                    Color32 pixel = cell[(yMin + sourceY) * source.width + xMin + sourceX];
                    pixel.a = 255;
                    output[(targetY + y) * FrameSize + targetX + x] = pixel;
                }
            }

            return output;
        }

        private static bool[] FloodBackground(
            Color32[] pixels,
            int textureWidth,
            int textureHeight,
            int xMin,
            int yMin,
            int width,
            int height)
        {
            bool[] visited = new bool[width * height];
            Queue<Vector2Int> queue = new();

            void Enqueue(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height) return;
                int localIndex = y * width + x;
                if (visited[localIndex]) return;
                Color32 pixel = pixels[(yMin + y) * textureWidth + xMin + x];
                if (!IsGeneratedBackground(pixel)) return;
                visited[localIndex] = true;
                queue.Enqueue(new Vector2Int(x, y));
            }

            for (int x = 0; x < width; x++)
            {
                Enqueue(x, 0);
                Enqueue(x, height - 1);
            }
            for (int y = 0; y < height; y++)
            {
                Enqueue(0, y);
                Enqueue(width - 1, y);
            }

            while (queue.Count > 0)
            {
                Vector2Int point = queue.Dequeue();
                Enqueue(point.x - 1, point.y);
                Enqueue(point.x + 1, point.y);
                Enqueue(point.x, point.y - 1);
                Enqueue(point.x, point.y + 1);
            }

            return visited;
        }

        private static bool IsGeneratedBackground(Color32 pixel)
        {
            if (pixel.a < 16) return true;
            int max = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
            int min = Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
            return min >= 224 && max - min <= 12;
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Unity project root could not be resolved.");
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string leaf = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
                throw new InvalidOperationException($"Invalid asset folder: {assetPath}");
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
