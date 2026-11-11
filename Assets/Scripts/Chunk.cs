using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public Block[,,] blocks = new Block[chunkSize, chunkSize, chunkSize];

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
            blocks[x, y, z] = block;
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
            return blocks[x, y, z] ?? BlockAir.Empty;

        return world.GetBlock(pos.x + x, pos.y + y, pos.z + z);
    }


    public static bool InRange(int index)
    {
        if (index < 0 || index >= chunkSize)
            return false;

        return true;
    }

    //Updates the chunk based on its contents and returns a 3dmesh
    void UpdateChunk()
    {
        MeshData meshData = new MeshData();

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    Block block = blocks[x, y, z];
                    if (block != null)
                        meshData = block.Blockdata(this, x, y, z, meshData);
                }
            }
        }
        RenderMesh(meshData);
    }

    //Sends the calculated mesh information
    //to the mesh and collision components
    void RenderMesh(MeshData meshData)
    {
        if (renderMesh == null)
            renderMesh = filter.mesh;

        renderMesh.Clear();
        renderMesh.vertices = meshData.vertices.ToArray();
        renderMesh.triangles = meshData.triangles.ToArray();
        renderMesh.uv = meshData.uv.ToArray();
        renderMesh.RecalculateNormals();

        coll.sharedMesh = null;
        if (colliderMesh != null)
            colliderMesh.Clear();
        if (meshData.colTriangles.Count == 0)
            return;

        if (colliderMesh == null)
            colliderMesh = new Mesh();
        colliderMesh.vertices = meshData.colVertices.ToArray();
        colliderMesh.triangles = meshData.colTriangles.ToArray();
        colliderMesh.RecalculateNormals();
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
