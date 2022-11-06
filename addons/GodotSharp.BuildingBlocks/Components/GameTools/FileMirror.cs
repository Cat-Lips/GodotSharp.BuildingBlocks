using Godot;

namespace GodotSharp.BuildingBlocks
{
    [SceneTree]
    public partial class FileMirror : Node
    {
        private float interval;

        [Export, Notify]
        public string Path
        {
            get => _path.Get();
            set => _path.Set(value);
        }

        [Notify]
        public string[] Files
        {
            get => _files.Get();
            private set => _files.Set(value ?? Array.Empty<string>());
        }

        [Export]
        public float RefreshInterval { get; set; }

        public FileMirror()
        {
            Files = null;
            RefreshInterval = 1;
            _path.Changed += Refresh;
        }

        public void Refresh()
        {
            if (interval > 0)
                interval = 0;

            if (string.IsNullOrWhiteSpace(Path))
                return;

            var mirror = Files;
            var actual = GetFiles();

            if (mirror.SequenceEqual(actual))
                return;

            Files = actual;

            string[] GetFiles()
                => DirAccess.DirExistsAbsolute(Path) ? DirAccess.GetFilesAt(Path) : Array.Empty<string>();
        }

        [GodotOverride]
        private void OnProcess(double delta)
        {
            if (interval < 0 || (interval += (float)delta) < RefreshInterval)
                return;

            Refresh();
        }

        public override partial void _Process(double delta);
    }
}
