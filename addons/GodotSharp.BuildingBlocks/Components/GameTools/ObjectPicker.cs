using Godot;

namespace GodotSharp.BuildingBlocks
{
    public static class ObjectPicker
    {
        public static void RegisterChildren<T>(Node root, InputEvent action, Action<T> onAction) where T : CollisionObject3D
        {
            root.ChildEnteredTree += OnChildEntered;
            root.ForEachChild(OnChildEntered);

            void OnChildEntered(Node node)
            {
                if (node is T body)
                {
                    body.InputEvent += OnChildInput;
                    body.TreeExiting += () => body.InputEvent -= OnChildInput;

                    void OnChildInput(Node camera, InputEvent e, Vector3 position, Vector3 normal, long shapeIndex)
                        => camera.Handle(e, action, () => onAction(body));
                }
            }
        }
    }
}
