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
        private int speedHash;
        private int movingHash;
        private int rollingHash;
        private int diggingHash;
        private int stealthedHash;
        private int attackHash;
        private int shootHash;
        private int hurtHash;
        private int deathHash;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            speedHash = Animator.StringToHash("Speed");
            movingHash = Animator.StringToHash("IsMoving");
            rollingHash = Animator.StringToHash("IsRolling");
            diggingHash = Animator.StringToHash("IsDigging");
            stealthedHash = Animator.StringToHash("IsStealthed");
            attackHash = Animator.StringToHash("Attack");
            shootHash = Animator.StringToHash("Shoot");
            hurtHash = Animator.StringToHash("Hurt");
            deathHash = Animator.StringToHash("Death");
        }

        public void SetMoving(float speed)
        {
            if (!IsControllerReady) return;
            SetFloatIfPresent(speedHash, speed);
            SetBoolIfPresent(movingHash, speed > 0.01f);
        }

        public void SetRolling(bool active)
        {
            if (!IsControllerReady) return;
            SetBoolIfPresent(rollingHash, active);
        }

        public void SetDigging(bool active)
        {
            if (!IsControllerReady) return;
            SetBoolIfPresent(diggingHash, active);
        }

        public void SetStealthed(bool active)
        {
            if (!IsControllerReady) return;
            SetBoolIfPresent(stealthedHash, active);
        }

        public void PlayMelee() => SetTriggerIfPresent(attackHash);
        public void PlayShoot() => SetTriggerIfPresent(shootHash);
        public void PlayHurt() => SetTriggerIfPresent(hurtHash);
        public void PlayDeath() => SetTriggerIfPresent(deathHash);

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

        private void SetBoolIfPresent(int hash, bool value)
        {
            if (IsControllerReady && HasParameter(hash)) animator.SetBool(hash, value);
        }

        private void SetFloatIfPresent(int hash, float value)
        {
            if (IsControllerReady && HasParameter(hash)) animator.SetFloat(hash, value);
        }

        private void SetTriggerIfPresent(int hash)
        {
            if (IsControllerReady && HasParameter(hash)) animator.SetTrigger(hash);
        }

        private bool HasParameter(int hash)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash == hash) return true;
            }
            return false;
        }
    }
}
