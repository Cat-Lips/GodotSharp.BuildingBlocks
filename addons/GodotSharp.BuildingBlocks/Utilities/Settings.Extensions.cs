using Godot;

namespace GodotSharp.BuildingBlocks
{
    public static class SettingsExtensions
    {
        public static T Get<[MustBeVariant] T>(this Settings source, Node key) => source.Get<T>("", key.Name);
        public static T Get<[MustBeVariant] T>(this Settings source, Node section, Node key) => source.Get<T>(section.Name, key.Name);

        public static void Set<[MustBeVariant] T>(this Settings source, Node key, T value) => source.Set("", key.Name, value);
        public static void Set<[MustBeVariant] T>(this Settings source, Node section, Node key, T value) => source.Set(section.Name, key.Name, value);

        public static bool TryGet<[MustBeVariant] T>(this Settings source, Node key, out T value) => source.TryGet("", key.Name, out value);
        public static bool TryGet<[MustBeVariant] T>(this Settings source, Node section, Node key, out T value) => source.TryGet(section.Name, key.Name, out value);

        public static T TryGet<[MustBeVariant] T>(this Settings source, Node key) => source.TryGet<T>("", key.Name);
        public static T TryGet<[MustBeVariant] T>(this Settings source, Node section, Node key) => source.TryGet<T>(section.Name, key.Name);
    }
}
