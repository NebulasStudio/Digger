using System;
using Sandsunder.Domain;
using Sandsunder.Networking.Fusion;
using Sandsunder.Simulation;

namespace Sandsunder.Server
{
    /// <summary>Headless composition root. Hosting providers remain outside the simulation.</summary>
    public sealed class ServerCompositionRoot
    {
        private readonly IAuthoritativeSession _session;

        public ServerCompositionRoot(IAuthoritativeSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public MatchSimulation Start(MatchIdentity identity, ulong authoritativeMapSeed, MatchRules rules, ServerConnection connection)
        {
            if (!_session.IsServer)
                throw new InvalidOperationException("Competitive matches require an authoritative server session.");
            _session.StartServer(identity, connection);
            return new MatchSimulation(new AuthoritativeMatchIdentity(identity, authoritativeMapSeed), rules);
        }
    }
}
