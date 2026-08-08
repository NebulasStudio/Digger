using System;

namespace Sandsunder.Simulation
{
    /// <summary>Deterministic, tick-based magazine state owned by gameplay authority.</summary>
    public sealed class SandboxRifleMagazine
    {
        public const int DefaultCapacity = 6;
        public const int DefaultReloadTicks = 72;

        public SandboxRifleMagazine(int capacity = DefaultCapacity, int reloadTicks = DefaultReloadTicks)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (reloadTicks <= 0) throw new ArgumentOutOfRangeException(nameof(reloadTicks));
            Capacity = capacity;
            ReloadDurationTicks = reloadTicks;
            Ammunition = capacity;
        }

        public int Capacity { get; }
        public int ReloadDurationTicks { get; }
        public int Ammunition { get; private set; }
        public int ReloadRemainingTicks { get; private set; }
        public bool IsReloading => ReloadRemainingTicks > 0;
        public bool CanFire => Ammunition > 0 && !IsReloading;

        public bool TryConsumeShot()
        {
            if (!CanFire) return false;
            Ammunition--;
            if (Ammunition == 0) TryBeginReload();
            return true;
        }

        public bool TryBeginReload()
        {
            if (IsReloading || Ammunition >= Capacity) return false;
            ReloadRemainingTicks = ReloadDurationTicks;
            return true;
        }

        public void AdvanceTicks(int ticks)
        {
            if (ticks < 0) throw new ArgumentOutOfRangeException(nameof(ticks));
            if (!IsReloading || ticks == 0) return;
            ReloadRemainingTicks = Math.Max(0, ReloadRemainingTicks - ticks);
            if (ReloadRemainingTicks == 0) Ammunition = Capacity;
        }
    }
}
