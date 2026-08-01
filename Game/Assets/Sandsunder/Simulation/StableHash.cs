namespace Sandsunder.Simulation
{
    internal static class StableHash
    {
        internal const ulong Offset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        internal static void Add(ref ulong hash, ulong value)
        {
            for (var shift = 0; shift < 64; shift += 8)
            {
                hash ^= (byte)(value >> shift);
                hash = unchecked(hash * Prime);
            }
        }

        internal static void Add(ref ulong hash, string value)
        {
            if (value == null)
            {
                Add(ref hash, ulong.MaxValue);
                return;
            }
            Add(ref hash, unchecked((ulong)value.Length));
            foreach (var character in value)
                Add(ref hash, character);
        }
    }
}
