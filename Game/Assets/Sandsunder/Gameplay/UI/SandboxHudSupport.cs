using System;
using System.Collections.Generic;
using Sandsunder.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Sandsunder.Gameplay.UI
{
    /// <summary>Build-safe references to reviewed art kept in the canonical Art/Runtime hierarchy.</summary>
    public sealed class SandboxHudArtRegistry : ScriptableObject
    {
        public Sprite GlassPanel;
        public Sprite Nomad;
        public Sprite Shovel;
        public Sprite Rifle;
        public Sprite Scimitar;
    }

    /// <summary>
    /// Presentation-only oxygen source. Authoritative gameplay owns the values and pushes snapshots;
    /// the HUD never advances or mutates oxygen state.
    /// </summary>
    public interface IPlayerOxygenProvider
    {
        float CurrentOxygen { get; }
        float MaximumOxygen { get; }
        bool IsSubterranean { get; }
        event Action<float, float> OxygenChanged;
    }

    public readonly struct SandboxHudItemDefinition
    {
        public SandboxHudItemDefinition(
            string itemId,
            string displayName,
            string category,
            float damage,
            float range,
            float cadence,
            string description)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Category = category;
            Damage = damage;
            Range = range;
            Cadence = cadence;
            Description = description;
        }

        public string ItemId { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public float Damage { get; }
        public float Range { get; }
        public float Cadence { get; }
        public string Description { get; }
    }

    /// <summary>Versioned sandbox projection. It is display data, never an authority for combat.</summary>
    public static class SandboxHudCatalog
    {
        public const int SchemaVersion = 1;
        public const string RulesetVersion = CombatRules.PrototypeVersion;

        private static readonly IReadOnlyDictionary<string, SandboxHudItemDefinition> Items =
            new Dictionary<string, SandboxHudItemDefinition>(StringComparer.Ordinal)
            {
                ["shovel.default"] = new(
                    "shovel.default", "Steel Shovel", "Tool / Melee",
                    SandboxGameplayCatalog.MilestoneOne.Shovel.Damage,
                    SandboxGameplayCatalog.MilestoneOne.Shovel.ReachMillimetres / 1000f,
                    SandboxGameplayCatalog.MilestoneOne.TicksPerSecond
                        / (float)SandboxGameplayCatalog.MilestoneOne.Shovel.CooldownTicks,
                    "Excavates sand and performs a short committed melee arc."),
                ["rifle.brass"] = new(
                    "rifle.brass", "Brass Rifle", "Ranged",
                    SandboxGameplayCatalog.MilestoneOne.Rifle.Damage,
                    SandboxGameplayCatalog.MilestoneOne.Rifle.ProjectileRangeMillimetres / 1000f,
                    SandboxGameplayCatalog.MilestoneOne.TicksPerSecond
                        / (float)SandboxGameplayCatalog.MilestoneOne.Rifle.CooldownTicks,
                    "A straight-firing brass rifle with muzzle flash and casing ejection."),
                ["sword.scimitar"] = new(
                    "sword.scimitar", "Desert Scimitar", "Melee",
                    SandboxGameplayCatalog.MilestoneOne.Scimitar.Damage,
                    SandboxGameplayCatalog.MilestoneOne.Scimitar.ReachMillimetres / 1000f,
                    SandboxGameplayCatalog.MilestoneOne.TicksPerSecond
                        / (float)SandboxGameplayCatalog.MilestoneOne.Scimitar.CooldownTicks,
                    "A close-range curved blade with a 90 degree authoritative attack arc."),
                ["key.subterranean"] = new(
                    "key.subterranean", "Subterranean Key", "Quest", 0f, 0f, 0f,
                    "Unlocks an eligible ruin door. It grants no permanent competitive power."),
                ["prototype_heal"] = new(
                    "prototype_heal", "Field Remedy", "Utility", 0f, 0f, 0f,
                    "A match utility pickup. Account progression remains horizontal."),
                ["consumable.oxygen-flask"] = new(
                    "consumable.oxygen-flask", "Oxygen Flask", "Utility", 0f, 0f, 0f,
                    $"Restores {SubterraneanOxygenRules.OxygenFlaskRestorePercent}% oxygen underground. Match-only; press E to use.")
            };

        public static SandboxHudItemDefinition Get(string itemId)
        {
            if (!string.IsNullOrEmpty(itemId) && Items.TryGetValue(itemId, out SandboxHudItemDefinition item))
            {
                return item;
            }

            return new SandboxHudItemDefinition(
                itemId ?? string.Empty,
                string.IsNullOrEmpty(itemId) ? "Empty slot" : itemId.Replace('.', ' '),
                "Inventory",
                0f,
                0f,
                0f,
                string.IsNullOrEmpty(itemId) ? "Select an occupied slot to inspect it." : "No sandbox display data is registered.");
        }
    }

    /// <summary>
    /// Build-safe sprite resolver. Resources entries are optional; deterministic pixel fallbacks keep
    /// standalone builds functional without UnityEditor.AssetDatabase.
    /// </summary>
    public static class SandboxHudSpriteLibrary
    {
        private const string GlassResource = "Sandsunder/UI/ui_glass_panel";
        private const string NomadResource = "Sandsunder/UI/nomad_32";
        private static readonly Dictionary<string, Sprite> ItemSprites = new(StringComparer.Ordinal);
        private static Sprite glassPanel;
        private static Sprite nomad;
        private static SandboxHudArtRegistry registry;

        public static void RegisterBuildSprites(Sprite glassPanelSprite, Sprite nomadSprite)
        {
            if (glassPanelSprite != null) glassPanel = glassPanelSprite;
            if (nomadSprite != null) nomad = nomadSprite;
        }

        public static Sprite GetGlassPanelSprite()
        {
            if (glassPanel != null) return glassPanel;
            // Small HUD panels need a real 9-slice frame. The reviewed brochure composite is a
            // full-screen background and is exposed separately instead of being distorted here.
            glassPanel = Resources.Load<Sprite>(GlassResource) ?? CreateGlassPanelSprite();
            return glassPanel;
        }

        public static Sprite GetBrochurePanelSprite()
        {
            return LoadRegistry()?.GlassPanel;
        }

        public static Sprite GetNomadSprite()
        {
            if (nomad != null) return nomad;
            nomad = LoadRegistry()?.Nomad
                ?? Resources.Load<Sprite>(NomadResource)
                ?? FindRuntimeNomadSprite()
                ?? CreateNomadSprite();
            return nomad;
        }

        public static Sprite GetItemSprite(string itemId)
        {
            string key = string.IsNullOrEmpty(itemId) ? "empty" : itemId;
            if (ItemSprites.TryGetValue(key, out Sprite sprite)) return sprite;

            SandboxHudArtRegistry art = LoadRegistry();
            sprite = key switch
            {
                "shovel.default" => art != null ? art.Shovel : null,
                "rifle.brass" => art != null ? art.Rifle : null,
                "sword.scimitar" => art != null ? art.Scimitar : null,
                _ => null
            };
            sprite ??= Resources.Load<Sprite>($"Sandsunder/UI/Items/{key.Replace('.', '_')}")
                ?? CreateItemSprite(key);
            ItemSprites[key] = sprite;
            return sprite;
        }

        private static SandboxHudArtRegistry LoadRegistry()
        {
            registry ??= Resources.Load<SandboxHudArtRegistry>("Sandsunder/UI/SandboxHudArtRegistry");
            return registry;
        }

        private static Sprite FindRuntimeNomadSprite()
        {
            TopDownPlayerController player = UnityEngine.Object.FindFirstObjectByType<TopDownPlayerController>();
            if (player == null) return null;

            SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer.sprite != null && renderer.sprite.name.IndexOf("nomad", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return renderer.sprite;
                }
            }

            return null;
        }

        private static Sprite CreateGlassPanelSprite()
        {
            const int size = 16;
            Texture2D texture = NewTexture(size, size, "ui_glass_panel_runtime_fallback");
            Color32 glass = new(17, 20, 25, 236);
            Color32 gold = new(214, 179, 54, 255);
            Color32 shine = new(72, 112, 121, 90);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                    texture.SetPixel(x, y, edge ? gold : (x - y > 7 ? shine : glass));
                }
            }
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 16f, 0, SpriteMeshType.FullRect,
                new Vector4(2, 2, 2, 2));
        }

        private static Sprite CreateNomadSprite()
        {
            Texture2D texture = NewTexture(16, 24, "nomad_32_runtime_paper_doll");
            Color32 hood = new(236, 232, 214, 255);
            Color32 coat = new(52, 102, 184, 255);
            Color32 scarf = new(38, 184, 198, 255);
            Color32 boots = new(58, 43, 34, 255);
            PaintRect(texture, 5, 17, 6, 6, hood);
            PaintRect(texture, 4, 9, 8, 9, coat);
            PaintRect(texture, 4, 15, 8, 2, scarf);
            PaintRect(texture, 4, 2, 3, 7, boots);
            PaintRect(texture, 9, 2, 3, 7, boots);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, 16, 24), new Vector2(.5f, .5f), 16f);
        }

        private static Sprite CreateItemSprite(string itemId)
        {
            Texture2D texture = NewTexture(16, 16, $"hud_{itemId}_fallback");
            Color32 gold = new(228, 185, 70, 255);
            Color32 cyan = new(0, 240, 230, 255);
            Color32 steel = new(201, 210, 218, 255);
            Color32 wood = new(126, 79, 43, 255);
            Color32 red = new(211, 58, 48, 255);

            if (itemId.Contains("shovel"))
            {
                PaintRect(texture, 7, 4, 2, 10, wood);
                PaintRect(texture, 5, 2, 6, 4, steel);
            }
            else if (itemId.Contains("rifle") || itemId.Contains("shotgun"))
            {
                PaintRect(texture, 2, 7, 12, 3, gold);
                PaintRect(texture, 2, 5, 5, 2, wood);
            }
            else if (itemId.Contains("sword"))
            {
                for (int i = 3; i < 14; i++) texture.SetPixel(i, i, steel);
                PaintRect(texture, 2, 2, 5, 2, gold);
            }
            else if (itemId.Contains("heal"))
            {
                PaintRect(texture, 3, 3, 10, 10, new Color32(238, 232, 218, 255));
                PaintRect(texture, 7, 5, 2, 6, red);
                PaintRect(texture, 5, 7, 6, 2, red);
            }
            else
            {
                PaintRect(texture, 4, 4, 8, 8, itemId == "empty" ? new Color32(0, 0, 0, 0) : cyan);
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 16f);
        }

        private static Texture2D NewTexture(int width, int height, string name)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] clear = new Color32[width * height];
            texture.SetPixels32(clear);
            return texture;
        }

        private static void PaintRect(Texture2D texture, int x, int y, int width, int height, Color32 color)
        {
            for (int iy = y; iy < y + height; iy++)
            {
                for (int ix = x; ix < x + width; ix++) texture.SetPixel(ix, iy, color);
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class ResponsiveInventoryLayout : MonoBehaviour
    {
        private RectTransform paperDoll;
        private RectTransform inventory;
        private RectTransform stats;
        private float lastWidth = -1f;

        public void Configure(RectTransform paperDollPanel, RectTransform inventoryPanel, RectTransform statPanel)
        {
            paperDoll = paperDollPanel;
            inventory = inventoryPanel;
            stats = statPanel;
            ApplyForWidth(Screen.width);
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyForWidth(Screen.width);
        }

        private void Update()
        {
            if (!Mathf.Approximately(lastWidth, Screen.width)) ApplyForWidth(Screen.width);
        }

        public void ApplyForWidth(float width)
        {
            if (paperDoll == null || inventory == null || stats == null) return;
            lastWidth = width;
            bool compact = width < 800f;
            paperDoll.gameObject.SetActive(!compact);

            if (compact)
            {
                SetAnchors(inventory, new Vector2(0f, .43f), Vector2.one, new Vector2(12, 12), new Vector2(-12, -54));
                SetAnchors(stats, Vector2.zero, new Vector2(1f, .43f), new Vector2(12, 12), new Vector2(-12, -6));
            }
            else
            {
                SetAnchors(paperDoll, Vector2.zero, new Vector2(.24f, 1f), new Vector2(12, 12), new Vector2(-6, -54));
                SetAnchors(inventory, new Vector2(.24f, 0f), new Vector2(.70f, 1f), new Vector2(6, 12), new Vector2(-6, -54));
                SetAnchors(stats, new Vector2(.70f, 0f), Vector2.one, new Vector2(6, 12), new Vector2(-12, -54));
            }
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
