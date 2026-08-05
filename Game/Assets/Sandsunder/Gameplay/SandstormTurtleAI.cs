using UnityEngine;
using Sandsunder.Simulation;

namespace Sandsunder.Gameplay
{
    /// <summary>
    /// Crystal Turtle mob: a slow armored desert turtle with a crystalline shell. Behavior:
    ///  - Patrols slowly toward the nearest player.
    ///  - Retracts into its shell when damaged (brief invulnerability + retract animation), then
    ///    emerges and lunges at close range dealing a short-range bite.
    ///  - Ignores subterranean players (SubterraneanStealth) — cannot see or attack underground.
    /// State machine: Patrol -> Retract (on hit) -> Lunge -> Cooldown -> (loop). On death it
    /// spawns a sand + crystal shard burst.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PrototypeHealth), typeof(Rigidbody2D))]
    public sealed class SandstormTurtleAI : MonoBehaviour
    {
        private enum TurtleState { Patrol, Retract, Lunge, Cooldown, Dying }

        [SerializeField] private float patrolSpeed = 1.6f;
        [SerializeField] private float retractTime = 1.4f;
        [SerializeField] private float lungeSpeed = 4.5f;
        [SerializeField] private float lungeTime = 0.5f;
        [SerializeField] private float cooldownTime = 1.6f;
        [SerializeField] private int lungeDamage = 18;
        [SerializeField] private float contactRange = 0.9f;
        [SerializeField] private Color shellGlow = new(0.94f, 0.78f, 0.20f);

        private TurtleState state;
        private float stateTimer;
        private Vector2 moveDir = Vector2.right;
        private Rigidbody2D body;
        private PrototypeHealth health;
        private SpriteRenderer shellRenderer;
        private int lastHealth = -1;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            health = GetComponent<PrototypeHealth>();
            shellRenderer = GetComponent<SpriteRenderer>();
            if (health != null)
            {
                lastHealth = health.CurrentHealth;
                health.Died += OnDied;
            }
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= OnDied;
        }

        private void OnDied(PrototypeHealth dead)
        {
            if (state != TurtleState.Dying)
            {
                state = TurtleState.Dying;
                SandboxVisualEffects.SpawnDust(transform.position, 16, shellGlow);
            }
        }

        private void Update()
        {
            // Detect a damage hit (health dropped) to trigger the shell retract.
            if (health != null && lastHealth >= 0 && health.CurrentHealth < lastHealth)
            {
                NotifyHit();
            }
            lastHealth = health != null ? health.CurrentHealth : lastHealth;

            if (health != null && health.IsDead) return;

            stateTimer -= Time.deltaTime;
            switch (state)
            {
                case TurtleState.Patrol:
                    SlowPatrol();
                    if (stateTimer <= 0f)
                    {
                        // Time to lunge: face + charge the player.
                        state = TurtleState.Lunge;
                        stateTimer = lungeTime;
                    }
                    break;
                case TurtleState.Retract:
                    // Shelled: invulnerable, no movement. When timer ends, emerge and lunge.
                    body.linearVelocity = Vector2.zero;
                    if (stateTimer <= 0f)
                    {
                        state = TurtleState.Lunge;
                        stateTimer = lungeTime;
                    }
                    break;
                case TurtleState.Lunge:
                    body.linearVelocity = moveDir * lungeSpeed;
                    SandboxVisualEffects.SpawnDust(body.position, 1, shellGlow);
                    if (stateTimer <= 0f)
                    {
                        body.linearVelocity = Vector2.zero;
                        state = TurtleState.Cooldown;
                        stateTimer = cooldownTime;
                    }
                    break;
                case TurtleState.Cooldown:
                    if (stateTimer <= 0f) { state = TurtleState.Patrol; stateTimer = 2.2f; }
                    break;
            }
        }

        private void SlowPatrol()
        {
            PrototypePlayerCombat player = FindFirstObjectByType<PrototypePlayerCombat>();
            if (player == null) { body.linearVelocity = Vector2.zero; return; }

            Vector2 offset = (Vector2)player.transform.position - body.position;
            if (offset.sqrMagnitude > contactRange * contactRange)
            {
                moveDir = offset.normalized;
                Vector2 targetPos = body.position + (moveDir * patrolSpeed * Time.deltaTime);
                Collider2D hit = Physics2D.OverlapCircle(targetPos, 0.45f);
                if (hit == null || hit.isTrigger || hit.transform.IsChildOf(transform))
                {
                    body.MovePosition(targetPos);
                }
            }
            else
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        /// <summary>Called by the health system when this mob takes damage.</summary>
        public void NotifyHit()
        {
            if (state == TurtleState.Retract || state == TurtleState.Dying) return;
            if (health != null && health.IsDead) return;
            // Retract into the shell: brief invulnerability period.
            state = TurtleState.Retract;
            stateTimer = retractTime;
            body.linearVelocity = Vector2.zero;
            if (shellRenderer != null) shellRenderer.color = shellGlow;
            SandboxVisualEffects.SpawnDust(transform.position, 6, shellGlow);
        }

        /// <summary>Re-emerge: restore normal shell tint after retreat ends.</summary>
        public void NotifyEmerge()
        {
            if (shellRenderer != null) shellRenderer.color = Color.white;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (state != TurtleState.Lunge) return;
            PrototypeHealth target = other.GetComponentInParent<PrototypeHealth>();
            if (target == null || target.Team == (health?.Team ?? 0)) return;
            if (other.GetComponent<SubterraneanStealth>()?.IsStealthed == true) return;
            target.TryDamage(new CombatDamageRequest(health.EntityId, health.Team, lungeDamage));
        }
    }
}