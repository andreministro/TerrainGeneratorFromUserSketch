using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class BlockTropicalLeaf : Block
{
    public BlockTropicalLeaf() : base()
    {

    }

    public override Tile TexturePosition(Direction direction)
    {
        Tile tile = new Tile();

        tile.x = 3;
        tile.y = 4;

        return tile;
    }

    public override bool IsSolid(Direction direction)
    {
        return false;
    }

}
