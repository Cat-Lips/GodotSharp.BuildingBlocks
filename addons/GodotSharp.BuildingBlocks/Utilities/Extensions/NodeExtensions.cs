using System.Diagnostics;
using Godot;
using Humanizer;

namespace GodotSharp.BuildingBlocks
{
    public static class NodeExtensions
    {
        #region Child Management

        public static IEnumerable<T> GetChildren<T>(this Node node) where T : Node
            => node.GetChildren().OfType<T>();

        public static void ForEachChild(this Node node, Action<Node> action)
            => node.GetChildren().ForEach(action);

        public static void ForEachChild<T>(this Node node, Action<T> action) where T : Node
            => node.GetChildren<T>().ForEach(action);

        public static IEnumerable<Node> RecurseChildren(this Node node)
            => node.SelectRecursive(x => x.GetChildren());

        public static IEnumerable<T> RecurseChildren<T>(this Node node) where T : Node
            => node.RecurseChildren().OfType<T>();

        public static void RecurseChildren(this Node node, Action<Node> action)
            => node.RecurseChildren().ForEach(action);

        public static void RecurseChildren<T>(this Node node, Action<T> action) where T : Node
            => node.RecurseChildren<T>().ForEach(action);

        public static void RemoveChildren(this Node node, bool free)
            => node.GetChildren().ForEach(x => node.RemoveChild(x, free));

        public static void RemoveChildren<T>(this Node node, bool free) where T : Node
            => node.GetChildren<T>().ForEach(x => node.RemoveChild(x, free));

        public static void RemoveChild(this Node parent, Node node, bool free)
        {
            parent.RemoveChild(node);
            if (free) node.QueueFree();
        }

        public static void DetachFromParent(this Node node, bool free = false)
        {
            node.GetParent()?.RemoveChild(node);
            if (free) node.QueueFree();
        }

        public static Aabb GetAabb(this Node3D node)
        {
            return node.RecurseChildren<VisualInstance3D>()
                .Select(x => x.GetAabb())
                .Aggregate((a, b) => a.Merge(b));
        }

        #endregion

        #region Transform Management

        public static Transform3D GetLocalRootTransform(this Node3D node)
        {
            var parent = node.GetParent<Node3D>();
            var result = node.TopLevel || parent is null
                ? node.Transform
                : (parent.GetLocalRootTransform() * node.Transform);
            return result;
        }

        public static void SetLocalRootTransform(this Node3D node, in Transform3D transform)
        {
            var parent = node.GetParent<Node3D>();
            node.Transform = parent is null ? transform
                : parent.GetLocalRootTransform().AffineInverse() * transform;
        }

        public static Transform3D GlobalTransform(this IEnumerable<Node3D> nodes)
        {
            Transform3D? transform = null;
            foreach (var node in nodes)
            {
                if (transform is null) transform = node.GlobalTransform;
                else transform *= node.GlobalTransform;
            }

            return transform ?? Transform3D.Identity;
        }

        public static Transform3D LookingAt(this Transform3D transform, Vector3 target)
            => transform.LookingAt(target, Vector3.Up);

        #endregion

        #region Readability

        #region Input

        public static void SetInputAsHandled(this Node node)
            => node.GetViewport().SetInputAsHandled();

        public static bool On(this Node _, bool condition, params Action[] actions)
        {
            if (condition)
            {
                actions.ForEach(x => x());
                return true;
            }

            return false;
        }

        #region ActionPressed

        public static bool Handle(this Node node, InputEvent e, StringName action, Action _action)
            => node.On(e.IsActionPressed(action), node.SetInputAsHandled, _action);

        public static bool Handle(this Node node, bool condition, InputEvent e, StringName action, Action _action)
            => node.On(condition && e.IsActionPressed(action), node.SetInputAsHandled, _action);

        public static bool Handle(this Node node, InputEvent e, StringName action, bool condition, Action _action)
            => node.Handle(condition, e, action, _action);

        public static bool Handle(this Node node, InputEvent e, StringName action, Action _action, bool condition)
            => node.Handle(condition, e, action, _action);

        #endregion

        #region IsMatch

        public static bool Handle(this Node node, InputEvent e, InputEvent match, Action _action)
            => node.On(e.IsMatch(match, false), node.SetInputAsHandled, _action);

        public static bool Handle(this Node node, bool condition, InputEvent e, InputEvent match, Action _action)
            => node.On(condition && e.IsMatch(match, false), node.SetInputAsHandled, _action);

        public static bool Handle(this Node node, InputEvent e, InputEvent match, bool condition, Action _action)
            => node.Handle(condition, e, match, _action);

        public static bool Handle(this Node node, InputEvent e, InputEvent match, Action _action, bool condition)
            => node.Handle(condition, e, match, _action);

        #endregion

        #endregion

        #region Multiplayer

        public static void AddSpawnableScene<T>(this MultiplayerSpawner source) where T : Node
            => source.AddSpawnableScene(App.GetScenePath<T>());

        public static void SpawnScene(this MultiplayerSpawner source, Node scene)
            => source.GetSpawnNode().AddChild(scene, forceReadableName: true);

        public static Node GetSpawnNode(this MultiplayerSpawner source)
            => source.GetNode(source.SpawnPath);

        public static void DespawnAll(this MultiplayerSpawner source, bool free)
        {
            var spawnParent = source.GetSpawnNode();
            SpawnedItems().ForEach(x => spawnParent.RemoveChild(x, free));

            IEnumerable<Node> SpawnedItems()
            {
                var spawnableScenes = new HashSet<string>(source._SpawnableScenes);
                return spawnParent.GetChildren().Where(x => spawnableScenes.Contains(x.SceneFilePath));
            }
        }

        public static bool SinglePlayerMode(this Node node)
            => node.Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer or null;

        public static bool MultiplayerServer(this Node node)
            => !node.SinglePlayerMode() && node.Multiplayer.IsServer();

        #endregion

        #region Themes

        public static void SetFontColor(this Label label, in Color color)
            => label.AddThemeColorOverride("font_color", color);

        public static void ResetFontColor(this Label label)
            => label.RemoveThemeColorOverride("font_color");

        #endregion

        public static string Humanize(this StringName source, LetterCasing casing)
            => source.ToString().Humanize(casing);

        public static void Enabled(this BaseButton node, bool enabled = true)
            => node.Disabled = !enabled;

        #endregion

        #region DefaultIfNull

        public static Noise DefaultIfNull(this Noise noise)
        {
            return noise ?? new FastNoiseLite
            {
                Seed = 7, //Random.Shared.Next(),
                NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
            };
        }

        public static Gradient DefaultIfNull(this Gradient gradient)
        {
            return gradient ?? new Gradient
            {
                Colors = TerrainType.DefaultTerrainTypes().Select(x => x.Color).ToArray(),
                Offsets = TerrainType.DefaultTerrainTypes().Select(x => x.Height).ToArray(),
            };
        }

        public static Texture2D NoiseIfNull(this Texture2D texture, int width = 100, int height = 100)
            => texture ?? DefaultIfNull(null, width, height);

        public static NoiseTexture2D DefaultIfNull(this NoiseTexture2D texture, int width = 100, int height = 100)
        {
            return texture ?? new NoiseTexture2D
            {
                Width = width,
                Height = height,
                Seamless = true,
                //AsNormalMap = true,
                //GenerateMipmaps = false,
                Noise = DefaultIfNull((Noise)null),
                ColorRamp = DefaultIfNull((Gradient)null),
            };
        }

        #endregion

        #region RayCasting

        public static bool CastRay(this Node3D node, in Vector3 direction, float distance, Action collision) => node.CastRay(node.GlobalPosition, direction, distance, collision);
        public static bool CastRay(this Node3D node, in Vector3 rayStart, in Vector3 direction, float distance, Action collision)
        {
            if (RayCollision(node, rayStart, direction, distance, out var _))
            {
                node.CallDeferred(collision);
                return true;
            }

            return false;
        }

        public static bool CastRay(this Node3D node, in Vector3 direction, float distance, Action<CollisionObject3D> collision) => node.CastRay(node.GlobalPosition, direction, distance, collision);
        public static bool CastRay(this Node3D node, Vector3 rayStart, in Vector3 direction, float distance, Action<CollisionObject3D> collision)
        {
            if (RayCollision(node, rayStart, direction, distance, out var result))
            {
                var collisionObject = (CollisionObject3D)result["collider"].AsGodotObject();
                node.CallDeferred(() => collision(collisionObject));
                return true;
            }

            return false;
        }

        public static bool CastRay(this Node3D node, in Vector3 direction, float distance, Action<CollisionObject3D, Vector3, Vector3> collision) => node.CastRay(node.GlobalPosition, direction, distance, collision);
        public static bool CastRay(this Node3D node, Vector3 rayStart, in Vector3 direction, float distance, Action<CollisionObject3D, Vector3, Vector3> collision)
        {
            if (RayCollision(node, rayStart, direction, distance, out var result))
            {
                var surfaceNormal = result["normal"].AsVector3();
                var intersectPoint = result["position"].AsVector3();
                var collisionObject = (CollisionObject3D)result["collider"].AsGodotObject();

                node.CallDeferred(() => collision(collisionObject, intersectPoint, surfaceNormal));
                return true;
            }

            return false;
        }

        public static bool CastRay(this Node3D node, in Vector3 direction, float distance, Action<CollisionObject3D, CollisionShape3D, Vector3, Vector3> collision) => node.CastRay(node.GlobalPosition, direction, distance, collision);
        public static bool CastRay(this Node3D node, Vector3 rayStart, in Vector3 direction, float distance, Action<CollisionObject3D, CollisionShape3D, Vector3, Vector3> collision)
        {
            if (RayCollision(node, rayStart, direction, distance, out var result))
            {
                var surfaceNormal = result["normal"].AsVector3();
                var intersectPoint = result["position"].AsVector3();
                var collisionObject = (CollisionObject3D)result["collider"].AsGodotObject();

                var shapeIndex = result["shape"].AsInt32();
                var shapeOwner = collisionObject.ShapeFindOwner(shapeIndex);
                var collisionShape = (CollisionShape3D)collisionObject.ShapeOwnerGetOwner(shapeOwner);

                node.CallDeferred(() => collision(collisionObject, collisionShape, intersectPoint, surfaceNormal));
                return true;
            }

            return false;
        }

        private static bool RayCollision(Node3D node, in Vector3 rayStart, in Vector3 direction, float distance, out Godot.Collections.Dictionary result)
        {
            Debug.Assert(Engine.IsInPhysicsFrame());

            var rayEnd = rayStart + direction * distance;
            var query = PhysicsRayQueryParameters3D.Create(rayStart, rayEnd,
                exclude: node is CollisionObject3D collider ? new() { collider.GetRid() } : null);
            result = node.GetWorld3D().DirectSpaceState.IntersectRay(query);
            return result.Count is not 0;
        }

        #endregion

        #region Str

        public static string Str(this MultiplayerSynchronizer sync)
        {
            var config = sync.ReplicationConfig;
            var props = config.GetProperties().ToDictionary(x => x, x => new { Spawn = config.PropertyGetSpawn(x), Sync = config.PropertyGetSync(x) });
            return $"{sync.RootPath} [{string.Join("|", props.Select(x => $"{x.Key} {x.Value}"))}]";
        }

        #endregion
    }
}
