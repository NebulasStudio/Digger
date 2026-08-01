namespace Sandsunder.Domain
{
    public sealed class PlayerState
    {
        internal PlayerState(PlayerId id, int seatIndex)
        {
            Id = id;
            SeatIndex = seatIndex;
            IsAlive = true;
            RespawnsRemaining = 1;
            RespawnAtTick = -1;
            LastMilestoneTick = long.MaxValue;
        }

        public PlayerId Id { get; }
        public int SeatIndex { get; }
        public bool IsAlive { get; internal set; }
        public bool AwaitingRespawn { get; internal set; }
        public long RespawnAtTick { get; internal set; }
        public bool IsPermanentlyEliminated { get; internal set; }
        public int RespawnsRemaining { get; internal set; }
        public int ObjectiveMilestones { get; internal set; }
        public long LastMilestoneTick { get; internal set; }
    }
}
