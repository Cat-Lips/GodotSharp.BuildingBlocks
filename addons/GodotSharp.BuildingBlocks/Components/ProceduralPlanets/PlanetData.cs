using Godot;

namespace GodotSharp.BuildingBlocks
{
    [Tool]
    public partial class PlanetData : Resource
    {
        [Export, Notify]
        public float Radius
        {
            get => _radius.Get();
            set => _radius.Set(Math.Clamp(value, 0, float.MaxValue));
        }

        [Export, Notify]
        public int Resolution
        {
            get => _resolution.Get();
            set => _resolution.Set(Math.Clamp(value, 2, int.MaxValue));
        }

        [Export, Notify]
        public PlanetNoise[] PlanetNoise
        {
            get => _planetNoise.Get();
            set => _planetNoise.Set(value ?? new PlanetNoise[1]);
        }

        [Export, Notify]
        public PlanetBiome[] PlanetBiomes
        {
            get => _planetBiomes.Get();
            set => _planetBiomes.Set(value ?? new PlanetBiome[1]);
        }

        [Export, Notify]
        public Noise BiomeNoise
        {
            get => _biomeNoise.Get();
            set => _biomeNoise.Set(value.DefaultIfNull());
        }

        [Export(PropertyHint.Range, "0,1"), Notify]
        public float BiomeAmplitude
        {
            get => _biomeAmplitude.Get();
            set => _biomeAmplitude.Set(Math.Clamp(value, 0, 1));
        }

        [Export(PropertyHint.Range, "0,1"), Notify]
        public float BiomeOffset
        {
            get => _biomeOffset.Get();
            set => _biomeOffset.Set(Math.Clamp(value, 0, 1));
        }

        [Export(PropertyHint.Range, "0,1"), Notify]
        public float BiomeBlend
        {
            get => _biomeBlend.Get();
            set => _biomeBlend.Set(Math.Clamp(value, 0, float.MaxValue));
        }

        public float MinHeight = float.MaxValue;
        public float MaxHeight = float.MinValue;

        public PlanetData()
        {
            Radius = 3;
            Resolution = 250;
            PlanetNoise = null;
            PlanetBiomes = null;
            BiomeNoise = null;
            BiomeAmplitude = .5f;
            BiomeOffset = .5f;
            BiomeBlend = .5f;
            Changed += ResetCalculatedData;
            PlanetBiomesChanged += () => _biomeTexture = null;
            ResourceName = nameof(PlanetData);

            void ResetCalculatedData()
            {
                MinHeight = float.MaxValue;
                MaxHeight = float.MinValue;
            }
        }

        public Vector3 GetPointOnPlanet(Vector3 pointOnSphere)
        {
            float? base_elevation = null;
            var elevation = PlanetNoise.Sum(Elevation);
            return pointOnSphere * Radius * (elevation + 1);

            float Elevation(PlanetNoise x)
            {
                var noise = x.Noise;
                var amplitude = x.Amplitude;
                var minHeight = x.MinHeight;

                var elevation = noise.GetNoise3Dv(pointOnSphere);
                elevation = (elevation + 1) * .5f * amplitude;
                elevation = Math.Max(0, elevation - minHeight);

                return base_elevation is null ? (base_elevation = elevation).Value
                    : x.UseFirstLayerAsMask ? elevation * base_elevation.Value
                    : elevation;
            }
        }

        public float GetBiomePercent(Vector3 pointOnSphere)
        {
            var percentHeight = (pointOnSphere.Y + 1) * .5f;
            var biomeNoise = (BiomeNoise.GetNoise3Dv(pointOnSphere) + 1) * .5f;
            percentHeight += (biomeNoise - BiomeOffset) * BiomeAmplitude;
            var blendRange = BiomeBlend * .5f;
            var biomeIndex = .0f;

            for (var i = 0; i < PlanetBiomes.Length; ++i)
            {
                var dst = percentHeight - PlanetBiomes[i].StartHeight;
                var weight = Mathf.Clamp(Mathf.InverseLerp(-blendRange, blendRange, dst), 0, 1);
                biomeIndex *= 1 - weight;
                biomeIndex += i * weight;
            }

            return biomeIndex / Math.Max(PlanetBiomes.Length - 1, 1);
        }

        private Texture2D _biomeTexture;
        public void GetOrCreateBiomeTexture(out Texture2D biomeTexture)
        {
            biomeTexture = (_biomeTexture ??= CreateBiomeTexture());

            Texture2D CreateBiomeTexture()
            {
                const int width = 128;
                var height = PlanetBiomes.Length;
                var image = Image.CreateFromData(width, height, false, Image.Format.Rgba8, GetBytes());
                var texture = ImageTexture.CreateFromImage(image);
                texture.ResourceName = "BiomeTexture";
                return texture;

                byte[] GetBytes()
                {
                    return PlanetBiomes.Select(x => GetBytes(x.Gradient)).ToArray().Combine();

                    static byte[] GetBytes(Gradient g)
                        => new GradientTexture1D { Width = width, Gradient = g }.GetImage().GetData();
                }
            }
        }
    }
}
