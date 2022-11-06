using Godot;
using Godot.Collections;

// https://www.youtube.com/playlist?list=PL43PN07AM4J_7ZkZAUotpfijJSoibrvbr

namespace GodotSharp.BuildingBlocks
{
    [Tool]
    public partial class Planet : Node3D
    {
        [Export, Notify]
        public PlanetData PlanetData
        {
            get => _planetData.Get();
            set => _planetData.Set(value ?? new());
        }

        public Planet()
        {
            PlanetData = null;
            Ready += GenerateMesh;
            TreeEntered += () => PlanetDataChanged += GenerateMesh;
            TreeExiting += () => PlanetDataChanged -= GenerateMesh;

            void GenerateMesh()
                => this.ForEachChild<PlanetFace>(x => x.GenerateMesh(PlanetData));
        }

        public override Array<Dictionary> _GetPropertyList() => new()
        {
            App.PropertyConfig(nameof(PlanetData), Variant.Type.Object, PropertyUsageFlags.NoInstanceState),
        };
    }
}
