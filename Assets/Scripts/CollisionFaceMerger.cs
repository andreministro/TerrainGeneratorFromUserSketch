using System;
using System.Collections.Generic;
using UnityEngine;
using Direction = Block.Direction;

internal sealed class CollisionFaceMerger
{
    // Each bit marks an exposed face. Rows are grouped by direction and plane.
    private readonly uint[] rows;
    private bool hasFaces;
    public int Size { get; }

    public CollisionFaceMerger(int size)
    {
        Size = size;
        rows = new uint[6 * size * size];
    }

    public void Add(Direction direction, int x, int y, int z)
    {
        int firstRow = (int)direction * Size * Size;
        switch (direction)
        {
            case Direction.up:
            case Direction.down:
                rows[firstRow + y * Size + z] |= 1u << x;
                break;
            case Direction.north:
            case Direction.south:
                rows[firstRow + z * Size + y] |= 1u << x;
                break;
            case Direction.east:
            case Direction.west:
                rows[firstRow + x * Size + y] |= 1u << z;
                break;
        }
        hasFaces = true;
    }

    public void AppendMesh(List<Vector3> vertices, List<int> triangles)
    {
        if (!hasFaces)
            return;

        for (int direction = 0; direction < 6; direction++)
        {
            for (int plane = 0; plane < Size; plane++)
            {
                int firstRow = (direction * Size + plane) * Size;
                for (int v = 0; v < Size; v++)
                {
                    uint remaining = rows[firstRow + v];
                    for (int u = 0; remaining != 0; u++, remaining >>= 1)
                    {
                        if ((remaining & 1u) == 0)
                            continue;

                        int width = 1;
                        while (u + width < Size && ((remaining >> width) & 1u) != 0)
                            width++;

                        uint columns = (uint)((1UL << width) - 1UL) << u;
                        int height = 1;
                        while (v + height < Size && (rows[firstRow + v + height] & columns) == columns)
                            height++;

                        for (int row = v; row < v + height; row++)
                            rows[firstRow + row] &= ~columns;
                        remaining &= ~(columns >> u);

                        AppendRectangle((Direction)direction, plane, u, v, width, height, vertices, triangles);
                    }
                }
            }
        }
        hasFaces = false;
    }

    private static void AppendRectangle(Direction direction, int plane, int u, int v, int width, int height,
        List<Vector3> vertices, List<int> triangles)
    {
        float u0 = u - 0.5f;
        float u1 = u0 + width;
        float v0 = v - 0.5f;
        float v1 = v0 + height;
        float high = plane + 0.5f;
        float low = plane - 0.5f;

        switch (direction)
        {
            case Direction.up:
                vertices.Add(new Vector3(u0, high, v1));
                vertices.Add(new Vector3(u1, high, v1));
                vertices.Add(new Vector3(u1, high, v0));
                vertices.Add(new Vector3(u0, high, v0));
                break;
            case Direction.down:
                vertices.Add(new Vector3(u0, low, v0));
                vertices.Add(new Vector3(u1, low, v0));
                vertices.Add(new Vector3(u1, low, v1));
                vertices.Add(new Vector3(u0, low, v1));
                break;
            case Direction.north:
                vertices.Add(new Vector3(u1, v0, high));
                vertices.Add(new Vector3(u1, v1, high));
                vertices.Add(new Vector3(u0, v1, high));
                vertices.Add(new Vector3(u0, v0, high));
                break;
            case Direction.south:
                vertices.Add(new Vector3(u0, v0, low));
                vertices.Add(new Vector3(u0, v1, low));
                vertices.Add(new Vector3(u1, v1, low));
                vertices.Add(new Vector3(u1, v0, low));
                break;
            case Direction.east:
                vertices.Add(new Vector3(high, v0, u0));
                vertices.Add(new Vector3(high, v1, u0));
                vertices.Add(new Vector3(high, v1, u1));
                vertices.Add(new Vector3(high, v0, u1));
                break;
            case Direction.west:
                vertices.Add(new Vector3(low, v0, u1));
                vertices.Add(new Vector3(low, v1, u1));
                vertices.Add(new Vector3(low, v1, u0));
                vertices.Add(new Vector3(low, v0, u0));
                break;
        }

        MeshData.AddQuadIndices(triangles, vertices.Count);
    }

    public void Clear()
    {
        if (!hasFaces)
            return;
        Array.Clear(rows, 0, rows.Length);
        hasFaces = false;
    }
}
