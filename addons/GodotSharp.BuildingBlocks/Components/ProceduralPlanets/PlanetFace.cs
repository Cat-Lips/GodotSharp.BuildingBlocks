using Godot;
using static Godot.Mesh;
using Array = Godot.Collections.Array;

namespace GodotSharp.BuildingBlocks
{
    [Tool]
    public partial class PlanetFace : MeshInstance3D
    {
        [Export] public Vector3 Normal { get; set; }

        public void GenerateMesh(PlanetData data)
        {
            var res = data.Resolution;
            var resMinus1 = res - 1;

            var vertex_count = res * res;
            var index_count = resMinus1 * resMinus1 * 6;

            var vertex_array = new Vector3[vertex_count];
            var uv_array = new Vector2[vertex_count];
            var normal_array = new Vector3[vertex_count];
            var index_array = new int[index_count];

            CalculateTriangles();
            CalculateNormals();

            var mesh_arrays = new Array();
            mesh_arrays.Resize((int)ArrayType.Max);
            mesh_arrays[(int)ArrayType.Vertex] = vertex_array.AsSpan();
            mesh_arrays[(int)ArrayType.Normal] = normal_array.AsSpan();
            mesh_arrays[(int)ArrayType.TexUV] = uv_array.AsSpan();
            mesh_arrays[(int)ArrayType.Index] = index_array.AsSpan();

            data.GetOrCreateBiomeTexture(out var biomeTexture);
            //App.CallDeferred(UpdateMesh);
            UpdateMesh();

            void CalculateTriangles()
            {
                var triIdx = 0;
                var axisA = new Vector3(Normal.Y, Normal.Z, Normal.X);
                var axisB = Normal.Cross(axisA);
                for (var x = 0; x < res; ++x)
                {
                    for (var y = 0; y < res; ++y)
                    {
                        var i = x + y * res;
                        var percent = new Vector2(x, y) / resMinus1;
                        var pointOnCube = Normal + (percent.X - .5f) * 2 * axisA + (percent.Y - .5f) * 2 * axisB;
                        var pointOnSphere = pointOnCube.Normalized();
                        var biomeGradient = data.GetBiomePercent(pointOnSphere);
                        var pointOnPlanet = data.GetPointOnPlanet(pointOnSphere);

                        vertex_array[i] = pointOnPlanet;
                        uv_array[i] = new(0, biomeGradient);

                        data.MinHeight = Math.Min(data.MinHeight, pointOnPlanet.Length());
                        data.MaxHeight = Math.Max(data.MaxHeight, pointOnPlanet.Length());

                        if (x != resMinus1 && y != resMinus1)
                        {
                            index_array[triIdx + 2] = i;
                            index_array[triIdx + 1] = i + res + 1;
                            index_array[triIdx] = i + res;

                            index_array[triIdx + 5] = i;
                            index_array[triIdx + 4] = i + 1;
                            index_array[triIdx + 3] = i + res + 1;

                            triIdx += 6;
                        }
                    }
                }
            }

            void CalculateNormals()
            {
                for (var a = 0; a < index_count; a += 3)
                {
                    var b = a + 1;
                    var c = a + 2;

                    var ab = vertex_array[index_array[b]] - vertex_array[index_array[a]];
                    var bc = vertex_array[index_array[c]] - vertex_array[index_array[b]];
                    var ca = vertex_array[index_array[a]] - vertex_array[index_array[c]];

                    var cross_ab_bc = ab.Cross(bc) * -1;
                    var cross_bc_ca = bc.Cross(ca) * -1;
                    var cross_ca_ab = ca.Cross(ab) * -1;

                    normal_array[index_array[a]] += cross_ab_bc + cross_bc_ca + cross_ca_ab;
                    normal_array[index_array[b]] += cross_ab_bc + cross_bc_ca + cross_ca_ab;
                    normal_array[index_array[c]] += cross_ab_bc + cross_bc_ca + cross_ca_ab;
                }

                for (var i = 0; i < normal_array.Length; ++i)
                    normal_array[i] = normal_array[i].Normalized();
            }

            void UpdateMesh()
            {
                var mesh = new ArrayMesh();
                mesh.AddSurfaceFromArrays(PrimitiveType.Triangles, mesh_arrays);

                Mesh = mesh;

                var material = (ShaderMaterial)MaterialOverride;
                material.SetShaderParameter("MinHeight", data.MinHeight);
                material.SetShaderParameter("MaxHeight", data.MaxHeight);
                material.SetShaderParameter("HeightColor", biomeTexture);
            }
        }
    }
}
