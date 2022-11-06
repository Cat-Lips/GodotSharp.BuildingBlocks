using System.Diagnostics;
using Godot;
using Godot.Collections;

// https://www.youtube.com/watch?v=ib4lkBURb0M

namespace GodotSharp.BuildingBlocks
{
    [SceneTree]
    public partial class Vehicle : VehicleBody3D
    {
        public event Action<float> Process;
        public event Action<InputEvent> UnhandledInput;

        [Export, Notify]
        public VehicleConfig Config
        {
            get => _config.Get();
            set => _config.Set((value ?? new()).Initialise(this, value is null));
        }

        [Export] public bool ManiaMode { get; set; }

        public bool Active { get; set; }
        private bool ActiveLastFrame { get; set; }

        public Aabb Bounds { get; private set; }
        public Vector3 Origin { get; private set; }

        public bool IsBodyOnFloor { get; private set; }
        public bool IsBodyInContact { get; private set; }
        public bool HasTraction => WheelsWithTraction().Any(x => x.IsInContact());
        public bool HasSteering => WheelsWithSteering().Any(x => x.IsInContact());
        public bool IsInContact => IsBodyInContact || Wheels().Any(x => x.IsInContact());
        public IEnumerable<PhysicsBody3D> TractionSurface => WheelsWithTraction().Select(x => (PhysicsBody3D)x.GetContactBody()).Where(x => x is not null);
        public IEnumerable<PhysicsBody3D> SteeringSurface => WheelsWithSteering().Select(x => (PhysicsBody3D)x.GetContactBody()).Where(x => x is not null);

        public void SetFloorType<TFloor>()
        {
            ContactMonitor = true;
            MaxContactsReported = 1;

            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;

            void OnBodyEntered(Node body)
            {
                if (body is TFloor floor)
                    IsBodyOnFloor = true;
                IsBodyInContact = true;
            }

            void OnBodyExited(Node body)
            {
                if (body is TFloor floor)
                    IsBodyOnFloor = false;
                IsBodyInContact = false;
            }
        }

        [GodotOverride]
        private void OnReady()
        {
            Debug.Assert(
                InputSync is not null && PhysicsSync is not null,
                "Please re-import vehicles (any changes to tscn requires re-import)");

            Config ??= null;
            Bounds = GetMeta("Bounds").AsAabb();
            Origin = GetMeta("Origin").AsVector3();
        }

        [GodotOverride]
        private void OnPhysicsProcess(double delta)
        {
            if (Active)
            {
                ActiveLastFrame = true;
                InputSync.SteerAxis = Input.GetAxis(MyInput.Right, MyInput.Left);
                InputSync.AccelerateAxis = Input.GetAxis(MyInput.Forward, MyInput.Back);
            }
            else if (ActiveLastFrame)
            {
                if (!ManiaMode)
                    InputSync.AccelerateAxis = 0;
                ActiveLastFrame = false;
            }

            InputSync.ApplyInput(this, delta, WheelsWithTraction());
        }

        [GodotOverride]
        private void OnIntegrateForces(PhysicsDirectBodyState3D state)
            => PhysicsSync.IntegrateForces(state);

        [GodotOverride]
        private void OnProcess(double delta)
            => Process?.Invoke((float)delta);

        [GodotOverride]
        private void OnUnhandledInput(InputEvent e)
        {
            if (!Active) return;
            UnhandledInput?.Invoke(e);
        }

        private IEnumerable<VehicleWheel3D> Wheels()
            => this.GetChildren<VehicleWheel3D>();

        private IEnumerable<VehicleWheel3D> WheelsWithSteering()
            => this.GetChildren<VehicleWheel3D>().Where(x => x.UseAsSteering);

        private IEnumerable<VehicleWheel3D> WheelsWithTraction()
            => this.GetChildren<VehicleWheel3D>().Where(x => x.UseAsTraction);

        public override partial void _Ready();
        public override partial void _Process(double delta);
        public override partial void _UnhandledInput(InputEvent e);
        public override partial void _PhysicsProcess(double delta);
        public override partial void _IntegrateForces(PhysicsDirectBodyState3D state);

        public override Array<Dictionary> _GetPropertyList() => new()
        {
            App.PropertyConfig(nameof(Config), Variant.Type.Int, PropertyUsageFlags.NoInstanceState),
        };
    }
}
