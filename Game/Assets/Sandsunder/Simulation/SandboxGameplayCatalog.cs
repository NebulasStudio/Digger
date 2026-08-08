using System;
using System.Collections.Generic;

namespace Sandsunder.Simulation
{
    public enum SandboxWeaponAttackKind
    {
        Melee = 0,
        Projectile = 1
    }

    /// <summary>
    /// Immutable weapon data expressed in deterministic simulation units.
    /// Angles are represented by the cosine of the half arc in permille.
    /// </summary>
    public readonly struct SandboxWeaponDefinition
    {
        public SandboxWeaponDefinition(
            string id,
            SandboxWeaponAttackKind attackKind,
            int damage,
            int cooldownTicks,
            int reachMillimetres,
            int arcCosinePermille,
            int projectileSpeedMillimetresPerSecond,
            int projectileRangeMillimetres)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Weapon id is required.", nameof(id));
            if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (cooldownTicks <= 0) throw new ArgumentOutOfRangeException(nameof(cooldownTicks));
            if (reachMillimetres < 0) throw new ArgumentOutOfRangeException(nameof(reachMillimetres));
            if (arcCosinePermille < 0 || arcCosinePermille > 1000)
                throw new ArgumentOutOfRangeException(nameof(arcCosinePermille));
            if (projectileSpeedMillimetresPerSecond < 0)
                throw new ArgumentOutOfRangeException(nameof(projectileSpeedMillimetresPerSecond));
            if (projectileRangeMillimetres < 0)
                throw new ArgumentOutOfRangeException(nameof(projectileRangeMillimetres));

            if (attackKind == SandboxWeaponAttackKind.Melee
                && (reachMillimetres == 0 || projectileSpeedMillimetresPerSecond != 0 || projectileRangeMillimetres != 0))
                throw new ArgumentException("Melee weapons require reach and cannot define projectile values.");
            if (attackKind == SandboxWeaponAttackKind.Projectile
                && (reachMillimetres != 0 || projectileSpeedMillimetresPerSecond == 0 || projectileRangeMillimetres == 0))
                throw new ArgumentException("Projectile weapons require speed and range and cannot define melee reach.");

            Id = id;
            AttackKind = attackKind;
            Damage = damage;
            CooldownTicks = cooldownTicks;
            ReachMillimetres = reachMillimetres;
            ArcCosinePermille = arcCosinePermille;
            ProjectileSpeedMillimetresPerSecond = projectileSpeedMillimetresPerSecond;
            ProjectileRangeMillimetres = projectileRangeMillimetres;
        }

        public string Id { get; }
        public SandboxWeaponAttackKind AttackKind { get; }
        public int Damage { get; }
        public int CooldownTicks { get; }
        public int ReachMillimetres { get; }
        public int ArcCosinePermille { get; }
        public int ProjectileSpeedMillimetresPerSecond { get; }
        public int ProjectileRangeMillimetres { get; }
    }

    /// <summary>Versioned balance catalog for the focused sandbox loadout.</summary>
    public sealed class SandboxGameplayCatalog
    {
        // cos(45 degrees), rounded down to permille: a 90 degree full melee arc.
        public const int NinetyDegreeArcCosinePermille = 707;
        public const int CurrentSchemaVersion = 1;
        public const string MilestoneOneVersion = "sandbox-gameplay-1";

        public static readonly SandboxGameplayCatalog MilestoneOne = new SandboxGameplayCatalog(
            CurrentSchemaVersion,
            MilestoneOneVersion,
            ticksPerSecond: 60,
            new[]
            {
                new SandboxWeaponDefinition(
                    "shovel.default", SandboxWeaponAttackKind.Melee,
                    damage: 12, cooldownTicks: 33, reachMillimetres: 1400,
                    arcCosinePermille: NinetyDegreeArcCosinePermille,
                    projectileSpeedMillimetresPerSecond: 0, projectileRangeMillimetres: 0),
                new SandboxWeaponDefinition(
                    "sword.scimitar", SandboxWeaponAttackKind.Melee,
                    damage: 18, cooldownTicks: 24, reachMillimetres: 1600,
                    arcCosinePermille: NinetyDegreeArcCosinePermille,
                    projectileSpeedMillimetresPerSecond: 0, projectileRangeMillimetres: 0),
                new SandboxWeaponDefinition(
                    "rifle.brass", SandboxWeaponAttackKind.Projectile,
                    damage: 6, cooldownTicks: 12, reachMillimetres: 0,
                    arcCosinePermille: 0,
                    projectileSpeedMillimetresPerSecond: 24000, projectileRangeMillimetres: 11000),
            });

        private readonly Dictionary<string, SandboxWeaponDefinition> weapons;

        public SandboxGameplayCatalog(
            int schemaVersion,
            string version,
            int ticksPerSecond,
            IReadOnlyList<SandboxWeaponDefinition> definitions)
        {
            if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Catalog version is required.", nameof(version));
            if (ticksPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
            if (definitions == null || definitions.Count == 0)
                throw new ArgumentException("At least one weapon definition is required.", nameof(definitions));

            SchemaVersion = schemaVersion;
            Version = version;
            TicksPerSecond = ticksPerSecond;
            weapons = new Dictionary<string, SandboxWeaponDefinition>(definitions.Count, StringComparer.Ordinal);
            for (int index = 0; index < definitions.Count; index++)
            {
                SandboxWeaponDefinition definition = definitions[index];
                if (!weapons.TryAdd(definition.Id, definition))
                    throw new ArgumentException($"Duplicate weapon id '{definition.Id}'.", nameof(definitions));
            }
        }

        public int SchemaVersion { get; }
        public string Version { get; }
        public int TicksPerSecond { get; }

        public SandboxWeaponDefinition Shovel => GetWeapon("shovel.default");
        public SandboxWeaponDefinition Scimitar => GetWeapon("sword.scimitar");
        public SandboxWeaponDefinition Rifle => GetWeapon("rifle.brass");

        public SandboxWeaponDefinition GetWeapon(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (!weapons.TryGetValue(id, out SandboxWeaponDefinition definition))
                throw new KeyNotFoundException($"Weapon '{id}' is not present in catalog '{Version}'.");
            return definition;
        }

        public bool TryGetWeapon(string id, out SandboxWeaponDefinition definition)
        {
            if (id != null)
            {
                return weapons.TryGetValue(id, out definition);
            }

            definition = default;
            return false;
        }
    }
}
