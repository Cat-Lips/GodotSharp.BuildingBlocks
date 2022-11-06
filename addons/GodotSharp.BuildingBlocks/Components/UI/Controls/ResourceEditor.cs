using Godot;

namespace GodotSharp.BuildingBlocks
{
    [SceneTree]
    public partial class ResourceEditor : Accordion
    {
        [Export, Notify]
        public Resource Resource
        {
            get => _resource.Get();
            set => _resource.Set(value);
        }

        public ResourceEditor()
        {
            _resource.Changed += ResetEditor;

            void ResetEditor()
            {
                Clear();
                CreateContent(Resource);

                void CreateContent(Resource resource)
                {
                    if (resource is null) return;

                    AddGroup(resource.ResourceName, resource.GetEditControls().ToArray());

                    foreach (var subResource in resource.GetSubResources())
                        CreateContent(subResource);
                }
            }
        }
    }
}
