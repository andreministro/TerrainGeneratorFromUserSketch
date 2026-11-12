using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

public partial class TerrainManager : MonoBehaviour
{
    internal const int MaxSketchSize = 4096;
    private const float MinVoxelSize = 0.01f;
    private const float SettingsRebuildDelaySeconds = 0.25f;

    public Dictionary<WorldPosition, Chunk> chunks = new Dictionary<WorldPosition, Chunk>();
    public GameObject chunkPrefab;
    public ReadImage readImg;

    [SerializeField, FormerlySerializedAs("scale"), Min(MinVoxelSize)]
    [Tooltip("Width, height and depth of one voxel in Unity units. Use a smaller sketch to reduce the number of voxels.")]
    private float voxelSize = 1f;
    [SerializeField, Min(1f)] private float buildBudgetMilliseconds = 8f;
    [SerializeField, Tooltip("Edit a biome in Play Mode to rebuild the current sketch with its new noise settings.")]
    private BiomeNoiseProfiles biomeNoise = BiomeNoiseProfiles.Default;

    internal bool IsBuilding => buildRoutine != null;
    internal int BuildVersion { get; private set; }
    internal float VoxelSize { get; private set; } = 1f;

    private Coroutine buildRoutine;
    private Transform chunkRoot;
    private int sketchWidth;
    private BiomeNoiseProfiles buildNoise;
    private volatile bool settingsNeedCheck;
    private float settingsRebuildAt = float.PositiveInfinity;

    private void Awake()
    {
        chunkRoot = new GameObject("Terrain Chunks").transform;
        chunkRoot.SetParent(transform, false);
        ApplyVoxelSize();
    }

    private void Start()
    {
        readImg.DisplayImage();
    }

    private void OnValidate()
    {
        // Unity can invoke validation off the main thread.
        settingsNeedCheck = true;
    }

    private void Update()
    {
        if (settingsNeedCheck)
        {
            settingsNeedCheck = false;
            ApplyVoxelSize();
            bool needsRebuild = !biomeNoise.Equals(buildNoise)
                || (UsesStreaming && overviewDetail != buildOverviewDetail);
            settingsRebuildAt = needsRebuild
                ? Time.unscaledTime + SettingsRebuildDelaySeconds
                : float.PositiveInfinity;
        }

        if (!IsBuilding && HasTerrain && Time.unscaledTime >= settingsRebuildAt)
        {
            if (UsesStreaming && streamGeneration != null && biomeNoise.Equals(buildNoise))
                BeginOverviewBuild(streamGeneration, regenerateTerrain: false);
            else
                BeginBuild();
        }

        UpdateStreaming();
    }

    private void ApplyVoxelSize()
    {
        float size = float.IsNaN(voxelSize) || float.IsInfinity(voxelSize)
            ? 1f
            : Mathf.Max(MinVoxelSize, voxelSize);
        if (VoxelSize == size)
            return;

        VoxelSize = size;
        chunkRoot.localScale = Vector3.one * size;
    }

    public void CreateChunk(int x, int y, int z)
    {
        CreateChunk(x, y, z, new TerrainGeneration(readImg, biomeNoise));
    }

    private Chunk CreateChunk(int x, int y, int z, TerrainGeneration terrainGeneration)
    {
        WorldPosition worldPos = new WorldPosition(x, y, z);

        GameObject newChunkObject = Instantiate(chunkPrefab, chunkRoot, false);
        newChunkObject.transform.SetLocalPositionAndRotation(new Vector3(x, y, z), Quaternion.identity);

        Chunk newChunk = newChunkObject.GetComponent<Chunk>();

        newChunk.pos = worldPos;
        newChunk.world = this;

        //Add it to the chunks dictionary with the position as the key
        chunks.Add(worldPos, newChunk);

        terrainGeneration.ChunkGeneration(newChunk);
        return newChunk;
    }
    

    public void CreateOneChunk()
    {
        for (int x = -1; x < 0; x++)
        {
            for (int y = -1; y < 0; y++)
            {
                for (int z = -1; z < 0; z++)
                {
                    CreateChunk(x * 16, y * 16, z * 16);
                }
            }
        }
    }

    internal static bool IsSupportedSketchSize(int width, int height)
    {
        return width == height && width >= 1 && width <= MaxSketchSize;
    }

    public void CreateFromSketch(int width, int height)
    {
        if (IsBuilding || HasTerrain)
            return;

        if (!IsSupportedSketchSize(width, height))
        {
            Debug.LogError($"Use a square sketch between 1 × 1 and {MaxSketchSize} × {MaxSketchSize}.", this);
            return;
        }

        sketchWidth = width;
        BeginBuild();
    }

    private void BeginBuild()
    {
        settingsRebuildAt = float.PositiveInfinity;
        buildNoise = biomeNoise;
        var terrainGeneration = new TerrainGeneration(readImg, buildNoise, cachedSketchWidth: UsesStreaming ? 0 : sketchWidth);
        if (UsesStreaming)
        {
            BeginOverviewBuild(terrainGeneration, regenerateTerrain: true);
            return;
        }

        int chunkSize = Chunk.chunkSize;
        int minCoordinate = -(sketchWidth / 2);
        int minChunk = GetChunkOrigin(minCoordinate);
        int maxChunk = GetChunkOrigin(minCoordinate + sketchWidth - 1);
        int topChunk = TopChunk;
        int chunksPerSide = (maxChunk - minChunk) / chunkSize + 1;
        int chunkCount = chunksPerSide * chunksPerSide * (topChunk / chunkSize + 2);
        buildRoutine = StartCoroutine(BuildTerrain(minChunk, maxChunk, topChunk, chunkSize, chunkCount, terrainGeneration));
    }

    private IEnumerator BuildTerrain(int minChunk, int maxChunk, int topChunk, int chunkSize, int chunkCount, TerrainGeneration terrainGeneration)
    {
        bool finished = false;
        try
        {
            // Let the progress panel render before starting the first batch.
            yield return null;

            var generatedChunks = new List<Chunk>(chunkCount);
            var frameWork = Stopwatch.StartNew();
            for (int x = minChunk; x <= maxChunk; x += chunkSize)
            {
                for (int y = -chunkSize; y <= topChunk; y += chunkSize)
                {
                    for (int z = minChunk; z <= maxChunk; z += chunkSize)
                    {
                        if (chunks.TryGetValue(new WorldPosition(x, y, z), out Chunk chunk))
                        {
                            chunk.ClearBlocks();
                            terrainGeneration.ChunkGeneration(chunk);
                        }
                        else
                        {
                            chunk = CreateChunk(x, y, z, terrainGeneration);
                        }
                        chunk.enabled = false;
                        generatedChunks.Add(chunk);

                        if (frameWork.Elapsed.TotalMilliseconds >= buildBudgetMilliseconds)
                        {
                            yield return null;
                            frameWork.Restart();
                        }
                    }
                }
            }

            // Face culling needs neighboring blocks, including those in other chunks.
            yield return null;
            frameWork.Restart();
            for (int i = 0; i < generatedChunks.Count; i++)
            {
                Chunk chunk = generatedChunks[i];
                chunk.RebuildMesh();
                chunk.enabled = true;

                if (i + 1 < generatedChunks.Count && frameWork.Elapsed.TotalMilliseconds >= buildBudgetMilliseconds)
                {
                    yield return null;
                    frameWork.Restart();
                }
            }

            BuildVersion++;
            finished = true;
        }
        finally
        {
            buildRoutine = null;
            if (!finished)
                DiscardPartialTerrain();
        }
    }

    private void OnDisable()
    {
        StopStreaming();
        if (buildRoutine == null)
            return;

        StopCoroutine(buildRoutine);
        buildRoutine = null;
        DiscardPartialTerrain();
    }

    private void DiscardPartialTerrain()
    {
        foreach (Chunk chunk in chunks.Values)
        {
            if (chunk != null)
                Destroy(chunk.gameObject);
        }

        chunks.Clear();
    }

    private static int GetChunkOrigin(int coordinate)
    {
        return Mathf.FloorToInt(coordinate / (float)Chunk.chunkSize) * Chunk.chunkSize;
    }

    public Chunk GetChunk(int x, int y, int z)
    {
        var position = new WorldPosition(GetChunkOrigin(x), GetChunkOrigin(y), GetChunkOrigin(z));
        if (meshingWindow.HasValue && !meshingWindow.Value.Contains(GetColumn(position)))
            return null;
        chunks.TryGetValue(position, out Chunk chunk);
        return chunk;
    }

    public Block GetBlock(int x, int y, int z)
    {
        Chunk containerChunk = GetChunk(x, y, z);

        if (containerChunk != null)
        {
            Block block = containerChunk.GetBlock(
                x - containerChunk.pos.x,
                y - containerChunk.pos.y,
                z - containerChunk.pos.z);

            return block;
        }
        else
        {
            return BlockAir.Empty;
        }
    }

    public void SetBlock(int x, int y, int z, Block block)
    {
        Chunk chunk = GetChunk(x, y, z);

        if (chunk != null)
        {
            chunk.SetBlock((x - chunk.pos.x), (y - chunk.pos.y), (z - chunk.pos.z), block);
            chunk.update = true;
        }
    }
}
