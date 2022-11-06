using Godot;

namespace GodotSharp.BuildingBlocks
{
    [InputMap]
    public partial class MyInput : Node
    {
        // TODO:  Rethink this
        public const string Cancel = "ui_cancel";

        // TODO:  Rethink this
        public static readonly InputEvent Select = new InputEventMouseButton
        {
            Pressed = true,
            ButtonIndex = MouseButton.Left,
        };
    }
}
