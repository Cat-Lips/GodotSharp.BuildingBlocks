using System.Diagnostics;
using Godot;

namespace GodotSharp.BuildingBlocks
{
    [Tool, SceneTree]
    public partial class AccordionToggle : Button
    {
        private const float ToggleRotation = MathF.PI * .5f; // 90 degrees

        [GodotOverride]
        private void OnReady()
        {
            Debug.Assert(CustomMinimumSize == Vector2.One * Size.Y);
            Debug.Assert(PivotOffset == CustomMinimumSize * .5f);

            OnToggle();
            Pressed += OnToggle;

            void OnToggle()
                => Rotation = ButtonPressed ? ToggleRotation : 0;
        }

        public override partial void _Ready();
    }
}
