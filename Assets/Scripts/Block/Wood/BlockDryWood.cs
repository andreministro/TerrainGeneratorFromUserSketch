using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class BlockDryWood : Block
{
    public BlockDryWood() : base()
    {

    }

    public override Tile TexturePosition(Direction direction)
    {
        Tile tile = new Tile();

        switch (direction)
        {
            case Direction.up:
                tile.x = 0;
                tile.y = 3;
                return tile;

            case Direction.down:
                tile.x = 0;
                tile.y = 3;
                return tile;
        }

        tile.x = 1;
        tile.y = 3;

        return tile;
    }
}
