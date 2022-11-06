using System.Diagnostics;
using Godot;
using GodotSharp.BuildingBlocks;
using static Godot.MultiplayerApi;

namespace NetworkTest
{
    [SceneTree]
    public partial class RpcVehicle : Node
    {
        private const bool log = false;

        public event Action<Vehicle> VehicleEnter;
        public event Action<Vehicle> VehicleExit;
        public event Action<Vehicle, int> VehicleDenied;

        private Func<Vehicle, bool> VehicleHasGold;
        private IDictionary<StringName, int> Drivers;
        private IReadOnlyDictionary<int, Player> Players;
        private IReadOnlyDictionary<StringName, Vehicle> Vehicles;

        public void Initialise(
            IDictionary<StringName, int> drivers,
            IReadOnlyDictionary<int, Player> players,
            IReadOnlyDictionary<StringName, Vehicle> vehicles,
            Func<Vehicle, bool> vehicleHasGold)
        {
            Drivers = drivers;
            Players = players;
            Vehicles = vehicles;
            VehicleHasGold = vehicleHasGold;
        }

        public void RequestVehicle(Vehicle vehicle)
        {
            if (this.SinglePlayerMode())
                this.CallDeferred(() => VehicleEnter?.Invoke(vehicle));
            else
                RpcId(Network.ServerAuthority, nameof(RpcServer_OnVehicleRequest), vehicle.Name);
        }

        public void ReleaseVehicle(Vehicle vehicle)
        {
            if (this.SinglePlayerMode())
                this.CallDeferred(() => VehicleExit?.Invoke(vehicle));
            else
                RpcId(Network.ServerAuthority, nameof(RpcServer_OnVehicleRelease), vehicle.Name);
        }

        public void ResetVehicle(Player player)
        {
            Debug.Assert(this.MultiplayerServer());
            if (!this.MultiplayerServer()) return;

            if (player.VehicleName is null) return;

            ServerOnly_ExitVehicle(player,
                GetVehicle(player.VehicleName, player.PlayerId),
                notify: false);
        }

        #region RpcClient

        // Can only be called by server; Executed on all clients (including server)
        [Rpc(RpcMode.Authority, CallLocal = true)]
        private void RpcClient_OnVehicleEnter(StringName vehicleName)
        {
            Debug.Assert(Multiplayer.GetRemoteSenderId() is Network.ServerAuthority);
            if (Multiplayer.GetRemoteSenderId() is not Network.ServerAuthority) return;

            var vehicle = GetVehicle(vehicleName);
            SetVehicleAuthority(vehicle, Multiplayer.GetUniqueId());

            VehicleEnter?.Invoke(vehicle);
        }

        // Can only be called by server; Executed on all clients (including server)
        [Rpc(RpcMode.Authority, CallLocal = true)]
        private void RpcClient_OnVehicleExit(StringName vehicleName)
        {
            Debug.Assert(Multiplayer.GetRemoteSenderId() is Network.ServerAuthority);
            if (Multiplayer.GetRemoteSenderId() is not Network.ServerAuthority) return;

            var vehicle = GetVehicle(vehicleName);
            SetVehicleAuthority(vehicle, Network.ServerAuthority);

            VehicleExit?.Invoke(vehicle);
        }

        // Can only be called by server; Executed on all clients (including server)
        [Rpc(RpcMode.Authority, CallLocal = true)]
        private void RpcClient_OnVehicleDenied(StringName vehicleName, int driverId)
        {
            Debug.Assert(Multiplayer.GetRemoteSenderId() is Network.ServerAuthority);
            if (Multiplayer.GetRemoteSenderId() is not Network.ServerAuthority) return;

            var vehicle = GetVehicle(vehicleName);
            VehicleDenied?.Invoke(vehicle, driverId);
        }

        #endregion

        #region RpcServer

        // Can be called by any client (including server); Executed on server only
        [Rpc(RpcMode.AnyPeer, CallLocal = true)]
        private void RpcServer_OnVehicleRequest(StringName vehicleName)
        {
            Debug.Assert(this.MultiplayerServer());
            var playerId = Multiplayer.GetRemoteSenderId();

            var player = GetPlayer(playerId);
            var vehicle = GetVehicle(vehicleName, playerId);
            if (player is null || vehicle is null) return;

            if (VehicleHasDriver(vehicleName, out var driverId) || VehicleHasGold(vehicle))
            {
                RpcId(playerId, nameof(RpcClient_OnVehicleDenied), vehicleName, driverId);
                return;
            }

            if (player.VehicleName is not null)
            {
                ServerOnly_ExitVehicle(player,
                    GetVehicle(player.VehicleName, playerId));
            }

            ServerOnly_EnterVehicle(player, vehicle);
        }

        // Can be called by any client (including server); Executed on server only
        [Rpc(RpcMode.AnyPeer, CallLocal = true)]
        private void RpcServer_OnVehicleRelease(StringName vehicleName)
        {
            Debug.Assert(this.MultiplayerServer());
            var playerId = Multiplayer.GetRemoteSenderId();

            ServerOnly_ExitVehicle(
                GetPlayer(playerId),
                GetVehicle(vehicleName, playerId));
        }

        private void ServerOnly_EnterVehicle(Player player, Vehicle vehicle)
        {
            if (player is null || vehicle is null) return;

            AddDriver(vehicle.Name, player.PlayerId);
            SetVehicleAuthority(vehicle, player.PlayerId);
            player.Status.VehicleName = vehicle.Name;

            Log.If(log, $"EnterVehicle [Player: {player.PlayerId}, Vehicle: {vehicle.Name}]");
            RpcId(player.PlayerId, nameof(RpcClient_OnVehicleEnter), vehicle.Name);
        }

        private void ServerOnly_ExitVehicle(Player player, Vehicle vehicle, bool notify = true)
        {
            if (player is null || vehicle is null) return;

            RemoveDriver(vehicle.Name, player.PlayerId);
            SetVehicleAuthority(vehicle, Network.ServerAuthority);

            if (!notify) return;
            player.Status.VehicleName = null;

            Log.If(log, $"ExitVehicle [Player: {player.PlayerId}, Vehicle: {vehicle.Name}]");
            RpcId(player.PlayerId, nameof(RpcClient_OnVehicleExit), vehicle.Name);
        }

        #endregion

        // Input auth is jerky and limits player experience
        // Physics sync will need to update server and approximately validate position
        private static void SetVehicleAuthority(Vehicle vehicle, int auth)
            //=> vehicle.InputSync.MultiplayerAuthority = auth;
            => vehicle.SetMultiplayerAuthority(auth);

        #region Private

        private Player GetPlayer(int playerId)
        {
            if (!Players.TryGetValue(playerId, out var player))
                Log.Warn($"Unknown Player [{Log.Var(playerId)}]");
            return player;
        }

        private Vehicle GetVehicle(StringName vehicleName) => GetVehicle(vehicleName, Multiplayer.GetUniqueId());
        private Vehicle GetVehicle(StringName vehicleName, int playerId)
        {
            if (!Vehicles.TryGetValue(vehicleName, out var vehicle))
                Log.Warn($"Unknown Vehicle [{Log.Var(vehicleName, playerId)}]");
            return vehicle;
        }

        private bool VehicleHasDriver(StringName vehicleName, out int driverId)
                => Drivers.TryGetValue(vehicleName, out driverId);

        private void RemoveDriver(StringName vehicleName, int playerId)
        {
            if (!Drivers.Remove(vehicleName, out var driverId))
                Log.Warn($"Vehicle Driver Missing [{Log.Var(vehicleName, playerId)}]");
            else if (driverId != playerId)
                Log.Warn($"Vehicle Driver Mismatch [{Log.Var(vehicleName, driverId, playerId)}]");
        }

        private void AddDriver(StringName vehicleName, int playerId)
        {
            if (!Drivers.TryAdd(vehicleName, playerId))
            {
                var driverId = Drivers[vehicleName];
                Log.Warn($"Vehicle Driver Exists [{Log.Var(vehicleName, driverId, playerId)}]");
                Drivers[vehicleName] = playerId;
            }
        }

        #endregion
    }
}
