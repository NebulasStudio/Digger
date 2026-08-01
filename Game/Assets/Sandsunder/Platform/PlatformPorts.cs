using System.Threading;
using System.Threading.Tasks;
using Sandsunder.Domain;

namespace Sandsunder.Platform
{
    public readonly struct ResultSubmission
    {
        public ResultSubmission(MatchIdentity identity, MatchOutcome outcome, string signedPayload)
        {
            Identity = identity;
            Outcome = outcome;
            SignedPayload = signedPayload;
        }

        public MatchIdentity Identity { get; }
        public MatchOutcome Outcome { get; }
        public string SignedPayload { get; }
    }

    public interface IMatchResultSink
    {
        /// <summary>Must be idempotent for match_id + account_id on the backend.</summary>
        Task SubmitAsync(ResultSubmission submission, CancellationToken cancellationToken);
    }

    public interface ICrashReporter
    {
        void Capture(System.Exception exception, MatchIdentity match = null);
    }
}
