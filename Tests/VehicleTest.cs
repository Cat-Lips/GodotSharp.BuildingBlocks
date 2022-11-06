using Godot;
using GodotSharp.BuildingBlocks;

namespace Test
{
    [SceneTree]
    public partial class VehicleTest : Game
    {
        public VehicleTest()
        {
            ObjectPicker.RegisterChildren<VehicleBody3D>(this, MyInput.Select, SetCameraTarget);
            Ready += () => SetCameraTarget(this.GetChildren<VehicleBody3D>().First());

            void SetCameraTarget(VehicleBody3D target)
                => _.Camera.Target = target;
        }
    }
}
