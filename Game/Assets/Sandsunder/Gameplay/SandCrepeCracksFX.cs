using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Fracture lines (starburst "crepe cracks") emitted live while the player channels a dig on
    /// the Right Mouse Button. The cracks are transient, drawn as thin sand-shadow line strips that
    /// fan out radially from the dig point and fade out, handing into the persistent per-cell
    /// Cracked/Opened overlay that DigTerrainView keeps.
    ///
    /// Zero per-frame allocations: the line renderer is pooled and reused.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public sealed class SandCrepeCracksFX : MonoBehaviour
    {
        private const int MaxArms = 8;
        private static SandCrepeCracksFX _instance;

        public static SandCrepeCracksFX Instance => _instance;

        [SerializeField] private int arms = 6;
        [SerializeField] private float crackLength = 0.35f;
        [SerializeField] private float life = 0.9f;
        [SerializeField] private Color crackColor = new(0.42f, 0.30f, 0.16f, 0.85f);

        private LineRenderer[] _lines;
        private float[] _ages;
        private readonly Vector3[] _positions = new Vector3[2];

        private void Awake()
        {
            _instance = this;
            _lines = new LineRenderer[MaxArms];
            _ages = new float[MaxArms];
            for (int i = 0; i < MaxArms; i++)
            {
                LineRenderer line = CreateLine(i);
                _lines[i] = line;
                _ages[i] = float.MaxValue; // inactive
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private LineRenderer CreateLine(int index)
        {
            GameObject go = new($"CrepeCrack_{index}");
            go.transform.SetParent(transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.numCapVertices = 2;
            line.startWidth = 0.045f;
            line.endWidth = 0.012f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = crackColor;
            line.endColor = new Color(crackColor.r, crackColor.g, crackColor.b, crackColor.a * 0.4f);
            line.gameObject.SetActive(false);
            return line;
        }

        /// <summary>Emit a starburst crack fan at a world point. Called once per dig strike.</summary>
        public void SpawnStarburst(Vector2 worldPoint, int depthDelta)
        {
            // Disabled: eliminate procedural crepe crack line strip decals from the game completely.
        }

        private void Update()
        {
            for (int i = 0; i < MaxArms; i++)
            {
                if (_ages[i] >= life) continue;

                _ages[i] += Time.deltaTime;
                float t = Mathf.Clamp01(_ages[i] / life);
                Color color = crackColor;
                color.a = crackColor.a * (1f - t);
                _lines[i].startColor = color;
                _lines[i].endColor = new Color(color.r, color.g, color.b, color.a * 0.4f);

                if (_ages[i] >= life)
                {
                    _lines[i].gameObject.SetActive(false);
                }
            }
        }
    }
}