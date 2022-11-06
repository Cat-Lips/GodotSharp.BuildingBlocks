using Godot;
using GodotSharp.BuildingBlocks;

namespace NetworkTest
{
    [SceneTree]
    public partial class Vehicles : Node3D
    {
        [GodotOverride]
        private void OnReady()
        {
            this.ForEachChild<Vehicle>(AddJumpFeature);

            void AddJumpFeature(Vehicle vehicle)
            {
                vehicle.SetFloorType<TerrainChunk>();
                vehicle.UnhandledInput += OnVehicleInput;

                void OnVehicleInput(InputEvent e)
                {
                    this.Handle(e, MyInput.Jump, vehicle.IsInContact,
                        () => vehicle.ApplyImpulse(Vector3.Up * vehicle.Mass * 10));
                }
            }
        }

        public override partial void _Ready();
    }
}
