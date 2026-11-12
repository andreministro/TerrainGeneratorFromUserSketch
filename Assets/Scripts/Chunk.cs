using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    private readonly BlockId[] blocks = new BlockId[chunkSize * chunkSize * chunkSize];
    private Dictionary<int, Block> customBlocks;
    private static MeshData spareMeshData;

    public static int chunkSize = 16;
    public bool update = true;

    public TerrainManager world;
    public WorldPosition pos;

    MeshFilter filter;
    MeshCollider coll;
    private Mesh renderMesh;
    private Mesh colliderMesh;

    private void Awake()
    {
        filter = gameObject.GetComponent<MeshFilter>();
        coll = gameObject.GetComponent<MeshCollider>();
    }

    private void Update()
    {
        if (update)
            RebuildMesh();
    }

    internal void RebuildMesh()
    {
        update = false;
        UpdateChunk();
    }

    public void SetBlock(int x, int y, int z, Block block)
    {
        if (InRange(x) && InRange(y) && InRange(z))
        {
            if (block == null)
            {
                SetBlockId(x, y, z, BlockId.Unset);
                return;
            }

            int index = BlockIndex(x, y, z);
            customBlocks ??= new Dictionary<int, Block>();
            customBlocks[index] = block;
            blocks[index] = BlockId.Custom;
        }
        else
        {
            world.SetBlock((pos.x + x), (pos.y + y), (pos.z + z), block);
        }
    }

    //Return blocks from chunk
    public Block GetBlock(int x, int y, int z)
    {
        if (InRange(x) && InRange(y) && InRange(z))
            return GetStoredBlock(BlockIndex(x, y, z)) ?? BlockAir.Empty;

        return world.GetBlock(pos.x + x, pos.y + y, pos.z + z);
    }

    private Block GetStoredBlock(int index)
    {
        BlockId id = blocks[index];
        return id == BlockId.Custom ? customBlocks[index] : BlockPalette.Get(id);
    }

    internal bool IsUnset(int x, int y, int z) => blocks[BlockIndex(x, y, z)] == BlockId.Unset;

    internal void SetBlockId(int x, int y, int z, BlockId id)
    {
        int index = BlockIndex(x, y, z);
        if (blocks[index] == BlockId.Custom)
            customBlocks.Remove(index);
        blocks[index] = id;
    }

    internal void ClearBlocks()
    {
        Array.Clear(blocks, 0, blocks.Length);
        customBlocks?.Clear();
    }

    private static int BlockIndex(int x, int y, int z) => (x * chunkSize + y) * chunkSize + z;

    public static bool InRange(int index)
    {
        if (index < 0 || index >= chunkSize)
            return false;

        return true;
    }

    //Updates the chunk based on its contents and returns a 3dmesh
    void UpdateChunk()
    {
        MeshData buffer = spareMeshData ?? new MeshData();
        spareMeshData = null;
        try
        {
            buffer.PrepareCollisionFaces(chunkSize);
            MeshData meshData = buffer;
            int index = 0;
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++, index++)
                    {
                        BlockId id = blocks[index];
                        if (id == BlockId.Unset || id == BlockId.Air)
                            continue;

                        meshData.MergeCollisionFaces = id != BlockId.Custom;
                        Block block = GetStoredBlock(index);
                        meshData = block.Blockdata(this, x, y, z, meshData);
                    }
                }
            }
            meshData.FinishCollisionFaces();
            RenderMesh(meshData);
        }
        finally
        {
            buffer.Clear();
            spareMeshData = buffer;
        }
    }

    //Sends the calculated mesh information
    //to the mesh and collision components
    void RenderMesh(MeshData meshData)
    {
        if (renderMesh == null)
            renderMesh = filter.mesh;

        renderMesh.Clear();
        renderMesh.SetVertices(meshData.vertices);
        renderMesh.SetTriangles(meshData.triangles, 0);
        renderMesh.SetUVs(0, meshData.uv);
        renderMesh.RecalculateNormals();

        coll.sharedMesh = null;
        if (colliderMesh != null)
            colliderMesh.Clear();
        if (meshData.colTriangles.Count == 0)
            return;

        if (colliderMesh == null)
            colliderMesh = new Mesh();
        colliderMesh.SetVertices(meshData.colVertices);
        colliderMesh.SetTriangles(meshData.colTriangles, 0);
        coll.sharedMesh = colliderMesh;
    }

    private void OnDestroy()
    {
        if (renderMesh != null)
            Destroy(renderMesh);
        if (colliderMesh != null)
            Destroy(colliderMesh);
    }
}
