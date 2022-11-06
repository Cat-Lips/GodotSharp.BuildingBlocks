#if TOOLS
using Godot;
using Humanizer;

namespace GodotSharp.BuildingBlocks
{
    [Tool]
    public partial class VehicleImport : EditorScenePostImport
    {
        private const float MassMultiplier = 10;

        public override GodotObject _PostImport(Node scene)
        {
            return scene is Node3D import
                ? CreateVehicle(import)
                : scene;

            VehicleBody3D CreateVehicle(Node3D scene)
            {
                var root = App.LoadScene<Vehicle>().Instantiate<VehicleBody3D>();
                root.Name = Humanize(scene.Name);
                root.Transform = scene.Transform;
                root.Scale = scene.Scale;
                Aabb? rootAabb = null;

                ParseParts();
                root.SetMeta("Origin", root.Position);
                root.SetMeta("Bounds", rootAabb ?? default);

                return root;

                void ParseParts()
                {
                    scene.RecurseChildren<MeshInstance3D>(mesh =>
                    {
                        var name = Humanize(mesh.Name);
                        var tag = name.ToLower();

                        if (tag.Contains("wheel"))
                        {
                            if (tag.Contains("front"))
                            {
                                if (tag.Contains("left"))
                                    CreateWheel("FrontLeft", mesh, steering: true);
                                else if (tag.Contains("right"))
                                    CreateWheel("FrontRight", mesh, steering: true);
                            }
                            else if (tag.Contains("rear") || tag.Contains("back"))
                            {
                                if (tag.Contains("left"))
                                    CreateWheel("RearLeft", mesh, traction: true);
                                else if (tag.Contains("right"))
                                    CreateWheel("RearRight", mesh, traction: true);
                            }
                            else
                            {
                                CreateWheel(name.Replace("Wheel", ""), mesh);
                            }
                        }
                        else
                        {
                            CopyMesh(mesh, $"{name}Mesh", root);
                            CreateShape(mesh, $"{name}Shape", root);
                        }
                    });

                    void CreateWheel(string location, MeshInstance3D mesh, bool steering = false, bool traction = false)
                    {
                        var bounds = mesh.GetLocalRootTransform() * mesh.GetAabb();
                        var center = bounds.GetCenter();
                        var radius = bounds.GetLongestAxisSize() * .5f;
                        var offset = bounds.GetShortestAxisSize() * .5f;
                        var transform = new Transform3D(Basis.Identity, center);

                        var wheel = new VehicleWheel3D
                        {
                            Name = $"Wheel_{location}",
                            UniqueNameInOwner = true,
                            WheelRadius = radius,
                            UseAsSteering = steering,
                            UseAsTraction = traction,
                        };

                        AddChild(wheel, transform, root);
                        CopyMesh(mesh, $"WheelMesh_{location}", wheel);
                        CreateShape(mesh, $"WheelShape_{location}", wheel, disabled: true);
                    }

                    void CopyMesh(MeshInstance3D mesh, string name, Node3D parent)
                    {
                        var transform = mesh.GetLocalRootTransform();

                        mesh = (MeshInstance3D)mesh.Duplicate();
                        mesh.Name = name;
                        mesh.UniqueNameInOwner = true;
                        mesh.RemoveChildren(free: true);

                        AddChild(mesh, transform, parent);

                        var aabb = mesh.GetAabb();
                        var mass = aabb.Volume * MassMultiplier;
                        mesh.SetMeta("Aabb", aabb);
                        mesh.SetMeta("Mass", mass);
                        root.Mass += mass;
                        rootAabb = rootAabb is null ? aabb : rootAabb.Value.Merge(aabb);
                    }

                    void CreateShape(MeshInstance3D mesh, string name, Node3D parent, bool disabled = false)
                    {
                        var shape = new CollisionShape3D
                        {
                            Name = name,
                            Disabled = disabled,
                            UniqueNameInOwner = true,
                            Shape = mesh.Mesh.CreateConvexShape(clean: true, simplify: true),
                        };

                        AddChild(shape, mesh.GetLocalRootTransform(), parent);
                    }

                    void AddChild(Node3D node, in Transform3D transform, Node3D parent)
                    {
                        parent.AddChild(node, forceReadableName: true);
                        node.SetLocalRootTransform(transform);
                        node.Owner = root;
                    }
                }

                static string Humanize(in string name)
                    => name.Humanize(LetterCasing.Title).Replace(" ", "");
            }
        }
    }
}
#endif
