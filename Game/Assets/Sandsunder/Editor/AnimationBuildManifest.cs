using System;
using UnityEngine;

namespace Sandsunder.Editor
{
    /// <summary>
    /// Declarative manifest mapping each generated sprite sheet to its Unity clip(s).
    /// Each entry defines the source sheet, the frame grid (columns x rows), the target clip name,
    /// the pivot / pixels-per-unit and playback settings. AnimationClipBuilder consumes this to
    /// slice + build clips automatically.
    /// </summary>
    [CreateAssetMenu(menuName = "Sandsunder/Animation Build Manifest", fileName = "AnimationBuildManifest")]
    public sealed class AnimationBuildManifest : ScriptableObject
    {
        [Serializable]
        public sealed class SheetEntry
        {
            [Tooltip("Path relative to Assets/ (e.g. Assets/Sandsunder/Art/Runtime/Animations/rifle_idle.png)")]
            public string sourcePath;

            [Tooltip("Clip name (no extension). The build writes Assets/Sandsunder/Art/Generated/<clipName>.anim")]
            public string clipName;

            [Tooltip("Frame columns in the sheet grid")]
            public int columns = 4;

            [Tooltip("Frame rows in the sheet grid")]
            public int rows = 1;

            [Tooltip("Pixels per unit for the sliced sprites")]
            public float pixelsPerUnit = 64f;

            [Tooltip("Playback frames per second")]
            public float fps = 12f;

            [Tooltip("Loop? Idle/patrol loop; one-shots (fire/reload/swing/death) do not.")]
            public bool loop = true;
        }

        [SerializeField] private SheetEntry[] sheets = Array.Empty<SheetEntry>();

        public SheetEntry[] Sheets => sheets;
    }
}