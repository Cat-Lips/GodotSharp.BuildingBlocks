using Godot;
using Humanizer;

namespace GodotSharp.BuildingBlocks
{
    [Tool, SceneTree]
    public partial class AccordionTitle : HBoxContainer
    {
        [GodotOverride]
        private void OnReady()
            => Title.Text = $">>> {Name.ToString().Humanize(LetterCasing.Title)}";

        public override partial void _Ready();
    }
}
