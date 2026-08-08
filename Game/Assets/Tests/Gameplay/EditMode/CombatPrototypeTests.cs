using NUnit.Framework;
using Sandsunder.Domain;
using Sandsunder.Simulation;

namespace Sandsunder.Tests.Gameplay
{
    public sealed class CombatPrototypeTests
    {
        [Test]
        public void PrototypeRules_AreVersionedAndMatchApprovedSliceValues()
        {
            CombatRules rules = CombatRules.PrototypeOne;

            Assert.That(rules.SchemaVersion, Is.EqualTo(1));
            Assert.That(rules.Version, Is.EqualTo("combat-prototype-1"));
            Assert.That(rules.PistolDamage, Is.EqualTo(6));
            Assert.That(rules.PistolCooldownTicks, Is.EqualTo(12));
            Assert.That(rules.PistolProjectileSpeedMillimetresPerSecond, Is.EqualTo(24000));
            Assert.That(rules.PistolRangeMillimetres, Is.EqualTo(11000));
            Assert.That(rules.RollDistanceMillimetres, Is.EqualTo(1200));
            Assert.That(rules.RollDurationTicks, Is.EqualTo(18));
            Assert.That(rules.RollInvulnerabilityTicks, Is.EqualTo(12));
            Assert.That(rules.RollCooldownTicks, Is.EqualTo(75));
            Assert.That(rules.ShovelArcCosinePermille,
                Is.EqualTo(SandboxGameplayCatalog.NinetyDegreeArcCosinePermille));
        }

        [Test]
        public void SandboxCatalog_IsVersionedAndMatchesFocusedLoadout()
        {
            SandboxGameplayCatalog catalog = SandboxGameplayCatalog.MilestoneOne;

            Assert.That(catalog.SchemaVersion, Is.EqualTo(1));
            Assert.That(catalog.Version, Is.EqualTo("sandbox-gameplay-1"));
            Assert.That(catalog.Shovel.Damage, Is.EqualTo(12));
            Assert.That(catalog.Shovel.CooldownTicks, Is.EqualTo(33));
            Assert.That(catalog.Shovel.ReachMillimetres, Is.EqualTo(1400));
            Assert.That(catalog.Scimitar.Damage, Is.EqualTo(18));
            Assert.That(catalog.Scimitar.CooldownTicks, Is.EqualTo(24));
            Assert.That(catalog.Scimitar.ReachMillimetres, Is.EqualTo(1600));
            Assert.That(catalog.Scimitar.ArcCosinePermille, Is.EqualTo(707));
            Assert.That(catalog.Rifle.Damage, Is.EqualTo(6));
            Assert.That(catalog.Rifle.CooldownTicks, Is.EqualTo(12));
            Assert.That(catalog.Rifle.ProjectileSpeedMillimetresPerSecond, Is.EqualTo(24000));
            Assert.That(catalog.Rifle.ProjectileRangeMillimetres, Is.EqualTo(11000));
        }

        [Test]
        public void RifleMagazine_IsTickDeterministicAndBlocksShotsDuringReload()
        {
            SandboxRifleMagazine magazine = new();

            for (int shot = 0; shot < SandboxRifleMagazine.DefaultCapacity; shot++)
            {
                Assert.That(magazine.TryConsumeShot(), Is.True);
            }

            Assert.That(magazine.Ammunition, Is.Zero);
            Assert.That(magazine.IsReloading, Is.True);
            Assert.That(magazine.TryConsumeShot(), Is.False);

            magazine.AdvanceTicks(SandboxRifleMagazine.DefaultReloadTicks - 1);
            Assert.That(magazine.CanFire, Is.False);
            magazine.AdvanceTicks(1);

            Assert.That(magazine.Ammunition, Is.EqualTo(SandboxRifleMagazine.DefaultCapacity));
            Assert.That(magazine.IsReloading, Is.False);
            Assert.That(magazine.TryConsumeShot(), Is.True);
        }

        [Test]
        public void ScimitarArc_IsExactlyNinetyDegreesAndUsesIndependentCooldown()
        {
            SandboxWeaponDefinition scimitar = SandboxGameplayCatalog.MilestoneOne.Scimitar;
            CombatantState player = new(1, team: 0, maximumHealth: 100, CombatRules.PrototypeOne);

            Assert.That(CombatMath.IsInsideArc(
                0, 0, 1000, 0, 1000, 999,
                scimitar.ReachMillimetres, scimitar.ArcCosinePermille), Is.True);
            Assert.That(CombatMath.IsInsideArc(
                0, 0, 1000, 0, 1000, 1001,
                scimitar.ReachMillimetres, scimitar.ArcCosinePermille), Is.False);

            Assert.That(player.TryUse(CombatAction.Scimitar), Is.True);
            player.AdvanceTo(scimitar.CooldownTicks - 1);
            Assert.That(player.TryUse(CombatAction.Scimitar), Is.False);
            player.AdvanceTo(scimitar.CooldownTicks);
            Assert.That(player.TryUse(CombatAction.Scimitar), Is.True);
        }

        [Test]
        public void ActionCooldown_RejectsUntilExactAuthoritativeTick()
        {
            CombatRules rules = CombatRules.PrototypeOne;
            CombatantState player = new(1, team: 0, rules.PlayerMaximumHealth, rules);

            Assert.That(player.TryUse(CombatAction.Pistol), Is.True);
            Assert.That(player.TryUse(CombatAction.Pistol), Is.False);
            player.AdvanceTo(rules.PistolCooldownTicks - 1);
            Assert.That(player.TryUse(CombatAction.Pistol), Is.False);
            player.AdvanceTo(rules.PistolCooldownTicks);
            Assert.That(player.TryUse(CombatAction.Pistol), Is.True);
        }

        [Test]
        public void Roll_GrantsFiniteIFrameAndRespectsCooldown()
        {
            CombatRules rules = CombatRules.PrototypeOne;
            CombatantState player = new(1, team: 0, rules.PlayerMaximumHealth, rules);
            CombatProjectileState projectile = new(7, ownerEntityId: 100, ownerTeam: 1, damage: 12, 7000, 7000);

            Assert.That(player.TryUse(CombatAction.Roll), Is.True);
            Assert.That(player.IsRolling, Is.True);
            Assert.That(player.IsInvulnerable, Is.True);
            Assert.That(player.TryUse(CombatAction.Pistol), Is.False);
            Assert.That(player.TryUse(CombatAction.Shovel), Is.False);
            Assert.That(projectile.TryHit(player), Is.EqualTo(CombatDamageResult.RejectedInvulnerable));
            Assert.That(player.Health, Is.EqualTo(100));

            player.AdvanceTo(rules.RollInvulnerabilityTicks);
            Assert.That(player.IsInvulnerable, Is.False);
            Assert.That(player.IsRolling, Is.True);
            Assert.That(player.TryUse(CombatAction.Roll), Is.False);
            player.AdvanceTo(rules.RollCooldownTicks);
            Assert.That(player.TryUse(CombatAction.Roll), Is.True);
        }

        [Test]
        public void ShovelArc_RequiresReachAndFacing()
        {
            CombatRules rules = CombatRules.PrototypeOne;

            Assert.That(CombatMath.IsInsideArc(0, 0, 1000, 0, 1200, 300,
                rules.ShovelReachMillimetres, rules.ShovelArcCosinePermille), Is.True);
            Assert.That(CombatMath.IsInsideArc(0, 0, 1000, 0, 0, 1200,
                rules.ShovelReachMillimetres, rules.ShovelArcCosinePermille), Is.False);
            Assert.That(CombatMath.IsInsideArc(0, 0, 1000, 0, -800, 0,
                rules.ShovelReachMillimetres, rules.ShovelArcCosinePermille), Is.False);
            Assert.That(CombatMath.IsInsideArc(0, 0, 1000, 0, 1401, 0,
                rules.ShovelReachMillimetres, rules.ShovelArcCosinePermille), Is.False);
        }

        [Test]
        public void DigNode_RevealsHiddenGridOutcomeOnThirdStrikeOnlyAndIsIdempotent()
        {
            DigGrid grid = new(1, 1, mapSeed: 42UL, new[] { "loot_test" }, emptyWeight: 0);
            GridCell cell = new(0, 0);
            CombatDigNodeState node = new(grid, cell, requiredStrikes: 3);

            Assert.That(grid.GetPublicCell(cell).RevealedLootId, Is.Null);
            Assert.That(node.Strike().RevealedNow, Is.False);
            Assert.That(node.Strike().RevealedNow, Is.False);
            CombatDigStrikeResult reveal = node.Strike();
            CombatDigStrikeResult duplicate = node.Strike();

            Assert.That(reveal.Changed, Is.True);
            Assert.That(reveal.RevealedNow, Is.True);
            Assert.That(reveal.RevealedLootId, Is.EqualTo("loot_test"));
            Assert.That(grid.GetPublicCell(cell).RevealedLootId, Is.EqualTo("loot_test"));
            Assert.That(duplicate.Changed, Is.False);
            Assert.That(duplicate.RevealedLootId, Is.Null);
        }

        [Test]
        public void Pickup_AndProjectile_AreSingleConsumptionAndOwnerSafe()
        {
            CombatPickupState pickup = new(11, "loot_test");
            Assert.That(pickup.TryCollect(1).Changed, Is.True);
            Assert.That(pickup.TryCollect(1).Changed, Is.False);

            CombatantState owner = new(1, team: 0, maximumHealth: 100, CombatRules.PrototypeOne);
            CombatantState target = new(2, team: 1, maximumHealth: 100, CombatRules.PrototypeOne);
            CombatantState secondTarget = new(3, team: 1, maximumHealth: 100, CombatRules.PrototypeOne);
            CombatProjectileState projectile = new(22, owner.EntityId, owner.Team, damage: 6, 24000, 11000);

            Assert.That(projectile.TryHit(owner), Is.EqualTo(CombatDamageResult.RejectedOwner));
            Assert.That(projectile.IsConsumed, Is.False);
            Assert.That(projectile.TryHit(target), Is.EqualTo(CombatDamageResult.Applied));
            Assert.That(projectile.IsConsumed, Is.True);
            Assert.That(target.Health, Is.EqualTo(94));
            Assert.That(projectile.TryHit(secondTarget), Is.EqualTo(CombatDamageResult.RejectedConsumed));
            Assert.That(secondTarget.Health, Is.EqualTo(100));
        }

        [Test]
        public void RollMotion_ClampsTargetAndProducesRepeatableTrajectory()
        {
            CombatRollMotion first = new(CombatRules.PrototypeOne, 9000, 6000, 380);
            CombatRollMotion second = new(CombatRules.PrototypeOne, 9000, 6000, 380);

            Assert.That(first.Begin(8500, 0, 1000, 0), Is.True);
            Assert.That(second.Begin(8500, 0, 1000, 0), Is.True);
            for (int tick = 0; tick < CombatRules.PrototypeOne.RollDurationTicks; tick++)
            {
                first.Step();
                second.Step();
            }

            Assert.That(first.PositionXMillimetres, Is.EqualTo(8620));
            Assert.That(first.PositionYMillimetres, Is.Zero);
            Assert.That(first.PositionXMillimetres, Is.EqualTo(second.PositionXMillimetres));
            Assert.That(first.IsActive, Is.False);
        }

        [Test]
        public void Oxygen_IsDeterministic_DepletesInOneHundredSeconds_AndRefillsAtFivePercent()
        {
            SubterraneanOxygenRules rules = SubterraneanOxygenRules.MilestoneOne;
            SubterraneanOxygenState first = new(rules);
            SubterraneanOxygenState second = new(rules);
            int firstDamage = 0;
            int secondDamage = 0;

            for (int tick = 0; tick < (rules.TicksPerSecond * 100) - 1; tick++)
            {
                firstDamage += first.Step(isSubterranean: true);
                secondDamage += second.Step(isSubterranean: true);
            }

            Assert.That(first.OxygenPercent, Is.GreaterThan(0d));
            firstDamage += first.Step(isSubterranean: true);
            secondDamage += second.Step(isSubterranean: true);
            Assert.That(first.OxygenPercent, Is.Zero);
            Assert.That(firstDamage, Is.Zero);

            for (int tick = 0; tick < rules.TicksPerSecond; tick++)
            {
                firstDamage += first.Step(isSubterranean: true);
                secondDamage += second.Step(isSubterranean: true);
            }

            Assert.That(firstDamage, Is.EqualTo(5));
            Assert.That(secondDamage, Is.EqualTo(firstDamage));
            Assert.That(second.ComputeStateHash(), Is.EqualTo(first.ComputeStateHash()));

            for (int tick = 0; tick < rules.TicksPerSecond * 20; tick++)
            {
                first.Step(isSubterranean: false);
            }

            Assert.That(first.OxygenPercent, Is.EqualTo(100d));
        }

        [Test]
        public void PlayerDepth_UsesAuthoritativeCellDepthAndThresholdTwo()
        {
            PlayerDepthState depth = new();

            Assert.That(depth.ApplyAuthoritativeCellDepth(1), Is.True);
            Assert.That(depth.IsSubterranean, Is.False);
            Assert.That(depth.ApplyAuthoritativeCellDepth(1), Is.False);
            Assert.That(depth.ApplyAuthoritativeCellDepth(2), Is.True);
            Assert.That(depth.IsSubterranean, Is.True);
            Assert.That(depth.ApplyAuthoritativeCellDepth(1), Is.False);
            Assert.That(depth.CurrentDepth, Is.EqualTo(2));
            Assert.That(depth.SetAuthoritativeDepth(0), Is.True);
            Assert.That(depth.IsSubterranean, Is.False);
        }

        [Test]
        public void OxygenFlask_RestoresThirtyFivePercentWithoutExceedingTank()
        {
            SubterraneanOxygenRules rules = SubterraneanOxygenRules.MilestoneOne;
            SubterraneanOxygenState state = new(rules);
            for (int tick = 0; tick < rules.TicksPerSecond * 80; tick++)
            {
                state.Step(isSubterranean: true);
            }

            Assert.That(state.OxygenPercent, Is.EqualTo(20d).Within(.001d));
            Assert.That(state.RestorePercent(SubterraneanOxygenRules.OxygenFlaskRestorePercent), Is.True);
            Assert.That(state.OxygenPercent, Is.EqualTo(55d).Within(.001d));
            Assert.That(state.RestorePercent(100), Is.True);
            Assert.That(state.OxygenPercent, Is.EqualTo(100d));
            Assert.That(state.RestorePercent(1), Is.False);
        }
    }
}
