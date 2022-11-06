using Godot;
using GodotSharp.BuildingBlocks;

namespace NetworkTest
{
    [SceneTree]
    public partial class Main : Game
    {
        private Vehicle ActiveVehicle { get; set; }

        [GodotOverride]
        private void OnReady()
        {
            InitialiseMenu();
            InitialiseCamera();
            InitialiseNetwork();
            InitialiseVehicles();
            InitialiseGameServer();
            ParseCommandLineArgs();

            void InitialiseMenu()
            {
                Menu.Initialise(StartServer, StopServer, CreateClient, CloseClient);
                Network.NetworkStateChanged += () => Menu.NetworkState = Network.NetworkState;

                void StartServer(int port)
                    => Network.StartServer(port, Menu.SetServerStatus);

                void StopServer()
                    => Network.StopServer();

                void CreateClient(string address, int port)
                    => Network.CreateClient(address, port, Menu.SetClientStatus);

                void CloseClient()
                    => Network.CloseClient();
            }

            void InitialiseCamera()
            {
                Camera.ItemSelected += OnCameraItemSelected;
                Camera.SelectModeChanged += OnCameraSelectModeChanged;

                void OnCameraItemSelected(CollisionObject3D item)
                {
                    if (item is Vehicle vehicle)
                        ActivateVehicle(vehicle);
                }

                void OnCameraSelectModeChanged()
                {
                    if (Menu.Visible = Camera.SelectMode)
                        DeactivateVehicle(exit: false);
                }
            }

            void InitialiseNetwork()
            {
                Network.Initialise<PlayerProfile>(InitPlayer, OnPlayerAdded, OnPlayerRemoved);
                Network.AddSpawnableScene<Vehicles>(GameServer.Initialise);
                Network.AddSpawnableScene<Player>(OnPlayerSpawned);
                Network.AddSpawnableScene<GoldBar>(Camera.Track);
                Network.AddSpawnableScene<FinishLine>(Camera.Track);

                void InitPlayer(PlayerProfile pp)
                {
                    pp.PlayerName = Menu.GetPlayerName();
                    pp.PlayerColor = Menu.GetPlayerColor();
                    pp.PlayerAvatar = Menu.GetPlayerAvatar();
                }

                void OnPlayerAdded(PlayerProfile pp)
                {
                    pp.OnReady(() =>
                    {
                        PlayerList.AddPlayer(pp);

                        if (this.MultiplayerServer())
                            GameServer.AddPlayer(pp);
                    });
                }

                void OnPlayerRemoved(PlayerProfile pp)
                {
                    PlayerList.RemovePlayer(pp);

                    if (this.MultiplayerServer())
                        GameServer.RemovePlayer(pp);
                }

                void OnPlayerSpawned(Player player)
                {
                    player.Status.OnReady(TrackPlayer);
                    player.Status.OnSync(PlayerList.UpdatePlayer);

                    void TrackPlayer()
                    {
                        if (player.IsLocal()) return;
                        Camera.Track(player, PlayerList.GetPlayerIcon(player.PlayerId));
                    }
                }
            }

            void InitialiseVehicles()
            {
                Vehicles vehicles = null;
                Terrain.TerrainReadyChanged += InitialiseVehicles;
                Network.NetworkStateChanged += InitialiseVehicles;

                void InitialiseVehicles()
                {
                    if (!Terrain.TerrainReady) return;

                    switch (Network.NetworkState)
                    {
                        case NetworkState.None:
                            RemoveVehicles();
                            AddVehicles();
                            break;
                        case NetworkState.ServerStarting:
                        case NetworkState.ClientConnecting:
                            RemoveVehicles();
                            break;
                        case NetworkState.ServerStarted:
                            AddVehicles();
                            GameServer.Start(Network, Terrain);
                            break;
                    }

                    void AddVehicles()
                    {
                        var placeholder = (InstancePlaceholder)Vehicles.Duplicate();
                        Network.AddChild(placeholder, forceReadableName: true);
                        vehicles = (Vehicles)placeholder.CreateInstance(replace: true);
                    }

                    void RemoveVehicles()
                    {
                        if (vehicles is null) return;

                        DeactivateVehicle(exit: true);

                        vehicles.DetachFromParent(free: true);
                        vehicles = null;
                    }
                };
            }

            void InitialiseGameServer()
            {
                GameServer.RpcVehicle.VehicleExit += OnVehicleExit;
                GameServer.RpcVehicle.VehicleEnter += OnVehicleEnter;
            }

            void ParseCommandLineArgs()
            {
                var args = OS.GetCmdlineArgs().Select(x => x.ToLower()).ToArray();
                if (args.Contains("--server")) InvokeStartServer();
                else if (args.Contains("--client")) InvokeCreateClient();
                else QuickHelp.Show();

                void InvokeStartServer()
                    => this.CallDeferred(() => Menu.StartServer.EmitSignal("pressed"));

                void InvokeCreateClient()
                    => this.CallDeferred(() => Menu.CreateClient.EmitSignal("pressed"));
            }
        }

        #region Vehicles

        private void OnVehicleEnter(Vehicle vehicle)
        {
            Camera.Target = vehicle;
            ActiveVehicle = vehicle;
            vehicle.Active = true;

            VehicleEditor.Vehicle = Network.NetworkState is NetworkState.None ? vehicle : null;
        }

        private void OnVehicleExit(Vehicle vehicle)
        {
            Camera.Target = null;
            ActiveVehicle = null;
            vehicle.Active = false;

            VehicleEditor.Vehicle = null;
        }

        private void ActivateVehicle(Vehicle vehicle)
        {
            if (ActiveVehicle == vehicle)
            {
                Camera.Target = vehicle;
                ActiveVehicle.Active = true;
                return;
            }

            GameServer.RequestVehicle(vehicle);
        }

        private void DeactivateVehicle(bool exit)
        {
            if (ActiveVehicle is null)
                return;

            if (!exit)
            {
                Camera.Target = null;
                ActiveVehicle.Active = false;
                return;
            }

            GameServer.ReleaseVehicle(ActiveVehicle);
        }

        #endregion

        public override partial void _Ready();
    }
}
