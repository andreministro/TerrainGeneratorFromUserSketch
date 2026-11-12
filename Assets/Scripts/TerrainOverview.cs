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
        var tile = new Tile(root.transform, material, chunkBounds, columns, rows, verticesPerTile: columns * rows * 4);
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
                    uv = BlockPalette.Get(surface).FaceUVs(Block.Direction.up);
                    surfaceUVs.Add(surface, uv);
                }
                uvs.AddRange(uv);
            }
            (lowerRow, upperRow) = (upperRow, lowerRow);
        }

        tile.Upload(vertices, uvs, triangles);
        vertices.Clear();
        uvs.Clear();
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
        private RectInt maskedArea;

        public int[] ChunkX { get; }
        public int[] ChunkZ { get; }
        public Bounds Bounds => renderer.bounds;

        public Tile(Transform parent, Material material, RectInt chunkBounds, int columns, int rows, int verticesPerTile)
        {
            this.chunkBounds = chunkBounds;
            ChunkX = new int[columns];
            ChunkZ = new int[rows];
            var tile = new GameObject("Overview Tile");
            tile.transform.SetParent(parent, false);
            mesh = new Mesh { name = "Terrain Overview Tile" };
            if (verticesPerTile > ushort.MaxValue)
                mesh.indexFormat = IndexFormat.UInt32;
            tile.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = tile.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        public void Upload(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateBounds();
            UploadTriangles(triangles);
            mesh.RecalculateNormals();
        }

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
            mesh.SetTriangles(triangles, 0, calculateBounds: false);
        }

        public void Dispose() => Object.Destroy(mesh);
    }
}
