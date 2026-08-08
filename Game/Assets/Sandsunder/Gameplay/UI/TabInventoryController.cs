using System;
using Sandsunder.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Sandsunder.Gameplay.UI
{
    /// <summary>Controller-first inventory modal. Opening it never changes simulation time.</summary>
    [DisallowMultipleComponent]
    public sealed class TabInventoryController : MonoBehaviour
    {
        [SerializeField] private GameObject inventoryRoot;
        [SerializeField] private Selectable initialSelection;

        private InputAction toggleAction;
        private InputAction closeAction;
        private Action refresh;

        public bool IsOpen { get; private set; }
        public event Action<bool> OpenChanged;

        public void Setup(GameObject root, Selectable firstSelection, Action refreshContent)
        {
            inventoryRoot = root;
            initialSelection = firstSelection;
            refresh = refreshContent;
            SetOpen(false);
        }

        private void OnEnable()
        {
            EnsureActions();
            toggleAction.performed += OnTogglePerformed;
            closeAction.performed += OnClosePerformed;
            toggleAction.Enable();
            closeAction.Enable();
        }

        private void OnDisable()
        {
            if (toggleAction != null)
            {
                toggleAction.performed -= OnTogglePerformed;
                toggleAction.Disable();
            }

            if (closeAction != null)
            {
                closeAction.performed -= OnClosePerformed;
                closeAction.Disable();
            }
        }

        private void OnDestroy()
        {
            toggleAction?.Dispose();
            closeAction?.Dispose();
        }

        public void Toggle()
        {
            SetOpen(!IsOpen);
        }

        public void SetOpen(bool open)
        {
            if (open && SandboxShopPanel.Instance != null && SandboxShopPanel.Instance.IsOpen)
            {
                SandboxShopPanel.Instance.SetOpen(false);
            }

            IsOpen = open;
            if (inventoryRoot != null) inventoryRoot.SetActive(open);

            if (open)
            {
                refresh?.Invoke();
                if (EventSystem.current != null && initialSelection != null)
                {
                    EventSystem.current.SetSelectedGameObject(initialSelection.gameObject);
                }
            }

            OpenChanged?.Invoke(open);
        }

        private void EnsureActions()
        {
            if (toggleAction != null) return;

            toggleAction = new InputAction("Inventory", InputActionType.Button);
            toggleAction.AddBinding("<Keyboard>/tab");
            toggleAction.AddBinding("<Gamepad>/start");

            closeAction = new InputAction("Close Inventory", InputActionType.Button);
            closeAction.AddBinding("<Keyboard>/escape");
            closeAction.AddBinding("<Gamepad>/buttonEast");
        }

        private void OnTogglePerformed(InputAction.CallbackContext context)
        {
            Toggle();
        }

        private void OnClosePerformed(InputAction.CallbackContext context)
        {
            if (IsOpen) SetOpen(false);
        }
    }
}
