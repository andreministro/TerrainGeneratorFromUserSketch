using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    internal const int MaxSketchSize = 256;

    public Dictionary<WorldPosition, Chunk> chunks = new Dictionary<WorldPosition, Chunk>();
    public GameObject chunkPrefab;
    public ReadImage readImg;
    
    [HideInInspector]
    public float scale = 1f;

    //float adjScale;

    // Start is called before the first frame update
    void Start()
    {
        //CreateOneChunk();
        //adjScale = scale * 0.5f;
        readImg.DisplayImage();
        Debug.Log("Time:" + Time.realtimeSinceStartup);
    }

    public void CreateChunk(int x, int y, int z)
    {
        CreateChunk(x, y, z, new TerrainGeneration(readImg));
    }

    private void CreateChunk(int x, int y, int z, TerrainGeneration terrainGeneration)
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
        var terrainGeneration = new TerrainGeneration(readImg);

        for (int x = minChunk; x <= maxChunk; x += chunkSize)
        {
            for (int y = -chunkSize; y <= topChunk; y += chunkSize)
            {
                for (int z = minChunk; z <= maxChunk; z += chunkSize)
                    CreateChunk(x, y, z, terrainGeneration);
            }
        }
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
