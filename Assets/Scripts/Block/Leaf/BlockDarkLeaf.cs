using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class BlockDarkLeaf : Block
{
    public BlockDarkLeaf() : base()
    {

    }

    public override Tile TexturePosition(Direction direction)
    {
        Tile tile = new Tile();

        tile.x = 4;
        tile.y = 2;

        return tile;
    }

    public override bool IsSolid(Direction direction)
    {
        return false;
    }
}
