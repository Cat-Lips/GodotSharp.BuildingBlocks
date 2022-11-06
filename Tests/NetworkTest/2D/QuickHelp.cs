using Godot;
using GodotSharp.BuildingBlocks;
using FileAccess = Godot.FileAccess;

namespace NetworkTest
{
    [Tool, SceneTree]
    public partial class QuickHelp : CanvasLayer
    {
        [Export(PropertyHint.File, "*.txt")]
        public string TextFile { get; set; }

        [GodotOverride]
        private void OnReady()
            => Content.Text = FileAccess.GetFileAsString(TextFile);

        [GodotOverride]
        private void OnUnhandledInput(InputEvent e)
            => this.Handle(e, MyInput.Cancel, () => this.DetachFromParent(free: true));

        public override partial void _Ready();
        public override partial void _UnhandledInput(InputEvent e);
    }
}
