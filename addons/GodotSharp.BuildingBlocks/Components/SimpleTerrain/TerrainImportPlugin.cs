#if TOOLS
using Godot;
using Godot.Collections;
using Humanizer;
using FileAccess = Godot.FileAccess;

namespace GodotSharp.BuildingBlocks
{
    [Tool]
    public partial class TerrainImportPlugin : EditorImportPlugin
    {
        private static readonly Vector3 Normal = Vector3.Up;

        public override Error _Import(string sourceFile, string savePath, Dictionary options, Array<string> platformVariants, Array<string> genFiles)
        {
            GD.Print($"{_GetVisibleName()}: {sourceFile}");
            LoadImageData(out var width, out var height, out var data);
            GD.Print($" - width: {width}, height: {height}, detail: {data.Length}");
            CreateShape(out var shape);
            CreateMesh(out var mesh);
            PackScene(out var scene);
            SaveScene(out var error);
            return error;

            void LoadImageData(out int width, out int height, out float[] data)
            {
                var file = FileAccess.Open(sourceFile, FileAccess.ModeFlags.Read);
                var error = FileAccess.GetOpenError();
                if (file is null) throw new FileNotFoundException(error.Humanize(), sourceFile);

                var bytes = file.GetBuffer((long)file.GetLength());
                file.Dispose();

                var image = new Image();
                var ext = Path.GetExtension(sourceFile).TrimStart('.').ToLower();
                switch (ext)
                {
                    case "bmp":
                        image.LoadBmpFromBuffer(bytes);
                        break;
                    case "jpg":
                        image.LoadJpgFromBuffer(bytes);
                        break;
                    case "png":
                        image.LoadPngFromBuffer(bytes);
                        break;
                    case "tga":
                        image.LoadTgaFromBuffer(bytes);
                        break;
                    default:
                        throw new NotImplementedException($"Unrecognised extension {ext}");
                }

                //image.ResizeToPo2();
                width = image.GetWidth();
                height = image.GetHeight();
                data = image.GetData().Select(x => (float)x).ToArray();
            }

            void CreateShape(out Shape3D shape)
            {
                shape = new HeightMapShape3D
                {
                    MapWidth = width,
                    MapDepth = height,
                    MapData = data,
                };
            }

            void CreateMesh(out Mesh mesh)
            {
                var array_mesh = shape.GetDebugMesh();
                var material = (StandardMaterial3D)array_mesh.SurfaceGetMaterial(0);
                GD.Print("Material: ", material);
                material.AlbedoColor = Colors.LawnGreen;
                mesh = array_mesh;
            }

            //void CreateMesh(out Mesh mesh)
            //{
            //    var vertex_count = (width - 1) * (height - 1) * 6;

            //    var uv_array = new Vector2[vertex_count];
            //    var normal_array = new Vector3[vertex_count];
            //    var vertex_array = new Vector3[vertex_count];

            //    GenerateArrays();
            //    mesh = BuildMesh();

            //    void GenerateArrays()
            //    {
            //        var uv_idx = -1;
            //        var normal_idx = -1;
            //        var vertex_idx = -1;

            //        for (var x = 0; x < width - 1; ++x)
            //        {
            //            for (var y = 0; y < height - 1; ++y)
            //            {
            //                CreateQuad(x, y);
            //            }
            //        }

            //        Debug.Assert(uv_idx == vertex_count - 1);
            //        Debug.Assert(normal_idx == vertex_count - 1);
            //        Debug.Assert(vertex_idx == vertex_count - 1);

            //        void CreateQuad(int x, int y)
            //        {
            //            AddTriangle(
            //                new Vector3(x, GetHeight(x, y), -y),
            //                new Vector3(x, GetHeight(x, y + 1), -y - 1),
            //                new Vector3(x + 1, GetHeight(x + 1, y + 1), -y - 1));

            //            AddTriangle(
            //                new Vector3(x, GetHeight(x, y), -y),
            //                new Vector3(x + 1, GetHeight(x + 1, y + 1), -y - 1),
            //                new Vector3(x + 1, GetHeight(x + 1, y), -y));

            //            void AddTriangle(in Vector3 v1, in Vector3 v2, in Vector3 v3)
            //            {
            //                vertex_array[++vertex_idx] = v1;
            //                vertex_array[++vertex_idx] = v2;
            //                vertex_array[++vertex_idx] = v3;

            //                uv_array[++uv_idx] = new(v1.x, -v1.z);
            //                uv_array[++uv_idx] = new(v2.x, -v2.z);
            //                uv_array[++uv_idx] = new(v3.x, -v3.z);

            //                var side1 = v2 - v1;
            //                var side2 = v2 - v3;
            //                var normal = side1.Cross(side2);

            //                normal_array[++normal_idx] = normal;
            //                normal_array[++normal_idx] = normal;
            //                normal_array[++normal_idx] = normal;
            //            }

            //            float GetHeight(int x, int y)
            //                => data[x * width + y];
            //        }
            //    }

            //    Mesh BuildMesh()
            //    {
            //        var st = new SurfaceTool();
            //        st.Begin(Mesh.PrimitiveType.Triangles);

            //        for (var i = 0; i < vertex_count; ++i)
            //        {
            //            st.SetUv(uv_array[i]);
            //            st.SetNormal(normal_array[i]);
            //            st.AddVertex(vertex_array[i]);
            //        }

            //        return st.Commit();
            //    }
            //}

            void PackScene(out PackedScene scene)
            {
                var root = new StaticBody3D
                {
                    Name = "Terrain",
                    TopLevel = true,
                };

                var collider = new CollisionShape3D
                {
                    Name = "Shape",
                    Shape = shape,
                };

                var visual = new MeshInstance3D
                {
                    Name = "Mesh",
                    Mesh = mesh,
                };

                root.AddChild(visual); visual.Owner = root;
                root.AddChild(collider); collider.Owner = root;

                scene = new PackedScene();
                scene.Pack(root);
            }

            void SaveScene(out Error error)
                => error = ResourceSaver.Save(scene, $"{savePath}.{_GetSaveExtension()}");
        }

        public override string _GetImporterName() => "GodotSharp.BuildingBlocks.TerrainImportPlugin";
        public override string _GetVisibleName() => "Terrain";
        public override string[] _GetRecognizedExtensions() => new[] { "bmp", "jpg", "png", "tga", };
        public override string _GetSaveExtension() => "scn";
        public override string _GetResourceType() => nameof(PackedScene);
        public override int _GetPresetCount() => 0;
        public override int _GetImportOrder() => 0;
        public override Array<Dictionary> _GetImportOptions(string path, int presetIndex) => new();
        public override bool _GetOptionVisibility(string path, StringName optionName, Dictionary options) => true;
    }
}
#endif
