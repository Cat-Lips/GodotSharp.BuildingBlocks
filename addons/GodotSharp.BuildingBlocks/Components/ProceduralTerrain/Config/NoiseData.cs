using System.Diagnostics;
using Godot;

namespace GodotSharp.BuildingBlocks
{
    public enum NormaliseMode
    {
        Local,
        Global,
    }

    [Tool]
    public partial class NoiseData : Resource
    {
        public int _ChunkSize => Seamless ? 239 : 241;
        private const int _NoiseSize = 241; // Seamless ? _ChunkSize + 2 : _ChunkSize;
        private static readonly int _HalfNoise = Mathf.CeilToInt(_NoiseSize * .5f);

        [Export, Notify]
        public NormaliseMode NormaliseMode
        {
            get => _normaliseMode.Get();
            set => _normaliseMode.Set(value);
        }

        [Export(PropertyHint.Range, "0,1,"), Notify]
        public float GlobalHeightRangeAdjustment
        {
            get => _globalHeightRangeAdjustment.Get();
            set => _globalHeightRangeAdjustment.Set(Math.Clamp(value, 0, 1));
        }

        [Export, Notify]
        public int Seed
        {
            get => _seed.Get();
            set => _seed.Set(value);
        }

        [Export, Notify]
        public float Scale
        {
            get => _scale.Get();
            set => _scale.Set(Math.Clamp(value, float.Epsilon, float.MaxValue));
        }

        [Export, Notify]
        public int Octaves
        {
            get => _octaves.Get();
            set => _octaves.Set(Math.Clamp(value, 1, int.MaxValue));
        }

        [Export, Notify]
        public float Lacunarity
        {
            get => _lacunarity.Get();
            set => _lacunarity.Set(Math.Clamp(value, 1, float.MaxValue));
        }

        [Export(PropertyHint.Range, "0,1,"), Notify]
        public float Persistance
        {
            get => _persistance.Get();
            set => _persistance.Set(Math.Clamp(value, 0, 1));
        }

        [Export, Notify]
        public Vector2I Offset
        {
            get => _offset.Get();
            set => _offset.Set(value);
        }

        [Export, Notify]
        public bool Seamless
        {
            get => _seamless.Get();
            set => _seamless.Set(value);
        }

        [Export, Notify]
        public bool FlatShaded
        {
            get => _flatShaded.Get();
            set => _flatShaded.Set(value);
        }

        public NoiseData()
            => ResourceName = nameof(NoiseData);

        public static NoiseData Default() => new()
        {
            Seed = 7,
            Scale = .5f,
            Octaves = 4,
            Lacunarity = 2,
            Persistance = .5f,
            Offset = Vector2I.Zero,
            NormaliseMode = NormaliseMode.Global,
            GlobalHeightRangeAdjustment = 0.35134345f,
            Seamless = false,
            FlatShaded = false,
        };

        public NoiseData Copy() => new()
        {
            Seed = Seed,
            Scale = Scale,
            Offset = Offset,
            Octaves = Octaves,
            Lacunarity = Lacunarity,
            Persistance = Persistance,
            NormaliseMode = NormaliseMode,
            GlobalHeightRangeAdjustment = GlobalHeightRangeAdjustment,
            Seamless = Seamless,
            FlatShaded = FlatShaded,
        };

        private static readonly Noise PerlinNoise = new FastNoiseLite
        {
            NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
        };

        public float[,] GenerateNoiseMap(in Vector2I center, float[,] overlay,
            out float normalisationOfLowestPoint, out float normalisationOfHighestPoint)
        {
            var size = _NoiseSize;
            var halfSize = _HalfNoise;

            var offset = center + Offset;
            var noiseMap = new float[size, size];

            var minNoiseHeight = float.MaxValue;
            var maxNoiseHeight = float.MinValue;
            var maxHeightRange = 0f;

            CreateNoiseMap();
            NormaliseNoiseMap();

            normalisationOfLowestPoint = minNoiseHeight / -maxHeightRange;
            normalisationOfHighestPoint = maxNoiseHeight / maxHeightRange;

            return noiseMap;

            void CreateNoiseMap()
            {
                RandomiseOctaveOffsets(out var octaveOffsets);

                for (var x = 0; x < size; ++x)
                {
                    for (var y = 0; y < size; ++y)
                    {
                        float amplitude = 1;
                        float frequency = 1;
                        float noiseHeight = 0;

                        for (var i = 0; i < Octaves; ++i)
                        {
                            var sx = (x - halfSize + octaveOffsets[i].X) / Scale * frequency;
                            var sy = (y - halfSize + octaveOffsets[i].Y) / Scale * frequency;

                            var noiseValue = PerlinNoise.GetNoise2D(sx, sy);
                            Debug.Assert(noiseValue is >= -1 and <= 1);
                            noiseHeight += noiseValue * amplitude;

                            amplitude *= Persistance;
                            frequency *= Lacunarity;
                        }

                        minNoiseHeight = Math.Min(minNoiseHeight, noiseHeight);
                        maxNoiseHeight = Math.Max(maxNoiseHeight, noiseHeight);

                        noiseMap[x, y] = overlay is null ? noiseHeight
                            : noiseHeight - overlay[x, y];
                    }
                }

                void RandomiseOctaveOffsets(out Vector2I[] octaveOffsets)
                {
                    const int RandRange = 100000;

                    var rng = new Random(Seed);
                    octaveOffsets = new Vector2I[Octaves];

                    float amplitude = 1;
                    for (var i = 0; i < Octaves; ++i)
                    {
                        var offsetX = rng.Next(-RandRange, RandRange) + offset.X;
                        var offsetY = rng.Next(-RandRange, RandRange) + offset.Y;
                        octaveOffsets[i] = new(offsetX, offsetY);

                        maxHeightRange += amplitude;
                        amplitude *= Persistance;
                    }
                }
            }

            void NormaliseNoiseMap()
            {
                var adjustedHeightRange = maxHeightRange * GlobalHeightRangeAdjustment;

                for (var x = 0; x < size; ++x)
                {
                    for (var y = 0; y < size; ++y)
                    {
                        var noiseHeight = noiseMap[x, y];
                        var normalisedHeight = NormaliseMode is NormaliseMode.Local
                            ? Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseHeight)
                            : Mathf.InverseLerp(-adjustedHeightRange, adjustedHeightRange, noiseHeight);
                        noiseMap[x, y] = normalisedHeight;
                    }
                }
            }
        }

        public float[,] GenerateIslandOverlay()
        {
            var size = _NoiseSize;
            float sizeF = size;

            var islandOverlay = new float[size, size];

            for (var x = 0; x < size; ++x)
            {
                for (var y = 0; y < size; ++y)
                {
                    var value = Math.Max(
                        Math.Abs(x / sizeF * 2 - 1),
                        Math.Abs(y / sizeF * 2 - 1));
                    islandOverlay[x, y] = EvaluateDropOff(value);
                }
            }

            return islandOverlay;

            static float EvaluateDropOff(float value)
            {
                var a = 3f;
                var b = 2.2f;

                var v = Mathf.Pow(value, a);
                return v / (v + Mathf.Pow(b - b * value, a));
            }
        }

        public Image CreateImage(float[,] noiseMap, Func<float, Color> getColor = null)
        {
            Debug.Assert(noiseMap.GetLength(0) == _NoiseSize);
            Debug.Assert(noiseMap.GetLength(1) == _NoiseSize);

            var size = _NoiseSize;
            var image = Image.Create(size, size, false, Image.Format.Rgb8);

            getColor ??= GetNoiseColor;
            for (var x = 0; x < size; ++x)
            {
                for (var y = 0; y < size; ++y)
                {
                    var gradient = noiseMap[x, y];
                    var color = getColor(gradient);
                    image.SetPixel(x, y, color);
                }
            }

            return image;

            static Color GetNoiseColor(float gradient)
                => new(gradient, gradient, gradient);
        }

        public Texture2D CreateTexture(Image image)
        {
            var texture = new ImageTexture();
            texture.SetImage(image);
            return texture;
        }

        public Material CreateMaterial(Texture2D texture)
        {
            return new StandardMaterial3D
            {
                AlbedoTexture = texture,
                //TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                TextureRepeat = false,
            };
        }

        public Mesh CreatePlane(Material material)
        {
            var size = _NoiseSize;

            return new PlaneMesh
            {
                Size = new(size, size),
                Material = material
            };
        }

        public float GetHeight(float noise, float heightMultiplier, Curve heightCurve)
        {
            return noise > heightCurve.MaxValue
                ? noise *= heightMultiplier // TODO:  Extrapolate
                : heightCurve.Sample(noise) * heightMultiplier;
        }

        public (Mesh Mesh, Shape3D Shape) CreateMesh(float[,] noiseMap, float heightMultiplier, Curve heightCurve, Material material, int lod)
        {
            Debug.Assert(lod is >= 0 and <= 6);
            CreateTriangleArrays(out var vertices, out var triangles, out var normals, out var uvs, out var heightMap);
            return (CreateMesh(vertices, triangles, normals, uvs), lod is 0 ? CreateShape(heightMap) : null);

            void CreateTriangleArrays(out Vector3[] vertices, out int[] triangles, out Vector3[] normals, out Vector2[] uvs, out float[] heightMap)
            {
                if (Seamless) CreateSeamlessTriangleArrays(out vertices, out triangles, out normals, out uvs, out heightMap);
                else CreateTriangleArrays(out vertices, out triangles, out normals, out uvs, out heightMap);

                void CreateTriangleArrays(out Vector3[] vertices, out int[] triangles, out Vector3[] normals, out Vector2[] uvs, out float[] heightMap)
                {
                    Debug.Assert(noiseMap.GetLength(0) == _NoiseSize);
                    Debug.Assert(noiseMap.GetLength(1) == _NoiseSize);

                    var meshSize = _NoiseSize;
                    float meshSizeF = meshSize;

                    var topLeft = (meshSize - 1) / -2f;
                    var lodIncrement = lod is 0 ? 1 : lod * 2;
                    var verticesPerLine = (meshSize - 1) / lodIncrement + 1;

                    var meshVertices = new Vector3[verticesPerLine * verticesPerLine];
                    var meshTriangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
                    var meshHeightMap = new float[meshVertices.Length];
                    var meshUVs = new Vector2[meshVertices.Length];

                    var vertexIndex = -1;
                    var triangleIndex = -1;
                    for (var y = 0; y < meshSize; y += lodIncrement)
                    {
                        for (var x = 0; x < meshSize; x += lodIncrement)
                        {
                            AddVertex(x, y, GetHeight(noiseMap[x, y], heightMultiplier, heightCurve));

                            if (x < meshSize - 1 && y < meshSize - 1)
                            {
                                var a = vertexIndex;
                                var b = vertexIndex + 1;
                                var c = vertexIndex + verticesPerLine;
                                var d = vertexIndex + verticesPerLine + 1;

                                AddTriangle(a, d, c);
                                AddTriangle(d, a, b);
                            }
                        }
                    }

                    if (FlatShaded)
                        ApplyFlatShading(ref meshVertices, ref meshTriangles, ref meshUVs);

                    uvs = meshUVs;
                    vertices = meshVertices;
                    heightMap = meshHeightMap;
                    triangles = meshTriangles;
                    CalculateNormals(out normals);

                    void AddVertex(int x, int y, float terrainHeight)
                    {
                        var uv = new Vector2(x / meshSizeF, y / meshSizeF);
                        var vertex = new Vector3(topLeft + x, terrainHeight, topLeft + y);

                        meshUVs[++vertexIndex] = uv;
                        meshVertices[vertexIndex] = vertex;
                        meshHeightMap[vertexIndex] = terrainHeight;
                    }

                    void AddTriangle(int a, int b, int c)
                    {
                        meshTriangles[++triangleIndex] = a;
                        meshTriangles[++triangleIndex] = b;
                        meshTriangles[++triangleIndex] = c;
                    }

                    void CalculateNormals(out Vector3[] normals)
                    {
                        normals = new Vector3[meshVertices.Length];

                        for (var i = 0; i < meshTriangles.Length; i += 3)
                        {
                            var vIdxA = meshTriangles[i];
                            var vIdxB = meshTriangles[i + 1];
                            var vIdxC = meshTriangles[i + 2];

                            var surfaceNormal = GetTriangleNormal(vIdxA, vIdxB, vIdxC);

                            normals[vIdxA] += surfaceNormal;
                            normals[vIdxB] += surfaceNormal;
                            normals[vIdxC] += surfaceNormal;
                        }

                        normals.ForEach(x => x.Normalize());

                        Vector3 GetTriangleNormal(int indexA, int indexB, int indexC)
                        {
                            var pointA = meshVertices[indexA];
                            var pointB = meshVertices[indexB];
                            var pointC = meshVertices[indexC];

                            var sideAB = pointB - pointA;
                            var sideAC = pointC - pointA;

                            return sideAC.Cross(sideAB).Normalized();
                        }
                    }
                }

                void CreateSeamlessTriangleArrays(out Vector3[] vertices, out int[] triangles, out Vector3[] normals, out Vector2[] uvs, out float[] heightMap)
                {
                    Debug.Assert(noiseMap.GetLength(0) == _NoiseSize);
                    Debug.Assert(noiseMap.GetLength(1) == _NoiseSize);

                    var lodIncrement = lod is 0 ? 1 : lod * 2;

                    var borderSize = _NoiseSize;
                    var meshSize = borderSize - 2;
                    var lodSizeF = borderSize - 2f * lodIncrement;

                    var topLeft = (meshSize - 1) / -2f;
                    var verticesPerLine = (meshSize - 1) / lodIncrement + 1;

                    var meshVertices = new Vector3[verticesPerLine * verticesPerLine];
                    var meshTriangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
                    var borderVertices = new Vector3[verticesPerLine * 4 + 4];
                    var borderTriangles = new int[verticesPerLine * 24];
                    var meshHeightMap = new float[meshVertices.Length];
                    var meshUVs = new Vector2[meshVertices.Length];

                    CreateBorderMeshLookup(out var borderMeshIndexLookup);

                    var meshTriangleIndex = -1;
                    var borderTriangleIndex = -1;
                    for (var y = 0; y < borderSize; y += lodIncrement)
                    {
                        for (var x = 0; x < borderSize; x += lodIncrement)
                        {
                            AddBorderMeshVertex(x, y, GetHeight(noiseMap[x, y], heightMultiplier, heightCurve));

                            if (x < borderSize - 1 && y < borderSize - 1)
                            {
                                var a = borderMeshIndexLookup[x, y];
                                var b = borderMeshIndexLookup[x + lodIncrement, y];
                                var c = borderMeshIndexLookup[x, y + lodIncrement];
                                var d = borderMeshIndexLookup[x + lodIncrement, y + lodIncrement];

                                AddBorderMeshTriangle(a, d, c);
                                AddBorderMeshTriangle(d, a, b);
                            }
                        }
                    }

                    if (FlatShaded)
                        ApplyFlatShading(ref meshVertices, ref meshTriangles, ref meshUVs);

                    uvs = meshUVs;
                    vertices = meshVertices;
                    heightMap = meshHeightMap;
                    triangles = meshTriangles;
                    CalculateNormals(out normals);

                    bool IsBorderVertex(int lookupValue)
                        => lookupValue < 0;

                    bool IsBorderTriangle(int a, int b, int c)
                        => a < 0 || b < 0 || c < 0;

                    int GetBorderIndex(int lookupValue)
                        => -lookupValue - 1;

                    Vector3 GetBorderVertex(int lookupValue)
                        => borderVertices[GetBorderIndex(lookupValue)];

                    void SetBorderVertex(int lookupValue, in Vector3 vertex)
                        => borderVertices[GetBorderIndex(lookupValue)] = vertex;

                    void CreateBorderMeshLookup(out int[,] borderMeshIndexLookup)
                    {
                        var meshIndex = 0;
                        var borderIndex = -1;
                        borderMeshIndexLookup = new int[borderSize, borderSize];
                        for (var y = 0; y < borderSize; y += lodIncrement)
                        {
                            for (var x = 0; x < borderSize; x += lodIncrement)
                            {
                                var isBorderVertex =
                                    x is 0 || x == borderSize - 1 ||
                                    y is 0 || y == borderSize - 1;

                                borderMeshIndexLookup[x, y] = isBorderVertex
                                    ? borderIndex--
                                    : meshIndex++;
                            }
                        }
                    }

                    void AddBorderMeshVertex(int x, int y, float terrainHeight)
                    {
                        var uv = new Vector2((x - lodIncrement) / lodSizeF, (y - lodIncrement) / lodSizeF);
                        var vertex = new Vector3(topLeft + uv.X * meshSize, terrainHeight, topLeft + uv.Y * meshSize);

                        var vertexIndex = borderMeshIndexLookup[x, y];

                        if (IsBorderVertex(vertexIndex))
                        {
                            SetBorderVertex(vertexIndex, vertex);
                        }
                        else
                        {
                            meshUVs[vertexIndex] = uv;
                            meshVertices[vertexIndex] = vertex;
                            meshHeightMap[vertexIndex] = terrainHeight;
                        }
                    }

                    void AddBorderMeshTriangle(int a, int b, int c)
                    {
                        if (IsBorderTriangle(a, b, c))
                        {
                            borderTriangles[++borderTriangleIndex] = a;
                            borderTriangles[++borderTriangleIndex] = b;
                            borderTriangles[++borderTriangleIndex] = c;
                        }
                        else
                        {
                            meshTriangles[++meshTriangleIndex] = a;
                            meshTriangles[++meshTriangleIndex] = b;
                            meshTriangles[++meshTriangleIndex] = c;
                        }
                    }

                    void CalculateNormals(out Vector3[] normals)
                    {
                        normals = new Vector3[meshVertices.Length];

                        for (var i = 0; i < meshTriangles.Length; i += 3)
                        {
                            var vIdxA = meshTriangles[i];
                            var vIdxB = meshTriangles[i + 1];
                            var vIdxC = meshTriangles[i + 2];

                            var surfaceNormal = GetTriangleNormal(vIdxA, vIdxB, vIdxC);

                            normals[vIdxA] += surfaceNormal;
                            normals[vIdxB] += surfaceNormal;
                            normals[vIdxC] += surfaceNormal;
                        }

                        for (var i = 0; i < borderTriangles.Length; i += 3)
                        {
                            var vIdxA = borderTriangles[i];
                            var vIdxB = borderTriangles[i + 1];
                            var vIdxC = borderTriangles[i + 2];

                            var surfaceNormal = GetTriangleNormal(vIdxA, vIdxB, vIdxC);

                            if (!IsBorderVertex(vIdxA)) normals[vIdxA] += surfaceNormal;
                            if (!IsBorderVertex(vIdxB)) normals[vIdxB] += surfaceNormal;
                            if (!IsBorderVertex(vIdxC)) normals[vIdxC] += surfaceNormal;
                        }

                        normals.ForEach(x => x.Normalize());

                        Vector3 GetTriangleNormal(int indexA, int indexB, int indexC)
                        {
                            var pointA = IsBorderVertex(indexA) ? GetBorderVertex(indexA) : meshVertices[indexA];
                            var pointB = IsBorderVertex(indexB) ? GetBorderVertex(indexB) : meshVertices[indexB];
                            var pointC = IsBorderVertex(indexC) ? GetBorderVertex(indexC) : meshVertices[indexC];

                            var sideAB = pointB - pointA;
                            var sideAC = pointC - pointA;

                            return sideAC.Cross(sideAB).Normalized();
                        }
                    }
                }

                void ApplyFlatShading(ref Vector3[] vertices, ref int[] triangles, ref Vector2[] uvs)
                {
                    var flatShadedUVs = new Vector2[triangles.Length];
                    var flatShadedVertices = new Vector3[triangles.Length];

                    for (var i = 0; i < triangles.Length; ++i)
                    {
                        flatShadedVertices[i] = vertices[triangles[i]];
                        flatShadedUVs[i] = uvs[triangles[i]];
                        triangles[i] = i;
                    }

                    uvs = flatShadedUVs;
                    vertices = flatShadedVertices;
                }
            }

            Mesh CreateMesh(Vector3[] vertices, int[] triangles, Vector3[] normals, Vector2[] uvs)
            {
                var content = new Godot.Collections.Array();
                content.Resize((int)Mesh.ArrayType.Max);
                content[(int)Mesh.ArrayType.Vertex] = vertices.AsSpan();
                content[(int)Mesh.ArrayType.Normal] = normals.AsSpan();
                content[(int)Mesh.ArrayType.Index] = triangles.AsSpan();
                content[(int)Mesh.ArrayType.TexUV] = uvs.AsSpan();

                var mesh = new ArrayMesh();
                mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, content);
                mesh.SurfaceSetMaterial(0, material);
                return mesh;
            }

            Shape3D CreateShape(float[] heightMap)
            {
                Debug.Assert(heightMap.Length == _ChunkSize * _ChunkSize);

                var size = _ChunkSize;

                return new HeightMapShape3D
                {
                    MapWidth = size,
                    MapDepth = size,
                    MapData = heightMap,
                };
            }
        }
    }
}
