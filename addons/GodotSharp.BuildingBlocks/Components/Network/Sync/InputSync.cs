using Godot;

namespace GodotSharp.BuildingBlocks
{
    [SceneTree]
    public partial class InputSync : MultiplayerSynchronizer
    {
        public int MultiplayerAuthority
        {   // A client should only ever have authority over input
            get => GetMultiplayerAuthority();
            set => SetMultiplayerAuthority(value);
        }   // And server should always validate input before processing

        protected virtual void OnServerValidateInput()
            => throw new NotImplementedException();

        [GodotOverride]
        private void OnReady()
        {
            Synchronized += () =>
            {
                if (this.MultiplayerServer())
                    OnServerValidateInput();
            };
        }

        public override partial void _Ready();
    }
}
