using System;

namespace Sandsunder.Simulation
{
    /// <summary>
    /// Pure deterministic roll trajectory. The server chooses start/direction and Unity
    /// only projects PositionX/PositionY after each step.
    /// </summary>
    public sealed class CombatRollMotion
    {
        private readonly CombatRules rules;
        private readonly int arenaHalfWidthMillimetres;
        private readonly int arenaHalfHeightMillimetres;
        private readonly int collisionRadiusMillimetres;
        private int startX;
        private int startY;
        private int targetX;
        private int targetY;
        private int elapsedTicks;
        private bool hasBegun;

        public CombatRollMotion(
            CombatRules rules,
            int arenaHalfWidthMillimetres,
            int arenaHalfHeightMillimetres,
            int collisionRadiusMillimetres)
        {
            if (arenaHalfWidthMillimetres <= 0)
                throw new ArgumentOutOfRangeException(nameof(arenaHalfWidthMillimetres));
            if (arenaHalfHeightMillimetres <= 0)
                throw new ArgumentOutOfRangeException(nameof(arenaHalfHeightMillimetres));
            if (collisionRadiusMillimetres < 0
                || collisionRadiusMillimetres >= arenaHalfWidthMillimetres
                || collisionRadiusMillimetres >= arenaHalfHeightMillimetres)
                throw new ArgumentOutOfRangeException(nameof(collisionRadiusMillimetres));

            this.rules = rules;
            this.arenaHalfWidthMillimetres = arenaHalfWidthMillimetres;
            this.arenaHalfHeightMillimetres = arenaHalfHeightMillimetres;
            this.collisionRadiusMillimetres = collisionRadiusMillimetres;
        }

        public bool IsActive => hasBegun && elapsedTicks < rules.RollDurationTicks;
        public int PositionXMillimetres { get; private set; }
        public int PositionYMillimetres { get; private set; }
        public int TargetXMillimetres => targetX;
        public int TargetYMillimetres => targetY;

        public bool Begin(int positionXMillimetres, int positionYMillimetres, int directionX, int directionY)
        {
            if (IsActive)
            {
                return false;
            }

            long magnitudeSquared = ((long)directionX * directionX) + ((long)directionY * directionY);
            if (magnitudeSquared == 0)
            {
                return false;
            }

            long magnitude = IntegerSquareRoot(magnitudeSquared);
            long displacementX = directionX * (long)rules.RollDistanceMillimetres / magnitude;
            long displacementY = directionY * (long)rules.RollDistanceMillimetres / magnitude;
            int maximumX = arenaHalfWidthMillimetres - collisionRadiusMillimetres;
            int maximumY = arenaHalfHeightMillimetres - collisionRadiusMillimetres;

            startX = Clamp(positionXMillimetres, -maximumX, maximumX);
            startY = Clamp(positionYMillimetres, -maximumY, maximumY);
            targetX = Clamp(startX + displacementX, -maximumX, maximumX);
            targetY = Clamp(startY + displacementY, -maximumY, maximumY);
            PositionXMillimetres = startX;
            PositionYMillimetres = startY;
            elapsedTicks = 0;
            hasBegun = true;
            return true;
        }

        public void Step()
        {
            if (!IsActive)
            {
                return;
            }

            elapsedTicks++;
            int sampleTick = Math.Min(elapsedTicks, rules.RollDurationTicks);
            PositionXMillimetres = startX
                + (int)((targetX - (long)startX) * sampleTick / rules.RollDurationTicks);
            PositionYMillimetres = startY
                + (int)((targetY - (long)startY) * sampleTick / rules.RollDurationTicks);
        }

        private static int Clamp(long value, int minimum, int maximum)
        {
            return (int)Math.Max(minimum, Math.Min(maximum, value));
        }

        private static long IntegerSquareRoot(long value)
        {
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
