using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Runtime weapon sprite-frame player. Each state (idle / fire / reload / swing) is a Sprite[]
    /// strip produced by the editor pipeline (AnimationClipBuilder + SpriteSheetImporter). Idle
    /// loops continuously; fire/reload/swing are one-shots that return to idle.
    ///
    /// No AnimatorController required -- frames are advanced by a deterministic timer.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WeaponAnimator : MonoBehaviour
    {
        public enum WeaponState { Idle, Fire, Reload, Swing }

        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] fireFrames;
        [SerializeField] private Sprite[] reloadFrames;
        [SerializeField] private Sprite[] swingFrames;
        [SerializeField] private float frameRate = 12f;

        private SpriteRenderer spriteRenderer;
        private WeaponState state = WeaponState.Idle;
        private Sprite[] current;
        private float timer;
        private int frameIndex;

        public WeaponState State => state;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            current = idleFrames;
            if (current != null && current.Length > 0) spriteRenderer.sprite = current[0];
        }

        public void Configure(Sprite[] idle, Sprite[] fire, Sprite[] reload, Sprite[] swing)
        {
            idleFrames = idle;
            fireFrames = fire;
            reloadFrames = reload;
            swingFrames = swing;
        }

        public void PlayIdle() => SetState(WeaponState.Idle, idleFrames, true);
        public void PlayFire() => SetState(WeaponState.Fire, fireFrames, false);
        public void PlayReload() => SetState(WeaponState.Reload, reloadFrames, false);
        public void PlaySwing() => SetState(WeaponState.Swing, swingFrames, false);

        private void SetState(WeaponState newState, Sprite[] frames, bool loop)
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }
            if (state == newState && current == frames)
            {
                return;
            }
            state = newState;
            current = frames;
            frameIndex = 0;
            timer = 0f;
            spriteRenderer.sprite = frames[0];
        }

        private void Update()
        {
            if (current == null || current.Length == 0 || spriteRenderer == null) return;

            timer += Time.deltaTime * frameRate;
            while (timer >= 1f)
            {
                timer -= 1f;
                frameIndex++;
                if (frameIndex >= current.Length)
                {
                    if (state == WeaponState.Idle)
                    {
                        frameIndex = 0;
                    }
                    else
                    {
                        // One-shot finished: snap back to idle.
                        state = WeaponState.Idle;
                        current = idleFrames;
                        frameIndex = 0;
                        timer = 0f;
                        if (current == null || current.Length == 0) return;
                    }
                }
                spriteRenderer.sprite = current[frameIndex];
            }
        }
    }
}