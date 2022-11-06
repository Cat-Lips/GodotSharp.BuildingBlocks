using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Godot;
using Humanizer;

// https://www.youtube.com/watch?v=n8D3vEx7NAE
// https://godotengine.org/article/multiplayer-in-godot-4-0-scene-replication

namespace GodotSharp.BuildingBlocks
{
    [SceneTree]
    public partial class Network : Node
    {
        public const int ServerAuthority = 1;

        #region Ports `n` Stuff

        private const int ListenerPortOffset = 1; // 1 for separate game/listener port, 0 for game/listener as one

        public const int MaxPort = 65535 - ListenerPortOffset;
        public const int MinPort = 49152;

        public static readonly int GamePort = MinPort + App.HashCode % (MaxPort - MinPort);
        public static readonly int ListenerPort = GamePort + ListenerPortOffset;

        public static int GetListenerPort(int gamePort) => gamePort + ListenerPortOffset;
        public static int GetGamePort(int listenerPort) => listenerPort - ListenerPortOffset;

        public static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            return port;
        }

        public static string GetLocalAddress()
        {
            return GetLocalAddresses().Last();

            static IEnumerable<string> GetLocalAddresses()
                => IP.GetLocalAddresses().Where(ip => ip.Count(".") is 3);
        }

        private static bool DedicatedServer => OS.HasFeature("dedicated_server");
        private static bool HeadlessServer => DisplayServer.GetName() is "headless";

        #endregion

        #region Client/Server

        [Notify]
        public NetworkState NetworkState
        {
            get => _networkState.Get();
            private set => _networkState.Set(value);
        }

        private Action<int> AddPlayer;
        private Action RemoveAllPlayers;
        public void Initialise<TPlayer>(Action<TPlayer> initPlayer = null, Action<TPlayer> playerAdded = null, Action<TPlayer> playerRemoved = null) where TPlayer : Node
        {
            Debug.Assert(NetworkState is NetworkState.None);
            Debug.Assert(Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer);

            var playerNodes = new Dictionary<int, TPlayer>();

            this.AddPlayer = AddPlayer;
            this.RemoveAllPlayers = RemoveAllPlayers;
            Multiplayer.PeerConnected += x => AddPlayer((int)x);
            Multiplayer.PeerDisconnected += x => RemovePlayer((int)x);

            void AddPlayer(int peerId)
            {
                Log.Debug($"Peer Connected [PeerId: {peerId}]");

                var player = App.InstantiateScene<TPlayer>($"{typeof(TPlayer).Name}_{peerId}");
                player.SetMultiplayerAuthority(peerId);

                if (peerId == Multiplayer.GetUniqueId())
                    initPlayer?.Invoke(player);

                AddChild(player, forceReadableName: true);

                playerNodes.Add(peerId, player);
                playerAdded?.Invoke(player);
            }

            void RemovePlayer(int peerId)
            {
                Log.Debug($"Peer Disconnected [PeerId: {peerId}]");

                if (playerNodes.Remove(peerId, out var player))
                    this.RemoveChild(player, free: true);

                playerRemoved?.Invoke(player);
            }

            void RemoveAllPlayers()
            {
                while (playerNodes.Count != 0)
                    RemovePlayer(playerNodes.Keys.First());
                _.Spawn.DespawnAll(free: true);
            }
        }

        public bool StartServer(int? port = null, Action<StatusType, string> status = null)
        {
            Debug.Assert(NetworkState is NetworkState.None);
            Debug.Assert(Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer);

            port ??= GamePort;
            var peer = new ENetMultiplayerPeer();
            NetworkState = NetworkState.ServerStarting;
            SendStatus(StatusType.Info, $"Starting Server...");
            var err = peer.CreateServer(port.Value);
            if (CreateServerError()) return false;
            Multiplayer.MultiplayerPeer = peer;
            NetworkState = NetworkState.ServerStarted;
            SendStatus(StatusType.Success, $"ACTIVE");

            if (!DedicatedServer)
                AddPlayer(Multiplayer.GetUniqueId());
            Debug.Assert(Multiplayer.GetUniqueId() is 1);

            return true;

            bool CreateServerError()
            {
                if (err is Error.Ok) return false;
                NetworkState = NetworkState.None;
                SendStatus(StatusType.Error, Message());
                return true;

                string Message() => $"Failed to start server on port {port}{err switch
                {
                    Error.CantCreate => null,
                    Error.AlreadyInUse => $" (port already in use)",
                    _ => $" ({err.Humanize()})"
                }}";
            }

            void SendStatus(StatusType type, string msg)
                => status?.Invoke(type, msg);
        }

        public bool CreateClient(string address = null, int? port = null, Action<StatusType, string> status = null)
        {
            Debug.Assert(NetworkState is NetworkState.None);
            Debug.Assert(Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer);

            port ??= GamePort;
            var peer = new ENetMultiplayerPeer();
            NetworkState = NetworkState.ClientConnecting;
            SendStatus(StatusType.Info, $"Connecting to server...");
            var err = peer.CreateClient(address, port.Value);
            if (CreateClientError()) return false;
            Multiplayer.MultiplayerPeer = peer;

            Multiplayer.ConnectionFailed += OnConnectionFailed;
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ServerDisconnected += OnServerDisconnected;

            return true;

            bool CreateClientError()
            {
                if (err is Error.Ok) return false;
                NetworkState = NetworkState.None;
                SendStatus(StatusType.Error, Message());
                return true;

                string Message() => $"Failed to connect to {address}:{port}{err switch
                {
                    Error.CantCreate => null,
                    Error.AlreadyInUse => $" (port already in use)",
                    _ => $" ({err.Humanize()})"
                }}";
            }

            void OnConnectionFailed()
            {
                Multiplayer.ConnectionFailed -= OnConnectionFailed;
                Multiplayer.ConnectedToServer -= OnConnectedToServer;
                Multiplayer.ServerDisconnected -= OnServerDisconnected;

                NetworkState = NetworkState.None;
                SendStatus(StatusType.Error, $"Failed to connect to {address}:{port}");
            }

            void OnConnectedToServer()
            {
                Multiplayer.ConnectionFailed -= OnConnectionFailed;
                Multiplayer.ConnectedToServer -= OnConnectedToServer;

                NetworkState = NetworkState.ClientConnected;
                SendStatus(StatusType.Success, $"CONNECTED");

                AddPlayer(Multiplayer.GetUniqueId());
                Debug.Assert(Multiplayer.GetUniqueId() is not 1);
            }

            void OnServerDisconnected()
            {
                Multiplayer.ServerDisconnected -= OnServerDisconnected;

                ClosePeer();
                SendStatus(StatusType.Warn, $"Lost Connection!");
            }

            void SendStatus(StatusType type, string msg)
                => status?.Invoke(type, msg);
        }

        public void StopServer()
        {
            Debug.Assert(NetworkState is
                NetworkState.ServerStarting or
                NetworkState.ServerStarted);

            ClosePeer();
        }

        public void CloseClient()
        {
            Debug.Assert(NetworkState is
                NetworkState.ClientConnecting or
                NetworkState.ClientConnected);

            ClosePeer();
        }

        private void ClosePeer()
        {
            RemoveAllPlayers();

            Multiplayer.MultiplayerPeer.Close();
            Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();

            NetworkState = NetworkState.None;
        }

        #endregion

        #region Sync Tools

        public void AddSpawnableScene<TScene>() where TScene : Node
            => _.Spawn.AddSpawnableScene<TScene>();

        public void AddSpawnableScene<TScene>(Action<TScene> onSceneSpawned, bool invokeOnServer = true) where TScene : Node
        {
            AddSpawnableScene<TScene>();

            if (invokeOnServer)
                _.Spawn.GetSpawnNode().ChildEnteredTree += OnSpawn;
            else
                _.Spawn.Spawned += OnSpawn;

            void OnSpawn(Node node)
            {
                if (node is TScene scene)
                    onSceneSpawned(scene);
            }
        }

        public void SpawnScene(Node scene)
            => _.Spawn.SpawnScene(scene);

        #endregion

        [GodotOverride]
        private void OnReady()
        {
            if (HeadlessServer)
                this.CallDeferred(() => StartServer());
        }

        public override partial void _Ready();
    }
}
