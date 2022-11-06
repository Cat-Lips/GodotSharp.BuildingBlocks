using Godot;
using Godot.Collections;

namespace GodotSharp.BuildingBlocks
{
    [SceneTree]
    public partial class VehicleInput : InputSync
    {
        // Set these on client when vehicle active
        [Export] public float SteerAxis { get; set; }
        [Export] public float AccelerateAxis { get; set; }

        // Server will validate input as soon as it's received
        protected override void OnServerValidateInput()
        {
            SteerAxis = Math.Clamp(SteerAxis, -1, 1);
            AccelerateAxis = Math.Clamp(AccelerateAxis, -1, 1);
        }

        // Both client & server can apply inputs to vehicle
        // PhysicsSync will maintain authoritative server control
        public void ApplyInput(Vehicle vehicle, double delta, IEnumerable<VehicleWheel3D> wheelsWithTraction)
        {
            var rpm = wheelsWithTraction.Average(x => x.GetRpm());
            var steering = SteerAxis * vehicle.Config.MaxSteer;
            var acceleration = AccelerateAxis * vehicle.Config.MaxTorque;

            vehicle.Steering = Mathf.Lerp(vehicle.Steering, steering, vehicle.Config.SteerSpeed * (float)delta);
            vehicle.EngineForce = acceleration * (1 - Math.Abs(rpm) / vehicle.Config.MaxRpm);
        }

        public override Array<Dictionary> _GetPropertyList() => new()
        {
            App.PropertyConfig(nameof(SteerAxis), Variant.Type.Float, PropertyUsageFlags.NoInstanceState, PropertyUsageFlags.NoEditor),
            App.PropertyConfig(nameof(AccelerateAxis), Variant.Type.Float, PropertyUsageFlags.NoInstanceState, PropertyUsageFlags.NoEditor),
        };
    }
}
