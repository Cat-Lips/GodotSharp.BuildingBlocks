using Godot;
using GodotSharp.BuildingBlocks;

namespace NetworkTest
{
    [SceneTree]
    public partial class PlayerStatus : Node
    {
        [Notify] public int PlayerId { get => _playerId.Get(); internal set => _playerId.Set(value); }
        [Notify] public StringName VehicleName { get => _vehicleName.Get(); internal set => _vehicleName.Set(value); }
        [Notify] public bool HasGold { get => _hasGold.Get(); internal set => _hasGold.Set(value); }
        public Vehicle Vehicle { get; private set; }

        [Notify] public int Deliveries { get => _deliveries.Get(); internal set => _deliveries.Set(value); }
        [Notify] public int Captures { get => _captures.Get(); internal set => _captures.Set(value); }
        [Notify] public int Steals { get => _steals.Get(); internal set => _steals.Set(value); }
        public int CurrentScore => Captures + Deliveries * 2;

        public void OnReady(Action action)
            => _.Sync.OnReady(action);

        public void OnSync(Action<PlayerStatus> action)
            => _.Sync.OnSync(() => action(this));

        public PlayerStatus()
        {
            _vehicleName.Changed += OnVehicleNameChanged;

            void OnVehicleNameChanged()
                => Vehicle = GameServer.Vehicles.TryGet(VehicleName);
        }

        [GodotOverride]
        private void OnReady()
        {
            _playerId.Changed += _.Sync.Add(PlayerId);
            _vehicleName.Changed += _.Sync.Add(VehicleName);
            _hasGold.Changed += _.Sync.Add(HasGold);

            _deliveries.Changed += _.Sync.Add(Deliveries);
            _captures.Changed += _.Sync.Add(Captures);
            _steals.Changed += _.Sync.Add(Steals);
        }

        public override partial void _Ready();
    }
}
