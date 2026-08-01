using System;
using Sandsunder.Domain;

namespace Sandsunder.Networking.Fusion
{
    public readonly struct ServerConnection
    {
        public ServerConnection(string endpoint, string transport, string singleUseTicket)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
            SingleUseTicket = singleUseTicket ?? throw new ArgumentNullException(nameof(singleUseTicket));
        }

        public string Endpoint { get; }
        public string Transport { get; }
        public string SingleUseTicket { get; }
    }

    /// <summary>
    /// Photon-independent port. A future Fusion package implements this using GameMode.Server.
    /// Domain and simulation must never reference Photon types.
    /// </summary>
    public interface IAuthoritativeSession
    {
        bool IsServer { get; }
        long Tick { get; }
        void StartServer(MatchIdentity identity, ServerConnection connection);
        void Stop(string reason);
    }

    public interface IPlayerInputSource<TInput> where TInput : struct
    {
        bool TryRead(PlayerId player, long tick, out TInput input);
    }
}
