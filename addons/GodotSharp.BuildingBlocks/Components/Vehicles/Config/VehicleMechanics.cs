using Godot;

namespace GodotSharp.BuildingBlocks
{
    [Tool, CodeComments]
    public partial class VehicleMechanics : Resource
    {
        // This value affects the roll of your vehicle. If set to 1.0 for all wheels, your
        // vehicle will be prone to rolling over, while a value of 0.0 will resist body
        // roll.
        [Export(PropertyHint.Range, "0,1,.001"), Notify]
        public float WheelRollInfluence
        {
            get => _wheelRollInfluence.Get();
            set => _wheelRollInfluence.Set(value);
        }

        // This is the distance in meters the wheel is lowered from its origin point. Don't
        // set this to 0.0 and move the wheel into position, instead move the origin point
        // of your wheel (the gizmo in Godot) to the position the wheel will take when bottoming
        // out, then use the rest length to move the wheel down to the position it should
        // be in when the car is in rest.
        [Export(PropertyHint.Range, "0,1,.001"), Notify]
        public float WheelRestLength
        {
            get => _wheelRestLength.Get();
            set => _wheelRestLength.Set(value);
        }

        // This determines how much grip this wheel has. It is combined with the friction
        // setting of the surface the wheel is in contact with. 0.0 means no grip, 1.0 is
        // normal grip. For a drift car setup, try setting the grip of the rear wheels slightly
        // lower than the front wheels, or use a lower value to simulate tire wear.
        // It's best to set this to 1.0 when starting out.
        [Export(PropertyHint.Range, "0,1,.001,or_greater"), Notify]
        public float WheelFrictionSlipTraction
        {
            get => _wheelFrictionSlipTraction.Get();
            set => _wheelFrictionSlipTraction.Set(value);
        }

        // This determines how much grip this wheel has. It is combined with the friction
        // setting of the surface the wheel is in contact with. 0.0 means no grip, 1.0 is
        // normal grip. For a drift car setup, try setting the grip of the rear wheels slightly
        // lower than the front wheels, or use a lower value to simulate tire wear.
        // It's best to set this to 1.0 when starting out.
        [Export(PropertyHint.Range, "0,1,.001,or_greater"), Notify]
        public float WheelFrictionSlipSteering
        {
            get => _wheelFrictionSlipSteering.Get();
            set => _wheelFrictionSlipSteering.Set(value);
        }

        // This determines how much grip this wheel has. It is combined with the friction
        // setting of the surface the wheel is in contact with. 0.0 means no grip, 1.0 is
        // normal grip. For a drift car setup, try setting the grip of the rear wheels slightly
        // lower than the front wheels, or use a lower value to simulate tire wear.
        // It's best to set this to 1.0 when starting out.
        [Export(PropertyHint.Range, "0,1,.001,or_greater"), Notify]
        public float WheelFrictionSlipOther
        {
            get => _wheelFrictionSlipOther.Get();
            set => _wheelFrictionSlipOther.Set(value);
        }

        // This is the distance the suspension can travel. As Godot units are equivalent
        // to meters, keep this setting relatively low. Try a value between 0.1 and 0.3
        // depending on the type of car.
        [Export(PropertyHint.Range, "0,1,.001,or_greater"), Notify]
        public float SuspensionTravel
        {
            get => _suspensionTravel.Get();
            set => _suspensionTravel.Set(value);
        }

        // This value defines the stiffness of the suspension. Use a value lower than 50
        // for an off-road car, a value between 50 and 100 for a race car and try something
        // around 200 for something like a Formula 1 car.
        [Export, Notify]
        public float SuspensionStiffness
        {
            get => _suspensionStiffness.Get();
            set => _suspensionStiffness.Set(value);
        }

        // The maximum force the spring can resist. This value should be higher than a quarter
        // of the Godot.RigidBody3D.Mass of the Godot.VehicleBody3D or the spring will not
        // carry the weight of the vehicle. Good results are often obtained by a value that
        // is about 3× to 4× this number.
        [Export, Notify]
        public float SuspensionMaxForce
        {
            get => _suspensionMaxForce.Get();
            set => _suspensionMaxForce.Set(value);
        }

        // The damping applied to the spring when the spring is being compressed. This value
        // should be between 0.0 (no damping) and 1.0. A value of 0.0 means the car will
        // keep bouncing as the spring keeps its energy. A good value for this is around
        // 0.3 for a normal car, 0.5 for a race car.
        [Export(PropertyHint.Range, "0,1,.001"), Notify]
        public float DampingCompression
        {
            get => _dampingCompression.Get();
            set => _dampingCompression.Set(value);
        }

        // The damping applied to the spring when relaxing. This value should be between
        // 0.0 (no damping) and 1.0. This value should always be slightly higher than the
        // Godot.VehicleWheel3D.DampingCompression property. For a Godot.VehicleWheel3D.DampingCompression
        // value of 0.3, try a relaxation value of 0.5.
        [Export(PropertyHint.Range, "0,1,.001"), Notify]
        public float DampingRelaxation
        {
            get => _dampingRelaxation.Get();
            set => _dampingRelaxation.Set(value);
        }

        public VehicleMechanics()
        {
            SetDefaultValues();
            ResourceName = nameof(VehicleMechanics);

            void SetDefaultValues()
            {
                var wheel = new VehicleWheel3D();
                WheelRollInfluence = wheel.WheelRollInfluence;
                WheelRestLength = wheel.WheelRestLength;
                WheelFrictionSlipTraction = wheel.WheelFrictionSlip;
                WheelFrictionSlipSteering = wheel.WheelFrictionSlip;
                WheelFrictionSlipOther = wheel.WheelFrictionSlip;
                SuspensionTravel = wheel.SuspensionTravel;
                SuspensionStiffness = wheel.SuspensionStiffness;
                SuspensionMaxForce = wheel.SuspensionMaxForce;
                DampingCompression = wheel.DampingCompression;
                DampingRelaxation = wheel.DampingRelaxation;
            }
        }

        internal void Initialise(VehicleWheel3D[] wheels)
        {
            CreateConnections();
            ApplyCurrentValues();

            void CreateConnections()
            {
                _wheelRollInfluence.Changed += ApplyWheelRollInfluence;
                _wheelRestLength.Changed += ApplyWheelRestLength;
                _wheelFrictionSlipTraction.Changed += ApplyWheelFrictionSlipTraction;
                _wheelFrictionSlipSteering.Changed += ApplyWheelFrictionSlipSteering;
                _wheelFrictionSlipOther.Changed += ApplyWheelFrictionSlipOther;
                _suspensionTravel.Changed += ApplySuspensionTravel;
                _suspensionStiffness.Changed += ApplySuspensionStiffness;
                _suspensionMaxForce.Changed += ApplySuspensionMaxForce;
                _dampingCompression.Changed += ApplyDampingCompression;
                _dampingRelaxation.Changed += ApplyDampingRelaxation;
            }

            void ApplyCurrentValues()
            {
                ApplyWheelRollInfluence();
                ApplyWheelRestLength();
                ApplyWheelFrictionSlipTraction();
                ApplyWheelFrictionSlipSteering();
                ApplyWheelFrictionSlipOther();
                ApplySuspensionTravel();
                ApplySuspensionStiffness();
                ApplySuspensionMaxForce();
                ApplyDampingCompression();
                ApplyDampingRelaxation();
            }

            void ApplyWheelRollInfluence() => wheels.ForEach(x => x.WheelRollInfluence = WheelRollInfluence);
            void ApplyWheelRestLength() => wheels.ForEach(x => x.WheelRestLength = WheelRestLength);
            void ApplyWheelFrictionSlipTraction() => Traction(wheels).ForEach(x => x.WheelFrictionSlip = WheelFrictionSlipTraction);
            void ApplyWheelFrictionSlipSteering() => Steering(wheels).ForEach(x => x.WheelFrictionSlip = WheelFrictionSlipSteering);
            void ApplyWheelFrictionSlipOther() => Other(wheels).ForEach(x => x.WheelFrictionSlip = WheelFrictionSlipOther);
            void ApplySuspensionTravel() => wheels.ForEach(x => x.SuspensionTravel = SuspensionTravel);
            void ApplySuspensionStiffness() => wheels.ForEach(x => x.SuspensionStiffness = SuspensionStiffness);
            void ApplySuspensionMaxForce() => wheels.ForEach(x => x.SuspensionMaxForce = SuspensionMaxForce);
            void ApplyDampingCompression() => wheels.ForEach(x => x.DampingCompression = DampingCompression);
            void ApplyDampingRelaxation() => wheels.ForEach(x => x.DampingRelaxation = DampingRelaxation);

            static IEnumerable<VehicleWheel3D> Traction(IEnumerable<VehicleWheel3D> wheels)
                => wheels.Where(x => x.UseAsTraction);

            static IEnumerable<VehicleWheel3D> Steering(IEnumerable<VehicleWheel3D> wheels)
                => wheels.Where(x => x.UseAsSteering);

            static IEnumerable<VehicleWheel3D> Other(IEnumerable<VehicleWheel3D> wheels)
                => wheels.Where(x => !x.UseAsTraction && !x.UseAsSteering);
        }
    }
}
