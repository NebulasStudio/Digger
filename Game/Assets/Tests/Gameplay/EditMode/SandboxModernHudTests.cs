using System;
using System.Linq;
using NUnit.Framework;
using Sandsunder.Gameplay;
using Sandsunder.Gameplay.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Sandsunder.Tests.Gameplay
{
    public sealed class SandboxModernHudTests
    {
        private float originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            DestroyAll<SandboxModernHUD>();
            DestroyAll<PrototypeInventoryHUD>();
            DestroyAll<SandboxInventoryWindow>();
            DestroyAll<EventSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = originalTimeScale;
            DestroyAll<SandboxModernHUD>();
            DestroyAll<PrototypeInventoryHUD>();
            DestroyAll<SandboxInventoryWindow>();
            DestroyAll<EventSystem>();
        }

        [Test]
        public void Awake_BuildsOneResponsiveControllerFirstHud()
        {
            SandboxModernHUD hud = new GameObject("HUD Test").AddComponent<SandboxModernHUD>();
            hud.EnsureInitialized();

            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(canvas => canvas.name == "SandsunderHUD_Canvas")
                .ToArray();

            Assert.That(canvases, Has.Length.EqualTo(1));
            Assert.That(hud.HudCanvas, Is.SameAs(canvases[0]));
            Assert.That(hud.HudCanvas.GetComponent<CanvasScaler>().uiScaleMode,
                Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(hud.HotbarSlotCount, Is.EqualTo(5));
            Assert.That(hud.InventorySlotCount, Is.EqualTo(15));
            Assert.That(hud.PaperDollSprite, Is.Not.Null);
            Assert.That(hud.InventoryRoot.activeSelf, Is.False);

            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(eventSystem.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
            Assert.That(eventSystem.sendNavigationEvents, Is.True);

            string[] forbiddenLegacyCanvases =
            {
                "SandboxInventory_Canvas", "InventoryHUD_Canvas", "StatusHUD_Canvas", "TabInventory_Canvas"
            };
            Assert.That(UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Any(canvas => forbiddenLegacyCanvases.Contains(canvas.name)), Is.False);
        }

        [Test]
        public void InventoryOpen_DoesNotMutateSimulationTime_AndSelectsCurrentItem()
        {
            SandboxModernHUD hud = new GameObject("HUD Test").AddComponent<SandboxModernHUD>();
            hud.EnsureInitialized();
            Time.timeScale = .65f;

            hud.InventoryController.SetOpen(true);

            Assert.That(hud.InventoryController.IsOpen, Is.True);
            Assert.That(hud.InventoryRoot.activeSelf, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(.65f));
            Assert.That(hud.InspectedItemName, Is.EqualTo("STEEL SHOVEL"));

            hud.InventoryController.SetOpen(false);
            Assert.That(Time.timeScale, Is.EqualTo(.65f));
        }

        [Test]
        public void OxygenProvider_EventRefreshesCyanBarProjection()
        {
            SandboxModernHUD hud = new GameObject("HUD Test").AddComponent<SandboxModernHUD>();
            hud.EnsureInitialized();
            TestOxygenProvider provider = new GameObject("Oxygen Provider").AddComponent<TestOxygenProvider>();

            hud.BindOxygenProvider(provider);
            Assert.That(hud.IsOxygenVisible, Is.False);

            provider.IsSubterranean = true;
            provider.Publish(25f, 100f);
            hud.BindOxygenProvider(null);
            hud.BindOxygenProvider(provider);

            Assert.That(hud.DisplayedOxygenRatio, Is.EqualTo(.25f).Within(.0001f));
            Assert.That(hud.IsOxygenVisible, Is.True);
            UnityEngine.Object.DestroyImmediate(provider.gameObject);
        }

        [Test]
        public void ResponsiveLayout_StacksContentAtNarrowWidths()
        {
            GameObject root = new("Responsive Test", typeof(RectTransform));
            RectTransform paper = NewRect(root.transform, "Paper");
            RectTransform inventory = NewRect(root.transform, "Inventory");
            RectTransform stats = NewRect(root.transform, "Stats");
            ResponsiveInventoryLayout layout = root.AddComponent<ResponsiveInventoryLayout>();
            layout.Configure(paper, inventory, stats);

            layout.ApplyForWidth(320f);

            Assert.That(paper.gameObject.activeSelf, Is.False);
            Assert.That(inventory.anchorMin.y, Is.EqualTo(.43f).Within(.001f));
            Assert.That(stats.anchorMax.y, Is.EqualTo(.43f).Within(.001f));
            UnityEngine.Object.DestroyImmediate(root);
        }

        [Test]
        public void HudRegistry_ReferencesCanonicalReviewedRuntimeArt()
        {
            SandboxHudArtRegistry registry = AssetDatabase.LoadAssetAtPath<SandboxHudArtRegistry>(
                "Assets/Resources/Sandsunder/UI/SandboxHudArtRegistry.asset");

            Assert.That(registry, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(registry.GlassPanel),
                Is.EqualTo("Assets/Sandsunder/Art/Runtime/UI/ui_glass_panel.png"));
            Assert.That(AssetDatabase.GetAssetPath(registry.Nomad),
                Is.EqualTo("Assets/Sandsunder/Art/Runtime/Characters/nomad_32.png"));
            Assert.That(AssetDatabase.GetAssetPath(registry.Shovel),
                Is.EqualTo("Assets/Sandsunder/Art/Runtime/Weapons/shovel_default_32.png"));
            Assert.That(AssetDatabase.GetAssetPath(registry.Rifle),
                Is.EqualTo("Assets/Sandsunder/Art/Runtime/Weapons/rifle_brass_32.png"));
            Assert.That(AssetDatabase.GetAssetPath(registry.Scimitar),
                Is.EqualTo("Assets/Sandsunder/Art/Runtime/Weapons/sword_scimitar_32.png"));
        }

        private static RectTransform NewRect(Transform parent, string name)
        {
            GameObject child = new(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static void DestroyAll<T>() where T : UnityEngine.Object
        {
            foreach (T item in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (item is Component component) UnityEngine.Object.DestroyImmediate(component.gameObject);
                else UnityEngine.Object.DestroyImmediate(item);
            }
        }
    }

    public sealed class TestOxygenProvider : MonoBehaviour, IPlayerOxygenProvider
    {
        public float CurrentOxygen { get; private set; } = 100f;
        public float MaximumOxygen { get; private set; } = 100f;
        public bool IsSubterranean { get; set; }
        public event Action<float, float> OxygenChanged;

        public void Publish(float current, float maximum)
        {
            CurrentOxygen = current;
            MaximumOxygen = maximum;
            OxygenChanged?.Invoke(current, maximum);
        }
    }
}
