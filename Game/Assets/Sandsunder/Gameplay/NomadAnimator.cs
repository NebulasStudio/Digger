using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Drives the Nomad AnimatorController from gameplay state. The controller already exposes
    /// Idle/Walk/Run/Roll/Dig (see Art/Generated/NomadAnimatorController.controller). This component
    /// maps movement, rolling, digging and subterranean stealth onto its parameters, and can be
    /// extended to the StealthCrouch / DigChannel / DeathBurst states.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class NomadAnimator : MonoBehaviour
    {
        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void SetMoving(float speed)
        {
            if (!IsControllerReady) return;
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsMoving", speed > 0.01f);
        }

        public void SetRolling(bool active)
        {
            if (!IsControllerReady) return;
            animator.SetBool("IsRolling", active);
        }

        public void SetDigging(bool active)
        {
            if (!IsControllerReady) return;
            animator.SetBool("IsDigging", active);
        }

        public void SetStealthed(bool active)
        {
            if (!IsControllerReady) return;
            // "IsStealthed" gates the StealthCrouch state (added in the SpriteSheetImporter pass).
            if (HasParameter("IsStealthed"))
            {
                animator.SetBool("IsStealthed", active);
            }
        }

        /// <summary>
        /// True when the Animator drives a real controller. Avoids the "Animator is not playing an
        /// AnimatorController" warning that fires when SetFloat/SetBool/parameters are called on an
        /// Animator with no controller assigned (e.g. hostile actors that skip the Nomad controller).
        /// </summary>
        private bool IsControllerReady
        {
            get
            {
                if (animator == null) animator = GetComponent<Animator>();
                return animator != null && animator.runtimeAnimatorController != null;
            }
        }

        private bool HasParameter(string name)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == name) return true;
            }
            return false;
        }
    }
}