using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class BlockSand : Block
{
    public BlockSand() : base()
    {

    }

    public override Tile TexturePosition(Direction direction)
    {
        Tile tile = new Tile();

        tile.x = 2;
        tile.y = 2;

        return tile;
    }

}
