using Godot;

namespace GodotSharp.BuildingBlocks
{
    [Tool]
    public partial class VehicleConfig : Resource
    {
        [Export, Notify]
        public VehicleFeatures Features
        {
            get => _features.Get();
            set => _features.Set(value ?? new());
        }

        [Export, Notify]
        public VehicleMechanics Mechanics
        {
            get => _mechanics.Get();
            set => _mechanics.Set(value ?? new());
        }

        [Export] public float MaxRpm { get; set; } = 500;
        [Export] public float MaxTorque { get; set; } = 500;
        [Export] public float SteerSpeed { get; set; } = 5;

        [Export(PropertyHint.Range, "0,1,.001")]
        public float MaxSteer { get; set; } = .4f;

        public VehicleConfig()
        {
            Features = null;
            Mechanics = null;
            ResourceName = nameof(VehicleConfig);
        }

        internal VehicleConfig Initialise(VehicleBody3D vehicle, bool setDefaults = false)
        {
            var wheels = vehicle.GetChildren<VehicleWheel3D>().ToArray();

            Features.Initialise(wheels);
            Mechanics.Initialise(wheels);

            if (setDefaults)
            {
                Mechanics.WheelRollInfluence = 0;
                Mechanics.SuspensionStiffness = MathF.Round(vehicle.Mass) / wheels.Length * 3;
            }

            return this;
        }
    }
}
