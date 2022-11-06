using System.Diagnostics;
using Godot;
using Godot.Collections;

// https://www.youtube.com/watch?v=wbpMiKiSKm8&list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3 (unity)

namespace GodotSharp.BuildingBlocks
{
    [Tool, SceneTree]
    public partial class Terrain : Node
    {
        private int asyncCount;

        [Notify]
        public bool TerrainReady
        {
            get => _terrainReady.Get();
            private set => _terrainReady.Set(value);
        }

        [Export(PropertyHint.MultilineText)]
        public string STATUS { get; set; }

        [Export]
        public Node3D Actor { get; set; }

        [Export, Notify]
        public TerrainData Data
        {
            get => _data.Get();
            set => _data.Set(value ?? TerrainData.Default());
        }

        [GodotOverride]
        private void OnReady()
        {
            Data ??= TerrainData.Default();
            DataChanged += () => dataChanged = true;
        }

        [GodotOverride]
        private void OnProcess(double _)
        {
            if (RedrawRequired(out var center))
            {
                UpdateTerrainChunks(center, Data.MaxViewDistance, dataChanged);
                dataChanged = false;
            }

            UpdateStatus();

            if (!TerrainReady && asyncCount is 0)
                TerrainReady = true;
        }

        [Conditional("TOOLS")]
        private void UpdateStatus()
        {
            if (!Engine.IsEditorHint()) return;
            if (!TerrainChunks.Any()) return;

            var normMinDip = TerrainChunks.Values.Min(x => x.DipNormal);
            var normMinPeak = TerrainChunks.Values.Min(x => x.PeakNormal);

            var normMaxDip = TerrainChunks.Values.Max(x => x.DipNormal);
            var normMaxPeak = TerrainChunks.Values.Max(x => x.PeakNormal);

            var normAvgDip = TerrainChunks.Values.Average(x => x.DipNormal);
            var normAvgPeak = TerrainChunks.Values.Average(x => x.PeakNormal);

            var normAvgAll = TerrainChunks.Values.SelectMany(x => new[] { x.DipNormal, x.PeakNormal }).Average();

            STATUS = asyncCount is 0 ? "" :
                $"UPDATE IN PROGRESS\n\n" +

                $"AsyncCount: {asyncCount}\n";
            STATUS +=
                $"ChunkCount: {TerrainChunks.Count}\n\n" +

                $"MinDip:  {normMinDip}\n" +
                $"MaxDip:  {normMaxDip}\n" +
                $"AvgDip:  {normAvgDip}\n\n" +

                $"MinPeak: {normMinPeak}\n" +
                $"MaxPeak: {normMaxPeak}\n" +
                $"AvgPeak: {normAvgPeak}\n\n" +

                $"AvgAll:  {normAvgAll} (recommended for Noise.{nameof(Data.Noise.GlobalHeightRangeAdjustment)})";
        }

        private Vector3 lastActorPos;
        private bool dataChanged = true;
        private int ActorMoveThreshold => Data.HalfChunk;
        private int SqrActorMoveThreshold => ActorMoveThreshold * ActorMoveThreshold;
        private bool RedrawRequired(out Vector3 center)
        {
            center = lastActorPos;
            if (dataChanged) return true;
            if (Actor is null) return false;
            if (ActorMovedPastThreshold(out var actorPos))
            {
                center = lastActorPos = actorPos;
                return true;
            }

            return false;

            bool ActorMovedPastThreshold(out Vector3 actorPos)
                => (actorPos = Actor.GlobalPosition).DistanceSquaredTo(lastActorPos) > SqrActorMoveThreshold;
        }

        private readonly System.Collections.Generic.Dictionary<Vector2I, TerrainChunk> TerrainChunks = new();
        private void UpdateTerrainChunks(in Vector3 center, float viewDistance, bool regenerateTerrain)
        {
            var centerCoordX = Mathf.RoundToInt(center.X / Data.ChunkSize);
            var centerCoordY = Mathf.RoundToInt(center.Z / Data.ChunkSize);

            var visibleChunks = Mathf.RoundToInt(viewDistance / Data.ChunkSize);

            var chunksToRemove = new HashSet<Vector2I>(TerrainChunks.Keys);
            for (var y = -visibleChunks; y <= visibleChunks; ++y)
            {
                for (var x = -visibleChunks; x <= visibleChunks; ++x)
                {
                    var chunkCoord = new Vector2I(centerCoordX + x, centerCoordY + y);
                    if (!TerrainChunks.TryGetValue(chunkCoord, out var chunk))
                        AddTerrainChunk(chunkCoord, out chunk);
                    else
                        chunksToRemove.Remove(chunkCoord);

                    chunk.SetLevelOfDetail(Data, center, regenerateTerrain);
                }
            }

            chunksToRemove.ForEach(RemoveTerrainChunk);

            void AddTerrainChunk(in Vector2I chunkCoord, out TerrainChunk chunk)
            {
                chunk = TerrainChunk.Instantiate();
                chunk.Initialise(Data, chunkCoord);
                TerrainChunks.Add(chunkCoord, chunk);
                AddChild(chunk);

                var _chunk = chunk;
                chunk.AsyncUpdateCountChanged += () => OnAsyncUpdateCountChanged(_chunk);

                void OnAsyncUpdateCountChanged(TerrainChunk chunk)
                {
                    asyncCount += chunk.AsyncUpdateCount;
                    chunk.AsyncUpdateCount = 0;
                }
            }

            void RemoveTerrainChunk(Vector2I key)
            {
                TerrainChunks.Remove(key, out var chunk);
                this.RemoveChild(chunk, free: true);
            }
        }

        public void GetHeightAt(float x, float z, Action<float> height)
        {
            App.RunAsync(() =>
            {
                var terrainHeight = Data.GetHeightAt(x, z);
                this.CallDeferred(() => height(terrainHeight));
            });
        }

        public override partial void _Ready();
        public override partial void _Process(double _);

        public override Array<Dictionary> _GetPropertyList() => new()
        {
            App.PropertyConfig(nameof(Data), Variant.Type.Object, PropertyUsageFlags.NoInstanceState),
            App.PropertyConfig(nameof(STATUS), Variant.Type.String, PropertyUsageFlags.NoInstanceState),
        };
    }
}
