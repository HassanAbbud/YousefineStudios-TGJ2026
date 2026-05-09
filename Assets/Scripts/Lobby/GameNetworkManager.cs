using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Lobby
{
    /// <summary>
    /// Bootstraps Unity Services, signs the player in, and starts Netcode
    /// using Relay (for cross-network multiplayer).
    /// </summary>
    public class GameNetworkManager : MonoBehaviour
    {
        public static GameNetworkManager Instance { get; private set; }

        [Tooltip("Drag the NetworkManager from the same scene here.")]
        public NetworkManager networkManager;

        public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Initialize Unity Services and sign the player in anonymously.
        /// Safe to call multiple times — does nothing if already signed in.
        /// </summary>
        public async Task EnsureSignedIn()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        /// <summary>
        /// Host path: allocate a Relay slot and start Netcode as host.
        /// Returns the join code that clients need to connect.
        /// </summary>
        public async Task<string> StartHostWithRelay(int maxConnections)
        {
            await EnsureSignedIn();

            // Allocate Relay (maxConnections excludes the host itself)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Configure UTP transport to use Relay
            var utp = networkManager.GetComponent<UnityTransport>();
            var serverData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            utp.SetRelayServerData(serverData);

            networkManager.StartHost();
            return joinCode;
        }

        /// <summary>
        /// Client path: redeem the join code, configure transport, start Netcode as client.
        /// </summary>
        public async Task StartClientWithRelay(string joinCode)
        {
            await EnsureSignedIn();

            JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var utp = networkManager.GetComponent<UnityTransport>();
            var serverData = AllocationUtils.ToRelayServerData(join, "dtls");
            utp.SetRelayServerData(serverData);

            networkManager.StartClient();
        }

        public void Shutdown()
        {
            if (networkManager != null && (networkManager.IsHost || networkManager.IsClient))
                networkManager.Shutdown();
        }
    }
}