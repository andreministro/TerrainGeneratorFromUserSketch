using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
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
        WorldPosition worldPos = new WorldPosition(x, y, z);

        //Instantiate the chunk at the coordinates using the chunk prefab
        GameObject newChunkObject = Instantiate(chunkPrefab, new Vector3(x, y, z), Quaternion.Euler(Vector3.zero)) as GameObject;

        Chunk newChunk = newChunkObject.GetComponent<Chunk>();

        newChunk.pos = worldPos;
        newChunk.world = this;

        //Add it to the chunks dictionary with the position as the key
        chunks.Add(worldPos, newChunk);

        var terrainGen = new TerrainGeneration();
        newChunk = terrainGen.ChunkGeneration(newChunk);
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

    public void CreateFromSketch(int width, int height)
    {
        if(width >= 32 && height >= 32)
        {
            if (isPowerOfTwo(width) && isPowerOfTwo(height))
            {
                if (width == 32 && height == 32)
                {
                    for (int x = -1; x < 1; x++)
                    {
                        for (int y = -1; y < 2; y++)
                        {
                            for (int z = -1; z < 1; z++)
                            {
                                CreateChunk(x * 16, y * 16, z * 16);
                            }
                        }
                    }
                }
                else if (width == 64 && height == 64)
                {
                    for (int x = -2; x < 2; x++)
                    {
                        for (int y = -1; y < 2; y++)
                        {
                            for (int z = -2; z < 2; z++)
                            {
                                CreateChunk(x * 16, y * 16, z * 16);
                            }
                        }
                    }
                }
                else if (width == 128 && height == 128)
                {
                    for (int x = -4; x < 4; x++)
                    {
                        for (int y = -1; y <= 3; y++)
                        {
                            for (int z = -4; z < 4; z++)
                            {
                                CreateChunk(x * 16, y * 16, z * 16);
                            }
                        }
                    }
                }
                else if (width == 256 && height == 256)
                {
                    for (int x = -8; x < 8; x++)
                    {
                        for (int y = -1; y <= 3; y++)
                        {
                            for (int z = -8; z < 8; z++)
                            {
                                CreateChunk(x * 16, y * 16, z * 16);
                            }
                        }
                    }
                }
                else if (width == 512 && height == 512)
                {
                    for (int x = -16; x < 16; x++)
                    {
                        for (int y = -1; y <= 3; y++)
                        {
                            for (int z = -16; z < 16; z++)
                            {
                                CreateChunk(x * 16, y * 16, z * 16);
                            }
                        }
                    }
                }
                else if (width == 1024 && height == 1024)
                {
                    for (int x = -32; x < 32; x++)
                    {
                        for (int y = -1; y <= 3; y++)
                        {
                            for (int z = -32; z < 32; z++)
                            {
                                CreateChunk(x * 16, y * 16, z * 16);
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log("The size you want for the terrain is larger than the largest this Framework supports. If you want to feed a larger sketch, change the CreateFromSketch() function from the TerrainManager script to contemplate the change");
                }
            }
            else
            {
                Debug.Log("Invalid terrain size. Input a sketch with a side size of a power of 2");
            }
        }
        else
        {
            Debug.Log("The terrain needs to be > 32");
        }
    }

    static bool isPowerOfTwo(int n)
    {
        return (int)(Mathf.Ceil((Mathf.Log(n) / Mathf.Log(2))))
              == (int)(Mathf.Floor(((Mathf.Log(n) / Mathf.Log(2)))));
    }

    public Chunk GetChunk(int x, int y, int z)
    {
        WorldPosition pos = new WorldPosition();
        float multiple = Chunk.chunkSize;
        pos.x = Mathf.FloorToInt(x / multiple) * Chunk.chunkSize;
        pos.y = Mathf.FloorToInt(y / multiple) * Chunk.chunkSize;
        pos.z = Mathf.FloorToInt(z / multiple) * Chunk.chunkSize;

        Chunk containerChunk = null;

        chunks.TryGetValue(pos, out containerChunk);

        return containerChunk;
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
            return new BlockAir();
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
