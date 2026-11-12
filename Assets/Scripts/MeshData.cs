using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class MeshData
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();
    public List<Vector2> uv = new List<Vector2>();

    public List<Vector3> colVertices = new List<Vector3>();
    public List<int> colTriangles = new List<int>();

    public bool useRenderDataForCol;
    private CollisionFaceMerger collisionFaces;
    internal bool MergeCollisionFaces { private get; set; }
    private bool RecordMergedCollision => MergeCollisionFaces && collisionFaces != null;

    public MeshData()
    { 
    }

    public void AddQuadTriangles()
    {
        AddQuadIndices(triangles, vertices.Count);
        if (useRenderDataForCol && !RecordMergedCollision)
            AddQuadIndices(colTriangles, colVertices.Count);
    }

    internal void AddBlockFace(Block.Direction direction, int x, int y, int z)
    {
        AddQuadTriangles();
        if (useRenderDataForCol && RecordMergedCollision)
            collisionFaces.Add(direction, x, y, z);
    }

    internal static void AddQuadIndices(List<int> indices, int vertexCount)
    {
        int first = vertexCount - 4;
        indices.Add(first);
        indices.Add(first + 1);
        indices.Add(first + 2);
        indices.Add(first);
        indices.Add(first + 2);
        indices.Add(first + 3);
    }

    internal void PrepareCollisionFaces(int size)
    {
        if (size > 32)
            collisionFaces = null;
        else if (collisionFaces == null || collisionFaces.Size != size)
            collisionFaces = new CollisionFaceMerger(size);
    }

    internal void FinishCollisionFaces()
    {
        collisionFaces?.AppendMesh(colVertices, colTriangles);
    }

    internal void Clear()
    {
        vertices.Clear();
        triangles.Clear();
        uv.Clear();
        colVertices.Clear();
        colTriangles.Clear();
        collisionFaces?.Clear();
        useRenderDataForCol = false;
        MergeCollisionFaces = false;
    }

    public void AddVertex(Vector3 vertex)
    {
        vertices.Add(vertex);
        if (useRenderDataForCol && !RecordMergedCollision)
        {
            colVertices.Add(vertex);
        }
    }
}
