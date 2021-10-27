using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class BlockGrassTropical : Block
{
    public BlockGrassTropical() : base()
    {

    }

    public override Tile TexturePosition(Direction direction)
    {
        Tile tile = new Tile();

        switch (direction)
        {
            case Direction.up:
                tile.x = 4;
                tile.y = 3;
                return tile;

            case Direction.down:
                tile.x = 1;
                tile.y = 0;
                return tile;
        }

        tile.x = 4;
        tile.y = 4;

        return tile;
    }
}
