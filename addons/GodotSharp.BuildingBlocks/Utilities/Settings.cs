using Godot;

namespace GodotSharp.BuildingBlocks
{
    public class Settings<T> : Settings
    {
        public Settings(bool autoSave = true)
            : base(typeof(T).Name, autoSave) { }
    }

    public class Settings
    {
        private readonly ConfigFile config = new();

        private readonly string path;
        private readonly bool autosave;

        public Settings(string name, bool _autosave = true)
        {
            autosave = _autosave;
            path = $"user://{name}.cfg";

            Load();
        }

        public T Get<[MustBeVariant] T>(string key) => Get<T>("", key);
        public T Get<[MustBeVariant] T>(string section, string key)
            => config.GetValue(section, key).As<T>();

        public void Set<[MustBeVariant] T>(string key, T value) => Set("", key, value);
        public void Set<[MustBeVariant] T>(string section, string key, T value)
        {
            config.SetValue(section, key, Variant.From(value));
            if (autosave) Save();
        }

        public bool TryGet<[MustBeVariant] T>(string key, out T value) => TryGet("", key, out value);
        public bool TryGet<[MustBeVariant] T>(string section, string key, out T value)
        {
            if (config.HasSectionKey(section, key))
            {
                value = Get<T>(section, key);
                return true;
            }

            value = default;
            return false;
        }

        public T TryGet<[MustBeVariant] T>(string key) => TryGet<T>("", key);
        public T TryGet<[MustBeVariant] T>(string section, string key)
            => TryGet<T>(section, key, out var value) ? value : value;

        public void Load()
            //=> config.LoadEncryptedPass(Path, App.Name);
            => config.Load(path);

        public void Save()
            //=> config.SaveEncryptedPass(Path, App.Name);
            => config.Save(path);

        public void Clear()
            => config.Clear();

        public string[] Keys() => Keys("");
        public string[] Keys(string section)
            => config.GetSectionKeys(section);
    }
}
