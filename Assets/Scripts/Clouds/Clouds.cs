using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clouds : MonoBehaviour
{
    public int cloudHeight = 100;
    private GameObject player;
    private Vector3 playerPos;

    [SerializeField]
    private Texture2D cloudPattern = null;
    [SerializeField]
    private Material cloudMaterial = null;
    bool[,] cloudData;

    int cloudTextureWidth;

    int cloudTileSize;
    Vector3Int offset;

    Dictionary<Vector2Int, GameObject> clouds = new Dictionary<Vector2Int, GameObject>();

    private void Start()
    {
        player = GameObject.Find("Player");
        cloudTextureWidth = cloudPattern.width;
        cloudTileSize = Chunk.chunkSize;
        offset = new Vector3Int(-(cloudTextureWidth / 2), 0, -(cloudTextureWidth / 2));

        transform.position = new Vector3(0, cloudHeight, 0);

        LoadCloudData();
        CreateClouds();
    }

    private void LoadCloudData()
    {
        cloudData = new bool[cloudTextureWidth, cloudTextureWidth];
        Color[] cloudTexture = cloudPattern.GetPixels();

        for(int x = 0; x < cloudTextureWidth; x++)      //Set bools depending on colour opacity in the color array
        {
            for (int y = 0; y < cloudTextureWidth; y++)
            {
                cloudData[x, y] = (cloudTexture[y * cloudTextureWidth + x].a > 0); 
            }
        }
    }

    private void CreateClouds()
    {
        for (int x = 0; x < cloudTextureWidth; x += cloudTileSize)      
        {
            for (int y = 0; y < cloudTextureWidth; y += cloudTileSize)
            {
                Vector3 position = new Vector3(x, cloudHeight, y);
                clouds.Add(CloudTilePosFromV3(position), CreateCloudTile(CreateCloudMesh(x, y), position));

                CreateCloudTile(CreateCloudMesh(x, y), new Vector3(x, 0, y) + transform.position + offset);
            }
        }

    }

    public void UpdateClouds()
    {
        playerPos = player.transform.position;

        for (int x = 0; x < cloudTextureWidth; x += cloudTileSize)
        {
            for (int y = 0; y < cloudTextureWidth; y += cloudTileSize)
            {
                Vector3 position = playerPos + new Vector3(x, 0, y) + offset;
                position = new Vector3(RoundToCloud(position.x), cloudHeight, RoundToCloud(position.z));
                Vector2Int cloudPosition = CloudTilePosFromV3(position);

                clouds[cloudPosition].transform.position = position;
            }
        }
    }

    private int RoundToCloud(float value)
    {
        return Mathf.FloorToInt(value / cloudTileSize) * cloudTileSize;
    }

    private Mesh CreateCloudMesh(int x, int z)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        int vertCount = 0;

        for (int xIncrement = 0; xIncrement < cloudTileSize; xIncrement++)
        {
            for (int zIncrement = 0; zIncrement < cloudTileSize; zIncrement++)
            {
                int xVal = x + xIncrement;
                int zVal = z + zIncrement;

                if (cloudData[xVal, zVal])
                {
                    vertices.Add(new Vector3(xIncrement, 0, zIncrement));    //4 verts for cloud face
                    vertices.Add(new Vector3(xIncrement, 0, zIncrement + 1));
                    vertices.Add(new Vector3(xIncrement + 1, 0, zIncrement + 1));
                    vertices.Add(new Vector3(xIncrement + 1, 0, zIncrement));

                    for (int i = 0; i < 4; i++)
                    {
                        normals.Add(Vector3.down);
                    }

                    triangles.Add(vertCount + 1); //1st triangle
                    triangles.Add(vertCount);
                    triangles.Add(vertCount + 2);

                    triangles.Add(vertCount + 2); //2nd triangle
                    triangles.Add(vertCount);
                    triangles.Add(vertCount + 3);

                    vertCount += 4;  //increment counter
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();

        return mesh;
    }

    private GameObject CreateCloudTile (Mesh mesh, Vector3 position)
    {
        GameObject newCloudTile = new GameObject();
        newCloudTile.transform.position = position;
        newCloudTile.transform.parent = transform;
        newCloudTile.name = "Cloud " + position.x + ", " + position.z;
        MeshFilter mF = newCloudTile.AddComponent<MeshFilter>();
        MeshRenderer mR = newCloudTile.AddComponent<MeshRenderer>();

        mR.material = cloudMaterial;
        mF.mesh = mesh;

        return newCloudTile;
    }

    private Vector2Int CloudTilePosFromV3(Vector3 pos)
    {
        return new Vector2Int(CloudTileCoordFromFloat(pos.x), CloudTileCoordFromFloat(pos.z));
    }

    private int CloudTileCoordFromFloat (float value)
    {
        float a = value / (float)cloudTextureWidth;  //get pos using cloud texture width as units
        a -= Mathf.FloorToInt(a); //get 0-1 value
        int b = Mathf.FloorToInt((float)cloudTextureWidth * a); // multiply cloud tex width by a to get pos is text globally

        return b;
    }
}
