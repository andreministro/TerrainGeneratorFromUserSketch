using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class BlockBrick : Block
{
    public BlockBrick() : base()
    {

    }

    public override Tile TexturePosition(Direction direction)
    {
        Tile tile = new Tile();

        tile.x = 0;
        tile.y = 5;

        return tile;
    }

}