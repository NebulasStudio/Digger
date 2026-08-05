using System;

namespace Sandsunder.Simulation
{
    /// <summary>
    /// Versioned prototype-only combat values. These are deterministic simulation data,
    /// not the shipping catalog; presentation code projects them into Unity units.
    /// </summary>
    public readonly struct CombatRules
    {
        public const int CurrentSchemaVersion = 1;
        public const string PrototypeVersion = "combat-prototype-1";

        public static readonly CombatRules PrototypeOne = new CombatRules(
            schemaVersion: CurrentSchemaVersion,
            version: PrototypeVersion,
            ticksPerSecond: 60,
            playerMaximumHealth: 100,
            pistolDamage: 6,
            pistolShotsPerMinute: 300,
            pistolProjectileSpeedMillimetresPerSecond: 24000,
            pistolRangeMillimetres: 11000,
            shovelDamage: 12,
            shovelCooldownTicks: 33,
            shovelReachMillimetres: 1400,
            shovelArcCosinePermille: 500,
            digStrikesRequired: 3,
            rollDurationTicks: 18,
            rollDistanceMillimetres: 1200,
            rollInvulnerabilityTicks: 12,
            rollCooldownTicks: 75,
            spitterMaximumHealth: 55,
            spitterDamage: 4,
            spitterAttackIntervalTicks: 160,
            spitterProjectileSpeedMillimetresPerSecond: 2200,
            spitterAttackRangeMillimetres: 6000,
            spitterPreferredRangeMillimetres: 4500,
            spitterMoveSpeedMillimetresPerSecond: 800,
            spitterTelegraphTicks: 28,
            respawnDelayTicks: 120,
            healingPickupAmount: 25);

        public CombatRules(
            int schemaVersion,
            string version,
            int ticksPerSecond,
            int playerMaximumHealth,
            int pistolDamage,
            int pistolShotsPerMinute,
            int pistolProjectileSpeedMillimetresPerSecond,
            int pistolRangeMillimetres,
            int shovelDamage,
            int shovelCooldownTicks,
            int shovelReachMillimetres,
            int shovelArcCosinePermille,
            int digStrikesRequired,
            int rollDurationTicks,
            int rollDistanceMillimetres,
            int rollInvulnerabilityTicks,
            int rollCooldownTicks,
            int spitterMaximumHealth,
            int spitterDamage,
            int spitterAttackIntervalTicks,
            int spitterProjectileSpeedMillimetresPerSecond,
            int spitterAttackRangeMillimetres,
            int spitterPreferredRangeMillimetres,
            int spitterMoveSpeedMillimetresPerSecond,
            int spitterTelegraphTicks,
            int respawnDelayTicks,
            int healingPickupAmount)
        {
            if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Version is required.", nameof(version));
            if (ticksPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
            if (playerMaximumHealth <= 0) throw new ArgumentOutOfRangeException(nameof(playerMaximumHealth));
            if (pistolDamage <= 0) throw new ArgumentOutOfRangeException(nameof(pistolDamage));
            if (pistolShotsPerMinute <= 0) throw new ArgumentOutOfRangeException(nameof(pistolShotsPerMinute));
            if (pistolProjectileSpeedMillimetresPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(pistolProjectileSpeedMillimetresPerSecond));
            if (pistolRangeMillimetres <= 0) throw new ArgumentOutOfRangeException(nameof(pistolRangeMillimetres));
            if (shovelDamage <= 0) throw new ArgumentOutOfRangeException(nameof(shovelDamage));
            if (shovelCooldownTicks <= 0) throw new ArgumentOutOfRangeException(nameof(shovelCooldownTicks));
            if (shovelReachMillimetres <= 0) throw new ArgumentOutOfRangeException(nameof(shovelReachMillimetres));
            if (shovelArcCosinePermille < 0 || shovelArcCosinePermille > 1000)
                throw new ArgumentOutOfRangeException(nameof(shovelArcCosinePermille));
            if (digStrikesRequired <= 0) throw new ArgumentOutOfRangeException(nameof(digStrikesRequired));
            if (rollDurationTicks <= 0) throw new ArgumentOutOfRangeException(nameof(rollDurationTicks));
            if (rollDistanceMillimetres <= 0) throw new ArgumentOutOfRangeException(nameof(rollDistanceMillimetres));
            if (rollInvulnerabilityTicks < 0 || rollInvulnerabilityTicks > rollDurationTicks)
                throw new ArgumentOutOfRangeException(nameof(rollInvulnerabilityTicks));
            if (rollCooldownTicks < rollDurationTicks) throw new ArgumentOutOfRangeException(nameof(rollCooldownTicks));
            if (spitterMaximumHealth <= 0) throw new ArgumentOutOfRangeException(nameof(spitterMaximumHealth));
            if (spitterDamage <= 0) throw new ArgumentOutOfRangeException(nameof(spitterDamage));
            if (spitterAttackIntervalTicks <= 0) throw new ArgumentOutOfRangeException(nameof(spitterAttackIntervalTicks));
            if (spitterProjectileSpeedMillimetresPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(spitterProjectileSpeedMillimetresPerSecond));
            if (spitterAttackRangeMillimetres <= 0)
                throw new ArgumentOutOfRangeException(nameof(spitterAttackRangeMillimetres));
            if (spitterPreferredRangeMillimetres <= 0
                || spitterPreferredRangeMillimetres > spitterAttackRangeMillimetres)
                throw new ArgumentOutOfRangeException(nameof(spitterPreferredRangeMillimetres));
            if (spitterMoveSpeedMillimetresPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(spitterMoveSpeedMillimetresPerSecond));
            if (spitterTelegraphTicks < 0 || spitterTelegraphTicks >= spitterAttackIntervalTicks)
                throw new ArgumentOutOfRangeException(nameof(spitterTelegraphTicks));
            if (respawnDelayTicks < 0) throw new ArgumentOutOfRangeException(nameof(respawnDelayTicks));
            if (healingPickupAmount <= 0) throw new ArgumentOutOfRangeException(nameof(healingPickupAmount));

            SchemaVersion = schemaVersion;
            Version = version;
            TicksPerSecond = ticksPerSecond;
            PlayerMaximumHealth = playerMaximumHealth;
            PistolDamage = pistolDamage;
            PistolShotsPerMinute = pistolShotsPerMinute;
            PistolProjectileSpeedMillimetresPerSecond = pistolProjectileSpeedMillimetresPerSecond;
            PistolRangeMillimetres = pistolRangeMillimetres;
            ShovelDamage = shovelDamage;
            ShovelCooldownTicks = shovelCooldownTicks;
            ShovelReachMillimetres = shovelReachMillimetres;
            ShovelArcCosinePermille = shovelArcCosinePermille;
            DigStrikesRequired = digStrikesRequired;
            RollDurationTicks = rollDurationTicks;
            RollDistanceMillimetres = rollDistanceMillimetres;
            RollInvulnerabilityTicks = rollInvulnerabilityTicks;
            RollCooldownTicks = rollCooldownTicks;
            SpitterMaximumHealth = spitterMaximumHealth;
            SpitterDamage = spitterDamage;
            SpitterAttackIntervalTicks = spitterAttackIntervalTicks;
            SpitterProjectileSpeedMillimetresPerSecond = spitterProjectileSpeedMillimetresPerSecond;
            SpitterAttackRangeMillimetres = spitterAttackRangeMillimetres;
            SpitterPreferredRangeMillimetres = spitterPreferredRangeMillimetres;
            SpitterMoveSpeedMillimetresPerSecond = spitterMoveSpeedMillimetresPerSecond;
            SpitterTelegraphTicks = spitterTelegraphTicks;
            RespawnDelayTicks = respawnDelayTicks;
            HealingPickupAmount = healingPickupAmount;
        }

        public int SchemaVersion { get; }
        public string Version { get; }
        public int TicksPerSecond { get; }
        public int PlayerMaximumHealth { get; }
        public int PistolDamage { get; }
        public int PistolShotsPerMinute { get; }
        public int PistolProjectileSpeedMillimetresPerSecond { get; }
        public int PistolRangeMillimetres { get; }
        public int ShovelDamage { get; }
        public int ShovelCooldownTicks { get; }
        public int ShovelReachMillimetres { get; }
        public int ShovelArcCosinePermille { get; }
        public int DigStrikesRequired { get; }
        public int RollDurationTicks { get; }
        public int RollDistanceMillimetres { get; }
        public int RollInvulnerabilityTicks { get; }
        public int RollCooldownTicks { get; }
        public int SpitterMaximumHealth { get; }
        public int SpitterDamage { get; }
        public int SpitterAttackIntervalTicks { get; }
        public int SpitterProjectileSpeedMillimetresPerSecond { get; }
        public int SpitterAttackRangeMillimetres { get; }
        public int SpitterPreferredRangeMillimetres { get; }
        public int SpitterMoveSpeedMillimetresPerSecond { get; }
        public int SpitterTelegraphTicks { get; }
        public int RespawnDelayTicks { get; }
        public int HealingPickupAmount { get; }

        public int PistolCooldownTicks => CeilingDivide(TicksPerSecond * 60, PistolShotsPerMinute);

        private static int CeilingDivide(int numerator, int denominator)
        {
            return (numerator + denominator - 1) / denominator;
        }
    }
}
