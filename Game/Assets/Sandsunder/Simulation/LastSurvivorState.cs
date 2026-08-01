using System.Collections.Generic;
using Sandsunder.Domain;

namespace Sandsunder.Simulation
{
    public sealed class LastSurvivorState
    {
        public PlayerId? Evaluate(IReadOnlyCollection<PlayerState> players)
        {
            if (players == null || players.Count < 2)
                return null;

            PlayerId? survivor = null;
            foreach (var player in players)
            {
                if (player.IsPermanentlyEliminated)
                    continue;
                if (survivor.HasValue)
                    return null;
                survivor = player.Id;
            }
            return survivor;
        }
    }
}
