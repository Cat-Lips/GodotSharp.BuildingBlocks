using Godot;

namespace GodotSharp.BuildingBlocks
{
    [Tool]
    public partial class PlanetBiome : Resource
    {
        [Export, Notify]
        public Gradient Gradient
        {
            get => _gradient.Get();
            set => _gradient.Set(value ?? new());
        }

        [Export(PropertyHint.Range, "0,1"), Notify]
        public float StartHeight
        {
            get => _startHeight.Get();
            set => _startHeight.Set(Math.Clamp(value, 0, 1));
        }

        public PlanetBiome()
        {
            Gradient = null;
            ResourceName = nameof(PlanetBiome);
        }
    }
}
