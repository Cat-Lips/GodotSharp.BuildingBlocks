using Godot;
using GodotSharp.BuildingBlocks;

namespace Test
{
    [SceneTree]
    public partial class TerrainTest : Game
    {
        [GodotOverride]
        private void OnReady()
        {
            RemoveChild(Players);
            if (Terrain.TerrainReady) AddChild(Players);
            else Terrain.TerrainReadyChanged += () => AddChild(Players);

            Camera.ItemSelected += OnItemSelected;

            void OnItemSelected(CollisionObject3D collider)
            {
                if (collider is VehicleBody3D player) Camera.Target = player;
                else if (collider is TerrainChunk chunk) chunk.Mesh.Visible = !chunk.Mesh.Visible;
            }
        }

        public override partial void _Ready();
    }
}
