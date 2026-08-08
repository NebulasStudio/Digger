using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Sandsunder.Gameplay;
using Sandsunder.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sandsunder.Tests.Gameplay
{
    public sealed class CombatPrototypePlayModeTests
    {
        [Test]
        public void HealthAdapter_AdvancesSixtySimulationTicksPerSecondAtDefaultPhysicsRate()
        {
            PrototypeHealth health = CreateHealth("Tick Rate Test", 90, team: 0);
            MethodInfo fixedUpdate = typeof(PrototypeHealth).GetMethod(
                "FixedUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fixedUpdate, Is.Not.Null);

            float previousFixedDeltaTime = Time.fixedDeltaTime;
            try
            {
                Time.fixedDeltaTime = 0.02f;
                for (int step = 0; step < 50; step++)
                {
                    fixedUpdate.Invoke(health, null);
                }

                Assert.That(health.State.Tick, Is.EqualTo(60));
            }
            finally
            {
                Time.fixedDeltaTime = previousFixedDeltaTime;
                Object.DestroyImmediate(health.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Projectile_RejectsOwnerThenDamagesOnlyOneTarget()
        {
            PrototypeHealth owner = CreateHealth("Owner", 1, team: 0);
            PrototypeHealth firstTarget = CreateHealth("First Target", 2, team: 1);
            PrototypeHealth secondTarget = CreateHealth("Second Target", 3, team: 1);
            PrototypeProjectile projectile = PrototypeProjectile.Spawn(
                Vector2.zero,
                Vector2.right,
                projectileId: 40,
                owner.EntityId,
                owner.Team,
                damage: 6,
                speed: 24f,
                range: 11f,
                telegraphSeconds: 0f,
                Color.white);

            Assert.That(projectile.ResolveHit(owner), Is.EqualTo(CombatDamageResult.RejectedOwner));
            Assert.That(projectile.ResolveHit(firstTarget), Is.EqualTo(CombatDamageResult.Applied));
            Assert.That(projectile.ResolveHit(secondTarget), Is.EqualTo(CombatDamageResult.RejectedConsumed));
            Assert.That(firstTarget.CurrentHealth, Is.EqualTo(94));
            Assert.That(secondTarget.CurrentHealth, Is.EqualTo(100));

            Object.Destroy(owner.gameObject);
            Object.Destroy(firstTarget.gameObject);
            Object.Destroy(secondTarget.gameObject);
            Object.Destroy(projectile.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pickup_ExplicitInteractionAppliesExactlyOnceWithoutInventoryObject()
        {
            GameObject playerObject = new("Player Pickup Test");
            playerObject.SetActive(false);
            playerObject.AddComponent<Rigidbody2D>();
            playerObject.AddComponent<CircleCollider2D>();
            playerObject.AddComponent<TopDownPlayerController>();
            PrototypeHealth health = playerObject.AddComponent<PrototypeHealth>();
            PrototypePlayerCombat player = playerObject.AddComponent<PrototypePlayerCombat>();
            health.Configure(1, configuredTeam: 0, configuredMaximumHealth: 100, shouldRespawn: true);
            player.Configure(1);
            playerObject.SetActive(true);

            PrototypePickup pickup = PrototypePickup.Spawn(Vector2.zero, 50, PrototypeDigGridAuthority.CounterLootId);
            Assert.That(pickup.TryCollect(player), Is.True);
            Assert.That(pickup.TryCollect(player), Is.False);
            Assert.That(player.PickupCount, Is.EqualTo(1));

            Object.Destroy(playerObject);
            Object.Destroy(pickup.gameObject);
            yield return null;
        }

        private static PrototypeHealth CreateHealth(string name, int entityId, int team)
        {
            GameObject target = new(name);
            target.SetActive(false);
            target.AddComponent<CircleCollider2D>();
            PrototypeHealth health = target.AddComponent<PrototypeHealth>();
            health.Configure(entityId, team, configuredMaximumHealth: 100, shouldRespawn: false);
            target.SetActive(true);
            return health;
        }
    }
}
