using System.Diagnostics;
using Godot;
using GodotSharp.BuildingBlocks;

namespace NetworkTest
{
    [SceneTree]
    public partial class GameServer : Node
    {
        private readonly Dictionary<int, Player> players = new();
        private IReadOnlyDictionary<int, Player> Players => players;

        private readonly Dictionary<StringName, int> drivers = new();
        private IReadOnlyDictionary<StringName, int> Drivers => drivers;

        private static readonly Dictionary<StringName, Vehicle> vehicles = new();
        public static IReadOnlyDictionary<StringName, Vehicle> Vehicles => vehicles;

        public void Initialise(Vehicles _vehicles)
        {
            Debug.Assert(!Drivers.Any());
            Debug.Assert(!Vehicles.Any());

            foreach (var vehicle in _vehicles.GetChildren<Vehicle>())
                vehicles.Add(vehicle.Name, vehicle);

            _vehicles.TreeExiting += () =>
            {
                drivers.Clear();
                players.Clear();
                vehicles.Clear();
            };

            RpcGold.Initialise(Drivers, Players, Vehicles);
            RpcVehicle.Initialise(drivers, Players, Vehicles, VehicleHasGold);

            bool VehicleHasGold(Vehicle vehicle)
                => vehicle == RpcGold.VehicleWithGold;
        }

        public void Start(Network network, Terrain terrain)
        {
            Debug.Assert(this.MultiplayerServer());
            if (!this.MultiplayerServer()) return;

            RpcGold.Start(network.SpawnScene, GetRandomSpawnPoint);

            void GetRandomSpawnPoint(Action<Vector3> spawn)
            {
                var spawnOrigin = Vector3I.Zero;
                var spawnRadius = 100;// terrain.Data.ChunkSize;

                var x = GD.RandRange(spawnOrigin.X - spawnRadius, spawnOrigin.X + spawnRadius);
                var z = GD.RandRange(spawnOrigin.Z - spawnRadius, spawnOrigin.Z + spawnRadius);

                terrain.GetHeightAt(x, z, y => spawn(new(x, y, z)));
            }
        }

        public void AddPlayer(PlayerProfile pp)
        {
            Debug.Assert(this.MultiplayerServer());
            if (!this.MultiplayerServer()) return;

            var playerId = pp.GetMultiplayerAuthority();
            var player = App.InstantiateScene<Player>($"{nameof(Player)}_{playerId}");
            pp.AddSibling(player, forceReadableName: true);

            player.Initialise(pp);
            players.Add(playerId, player);
        }

        public void RemovePlayer(PlayerProfile pp)
        {
            Debug.Assert(this.MultiplayerServer());
            if (!this.MultiplayerServer()) return;

            var playerId = pp.GetMultiplayerAuthority();
            players.Remove(playerId, out var player);
            RpcVehicle.ResetVehicle(player);
            player.DetachFromParent(free: true);
        }

        public void RequestVehicle(Vehicle vehicle)
            => RpcVehicle.RequestVehicle(vehicle);

        public void ReleaseVehicle(Vehicle vehicle)
            => RpcVehicle.ReleaseVehicle(vehicle);
    }
}
