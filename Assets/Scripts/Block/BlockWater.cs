using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class BlockWater : Block
{
    public BlockWater() : base()
    {

    }

    public override Tile TexturePosition(Direction direction)
    {
        Tile tile = new Tile();

        tile.x = 1;
        tile.y = 2;

        return tile;
    }

    public override bool IsSolid(Direction direction)
    {
        return true;
    }

}