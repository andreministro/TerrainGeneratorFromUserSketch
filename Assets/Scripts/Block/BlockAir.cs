using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class BlockAir : Block
{
    public BlockAir() : base()
    {

    }

    public override MeshData Blockdata(Chunk chunk, int x, int y, int z, MeshData meshData)
    {
        return meshData;
    }

    public override bool IsSolid(Block.Direction direction)
    {
        return false;
    }
}
