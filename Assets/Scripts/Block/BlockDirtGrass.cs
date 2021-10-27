using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class BlockDirtGrass : Block
{
    public BlockDirtGrass() : base()
    {

    }

    public override Tile TexturePosition(Direction direction)
    {
        Tile tile = new Tile();

        tile.x = 5;
        tile.y = 4;

        return tile;
    }

    public override bool IsSolid(Direction direction)
    {
        return true;
    }
}