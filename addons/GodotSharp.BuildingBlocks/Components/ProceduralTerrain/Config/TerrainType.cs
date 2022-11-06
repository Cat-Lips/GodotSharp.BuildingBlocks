using Godot;

namespace GodotSharp.BuildingBlocks
{
    public enum Slope
    {
        Flat,
        Gentle,
        Medium,
        Steep,
    }

    [Tool]
    public partial class TerrainType : Resource
    {
        [Export]
        public string Name { get; set; }

        [Export(PropertyHint.Range, "0,1,"), Notify]
        public float Height
        {
            get => _height.Get();
            set => _height.Set(Math.Clamp(value, 0, 1));
        }

        [Export, Notify]
        public Color Color
        {
            get => _color.Get();
            set => _color.Set(value);
        }

        [Export, Notify]
        public Slope Slope
        {
            get => _slope.Get();
            set => _slope.Set(value);
        }

        public TerrainType()
            => ResourceName = nameof(TerrainType);

        public static TerrainType Default() => new()
        {
            Name = "Grass",
            Height = 0,
            Color = Colors.LawnGreen,
            Slope = Slope.Flat,
        };

        public TerrainType Copy() => new()
        {
            Name = Name,
            Height = Height,
            Color = Color,
            Slope = Slope,
        };

        public static TerrainType[] Copy(TerrainType[] regions)
            => regions.Select(region => region.Copy()).ToArray();

        public static Curve Copy(Curve curve)
            => (Curve)curve.Duplicate(true);

        private static TerrainType[] _defaultTerrainTypes;
        public static TerrainType[] DefaultTerrainTypes()
        {
            return _defaultTerrainTypes ??= new TerrainType[]
            {
                new() { Name = "DeepWater", Color = Colors.DarkBlue, Height = .3f, Slope = Slope.Flat },
                new() { Name = "ShallowWater", Color = Colors.LightSeaGreen, Height = .4f, Slope = Slope.Flat },
                new() { Name = "Sand", Color = Colors.SandyBrown, Height = .45f, Slope = Slope.Gentle },
                new() { Name = "Grass", Color = Colors.LawnGreen, Height = .55f, Slope = Slope.Gentle },
                new() { Name = "Forest", Color = Colors.ForestGreen, Height = .6f, Slope = Slope.Medium },
                new() { Name = "LowRock", Color = Colors.SaddleBrown, Height = .7f, Slope = Slope.Medium },
                new() { Name = "HighRock", Color = Colors.SaddleBrown.Darkened(.3f), Height = .9f, Slope = Slope.Steep },
                new() { Name = "Snow", Color = Colors.Snow, Height = 1, Slope = Slope.Steep },
            };
        }

        public static Curve CreateTerrainCurve(TerrainType[] regions)
        {
            var curve = new Curve();
            curve.AddPoint(Vector2.Zero);
            GetPoints().ForEach(x => curve.AddPoint(x.Point, x.Tangent, x.Tangent));
            return curve;

            IEnumerable<(Vector2 Point, float Tangent)> GetPoints()
            {
                var maxHeight = 0f;
                var points = GetPoints().ToArray();
                return GetTangentsAndNormaliseHeights().ToArray();

                IEnumerable<Vector2> GetPoints()
                {
                    var lastHeight = 0f;
                    foreach (var region in regions)
                    {
                        var newHeight = lastHeight + region.Height * Gradient(region.Slope);
                        yield return new(region.Height, newHeight);
                        if (newHeight > maxHeight) maxHeight = newHeight;
                        lastHeight = newHeight;
                    }

                    static float Gradient(Slope slope) => slope switch
                    {
                        Slope.Flat => 0,
                        Slope.Gentle => .25f,
                        Slope.Medium => .5f,
                        Slope.Steep => 1,
                        _ => throw new NotImplementedException(),
                    };
                }

                IEnumerable<(Vector2 Point, float Tangent)> GetTangentsAndNormaliseHeights()
                {
                    var lastHeight = 0f;
                    var baseTangent = Mathf.DegToRad(45);
                    foreach (var p in points)
                    {
                        var normalisedHeight = p.Y is 0 ? 0 : p.Y / maxHeight;
                        var tangent = baseTangent * (normalisedHeight - lastHeight);
                        yield return (new(p.X, normalisedHeight), tangent);
                        lastHeight = normalisedHeight;
                    }
                }
            }
        }
    }
}
