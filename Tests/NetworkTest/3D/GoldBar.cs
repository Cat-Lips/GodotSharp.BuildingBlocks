using System.Diagnostics;
using Godot;
using GodotSharp.BuildingBlocks;
using static NetworkTest.Constants;

// Glow Effect: https://www.youtube.com/watch?v=7R4NkOF5b2A

namespace NetworkTest
{
    [Tool, SceneTree]
    public partial class GoldBar : Area3D
    {
        private static readonly float RotateSpeed = Mathf.DegToRad(45);

        [Notify] public StringName VehicleName { get => _vehicleName.Get(); internal set => _vehicleName.Set(value); }
        private Vehicle Vehicle { get; set; }
        private Vector3 Offset { get; set; }

        public GoldBar()
        {
            if (Engine.IsEditorHint()) return;
            _vehicleName.Changed += OnVehicleNameChanged;

            void OnVehicleNameChanged()
            {
                Vehicle = GameServer.Vehicles.TryGet(VehicleName);
                if (Vehicle is null) return;

                var offset = Vehicle.Bounds.End.Y - Vehicle.Origin.Y;
                Offset = new Vector3(Vehicle.Origin.X, offset, Vehicle.Origin.Z) + UpBy1;
            }
        }

        [GodotOverride]
        private void OnReady()
        {
            if (Engine.IsEditorHint()) return;
            InitialiseDataSync();

            void InitialiseDataSync()
            {
                _.DataSync.Add(Position, "position"); // No sync required
                _vehicleName.Changed += _.DataSync.Add(VehicleName);
            }
        }

        [GodotOverride]
        private void OnProcess(double delta)
        {
            if (Vehicle is null)
            {
                RotateY(RotateSpeed * (float)delta);
                return;
            }

            Debug.Assert(!Engine.IsEditorHint());

            var rotY = Rotation.Y;
            GlobalTransform = Vehicle.GlobalTransform.TranslatedLocal(Offset);
            RotateY(Rotation.Y + RotateSpeed * (float)delta);
        }

        public override partial void _Ready();
        public override partial void _Process(double delta);
    }
}
