using System.Reflection;
using System.Runtime.CompilerServices;
using AsyncAwaitBestPractices;
using Godot;
using Godot.Collections;
using Humanizer;

namespace GodotSharp.BuildingBlocks
{
    public static class App
    {
        public static readonly string Name
            = ProjectSettings.GetSetting("application/config/name").AsString();

        public static void RunAsync(Action action) => Task.Run(action).SafeFireAndForget();
        public static CancellationTokenSource RunAsync(Action<CancellationToken> action)
        {
            var cs = new CancellationTokenSource();
            Task.Run(() => action(cs.Token)).SafeFireAndForget();
            return cs;
        }

        public static void CallDeferred(this GodotObject source, Action action)
        {
            Callable.From(() =>
            {
                if (GodotObject.IsInstanceValid(source))
                    action();
            }).CallDeferred();
        }

        public static void CallDeferred(this GodotObject source, CancellationToken ct, Action action, Action<bool> onComplete = null)
        {
            Callable.From(() =>
            {
                var invokeAction = GodotObject.IsInstanceValid(source) && !ct.IsCancellationRequested;
                if (invokeAction) action();
                onComplete?.Invoke(invokeAction);
            }).CallDeferred();
        }

        public static void RunParallel(params Action[] actions)
            => Parallel.ForEach(actions, action => action());

        public static void RunParallel<T>(Action<T> action, params T[] args)
            => Parallel.ForEach(args, action);

        public static PackedScene LoadScene<T>() where T : Node
            => GD.Load<PackedScene>(GetScenePath<T>());

        public static T InstantiateScene<T>() where T : Node
            => LoadScene<T>().Instantiate<T>();

        public static T InstantiateScene<T>(string name) where T : Node
        {
            var t = InstantiateScene<T>();
            t.Name = name;
            return t;
        }

        public static void ThrowIfError(Error error,
            [CallerFilePath] string path = null,
            [CallerMemberName] string member = null,
            [CallerArgumentExpression(nameof(error))] string expr = null)
        {
            if (error is Error.Ok) return;
            var msg = $"[{Path.GetFileNameWithoutExtension(path)}.{member}] {error.Humanize()}: {expr}";
            GD.PushError(msg); throw new InvalidOperationException(msg);
        }

        public static T Load<T>(string resource, [CallerFilePath] string path = null) where T : Resource
        {
            path = Path.GetDirectoryName(path);
            path = ProjectSettings.LocalizePath(path);
            return GD.Load<T>($"{path}/{resource}");
        }

        public static ShaderMaterial LoadShader<T>([CallerFilePath] string path = null)
            => new() { Shader = Load<Shader>(typeof(T).Name + ".gdshader", path) };

        public static string GetScriptPath<T>() where T : GodotObject
            => typeof(T).GetCustomAttribute<ScriptPathAttribute>(false).Path;

        public static string GetScenePath<T>() where T : GodotObject
            => GetScriptPath<T>().Replace(".cs", ".tscn");

        public static Dictionary PropertyConfig(string name, Variant.Type type, params PropertyUsageFlags[] usage) => new()
        {
            { "name", name },
            { "type", (int)type },
            { "usage", (int)usage.Aggregate((x, y) => x | y) },
        };

        // https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core
        public static readonly int HashCode = Name.GetDeterministicHashCode();

        private static int GetDeterministicHashCode(this string str)
        {
            unchecked
            {
                var hash1 = (5381 << 16) + 5381;
                var hash2 = hash1;

                for (var i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + hash2 * 1566083941;
            }
        }
    }
}
