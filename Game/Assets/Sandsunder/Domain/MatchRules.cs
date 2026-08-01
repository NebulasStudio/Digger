using System;

namespace Sandsunder.Domain
{
    public readonly struct FixedTickConfig
    {
        public FixedTickConfig(int tickRate, int inputRate, int snapshotRate, int interpolationMilliseconds, int rewindMilliseconds)
        {
            if (tickRate <= 0) throw new ArgumentOutOfRangeException(nameof(tickRate));
            if (inputRate <= 0 || inputRate > tickRate) throw new ArgumentOutOfRangeException(nameof(inputRate));
            if (snapshotRate <= 0 || snapshotRate > tickRate) throw new ArgumentOutOfRangeException(nameof(snapshotRate));
            if (interpolationMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(interpolationMilliseconds));
            if (rewindMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(rewindMilliseconds));

            TickRate = tickRate;
            InputRate = inputRate;
            SnapshotRate = snapshotRate;
            InterpolationMilliseconds = interpolationMilliseconds;
            RewindMilliseconds = rewindMilliseconds;
        }

        public int TickRate { get; }
        public int InputRate { get; }
        public int SnapshotRate { get; }
        public int InterpolationMilliseconds { get; }
        public int RewindMilliseconds { get; }
        public double SecondsPerTick => 1d / TickRate;

        public static FixedTickConfig CompetitiveDefault => new FixedTickConfig(30, 30, 15, 100, 200);
    }

    public sealed class MatchRules
    {
        public MatchRules(
            FixedTickConfig tick,
            long centerOpenTick,
            long suddenDeathTick,
            long matchEndTick,
            int requiredSeals = 3,
            int requiredStations = 2,
            int ritualChannelTicks = 240,
            int suddenDeathRitualChannelTicks = 150,
            int respawnDelayTicks = 0,
            int maxPlayers = 6,
            int extractionExitCount = 3,
            int suddenDeathExitId = 0)
        {
            if (centerOpenTick <= 0) throw new ArgumentOutOfRangeException(nameof(centerOpenTick));
            if (suddenDeathTick <= centerOpenTick) throw new ArgumentOutOfRangeException(nameof(suddenDeathTick));
            if (matchEndTick <= suddenDeathTick) throw new ArgumentOutOfRangeException(nameof(matchEndTick));
            if (requiredSeals <= 0) throw new ArgumentOutOfRangeException(nameof(requiredSeals));
            if (requiredStations <= 0) throw new ArgumentOutOfRangeException(nameof(requiredStations));
            if (ritualChannelTicks <= 0) throw new ArgumentOutOfRangeException(nameof(ritualChannelTicks));
            if (suddenDeathRitualChannelTicks <= 0 || suddenDeathRitualChannelTicks > ritualChannelTicks) throw new ArgumentOutOfRangeException(nameof(suddenDeathRitualChannelTicks));
            if (respawnDelayTicks < 0) throw new ArgumentOutOfRangeException(nameof(respawnDelayTicks));
            if (maxPlayers < 2) throw new ArgumentOutOfRangeException(nameof(maxPlayers));
            if (extractionExitCount <= 0) throw new ArgumentOutOfRangeException(nameof(extractionExitCount));
            if (suddenDeathExitId < 0 || suddenDeathExitId >= extractionExitCount) throw new ArgumentOutOfRangeException(nameof(suddenDeathExitId));

            Tick = tick;
            CenterOpenTick = centerOpenTick;
            SuddenDeathTick = suddenDeathTick;
            MatchEndTick = matchEndTick;
            RequiredSeals = requiredSeals;
            RequiredStations = requiredStations;
            RitualChannelTicks = ritualChannelTicks;
            SuddenDeathRitualChannelTicks = suddenDeathRitualChannelTicks;
            RespawnDelayTicks = respawnDelayTicks == 0 ? 8 * tick.TickRate : respawnDelayTicks;
            MaxPlayers = maxPlayers;
            ExtractionExitCount = extractionExitCount;
            SuddenDeathExitId = suddenDeathExitId;
        }

        public FixedTickConfig Tick { get; }
        public long CenterOpenTick { get; }
        public long SuddenDeathTick { get; }
        public long MatchEndTick { get; }
        public int RequiredSeals { get; }
        public int RequiredStations { get; }
        public int RitualChannelTicks { get; }
        public int SuddenDeathRitualChannelTicks { get; }
        public int RespawnDelayTicks { get; }
        public int MaxPlayers { get; }
        public int ExtractionExitCount { get; }
        public int SuddenDeathExitId { get; }

        public static MatchRules MvpDefault
        {
            get
            {
                var tick = FixedTickConfig.CompetitiveDefault;
                return new MatchRules(
                    tick,
                    centerOpenTick: 6L * 60 * tick.TickRate,
                    suddenDeathTick: 14L * 60 * tick.TickRate,
                    matchEndTick: 15L * 60 * tick.TickRate);
            }
        }
    }
}
