using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Block
{

    public enum Direction { north, east, south, west, up, down };
    public struct Tile { public int x; public int y; }
    const float tileSize = 0.125f;
    private Vector2[][] cachedFaceUVs;

    public Block()
    {
    }

    public virtual MeshData Blockdata(Chunk chunk, int x, int y, int z, MeshData meshData)
    {
        meshData.useRenderDataForCol = true;

        if (!chunk.GetBlock(x, y + 1, z).IsSolid(Direction.down))
            meshData = FaceDataUp(chunk, x, y, z, meshData);
        if (!chunk.GetBlock(x, y - 1, z).IsSolid(Direction.up))
            meshData = FaceDataDown(chunk, x, y, z, meshData);
        if (!chunk.GetBlock(x, y, z + 1).IsSolid(Direction.south))
            meshData = FaceDataNorth(chunk, x, y, z, meshData);
        if (!chunk.GetBlock(x, y, z - 1).IsSolid(Direction.north))
            meshData = FaceDataSouth(chunk, x, y, z, meshData);
        if (!chunk.GetBlock(x + 1, y, z).IsSolid(Direction.west))
            meshData = FaceDataEast(chunk, x, y, z, meshData);
        if (!chunk.GetBlock(x - 1, y, z).IsSolid(Direction.east))
            meshData = FaceDataWest(chunk, x, y, z, meshData);

        return meshData;

    }

    protected virtual MeshData FaceDataUp(Chunk chunk, int x, int y, int z, MeshData meshData)
    {
        meshData.AddVertex(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
        meshData.AddVertex(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
        meshData.AddVertex(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
        meshData.AddVertex(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));

        meshData.AddBlockFace(Direction.up, x, y, z);
        meshData.uv.AddRange(GetFaceUVs(Direction.up));
        return meshData;
    }

    protected virtual MeshData FaceDataDown(Chunk chunk, int x, int y, int z, MeshData meshData)
    {
        meshData.AddVertex(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
        meshData.AddVertex(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
        meshData.AddVertex(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));
        meshData.AddVertex(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));

        meshData.AddBlockFace(Direction.down, x, y, z);
        meshData.uv.AddRange(GetFaceUVs(Direction.down));
        return meshData;
    }

    protected virtual MeshData FaceDataNorth(Chunk chunk, int x, int y, int z, MeshData meshData)
    {
        meshData.AddVertex(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));
        meshData.AddVertex(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
        meshData.AddVertex(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
        meshData.AddVertex(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));

        meshData.AddBlockFace(Direction.north, x, y, z);
        meshData.uv.AddRange(GetFaceUVs(Direction.north));
        return meshData;
    }

    protected virtual MeshData FaceDataEast(Chunk chunk, int x, int y, int z, MeshData meshData)
    {
        meshData.AddVertex(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
        meshData.AddVertex(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
        meshData.AddVertex(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
        meshData.AddVertex(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));

        meshData.AddBlockFace(Direction.east, x, y, z);
        meshData.uv.AddRange(GetFaceUVs(Direction.east));
        return meshData;
    }

    protected virtual MeshData FaceDataSouth(Chunk chunk, int x, int y, int z, MeshData meshData)
    {
        meshData.AddVertex(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
        meshData.AddVertex(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
        meshData.AddVertex(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
        meshData.AddVertex(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));

        meshData.AddBlockFace(Direction.south, x, y, z);
        meshData.uv.AddRange(GetFaceUVs(Direction.south));
        return meshData;
    }

    protected virtual MeshData FaceDataWest(Chunk chunk, int x, int y, int z, MeshData meshData)
    {
        meshData.AddVertex(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));
        meshData.AddVertex(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
        meshData.AddVertex(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
        meshData.AddVertex(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));

        meshData.AddBlockFace(Direction.west, x, y, z);
        meshData.uv.AddRange(GetFaceUVs(Direction.west));
        return meshData;
    }

    //Check Solidity of a block's face
    public virtual bool IsSolid(Direction direction)
    {
        switch (direction)
        {
            case Direction.north:
                return true;
            case Direction.east:
                return true;
            case Direction.south:
                return true;
            case Direction.west:
                return true;
            case Direction.up:
                return true;
            case Direction.down:
                return true;
        }
        return false;
    }

    public virtual Tile TexturePosition(Direction direction)
    {
        Tile tile = new Tile();

        tile.x = 0;
        tile.y = 0;

        return tile;
    }

    internal void CacheFaceUVs()
    {
        cachedFaceUVs = new Vector2[6][];
        for (int i = 0; i < cachedFaceUVs.Length; i++)
            cachedFaceUVs[i] = FaceUVs((Direction)i);
    }

    private Vector2[] GetFaceUVs(Direction direction)
    {
        return cachedFaceUVs == null ? FaceUVs(direction) : cachedFaceUVs[(int)direction];
    }

    public virtual Vector2[] FaceUVs(Direction direction)
    {
        Vector2[] UVs = new Vector2[4];
        Tile tilePos = TexturePosition(direction);

        UVs[0] = new Vector2(tileSize * tilePos.x + tileSize, tileSize * tilePos.y);

        UVs[1] = new Vector2(tileSize * tilePos.x + tileSize, tileSize * tilePos.y + tileSize);

        UVs[2] = new Vector2(tileSize * tilePos.x, tileSize * tilePos.y + tileSize);

        UVs[3] = new Vector2(tileSize * tilePos.x, tileSize * tilePos.y);
        return UVs;
    }
}
