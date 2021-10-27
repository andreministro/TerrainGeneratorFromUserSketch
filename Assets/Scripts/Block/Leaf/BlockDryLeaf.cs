using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class BlockDryLeaf : Block
{
    public BlockDryLeaf() : base()
    {

    }

    public override Tile TexturePosition(Direction direction)
    {
        Tile tile = new Tile();

        tile.x = 2;
        tile.y = 4;

        return tile;
    }

    public override bool IsSolid(Direction direction)
    {
        return false;
    }
}
