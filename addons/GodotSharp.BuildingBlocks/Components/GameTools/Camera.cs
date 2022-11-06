using Godot;

namespace GodotSharp.BuildingBlocks
{
    public partial class Camera : Camera3D
    {
        private bool processSelect;

        [Export, Notify]
        public Node3D Target
        {
            get => _target.Get();
            set => _target.Set(value);
        }

        [Export] public float Speed { get; set; } = 3;
        [Export] public Vector3 Offset { get; set; } = new Vector3(0, 3, 5);

        [Export, Notify]
        public bool SelectMode
        {
            get => _selectMode.Get();
            set => _selectMode.Set(value);
        }

        public event Action<CollisionObject3D> ItemSelected;

        [GodotOverride]
        private void OnReady()
        {
            OnTargetChanged();
            OnSelectModeChanged();
            TargetChanged += OnTargetChanged;
            SelectModeChanged += OnSelectModeChanged;

            void OnTargetChanged()
            {
                if (Target is not null)
                    SelectMode = false;
            }

            void OnSelectModeChanged()
            {
                if (SelectMode)
                {
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                }
                else
                {
                    GetViewport().GuiReleaseFocus();
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                }
            }
        }

        [GodotOverride]
        private void OnProcess(double _)
            => this.ProcessTracking();

        [GodotOverride]
        private void OnPhysicsProcess(double delta)
        {
            if (SelectMode)
            {
                if (processSelect)
                    this.ProcessSelect(ItemSelected);
            }
            else
            {
                if (Target is null)
                    this.ProcessAsFreeLookCamera(Speed, (float)delta);
                else if (Target is RigidBody3D body)
                    this.ProcessAsChaseCamera(body, Offset, Speed, (float)delta);
                else
                    this.ProcessAsFollowCamera(Target, Offset, Speed, (float)delta);
            }

            processSelect = false;
        }

        [GodotOverride]
        private void OnUnhandledInput(InputEvent e)
        {
            this.Handle(e, MyInput.Cancel, () => { Target = null; SelectMode = !SelectMode; });
            this.Handle(e, MyInput.Select, () => processSelect = true, SelectMode);
            if (!SelectMode && Target is null) this.ProcessInputAsFreeLookCamera(e);
        }

        public override partial void _Ready();
        public override partial void _Process(double _);
        public override partial void _PhysicsProcess(double delta);
        public override partial void _UnhandledInput(InputEvent e);
    }
}
