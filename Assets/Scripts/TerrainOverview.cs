using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

internal enum OverviewDetail
{
    [InspectorName("Coarse (16 voxels)")] Coarse = 16,
    [InspectorName("Standard (8 voxels)")] Standard = 8,
    [InspectorName("Fine (4 voxels)")] Fine = 4,
    [InspectorName("Very Fine (2 voxels)")] VeryFine = 2
}

internal sealed class TerrainOverview
{
    private const int ChunkColumnsPerTile = 16;
    private const int MaxOverviewCells = 1_100_000;
    private const int MaxPlantColumnsAcross = 64;
    private const int PlantSamplesPerColumn = 96;

    private readonly GameObject root;
    private readonly Material material;
    private readonly int chunkSize;
    private readonly int sampleStep;
    private readonly int minimum;
    private readonly int end;
    private readonly int firstChunk;
    private readonly int lastChunk;
    private readonly int upperElevation;
    private readonly int tilesAcross;
    private readonly List<Tile> tiles = new List<Tile>();
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Vector2> uvs = new List<Vector2>();
    private readonly List<int> triangles = new List<int>();
    private readonly Dictionary<BlockId, Vector2[]> surfaceUVs = new Dictionary<BlockId, Vector2[]>();
    private readonly Vector3[] firstRow;
    private readonly Vector3[] secondRow;

    public int TileCount => tilesAcross * tilesAcross;

    public TerrainOverview(Transform parent, Material material, int width, int topChunk, OverviewDetail detail)
    {
        this.material = material;
        chunkSize = Chunk.chunkSize;
        minimum = -(width / 2);
        end = minimum + width;
        firstChunk = Mathf.FloorToInt(minimum / (float)chunkSize);
        lastChunk = Mathf.FloorToInt((end - 1) / (float)chunkSize);
        upperElevation = topChunk + chunkSize - 1;
        tilesAcross = Mathf.CeilToInt((lastChunk - firstChunk + 1) / (float)ChunkColumnsPerTile);
        sampleStep = GetSampleStep(width, chunkSize, detail);
        int tileSamples = ChunkColumnsPerTile * chunkSize / sampleStep + 1;
        firstRow = new Vector3[tileSamples];
        secondRow = new Vector3[tileSamples];

        root = new GameObject("Terrain Overview");
        root.transform.SetParent(parent, false);
        root.SetActive(false);
    }

    private static int GetSampleStep(int width, int chunkSize, OverviewDetail detail)
    {
        int requested = Mathf.Max(1, (int)detail);
        int step = chunkSize;
        // Keep the grid aligned to chunk boundaries and bound the vertex count on large sketches.
        while (step % 2 == 0 && step / 2 >= requested)
        {
            int candidate = step / 2;
            int cellsAcross = Mathf.CeilToInt((width + candidate - 1) / (float)candidate);
            if (cellsAcross * cellsAcross > MaxOverviewCells)
                break;
            step = candidate;
        }
        return step;
    }

    public void AddTile(int index, TerrainGeneration generation)
    {
        int chunkX = firstChunk + index % tilesAcross * ChunkColumnsPerTile;
        int chunkZ = firstChunk + index / tilesAcross * ChunkColumnsPerTile;
        int chunkEndX = Mathf.Min(chunkX + ChunkColumnsPerTile, lastChunk + 1);
        int chunkEndZ = Mathf.Min(chunkZ + ChunkColumnsPerTile, lastChunk + 1);
        var chunkBounds = new RectInt(chunkX, chunkZ, chunkEndX - chunkX, chunkEndZ - chunkZ);
        int firstSample = Mathf.FloorToInt(minimum / (float)sampleStep) * sampleStep;
        int startX = Mathf.Max(chunkX * chunkSize, firstSample);
        int startZ = Mathf.Max(chunkZ * chunkSize, firstSample);
        int endX = Mathf.Min(chunkEndX * chunkSize, end);
        int endZ = Mathf.Min(chunkEndZ * chunkSize, end);
        int columns = Mathf.CeilToInt((endX - startX) / (float)sampleStep);
        int rows = Mathf.CeilToInt((endZ - startZ) / (float)sampleStep);
        var tile = new Tile(root.transform, material, chunkBounds, columns, rows);
        tiles.Add(tile);

        var lowerRow = firstRow;
        var upperRow = secondRow;
        SampleRow(lowerRow, startX, startZ, columns, generation);
        for (int row = 0; row < rows; row++)
        {
            int z = startZ + row * sampleStep;
            SampleRow(upperRow, startX, z + sampleStep, columns, generation);
            tile.ChunkZ[row] = Mathf.FloorToInt(z / (float)chunkSize);
            for (int column = 0; column < columns; column++)
            {
                int x = startX + column * sampleStep;
                if (row == 0)
                    tile.ChunkX[column] = Mathf.FloorToInt(x / (float)chunkSize);
                vertices.Add(upperRow[column]);
                vertices.Add(upperRow[column + 1]);
                vertices.Add(lowerRow[column + 1]);
                vertices.Add(lowerRow[column]);

                int x0 = Mathf.Max(x, minimum);
                int x1 = Mathf.Min(x + sampleStep, end);
                int z0 = Mathf.Max(z, minimum);
                int z1 = Mathf.Min(z + sampleStep, end);
                generation.GetOverviewSurface((x0 + x1 - 1) >> 1, (z0 + z1 - 1) >> 1, out BlockId surface);
                if (!surfaceUVs.TryGetValue(surface, out Vector2[] uv))
                {
                    uv = BlockPalette.Get(surface).GetFaceUVs(Block.Direction.up);
                    surfaceUVs.Add(surface, uv);
                }
                uvs.AddRange(uv);
            }
            (lowerRow, upperRow) = (upperRow, lowerRow);
        }

        AddTilePlants(tile, chunkBounds, startX, startZ, columns, generation);
        tile.Upload(vertices, uvs, triangles);
        vertices.Clear();
        uvs.Clear();
    }

    private void AddTilePlants(Tile tile, RectInt chunkBounds, int startX, int startZ, int columns,
        TerrainGeneration generation)
    {
        // Sample at most 64×64 chunk columns, independently of the overview mesh resolution.
        int stride = Mathf.CeilToInt((lastChunk - firstChunk + 1) / (float)MaxPlantColumnsAcross);
        foreach (Vector2Int column in chunkBounds.allPositionsWithin)
        {
            if ((column.x - firstChunk) % stride != 0 || (column.y - firstChunk) % stride != 0)
                continue;

            int originX = column.x * chunkSize;
            int originZ = column.y * chunkSize;
            int area = chunkSize * chunkSize;
            uint hash = unchecked((uint)column.x * 73856093u ^ (uint)column.y * 19349663u);
            // A coprime stride visits distinct candidates in the 16×16 column.
            int index = (int)(hash % (uint)area);
            BlockId selectedPlant = BlockId.Unset;
            int plantX = 0;
            int plantZ = 0;
            for (int sample = 0; sample < Mathf.Min(area, PlantSamplesPerColumn); sample++)
            {
                int x = originX + index % chunkSize;
                int z = originZ + index / chunkSize;
                index = (index + 73) % area;
                if (x < minimum || z < minimum || x >= end || z >= end)
                    continue;

                BlockId plant = generation.GetOverviewPlant(x, z, upperElevation);
                if (plant == BlockId.Unset)
                    continue;

                if (selectedPlant == BlockId.Unset || (plant != BlockId.DryBush && plant != BlockId.FrozenBush))
                {
                    selectedPlant = plant;
                    plantX = x;
                    plantZ = z;
                }
                if (plant != BlockId.DryBush && plant != BlockId.FrozenBush)
                    break;
            }
            if (selectedPlant == BlockId.Unset)
                continue;

            int cellX = (plantX - startX) / sampleStep;
            int cellZ = (plantZ - startZ) / sampleStep;
            int firstVertex = (cellZ * columns + cellX) * 4;
            // Place the base on the actual coarse triangle, including on steep slopes.
            Vector3 southwest = vertices[firstVertex + 3];
            float u = (plantX - southwest.x) / (vertices[firstVertex + 2].x - southwest.x);
            float v = (plantZ - southwest.z) / (vertices[firstVertex].z - southwest.z);
            float northwest = vertices[firstVertex].y;
            float southeast = vertices[firstVertex + 2].y;
            float y = u + v <= 1f
                ? northwest * v + southeast * u + southwest.y * (1f - u - v)
                : northwest * (1f - u) + southeast * (1f - v) + vertices[firstVertex + 1].y * (u + v - 1f);
            int plantStart = vertices.Count;
            AddPlant(plantX, y, plantZ, selectedPlant);
            tile.AddPlant(column, plantStart, vertices.Count);
        }
    }

    private void AddPlant(int x, float y, int z, BlockId plant)
    {
        var origin = new Vector3(x, y, z);
        var (trunk, canopyBase, canopyTop, radius) = plant switch
        {
            BlockId.NormalLeaf => (BlockId.NormalWood, 4f, 8f, 2.3f),
            BlockId.DarkLeaf => (BlockId.NormalWood, 4f, 12f, 1.8f),
            BlockId.TropicalLeaf => (BlockId.TropicalWood, 7f, 10f, 3f),
            BlockId.DryLeaf => (BlockId.DryWood, 7f, 8.5f, 3.6f),
            BlockId.Cactus => (BlockId.Unset, 0f, 4f, 0.5f),
            _ => (BlockId.Unset, 0f, 1.3f, 0.65f)
        };
        if (trunk != BlockId.Unset)
            AddPlantBox(origin, 0f, canopyBase + 0.25f, 0.45f, trunk);
        AddPlantBox(origin, canopyBase, canopyTop, radius, plant);
    }

    private void AddPlantBox(Vector3 origin, float bottom, float top, float radius, BlockId blockId)
    {
        Block block = BlockPalette.Get(blockId);
        Vector3 a = origin + new Vector3(-radius, bottom, -radius);
        Vector3 b = origin + new Vector3(radius, bottom, -radius);
        Vector3 c = origin + new Vector3(radius, bottom, radius);
        Vector3 d = origin + new Vector3(-radius, bottom, radius);
        Vector3 lift = Vector3.up * (top - bottom);
        AddFace(d + lift, c + lift, b + lift, a + lift, Block.Direction.up);
        AddFace(a, b, c, d, Block.Direction.down);
        AddFace(c, c + lift, d + lift, d, Block.Direction.north);
        AddFace(a, a + lift, b + lift, b, Block.Direction.south);
        AddFace(b, b + lift, c + lift, c, Block.Direction.east);
        AddFace(d, d + lift, a + lift, a, Block.Direction.west);

        void AddFace(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Block.Direction direction)
        {
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            uvs.AddRange(block.GetFaceUVs(direction));
        }
    }

    private void SampleRow(Vector3[] row, int startX, int z, int columns, TerrainGeneration generation)
    {
        int edgeZ = Mathf.Clamp(z, minimum, end);
        int sampleZ = Mathf.Min(edgeZ, end - 1);
        for (int i = 0; i <= columns; i++)
        {
            int edgeX = Mathf.Clamp(startX + i * sampleStep, minimum, end);
            int height = generation.GetOverviewSurface(Mathf.Min(edgeX, end - 1), sampleZ, out _);
            row[i] = new Vector3(edgeX - 0.5f, Mathf.Clamp(height, -chunkSize, upperElevation) + 0.5f, edgeZ - 0.5f);
        }
    }

    public void Finish() => root.SetActive(true);

    public IEnumerable<Bounds> GetBounds()
    {
        foreach (Tile tile in tiles)
            yield return tile.Bounds;
    }

    public void ShowOutside(RectInt window)
    {
        foreach (Tile tile in tiles)
            tile.ShowOutside(window, triangles);
    }

    public void Dispose()
    {
        if (root != null)
        {
            root.SetActive(false);
            Object.Destroy(root);
        }
        foreach (Tile tile in tiles)
            tile.Dispose();
    }

    private sealed class Tile
    {
        private readonly RectInt chunkBounds;
        private readonly Mesh mesh;
        private readonly MeshRenderer renderer;
        private readonly List<(Vector2Int Column, int Start, int End)> plants =
            new List<(Vector2Int Column, int Start, int End)>();
        private RectInt maskedArea;

        public int[] ChunkX { get; }
        public int[] ChunkZ { get; }
        public Bounds Bounds => renderer.bounds;

        public Tile(Transform parent, Material material, RectInt chunkBounds, int columns, int rows)
        {
            this.chunkBounds = chunkBounds;
            ChunkX = new int[columns];
            ChunkZ = new int[rows];
            var tile = new GameObject("Overview Tile");
            tile.transform.SetParent(parent, false);
            mesh = new Mesh { name = "Terrain Overview Tile" };
            tile.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = tile.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        public void Upload(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            if (vertices.Count > ushort.MaxValue)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateBounds();
            UploadTriangles(triangles);
            mesh.RecalculateNormals();
        }

        public void AddPlant(Vector2Int column, int start, int end) => plants.Add((column, start, end));

        public void ShowOutside(RectInt window, List<int> triangles)
        {
            int minX = Mathf.Max(chunkBounds.xMin, window.xMin);
            int minZ = Mathf.Max(chunkBounds.yMin, window.yMin);
            int maxX = Mathf.Min(chunkBounds.xMax, window.xMax);
            int maxZ = Mathf.Min(chunkBounds.yMax, window.yMax);
            RectInt overlap = minX < maxX && minZ < maxZ
                ? new RectInt(minX, minZ, maxX - minX, maxZ - minZ)
                : default;
            if (overlap == maskedArea)
                return;

            maskedArea = overlap;
            renderer.enabled = overlap != chunkBounds;
            if (renderer.enabled)
                UploadTriangles(triangles);
        }

        private void UploadTriangles(List<int> triangles)
        {
            triangles.Clear();
            int cell = 0;
            foreach (int z in ChunkZ)
            {
                foreach (int x in ChunkX)
                {
                    cell++;
                    if (!maskedArea.Contains(new Vector2Int(x, z)))
                        MeshData.AddQuadIndices(triangles, cell * 4);
                }
            }
            foreach (var plant in plants)
            {
                if (maskedArea.Contains(plant.Column))
                    continue;
                for (int end = plant.Start + 4; end <= plant.End; end += 4)
                    MeshData.AddQuadIndices(triangles, end);
            }
            mesh.SetTriangles(triangles, 0, calculateBounds: false);
        }

        public void Dispose() => Object.Destroy(mesh);
    }
}
