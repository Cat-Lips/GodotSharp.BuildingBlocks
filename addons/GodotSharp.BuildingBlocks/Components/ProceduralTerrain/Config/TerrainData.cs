using Godot;

namespace GodotSharp.BuildingBlocks
{
    public enum DrawMode
    {
        Mesh,
        ColorMap,
        NoiseMap,
        IslandOverlay,
    }

    [Tool]
    public partial class TerrainData : Resource
    {
        private const int LodLimit = 7;

        public int ChunkSize => Noise._ChunkSize - 1;
        public int HalfChunk => Mathf.CeilToInt(ChunkSize * .5f);

        [Export, Notify]
        public DrawMode DrawMode
        {
            get => _drawMode.Get();
            set => _drawMode.Set(value);
        }

        [Export, Notify]
        public NoiseData Noise
        {
            get => _noise.Get();
            set => _noise.Set(value ?? NoiseData.Default());
        }

        [Export, Notify]
        public TerrainType[] Regions
        {
            get => _regions.Get();
            set => _regions.Set(value ?? TerrainType.DefaultTerrainTypes());
        }

        [Export, Notify]
        public Curve MeshHeightCurve
        {
            get => _meshHeightCurve.Get();
            set => _meshHeightCurve.Set(value ?? TerrainType.CreateTerrainCurve(Regions));
        }

        [Export, Notify]
        public float MeshHeightMultiplier
        {
            get => _meshHeightMultiplier.Get();
            set => _meshHeightMultiplier.Set(value);
        }

        [Export, Notify]
        public float[] MeshLevelOfDetailViewDistances
        {
            get => _meshLevelOfDetailViewDistances.Get();
            set => _meshLevelOfDetailViewDistances.Set(value ?? DefaultLOD());
        }

        [Export, Notify]
        public bool ApplyIslandOverlay
        {
            get => _applyIslandOverlay.Get();
            set => _applyIslandOverlay.Set(value);
        }

        private float[] _lodSqrDistances;
        public float[] LodSqrDistances => _lodSqrDistances ??= MeshLevelOfDetailViewDistances.Select(x => x * x).ToArray();

        private float? _maxViewDistance;
        public float MaxViewDistance => _maxViewDistance ??= MeshLevelOfDetailViewDistances.LastOrDefault();

        private float[,] _islandOverlay;
        public float[,] IslandOverlay => _islandOverlay ??= Noise.GenerateIslandOverlay();

        public TerrainData() : this(true) { }
        public TerrainData(bool connectEvents)
        {
            ResourceName = nameof(TerrainData);

            if (connectEvents)
            {
                _regions.Changed += () => MeshHeightCurve = null;
                _meshLevelOfDetailViewDistances.Changed += ValidateViewDistances;
            }

            void ValidateViewDistances()
            {
                if (MeshLevelOfDetailViewDistances.Length > LodLimit)
                    MeshLevelOfDetailViewDistances = MeshLevelOfDetailViewDistances[..LodLimit];

                _lodSqrDistances = null;
                _maxViewDistance = null;
            }
        }

        public static TerrainData Default() => new(true)
        {
            Noise = null,
            Regions = null,
            //MeshHeightCurve = null,
            MeshHeightMultiplier = 10,
            MeshLevelOfDetailViewDistances = null,
            ApplyIslandOverlay = false,
            DrawMode = DrawMode.Mesh,
        };

        public TerrainData Copy(bool connectEvents = false) => new(connectEvents)
        {
            Noise = Noise.Copy(),
            Regions = TerrainType.Copy(Regions),
            MeshHeightCurve = TerrainType.Copy(MeshHeightCurve),
            MeshHeightMultiplier = MeshHeightMultiplier,
            MeshLevelOfDetailViewDistances = MeshLevelOfDetailViewDistances,
            ApplyIslandOverlay = ApplyIslandOverlay,
            DrawMode = DrawMode,
        };

        public float[,] GetHeightMap(in Vector2I center, out float maxNormalisationOfDips, out float maxNormalisationOfPeaks)
        {
            if (DrawMode is DrawMode.IslandOverlay)
            {
                maxNormalisationOfDips = 0;
                maxNormalisationOfPeaks = 0;
                return IslandOverlay;
            }

            return Noise.GenerateNoiseMap(center, ApplyIslandOverlay ? IslandOverlay : null, out maxNormalisationOfDips, out maxNormalisationOfPeaks);
        }

        public Material GetMaterial(float[,] heightMap)
        {
            var image = DrawMode is DrawMode.NoiseMap or DrawMode.IslandOverlay
                ? Noise.CreateImage(heightMap)
                : Noise.CreateImage(heightMap, GetRegionColor);
            var texture = Noise.CreateTexture(image);
            return Noise.CreateMaterial(texture);

            Color GetRegionColor(float height)
                => (Regions.FirstOrDefault(x => height <= x.Height) ?? Regions.Last()).Color;
        }

        public (Mesh Mesh, Shape3D Shape) GetMesh(float[,] heightMap, Material material, int lod)
        {
            return DrawMode is DrawMode.Mesh
                ? Noise.CreateMesh(heightMap, MeshHeightMultiplier, MeshHeightCurve, material, lod)
                : (Noise.CreatePlane(material), null);
        }

        public float GetHeightAt(float x, float z)
        {
            var size = ChunkSize;
            var halfSize = HalfChunk;

            var chunkCenterX = Mathf.RoundToInt(x / size) * size;
            var chunkCenterY = Mathf.RoundToInt(z / size) * size;
            var chunkCenter = new Vector2I(chunkCenterX, chunkCenterY);

            var noiseOffsetX = Mathf.RoundToInt(x % halfSize) + halfSize;
            var noiseOffsetY = Mathf.RoundToInt(z % halfSize) + halfSize;

            // Could optimise this, but will likely switch to Godot.NoiseTexture later anyway
            var noiseMap = Noise.GenerateNoiseMap(chunkCenter, ApplyIslandOverlay ? IslandOverlay : null, out var _, out var _);
            return Noise.GetHeight(noiseMap[noiseOffsetX, noiseOffsetY], MeshHeightMultiplier, MeshHeightCurve);
        }

        private bool isAsyncSafe = !Engine.IsEditorHint();
        public TerrainData GetEditorSafeAsyncCopy()
        {
            if (isAsyncSafe) return this;
            var copy = Copy();
            copy.isAsyncSafe = true;
            return copy;
        }

        private float[] DefaultLOD()
        {
            if (Engine.IsEditorHint())
                return new float[1];

            var lod = new float[LodLimit];
            var last = lod[0] = ChunkSize * 2;
            for (var i = 1; i < lod.Length; ++i)
                lod[i] = last += ChunkSize;

            //return lod;
            return lod.Take(2).ToArray();
        }
    }
}
