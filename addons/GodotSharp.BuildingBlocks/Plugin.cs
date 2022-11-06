#if TOOLS
using Godot;

namespace GodotSharp.BuildingBlocks
{
    [Tool]
    public partial class Plugin : EditorPlugin
    {
        public Plugin()
        {
            var terrainImportPlugin = new TerrainImportPlugin();

            TreeEntered += ActivatePlugins;
            TreeExiting += DeactivatePlugins;

            void ActivatePlugins()
                => AddImportPlugin(terrainImportPlugin);

            void DeactivatePlugins()
                => RemoveImportPlugin(terrainImportPlugin);
        }
    }
}
#endif
