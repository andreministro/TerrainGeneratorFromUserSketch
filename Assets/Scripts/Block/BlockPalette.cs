internal enum BlockId : byte
{
    Unset,
    Air,
    Stone,
    Water,
    Sand,
    Snow,
    DirtGrass,
    GrassNormal,
    GrassDark,
    GrassTropical,
    GrassDry,
    RedFlower,
    BlueFlower,
    PinkFlower,
    DryBush,
    FrozenBush,
    NormalLeaf,
    DarkLeaf,
    TropicalLeaf,
    DryLeaf,
    NormalWood,
    TropicalWood,
    DryWood,
    Cactus,
    Custom = byte.MaxValue
}

internal static class BlockPalette
{
    private static readonly Block[] definitions = CreateDefinitions();

    private static Block[] CreateDefinitions()
    {
        var blocks = new Block[(int)BlockId.Cactus + 1];
        blocks[(int)BlockId.Air] = BlockAir.Empty;
        blocks[(int)BlockId.Stone] = new Block();
        blocks[(int)BlockId.Water] = new BlockWater();
        blocks[(int)BlockId.Sand] = new BlockSand();
        blocks[(int)BlockId.Snow] = new BlockSnow();
        blocks[(int)BlockId.DirtGrass] = new BlockDirtGrass();
        blocks[(int)BlockId.GrassNormal] = new BlockGrassNormal();
        blocks[(int)BlockId.GrassDark] = new BlockGrassDark();
        blocks[(int)BlockId.GrassTropical] = new BlockGrassTropical();
        blocks[(int)BlockId.GrassDry] = new BlockGrassDry();
        blocks[(int)BlockId.RedFlower] = new BlockRedFlower();
        blocks[(int)BlockId.BlueFlower] = new BlockBlueFlower();
        blocks[(int)BlockId.PinkFlower] = new BlockPinkFlower();
        blocks[(int)BlockId.DryBush] = new BlockDryBush();
        blocks[(int)BlockId.FrozenBush] = new BlockFrozenBush();
        blocks[(int)BlockId.NormalLeaf] = new BlockNormalLeaf();
        blocks[(int)BlockId.DarkLeaf] = new BlockDarkLeaf();
        blocks[(int)BlockId.TropicalLeaf] = new BlockTropicalLeaf();
        blocks[(int)BlockId.DryLeaf] = new BlockDryLeaf();
        blocks[(int)BlockId.NormalWood] = new BlockNormalWood();
        blocks[(int)BlockId.TropicalWood] = new BlockTropicalWood();
        blocks[(int)BlockId.DryWood] = new BlockDryWood();
        blocks[(int)BlockId.Cactus] = new BlockCactus();

        for (int i = (int)BlockId.Stone; i < blocks.Length; i++)
            blocks[i].CacheFaceUVs();

        return blocks;
    }

    internal static Block Get(BlockId id) => definitions[(int)id];
}
