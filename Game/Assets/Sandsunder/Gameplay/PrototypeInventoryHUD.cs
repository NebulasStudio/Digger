using System;
using System.Collections.Generic;
using Sandsunder.Gameplay.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Sandbox inventory state and selection adapter. Rendering is owned exclusively by
    /// <see cref="SandboxModernHUD"/>, which keeps the scene to one HUD Canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrototypeInventoryHUD : MonoBehaviour
    {
        public const int HotbarCapacity = 5;
        public const int BackpackCapacity = 15;
        public const int TotalCapacity = HotbarCapacity + BackpackCapacity;

        private static PrototypeInventoryHUD instance;
        private readonly List<string> inventoryItems = new(TotalCapacity)
        {
            "shovel.default"
        };

        private int selectedIndex;

        public static PrototypeInventoryHUD Instance => instance;
        public IReadOnlyList<string> InventoryItems => inventoryItems;
        public int SelectedIndex => selectedIndex;
        public event Action InventoryChanged;
        public event Action<int> SelectionChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (instance != null || FindFirstObjectByType<PrototypeInventoryHUD>() != null) return;
            new GameObject("PrototypeInventoryModel_Auto").AddComponent<PrototypeInventoryHUD>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > .05f) SetSelectedSlot((selectedIndex - 1 + HotbarCapacity) % HotbarCapacity);
                else if (scroll < -.05f) SetSelectedSlot((selectedIndex + 1) % HotbarCapacity);
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) SetSelectedSlot(0);
                else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) SetSelectedSlot(1);
                else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) SetSelectedSlot(2);
                else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) SetSelectedSlot(3);
                else if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) SetSelectedSlot(4);
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.leftShoulder.wasPressedThisFrame)
                    SetSelectedSlot((selectedIndex - 1 + HotbarCapacity) % HotbarCapacity);
                else if (gamepad.rightShoulder.wasPressedThisFrame)
                    SetSelectedSlot((selectedIndex + 1) % HotbarCapacity);
            }
        }

        public void AddItem(string itemId)
        {
            TryAddItem(itemId, allowDuplicate: false);
        }

        public bool TryAddItem(string itemId, bool allowDuplicate)
        {
            if (string.IsNullOrWhiteSpace(itemId)
                || (!allowDuplicate && inventoryItems.Contains(itemId))
                || inventoryItems.Count >= TotalCapacity)
            {
                return false;
            }

            inventoryItems.Add(itemId);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool TryRemoveAt(int index, string expectedItemId)
        {
            if (index < 0 || index >= inventoryItems.Count || inventoryItems[index] != expectedItemId) return false;
            inventoryItems.RemoveAt(index);
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, HotbarCapacity - 1));
            InventoryChanged?.Invoke();
            SelectionChanged?.Invoke(selectedIndex);
            return true;
        }

        public bool HasItem(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && inventoryItems.Contains(itemId);
        }

        public string GetItemAt(int index)
        {
            return index >= 0 && index < inventoryItems.Count ? inventoryItems[index] : string.Empty;
        }

        public Sprite GetItemSprite(string itemId)
        {
            return SandboxHudSpriteLibrary.GetItemSprite(itemId);
        }

        public void SetSelectedSlot(int index)
        {
            if (index < 0 || index >= HotbarCapacity || index == selectedIndex) return;
            selectedIndex = index;
            SelectionChanged?.Invoke(index);
        }
    }
}
