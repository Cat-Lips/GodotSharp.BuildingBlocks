using Godot;

// https://www.youtube.com/watch?v=rWeQ30h25Yg

namespace GodotSharp.BuildingBlocks
{
    [Tool, SceneTree]
    public partial class BaseTerrain : Node
    {
        private Mesh mesh;

        [GodotOverride]
        private void OnProcess(double delta)
        {
            if (mesh != _.Floor.Mesh.Mesh)
            {
                mesh = _.Floor.Mesh.Mesh;
                if (mesh is null) return;

                var bounds = mesh.GetAabb();

                var x = bounds.End.X;
                var z = bounds.End.Z;
                var nx = bounds.Position.X;
                var nz = bounds.Position.Z;

                _.Bounds.ShapeX.Position = new(x, 0, 0);
                _.Bounds.ShapeZ.Position = new(0, 0, z);
                _.Bounds.Shape_X.Position = new(nx, 0, 0);
                _.Bounds.Shape_Z.Position = new(0, 0, nz);
            }
        }

        public override partial void _Process(double delta);
    }
}
