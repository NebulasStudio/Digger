using System;

namespace Sandsunder.Simulation
{
    /// <summary>
    /// Stable PCG32 implementation. Its sequence is part of the ruleset contract;
    /// changing it requires a new ruleset version.
    /// </summary>
    public sealed class DeterministicRng
    {
        private ulong _state;
        private readonly ulong _increment;

        public DeterministicRng(ulong seed, ulong stream = 54UL)
        {
            _increment = (stream << 1) | 1UL;
            _state = 0UL;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            var oldState = _state;
            _state = unchecked(oldState * 6364136223846793005UL + _increment);
            var xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            var rotation = (int)(oldState >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        public int NextInt(int exclusiveMaximum)
        {
            if (exclusiveMaximum <= 0)
                throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));

            var bound = (uint)exclusiveMaximum;
            var threshold = unchecked((uint)(0 - bound)) % bound;
            while (true)
            {
                var value = NextUInt();
                if (value >= threshold)
                    return (int)(value % bound);
            }
        }
    }
}
