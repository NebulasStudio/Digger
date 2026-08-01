using Sandsunder.Domain;

namespace Sandsunder.Presentation
{
    /// <summary>Presentation boundary; concrete MonoBehaviours consume immutable simulation views.</summary>
    public interface IMatchView
    {
        void ShowPhase(MatchPhase phase, long authoritativeTick);
        void ShowOutcome(MatchOutcome outcome);
    }
}
