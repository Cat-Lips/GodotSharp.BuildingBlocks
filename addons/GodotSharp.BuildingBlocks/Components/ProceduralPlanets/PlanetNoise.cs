using Godot;

namespace GodotSharp.BuildingBlocks
{
    [Tool]
    public partial class PlanetNoise : Resource
    {
        [Export, Notify]
        public Noise Noise
        {
            get => _noise.Get();
            set => _noise.Set(value.DefaultIfNull());
        }

        [Export, Notify]
        public float Amplitude
        {
            get => _amplitude.Get();
            set => _amplitude.Set(Math.Clamp(value, 0, float.MaxValue));
        }

        [Export(PropertyHint.Range, "0,1"), Notify]
        public float MinHeight
        {
            get => _minHeight.Get();
            set => _minHeight.Set(Math.Clamp(value, 0, 1));
        }

        [Export, Notify]
        public bool UseFirstLayerAsMask
        {
            get => _useFirstLayerAsMask.Get();
            set => _useFirstLayerAsMask.Set(value);
        }

        public PlanetNoise()
        {
            Noise = null;
            Amplitude = 1;
            MinHeight = .5f;
            ResourceName = nameof(PlanetNoise);
        }
    }
}
