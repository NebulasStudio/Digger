using UnityEngine;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Presentation-only driver for the Dune Spitter controller. It owns only Spitter parameters,
    /// preventing Nomad state code from sending invalid parameters to the hostile controller.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class SpitterAnimator : MonoBehaviour
    {
        private static readonly int IsCharging = Animator.StringToHash("IsCharging");
        private static readonly int Death = Animator.StringToHash("Death");

        [SerializeField] private float automaticChargeDuration = 0.24f;

        private Animator animator;
        private PrototypeHealth health;
        private float chargeRemaining;
        private bool deathPlayed;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= OnDied;
        }

        private void Update()
        {
            if (chargeRemaining <= 0f) return;

            chargeRemaining = Mathf.Max(0f, chargeRemaining - Time.deltaTime);
            if (chargeRemaining <= 0f)
            {
                SetCharging(false);
            }
        }

        public void Configure(PrototypeHealth configuredHealth)
        {
            if (health == configuredHealth) return;
            if (health != null) health.Died -= OnDied;
            health = configuredHealth;
            if (health != null) health.Died += OnDied;
        }

        public void PlayCharge()
        {
            chargeRemaining = Mathf.Max(automaticChargeDuration, 0.01f);
            SetCharging(true);
        }

        public void SetCharging(bool active)
        {
            if (!IsControllerReady || !HasParameter(IsCharging)) return;
            if (!active) chargeRemaining = 0f;
            animator.SetBool(IsCharging, active);
        }

        public void PlayDeath()
        {
            if (deathPlayed || !IsControllerReady || !HasParameter(Death)) return;
            deathPlayed = true;
            chargeRemaining = 0f;
            if (HasParameter(IsCharging)) animator.SetBool(IsCharging, false);
            // PrototypeHealth disables every child renderer before raising Died. Keep just the
            // animated body alive long enough for the non-authoritative death presentation.
            SpriteRenderer bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) bodyRenderer.enabled = true;
            animator.SetTrigger(Death);
        }

        private void OnDied(PrototypeHealth _) => PlayDeath();

        private bool IsControllerReady => animator != null && animator.runtimeAnimatorController != null;

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
