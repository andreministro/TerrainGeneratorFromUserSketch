using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class BlockPinkFlower : Block
{
    public BlockPinkFlower() : base()
    {

    }

    public override Tile TexturePosition(Direction direction)
    {
        Tile tile = new Tile();

        switch (direction)
        {
            case Direction.up:
                tile.x = 5;
                tile.y = 5;
                return tile;
        }

        tile.x = 5;
        tile.y = 3;

        return tile;
    }

    public override bool IsSolid(Direction direction)
    {
        return false;
    }
}