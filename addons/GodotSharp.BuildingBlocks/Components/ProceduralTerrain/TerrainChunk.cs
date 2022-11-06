using System.Diagnostics;
using Godot;

namespace GodotSharp.BuildingBlocks
{
    [Tool, SceneTree]
    public partial class TerrainChunk : StaticBody3D
    {
        private static readonly PackedScene Scene = App.LoadScene<TerrainChunk>();
        public static TerrainChunk Instantiate() => Scene.Instantiate<TerrainChunk>();

        [Notify]
        public int AsyncUpdateCount
        {
            get => _asyncUpdateCount.Get();
            set => _asyncUpdateCount.Set(value);
        }

        public float DipNormal { get; private set; }
        public float PeakNormal { get; private set; }

        private Vector2I ChunkCoord { get; set; }
        private Vector2I ChunkPos { get; set; }
        private Rect2I ChunkBounds { get; set; }

        private int Lod { get; set; }
        private const int NotVisible = -1;

        public void Initialise(TerrainData source, in Vector2I chunkCoord)
        {
            Lod = NotVisible;
            ChunkCoord = chunkCoord;
            Name = $"Chunk {chunkCoord}";
            ChunkPos = ChunkCoord * source.ChunkSize;
            Position = new Vector3(ChunkPos.X, 0, ChunkPos.Y);
            ChunkBounds = new Rect2I(ChunkPos, 0, 0).Grow(source.HalfChunk);
            Debug.Assert(ChunkBounds.Size == new Vector2I(source.ChunkSize, source.ChunkSize));
            Debug.Assert(ChunkBounds.Position == new Vector2I(ChunkPos.X - source.HalfChunk, ChunkPos.Y - source.HalfChunk));

            GetHeightMapAsync(source);
        }

        public void SetLevelOfDetail(TerrainData source, in Vector3 viewerPosition, bool regenerateChunkData)
        {
            if (regenerateChunkData)
                GetHeightMapAsync(source);

            var lod = GetLod(source, viewerPosition);
            if (Lod != lod)
            {
                Lod = lod;
                GetMeshAsync(source);
            }

            int GetLod(TerrainData source, in Vector3 viewerPosition)
            {
                var sqrDistanceFromViewer = viewerPosition.DistanceSquaredFrom(ChunkBounds);

                for (var i = 0; i < source.LodSqrDistances.Length; ++i)
                {
                    if (sqrDistanceFromViewer <= source.LodSqrDistances[i])
                        return i;
                }

                return NotVisible;
            }
        }

        private Material material;
        private float[,] heightMap;

        private CancellationTokenSource asyncGetHeightMap = new();
        private void GetHeightMapAsync(TerrainData source)
        {
            asyncGetMesh.Cancel();
            asyncGetHeightMap.Cancel();
            source = source.GetEditorSafeAsyncCopy();

            ++AsyncUpdateCount;
            asyncGetHeightMap = App.RunAsync(asyncToken =>
            {
                var heightMap = source.GetHeightMap(ChunkPos, out var dipNormal, out var peakNormal);
                var material = source.GetMaterial(heightMap);

                this.CallDeferred(asyncToken, () =>
                {
                    this.heightMap = heightMap;
                    this.material = material;
                    PeakNormal = peakNormal;
                    DipNormal = dipNormal;
                    GetMeshAsync(source);

                }, x => --AsyncUpdateCount);
            });
        }

        private CancellationTokenSource asyncGetMesh = new();
        private void GetMeshAsync(TerrainData source)
        {
            asyncGetMesh.Cancel();
            if (heightMap is null) return;
            if (LodNotVisible(out var lod)) return;
            source = source.GetEditorSafeAsyncCopy();

            ++AsyncUpdateCount;
            asyncGetMesh = App.RunAsync(asyncToken =>
            {
                var (mesh, shape) = source.GetMesh(heightMap, material, lod);
                this.CallDeferred(asyncToken, () =>
                {
                    _.Mesh.Mesh = mesh;
                    _.Shape.Shape = shape;
                }, x => --AsyncUpdateCount);
            });

            bool LodNotVisible(out int lod)
            {
                if ((lod = Lod) is NotVisible)
                {
                    _.Mesh.Mesh = null;
                    _.Shape.Shape = null;
                    return true;
                }

                return false;
            }
        }
    }
}
