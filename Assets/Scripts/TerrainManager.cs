using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TerrainManager : MonoBehaviour
{
    internal const int MaxSketchSize = 512;

    public Dictionary<WorldPosition, Chunk> chunks = new Dictionary<WorldPosition, Chunk>();
    public GameObject chunkPrefab;
    public ReadImage readImg;

    [SerializeField, Min(1f)] private float buildBudgetMilliseconds = 8f;

    internal bool IsBuilding => buildRoutine != null;
    internal int CompletedBuildSteps { get; private set; }
    internal int TotalBuildSteps { get; private set; }

    private Coroutine buildRoutine;
    
    [HideInInspector]
    public float scale = 1f;

    private void Start()
    {
        readImg.DisplayImage();
    }

    public void CreateChunk(int x, int y, int z)
    {
        CreateChunk(x, y, z, new TerrainGeneration(readImg));
    }

    private Chunk CreateChunk(int x, int y, int z, TerrainGeneration terrainGeneration)
    {
        WorldPosition worldPos = new WorldPosition(x, y, z);

        //Instantiate the chunk at the coordinates using the chunk prefab
        GameObject newChunkObject = Instantiate(chunkPrefab, new Vector3(x, y, z), Quaternion.Euler(Vector3.zero)) as GameObject;

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
        if (IsBuilding || chunks.Count != 0)
            return;

        if (!IsSupportedSketchSize(width, height))
        {
            Debug.LogError($"Use a square sketch between 1 × 1 and {MaxSketchSize} × {MaxSketchSize}.", this);
            return;
        }

        int chunkSize = Chunk.chunkSize;
        int minCoordinate = -(width / 2);
        int minChunk = GetChunkOrigin(minCoordinate);
        int maxChunk = GetChunkOrigin(minCoordinate + width - 1);
        int topChunk = width <= 64 ? chunkSize : 3 * chunkSize;
        int chunksPerSide = (maxChunk - minChunk) / chunkSize + 1;
        int chunkCount = chunksPerSide * chunksPerSide * (topChunk / chunkSize + 2);
        TotalBuildSteps = chunkCount * 2;
        CompletedBuildSteps = 0;
        buildRoutine = StartCoroutine(BuildTerrain(minChunk, maxChunk, topChunk, chunkSize, chunkCount));
    }

    private IEnumerator BuildTerrain(int minChunk, int maxChunk, int topChunk, int chunkSize, int chunkCount)
    {
        bool finished = false;
        try
        {
            // Let the progress panel render before starting the first batch.
            yield return null;

            var generatedChunks = new List<Chunk>(chunkCount);
            var terrainGeneration = new TerrainGeneration(readImg);
            var frameWork = Stopwatch.StartNew();
            for (int x = minChunk; x <= maxChunk; x += chunkSize)
            {
                for (int y = -chunkSize; y <= topChunk; y += chunkSize)
                {
                    for (int z = minChunk; z <= maxChunk; z += chunkSize)
                    {
                        Chunk chunk = CreateChunk(x, y, z, terrainGeneration);
                        chunk.enabled = false;
                        generatedChunks.Add(chunk);
                        CompletedBuildSteps++;

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
            foreach (Chunk chunk in generatedChunks)
            {
                chunk.RebuildMesh();
                chunk.enabled = true;
                CompletedBuildSteps++;

                if (CompletedBuildSteps < TotalBuildSteps && frameWork.Elapsed.TotalMilliseconds >= buildBudgetMilliseconds)
                {
                    yield return null;
                    frameWork.Restart();
                }
            }

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
        if (buildRoutine == null)
            return;

        StopCoroutine(buildRoutine);
        buildRoutine = null;
        DiscardPartialTerrain();
    }

    private void DiscardPartialTerrain()
    {
        foreach (Chunk chunk in chunks.Values)
            Destroy(chunk.gameObject);

        chunks.Clear();
        CompletedBuildSteps = 0;
        TotalBuildSteps = 0;
    }

    private static int GetChunkOrigin(int coordinate)
    {
        return Mathf.FloorToInt(coordinate / (float)Chunk.chunkSize) * Chunk.chunkSize;
    }

    public Chunk GetChunk(int x, int y, int z)
    {
        var position = new WorldPosition(GetChunkOrigin(x), GetChunkOrigin(y), GetChunkOrigin(z));
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
