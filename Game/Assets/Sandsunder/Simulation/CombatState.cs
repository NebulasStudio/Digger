using System;

namespace Sandsunder.Simulation
{
    public enum CombatAction
    {
        Pistol = 0,
        Shovel = 1,
        Roll = 2,
        SpitterShot = 3
    }

    public enum CombatDamageResult
    {
        Applied = 0,
        RejectedDead = 1,
        RejectedInvulnerable = 2,
        RejectedOwner = 3,
        RejectedFriendly = 4,
        RejectedConsumed = 5
    }

    public readonly struct CombatDamageRequest
    {
        public CombatDamageRequest(int sourceEntityId, int sourceTeam, int damage)
        {
            if (sourceEntityId < 0) throw new ArgumentOutOfRangeException(nameof(sourceEntityId));
            if (sourceTeam < 0) throw new ArgumentOutOfRangeException(nameof(sourceTeam));
            if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage));

            SourceEntityId = sourceEntityId;
            SourceTeam = sourceTeam;
            Damage = damage;
        }

        public int SourceEntityId { get; }
        public int SourceTeam { get; }
        public int Damage { get; }
    }

    /// <summary>
    /// Tick-driven authoritative-ready state for health, cooldowns and roll immunity.
    /// It deliberately contains no Unity, input, or transport concepts.
    /// </summary>
    public sealed class CombatantState
    {
        private readonly CombatRules rules;
        private long tick;
        private long nextPistolTick;
        private long nextShovelTick;
        private long nextRollTick;
        private long nextSpitterShotTick;
        private long rollEndsTick;
        private long invulnerabilityEndsTick;

        public CombatantState(int entityId, int team, int maximumHealth, CombatRules rules)
        {
            if (entityId < 0) throw new ArgumentOutOfRangeException(nameof(entityId));
            if (team < 0) throw new ArgumentOutOfRangeException(nameof(team));
            if (maximumHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumHealth));

            EntityId = entityId;
            Team = team;
            MaximumHealth = maximumHealth;
            Health = maximumHealth;
            this.rules = rules;
        }

        public int EntityId { get; }
        public int Team { get; }
        public int MaximumHealth { get; }
        public int Health { get; private set; }
        public long Tick => tick;
        public bool IsDead => Health == 0;
        public bool IsRolling => !IsDead && tick < rollEndsTick;
        public bool IsInvulnerable => !IsDead && tick < invulnerabilityEndsTick;

        public long PistolCooldownRemainingTicks => Remaining(nextPistolTick);
        public long ShovelCooldownRemainingTicks => Remaining(nextShovelTick);
        public long RollCooldownRemainingTicks => Remaining(nextRollTick);
        public long SpitterShotCooldownRemainingTicks => Remaining(nextSpitterShotTick);

        public void AdvanceTo(long authoritativeTick)
        {
            if (authoritativeTick < tick)
                throw new ArgumentOutOfRangeException(nameof(authoritativeTick), "Combat time cannot move backwards.");

            tick = authoritativeTick;
        }

        public void AdvanceOneTick()
        {
            tick++;
        }

        public bool TryUse(CombatAction action)
        {
            if (IsDead)
            {
                return false;
            }

            if (IsRolling && action != CombatAction.Roll)
            {
                return false;
            }

            switch (action)
            {
                case CombatAction.Pistol:
                    return TryConsume(ref nextPistolTick, rules.PistolCooldownTicks);
                case CombatAction.Shovel:
                    return TryConsume(ref nextShovelTick, rules.ShovelCooldownTicks);
                case CombatAction.Roll:
                    if (!TryConsume(ref nextRollTick, rules.RollCooldownTicks))
                    {
                        return false;
                    }

                    rollEndsTick = tick + rules.RollDurationTicks;
                    invulnerabilityEndsTick = tick + rules.RollInvulnerabilityTicks;
                    return true;
                case CombatAction.SpitterShot:
                    return TryConsume(ref nextSpitterShotTick, rules.SpitterAttackIntervalTicks);
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        public CombatDamageResult TryApplyDamage(CombatDamageRequest request)
        {
            if (IsDead) return CombatDamageResult.RejectedDead;
            if (request.SourceEntityId == EntityId) return CombatDamageResult.RejectedOwner;
            if (request.SourceTeam == Team) return CombatDamageResult.RejectedFriendly;
            if (IsInvulnerable) return CombatDamageResult.RejectedInvulnerable;

            Health = Math.Max(0, Health - request.Damage);
            return CombatDamageResult.Applied;
        }

        public int Heal(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (IsDead) return 0;

            int previous = Health;
            Health = Math.Min(MaximumHealth, Health + amount);
            return Health - previous;
        }

        public void Reset()
        {
            Health = MaximumHealth;
            nextPistolTick = tick;
            nextShovelTick = tick;
            nextRollTick = tick;
            nextSpitterShotTick = tick;
            rollEndsTick = tick;
            invulnerabilityEndsTick = tick;
        }

        public ulong ComputeStateHash()
        {
            ulong hash = StableHash.Offset;
            StableHash.Add(ref hash, unchecked((ulong)rules.SchemaVersion));
            StableHash.Add(ref hash, unchecked((ulong)EntityId));
            StableHash.Add(ref hash, unchecked((ulong)Team));
            StableHash.Add(ref hash, unchecked((ulong)MaximumHealth));
            StableHash.Add(ref hash, unchecked((ulong)Health));
            StableHash.Add(ref hash, unchecked((ulong)tick));
            StableHash.Add(ref hash, unchecked((ulong)nextPistolTick));
            StableHash.Add(ref hash, unchecked((ulong)nextShovelTick));
            StableHash.Add(ref hash, unchecked((ulong)nextRollTick));
            StableHash.Add(ref hash, unchecked((ulong)nextSpitterShotTick));
            StableHash.Add(ref hash, unchecked((ulong)rollEndsTick));
            StableHash.Add(ref hash, unchecked((ulong)invulnerabilityEndsTick));
            return hash;
        }

        private long Remaining(long untilTick)
        {
            return Math.Max(0, untilTick - tick);
        }

        private bool TryConsume(ref long nextAvailableTick, int cooldownTicks)
        {
            if (tick < nextAvailableTick)
            {
                return false;
            }

            nextAvailableTick = tick + cooldownTicks;
            return true;
        }
    }

    public sealed class CombatProjectileState
    {
        public CombatProjectileState(
            int projectileId,
            int ownerEntityId,
            int ownerTeam,
            int damage,
            int speedMillimetresPerSecond,
            int rangeMillimetres)
        {
            if (projectileId < 0) throw new ArgumentOutOfRangeException(nameof(projectileId));
            if (ownerEntityId < 0) throw new ArgumentOutOfRangeException(nameof(ownerEntityId));
            if (ownerTeam < 0) throw new ArgumentOutOfRangeException(nameof(ownerTeam));
            if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (speedMillimetresPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(speedMillimetresPerSecond));
            if (rangeMillimetres <= 0) throw new ArgumentOutOfRangeException(nameof(rangeMillimetres));

            ProjectileId = projectileId;
            OwnerEntityId = ownerEntityId;
            OwnerTeam = ownerTeam;
            Damage = damage;
            SpeedMillimetresPerSecond = speedMillimetresPerSecond;
            RangeMillimetres = rangeMillimetres;
        }

        public int ProjectileId { get; }
        public int OwnerEntityId { get; }
        public int OwnerTeam { get; }
        public int Damage { get; }
        public int SpeedMillimetresPerSecond { get; }
        public int RangeMillimetres { get; }
        public bool IsConsumed { get; private set; }

        public CombatDamageResult TryHit(CombatantState target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (IsConsumed) return CombatDamageResult.RejectedConsumed;

            CombatDamageResult result = target.TryApplyDamage(
                new CombatDamageRequest(OwnerEntityId, OwnerTeam, Damage));
            if (result == CombatDamageResult.Applied
                || result == CombatDamageResult.RejectedInvulnerable)
            {
                IsConsumed = true;
            }

            return result;
        }
    }

    public static class CombatMath
    {
        public static bool IsInsideArc(
            int originXMillimetres,
            int originYMillimetres,
            int facingX,
            int facingY,
            int targetXMillimetres,
            int targetYMillimetres,
            int reachMillimetres,
            int minimumCosinePermille)
        {
            if (reachMillimetres < 0) throw new ArgumentOutOfRangeException(nameof(reachMillimetres));
            if (minimumCosinePermille < 0 || minimumCosinePermille > 1000)
                throw new ArgumentOutOfRangeException(nameof(minimumCosinePermille));

            long offsetX = targetXMillimetres - (long)originXMillimetres;
            long offsetY = targetYMillimetres - (long)originYMillimetres;
            long distanceSquared = (offsetX * offsetX) + (offsetY * offsetY);
            if (distanceSquared > (long)reachMillimetres * reachMillimetres)
            {
                return false;
            }

            if (distanceSquared == 0)
            {
                return true;
            }

            long facingLengthSquared = ((long)facingX * facingX) + ((long)facingY * facingY);
            if (facingLengthSquared == 0)
            {
                return false;
            }

            long dot = ((long)facingX * offsetX) + ((long)facingY * offsetY);
            if (dot < 0)
            {
                return false;
            }

            decimal scaledDot = dot * 1000m;
            decimal threshold = minimumCosinePermille;
            return (scaledDot * scaledDot)
                >= (threshold * threshold * facingLengthSquared * distanceSquared);
        }
    }
}
