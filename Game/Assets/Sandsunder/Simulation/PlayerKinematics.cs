using System;

namespace Sandsunder.Simulation
{
    /// <summary>
    /// Versioned, vendor-free rules for deterministic player locomotion.
    /// Distances use integer millimetres and inputs use a signed 1000-unit axis.
    /// </summary>
    public readonly struct PlayerKinematicsRules
    {
        public const int AxisUnits = 1000;
        public const int CurrentSchemaVersion = 1;

        public static readonly PlayerKinematicsRules MilestoneOne = new PlayerKinematicsRules(
            CurrentSchemaVersion,
            ticksPerSecond: 60,
            speedMillimetresPerSecond: 5200,
            aimDeadZoneUnits: 200,
            arenaHalfWidthMillimetres: 24000,
            arenaHalfHeightMillimetres: 16000,
            collisionRadiusMillimetres: 380);

        public PlayerKinematicsRules(
            int schemaVersion,
            int ticksPerSecond,
            int speedMillimetresPerSecond,
            int aimDeadZoneUnits,
            int arenaHalfWidthMillimetres,
            int arenaHalfHeightMillimetres,
            int collisionRadiusMillimetres)
        {
            if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            if (ticksPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
            if (speedMillimetresPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(speedMillimetresPerSecond));
            if (aimDeadZoneUnits < 0 || aimDeadZoneUnits >= AxisUnits)
                throw new ArgumentOutOfRangeException(nameof(aimDeadZoneUnits));
            if (arenaHalfWidthMillimetres <= 0)
                throw new ArgumentOutOfRangeException(nameof(arenaHalfWidthMillimetres));
            if (arenaHalfHeightMillimetres <= 0)
                throw new ArgumentOutOfRangeException(nameof(arenaHalfHeightMillimetres));
            if (collisionRadiusMillimetres <= 0
                || collisionRadiusMillimetres >= arenaHalfWidthMillimetres
                || collisionRadiusMillimetres >= arenaHalfHeightMillimetres)
                throw new ArgumentOutOfRangeException(nameof(collisionRadiusMillimetres));

            SchemaVersion = schemaVersion;
            TicksPerSecond = ticksPerSecond;
            SpeedMillimetresPerSecond = speedMillimetresPerSecond;
            AimDeadZoneUnits = aimDeadZoneUnits;
            ArenaHalfWidthMillimetres = arenaHalfWidthMillimetres;
            ArenaHalfHeightMillimetres = arenaHalfHeightMillimetres;
            CollisionRadiusMillimetres = collisionRadiusMillimetres;
        }

        public int SchemaVersion { get; }
        public int TicksPerSecond { get; }
        public int SpeedMillimetresPerSecond { get; }
        public int AimDeadZoneUnits { get; }
        public int ArenaHalfWidthMillimetres { get; }
        public int ArenaHalfHeightMillimetres { get; }
        public int CollisionRadiusMillimetres { get; }
    }

    public readonly struct PlayerKinematicsInput
    {
        private PlayerKinematicsInput(
            int moveX,
            int moveY,
            int aimX,
            int aimY,
            bool hasFocus)
        {
            MoveX = ClampAxis(moveX);
            MoveY = ClampAxis(moveY);
            AimX = ClampAxis(aimX);
            AimY = ClampAxis(aimY);
            HasFocus = hasFocus;
        }

        public int MoveX { get; }
        public int MoveY { get; }
        public int AimX { get; }
        public int AimY { get; }
        public bool HasFocus { get; }

        public static PlayerKinematicsInput Create(
            int moveX,
            int moveY,
            int aimX,
            int aimY,
            bool hasFocus = true)
        {
            return new PlayerKinematicsInput(moveX, moveY, aimX, aimY, hasFocus);
        }

        public static PlayerKinematicsInput FromDigitalMovement(
            bool up,
            bool down,
            bool left,
            bool right,
            int aimX,
            int aimY,
            bool hasFocus = true)
        {
            int moveX = (right ? PlayerKinematicsRules.AxisUnits : 0)
                - (left ? PlayerKinematicsRules.AxisUnits : 0);
            int moveY = (up ? PlayerKinematicsRules.AxisUnits : 0)
                - (down ? PlayerKinematicsRules.AxisUnits : 0);
            return Create(moveX, moveY, aimX, aimY, hasFocus);
        }

        private static int ClampAxis(int value)
        {
            return Math.Max(-PlayerKinematicsRules.AxisUnits, Math.Min(PlayerKinematicsRules.AxisUnits, value));
        }
    }

    public readonly struct PlayerKinematicsState
    {
        internal PlayerKinematicsState(
            long tick,
            long positionXMillimetres,
            long positionYMillimetres,
            int facingX,
            int facingY,
            long movementRemainderX,
            long movementRemainderY)
        {
            Tick = tick;
            PositionXMillimetres = positionXMillimetres;
            PositionYMillimetres = positionYMillimetres;
            FacingX = facingX;
            FacingY = facingY;
            MovementRemainderX = movementRemainderX;
            MovementRemainderY = movementRemainderY;
        }

        public long Tick { get; }
        public long PositionXMillimetres { get; }
        public long PositionYMillimetres { get; }
        public int FacingX { get; }
        public int FacingY { get; }
        internal long MovementRemainderX { get; }
        internal long MovementRemainderY { get; }
    }

    /// <summary>
    /// Authoritative deterministic kinematics. Unity transforms and rigidbodies only
    /// project this state and must never be read back as simulation truth.
    /// </summary>
    public sealed class PlayerKinematics
    {
        private readonly PlayerKinematicsRules rules;
        private long tick;
        private long positionXMillimetres;
        private long positionYMillimetres;
        private int facingX = PlayerKinematicsRules.AxisUnits;
        private int facingY;
        private long movementRemainderX;
        private long movementRemainderY;

        public PlayerKinematics(
            PlayerKinematicsRules rules,
            long initialPositionXMillimetres = 0,
            long initialPositionYMillimetres = 0)
        {
            this.rules = rules;
            positionXMillimetres = initialPositionXMillimetres;
            positionYMillimetres = initialPositionYMillimetres;
        }

        public PlayerKinematicsState State => new PlayerKinematicsState(
            tick,
            positionXMillimetres,
            positionYMillimetres,
            facingX,
            facingY,
            movementRemainderX,
            movementRemainderY);

        public void Step(PlayerKinematicsInput input)
        {
            tick++;

            int moveX = input.HasFocus ? input.MoveX : 0;
            int moveY = input.HasFocus ? input.MoveY : 0;
            ClampVectorToUnitCircle(ref moveX, ref moveY);

            long denominator = (long)PlayerKinematicsRules.AxisUnits * rules.TicksPerSecond;
            long numeratorX = moveX * (long)rules.SpeedMillimetresPerSecond + movementRemainderX;
            long numeratorY = moveY * (long)rules.SpeedMillimetresPerSecond + movementRemainderY;
            long deltaX = numeratorX / denominator;
            long deltaY = numeratorY / denominator;
            movementRemainderX = numeratorX % denominator;
            movementRemainderY = numeratorY % denominator;
            positionXMillimetres += deltaX;
            positionYMillimetres += deltaY;
            ApplyArenaBounds();

            if (!input.HasFocus)
            {
                return;
            }

            int aimX = input.AimX;
            int aimY = input.AimY;
            long aimMagnitudeSquared = (long)aimX * aimX + (long)aimY * aimY;
            long deadZoneSquared = (long)rules.AimDeadZoneUnits * rules.AimDeadZoneUnits;
            if (aimMagnitudeSquared <= deadZoneSquared)
            {
                return;
            }

            NormalizeToUnit(ref aimX, ref aimY);
            facingX = aimX;
            facingY = aimY;
        }

        public ulong ComputeStateHash()
        {
            ulong hash = StableHash.Offset;
            StableHash.Add(ref hash, unchecked((ulong)rules.SchemaVersion));
            StableHash.Add(ref hash, unchecked((ulong)tick));
            StableHash.Add(ref hash, unchecked((ulong)positionXMillimetres));
            StableHash.Add(ref hash, unchecked((ulong)positionYMillimetres));
            StableHash.Add(ref hash, unchecked((ulong)facingX));
            StableHash.Add(ref hash, unchecked((ulong)facingY));
            StableHash.Add(ref hash, unchecked((ulong)movementRemainderX));
            StableHash.Add(ref hash, unchecked((ulong)movementRemainderY));
            return hash;
        }

        private static void ClampVectorToUnitCircle(ref int x, ref int y)
        {
            long magnitudeSquared = (long)x * x + (long)y * y;
            long maximumSquared = (long)PlayerKinematicsRules.AxisUnits * PlayerKinematicsRules.AxisUnits;
            if (magnitudeSquared <= maximumSquared)
            {
                return;
            }

            long magnitude = IntegerSquareRoot(magnitudeSquared);
            x = (int)(x * (long)PlayerKinematicsRules.AxisUnits / magnitude);
            y = (int)(y * (long)PlayerKinematicsRules.AxisUnits / magnitude);
        }

        private void ApplyArenaBounds()
        {
            long maximumX = rules.ArenaHalfWidthMillimetres - rules.CollisionRadiusMillimetres;
            long maximumY = rules.ArenaHalfHeightMillimetres - rules.CollisionRadiusMillimetres;
            long clampedX = Math.Max(-maximumX, Math.Min(maximumX, positionXMillimetres));
            long clampedY = Math.Max(-maximumY, Math.Min(maximumY, positionYMillimetres));

            if (clampedX != positionXMillimetres)
            {
                positionXMillimetres = clampedX;
                movementRemainderX = 0;
            }

            if (clampedY != positionYMillimetres)
            {
                positionYMillimetres = clampedY;
                movementRemainderY = 0;
            }
        }

        private static void NormalizeToUnit(ref int x, ref int y)
        {
            long magnitudeSquared = (long)x * x + (long)y * y;
            long magnitude = IntegerSquareRoot(magnitudeSquared);
            x = (int)(x * (long)PlayerKinematicsRules.AxisUnits / magnitude);
            y = (int)(y * (long)PlayerKinematicsRules.AxisUnits / magnitude);
        }

        private static long IntegerSquareRoot(long value)
        {
            if (value <= 0)
            {
                return 0;
            }

            long current = value;
            long next = (current + 1) / 2;
            while (next < current)
            {
                current = next;
                next = (current + value / current) / 2;
            }

            return current;
        }
    }
}
