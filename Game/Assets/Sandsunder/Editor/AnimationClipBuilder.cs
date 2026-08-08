using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sandsunder.Editor
{
    /// <summary>
    /// Editor batch pipeline (Feature 3) that consumes an AnimationBuildManifest and, for each
    /// sheet entry, slices the sheet into frames and builds an AnimationClip under
    /// Assets/Sandsunder/Art/Generated/. Idempotent: re-running rebuilds clips from the manifest.
    ///
    /// Menu: Sandsunder > Art > Build Animation Clips From Manifest
    /// </summary>
    public static class AnimationClipBuilder
    {
        private const string GeneratedRoot = "Assets/Sandsunder/Art/Generated";

        [MenuItem("Sandsunder/Art/Build Animation Clips From Manifest")]
        public static void BuildAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:Sandsunder.Editor.AnimationBuildManifest");
            if (guids.Length == 0)
            {
                Debug.LogWarning("No AnimationBuildManifest asset found. Create one via Assets > Create > Sandsunder > Animation Build Manifest.");
                return;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var manifest = AssetDatabase.LoadAssetAtPath<AnimationBuildManifest>(path);
                if (manifest == null) continue;
                BuildManifest(manifest);
            }
        }

        public static void BuildManifest(AnimationBuildManifest manifest)
        {
            if (manifest == null || manifest.Sheets == null || manifest.Sheets.Length == 0) return;

            var clipNames = new HashSet<string>();

            foreach (AnimationBuildManifest.SheetEntry entry in manifest.Sheets)
            {
                if (entry == null || string.IsNullOrEmpty(entry.sourcePath) || string.IsNullOrEmpty(entry.clipName))
                {
                    continue;
                }

                if (!clipNames.Add(entry.clipName))
                {
                    throw new System.InvalidOperationException(
                        $"Animation manifest contains more than one entry for clip '{entry.clipName}'. Each generated clip must have one source sheet.");
                }

                Sprite[] frames = SpriteSheetImporter.SliceSheet(
                    entry.sourcePath,
                    entry.columns,
                    entry.rows,
                    entry.pixelsPerUnit);

                AnimationClip clip = SpriteSheetImporter.BuildClip(frames, entry.clipName, entry.fps, entry.loop);

                Debug.Log($"[AnimationClipBuilder] Built {entry.clipName} ({frames.Length} frames) from {entry.sourcePath}");
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>Runtime helper: load every frame of a clip as a Sprite[] for the frame-player.</summary>
        public static Sprite[] ClipFrames(AnimationClip clip)
        {
            if (clip == null) return null;
            var curve = AnimationUtility.GetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
            if (curve == null) return null;
            return curve.Select(k => k.value as Sprite).Where(s => s != null).ToArray();
        }
    }
}
