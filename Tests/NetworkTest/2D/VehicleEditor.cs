using System.Diagnostics;
using Godot;
using GodotSharp.BuildingBlocks;
using Humanizer;
using static Godot.ResourceSaver;

namespace NetworkTest
{
    [SceneTree]
    public partial class VehicleEditor : CanvasLayer
    {
        private const string SaveFormat = "tres";
        //private const string SaveFormat = "res";

        [Notify]
        public Vehicle Vehicle
        {
            get => _vehicle.Get();
            set => _vehicle.Set(value);
        }

        public VehicleEditor()
        {
            _vehicle.Changed += OnVehicleChanged;

            void OnVehicleChanged()
            {
                if (Vehicle is null)
                {
                    Visible = false;
                    Editor.Resource = null;
                    Title.Text = string.Empty;
                }
                else
                {
                    Visible = true;
                    Editor.Resource = Vehicle.Config;
                    Title.Text = $"{Vehicle.Name.Humanize(LetterCasing.Title)} (Mass: {Math.Round(Vehicle.Mass, 3)})";
                }
            }
        }

        [GodotOverride]
        private void OnReady()
        {
            InitEditor();
            InitButtons();

            void InitEditor()
            {
                ShowEditor();
                Toggle.Pressed += ShowEditor;

                void ShowEditor()
                {
                    if (Toggle.ButtonPressed)
                    {
                        Toggle.Flat = false;
                        Toggle.Text = "HIDE";
                        Content.Visible = true;
                    }
                    else
                    {
                        Toggle.Flat = true;
                        Toggle.Text = "Edit Vehicle";
                        Content.Visible = false;
                    }
                }
            }

            void InitButtons()
            {
                InitButtons();
                VehicleChanged += InitButtons;
                SavedConfigs.FilesChanged += InitButtons;

                Save.Pressed += OnSave;
                Reset.Pressed += OnReset;
                Revert.Pressed += OnRevert;
                Load.GetPopup().OnItemSelected(OnLoad);

                void InitButtons()
                {
                    Load.GetPopup().SetMenuItems(SavedConfigs.Files);
                    Load.Disabled = SavedConfigs.Files.Length is 0;
                    Revert.Disabled = !SavedConfigFilesContains(Vehicle?.Name);

                    bool SavedConfigFilesContains(string name)
                    {
                        return name is not null && SavedConfigs.Files
                            .Select(Path.GetFileNameWithoutExtension)
                            .Contains(name);
                    }
                }

                void OnLoad(string file)
                {
                    Debug.Assert(Vehicle is not null);
                    if (Vehicle is null) return;

                    Vehicle.Config = LoadConfig(SavedConfigs.Path, file);
                    Editor.Resource = Vehicle.Config;
                }

                void OnSave()
                {
                    Debug.Assert(Vehicle is not null);
                    if (Vehicle is null) return;

                    SaveConfig(Vehicle.Config, SavedConfigs.Path, Vehicle.Name);
                    SavedConfigs.Refresh();
                }

                void OnReset()
                {
                    Debug.Assert(Vehicle is not null);
                    if (Vehicle is null) return;

                    Vehicle.Config = null;
                    Editor.Resource = Vehicle.Config;
                }

                void OnRevert()
                {
                    Debug.Assert(Vehicle is not null);
                    if (Vehicle is null) return;

                    Vehicle.Config = LoadConfig(SavedConfigs.Path, Vehicle.Name);
                    Editor.Resource = Vehicle.Config;
                }
            }

            static VehicleConfig LoadConfig(string path, string file)
            {
                Debug.Assert(DirAccess.DirExistsAbsolute(path));

                file = file.EndsWith(SaveFormat) ? $"{path}/{file}" : $"{path}/{file}.{SaveFormat}";
                var config = ResourceLoader.Load<VehicleConfig>(
                    file, cacheMode: ResourceLoader.CacheMode.Ignore);

                if (config is null)
                    Error.Failed.ThrowOnError("Error loading config", file);

                return config;
            }

            static void SaveConfig(VehicleConfig config, string path, string name)
            {
                var err = DirAccess.MakeDirRecursiveAbsolute(path);

                if (err != Error.Ok)
                    err.ThrowOnError("Error creating folder", path);

                Debug.Assert(!name.EndsWith(SaveFormat));
                var file = $"{path}/{name}.{SaveFormat}";
                err = ResourceSaver.Save(config, file, SaverFlags.ChangePath);

                if (err != Error.Ok)
                    err.ThrowOnError("Error saving config", file);
            }
        }

        public override partial void _Ready();
    }
}
