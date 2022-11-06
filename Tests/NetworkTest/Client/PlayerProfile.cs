using Godot;

namespace NetworkTest
{
    public enum AvatarType
    {
        Capsule,
        Cylinder,
        Sphere,
        Prism,
        Torus,
        Box,
    }

    [SceneTree]
    public partial class PlayerProfile : Node
    {
        public string PlayerName { get; set; }
        public Color PlayerColor { get; set; }
        public AvatarType PlayerAvatar { get; set; }

        public void OnReady(Action action)
            => _.Sync.OnReady(action);

        [GodotOverride]
        private void OnReady()
        {
            _.Sync.Add(PlayerName); // No sync required
            _.Sync.Add(PlayerColor); // No sync required
            _.Sync.Add(PlayerAvatar); // No sync required
        }

        public override partial void _Ready();
    }
}
