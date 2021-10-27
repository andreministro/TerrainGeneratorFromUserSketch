using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class BlockTropicalWood : Block
{
    public BlockTropicalWood() : base()
    {

    }

    public override Tile TexturePosition(Direction direction)
    {
        Tile tile = new Tile();

        switch (direction)
        {
            case Direction.up:
                tile.x = 0;
                tile.y = 4;
                return tile;

            case Direction.down:
                tile.x = 0;
                tile.y = 4;
                return tile;
        }

        tile.x = 1;
        tile.y = 4;

        return tile;
    }
}
