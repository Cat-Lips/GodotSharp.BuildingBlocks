using System.Diagnostics;
using Godot;
using GodotSharp.BuildingBlocks;
using static NetworkTest.Constants;

namespace NetworkTest
{
    [SceneTree]
    public partial class Player : Node3D
    {
        private ShaderMaterial shader = App.LoadShader<Player>(); // TODO: Funky shader fx, glow, spin, etc

        public int PlayerId => Status.PlayerId;
        public string PlayerName => Profile.PlayerName;
        public StringName VehicleName => Status.VehicleName;
        private Vehicle Vehicle => Status.Vehicle;
        private Vector3 Offset { get; set; }

        public bool IsLocal()
        {
            Debug.Assert(PlayerId is not 0, "Wait for PlayerStatus.OnReady");
            return PlayerId == Multiplayer.GetUniqueId();
        }

        public void Initialise(PlayerProfile pp)
        {
            Profile.PlayerName = pp.PlayerName;
            Profile.PlayerColor = pp.PlayerColor;
            Profile.PlayerAvatar = pp.PlayerAvatar;
            Status.PlayerId = pp.GetMultiplayerAuthority();
        }

        [GodotOverride]
        private void OnReady()
        {
            Profile.OnReady(InitPlayer);

            void InitPlayer()
            {
                _.Mesh.Mesh = Profile.PlayerAvatar switch
                {
                    AvatarType.Capsule => new CapsuleMesh(),
                    AvatarType.Cylinder => new CylinderMesh(),
                    AvatarType.Sphere => new SphereMesh(),
                    AvatarType.Prism => new PrismMesh(),
                    AvatarType.Torus => new TorusMesh(),
                    AvatarType.Box => new BoxMesh(),
                    _ => throw new NotImplementedException(),
                };

                _.Mesh.MaterialOverride = shader;
                shader.SetShaderParameter("albedo", Profile.PlayerColor);

                if (IsLocal())
                {
                    Visible = false;
                    return;
                }

                OnVehicleChanged();
                Status.VehicleNameChanged += OnVehicleChanged;

                void OnVehicleChanged()
                {
                    Visible = Vehicle is not null;
                    if (Vehicle is null) return;

                    var offset = Vehicle.Bounds.End.Y - Vehicle.Origin.Y;
                    Offset = new Vector3(Vehicle.Origin.X, offset, Vehicle.Origin.Z) + UpBy3;
                }
            }
        }

        [GodotOverride]
        private void OnProcess(double _)
        {
            if (Vehicle is null) return;
            GlobalTransform = Vehicle.GlobalTransform.TranslatedLocal(Offset);
        }

        public override partial void _Ready();
        public override partial void _Process(double _);
    }
}
