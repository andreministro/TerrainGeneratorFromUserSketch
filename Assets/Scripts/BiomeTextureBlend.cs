using System;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class BiomeTextureBlend : IDisposable
{
    // Land biomes occupy seven channels in enum order, starting with HotDesert.
    private const int LandBiomeCount = (int)Biome.Savannah - (int)Biome.HotDesert + 1;
    private static readonly BlockId[] SurfaceBlocks =
    {
        BlockId.Sand, BlockId.DirtGrass, BlockId.GrassNormal, BlockId.Snow,
        BlockId.GrassDark, BlockId.GrassTropical, BlockId.GrassDry
    };

    private readonly ReadImage sketch;
    private readonly int radius;
    private readonly int[] columnCounts;
    private readonly int[] counts = new int[LandBiomeCount];
    private readonly Texture2D firstWeights;
    private readonly Texture2D secondWeights;
    private readonly int width;
    private readonly int height;

    public int Rows => height;
    public int Radius => radius;

    public BiomeTextureBlend(ReadImage sketch, int radius)
    {
        this.sketch = sketch;
        this.radius = radius;
        width = sketch.mapWidth;
        height = sketch.mapHeight;
        columnCounts = new int[width * LandBiomeCount];
        firstWeights = CreateTexture("Biome weights 1");
        secondWeights = CreateTexture("Biome weights 2");

        for (int z = 0; z < Mathf.Min(radius, height); z++)
            CountRow(z, 1);
    }

    private Texture2D CreateTexture(string name) => new Texture2D(width, height, TextureFormat.RGBA32, false, true)
    {
        name = name,
        wrapMode = TextureWrapMode.Clamp,
        filterMode = FilterMode.Bilinear,
        hideFlags = HideFlags.DontSave
    };

    public void WriteRow(int z)
    {
        if (z + radius < height)
            CountRow(z + radius, 1);
        if (z - radius - 1 >= 0)
            CountRow(z - radius - 1, -1);

        Array.Clear(counts, 0, counts.Length);
        for (int x = 0; x < Mathf.Min(radius, width); x++)
            CountColumn(x, 1);

        var first = firstWeights.GetRawTextureData<Color32>();
        var second = secondWeights.GetRawTextureData<Color32>();
        for (int x = 0; x < width; x++)
        {
            if (x + radius < width)
                CountColumn(x + radius, 1);
            if (x - radius - 1 >= 0)
                CountColumn(x - radius - 1, -1);

            int landCount = counts[0] + counts[1] + counts[2] + counts[3] + counts[4] + counts[5] + counts[6];
            int index = z * width + x;
            if (landCount == 0 || GetBiome(x, z) < Biome.HotDesert)
            {
                first[index] = default;
                second[index] = default;
                continue;
            }

            first[index] = new Color32(Weight(0), Weight(1), Weight(2), Weight(3));
            second[index] = new Color32(Weight(4), Weight(5), Weight(6), 0);

            byte Weight(int biome) => (byte)((counts[biome] * 255 + landCount / 2) / landCount);
        }
    }

    private Biome GetBiome(int x, int z) => sketch.GetBiome(x - width / 2, z - height / 2);

    private void CountRow(int z, int change)
    {
        for (int x = 0; x < width; x++)
        {
            int biome = (int)GetBiome(x, z) - (int)Biome.HotDesert;
            if ((uint)biome < LandBiomeCount)
                columnCounts[x * LandBiomeCount + biome] += change;
        }
    }

    private void CountColumn(int x, int change)
    {
        int start = x * LandBiomeCount;
        for (int biome = 0; biome < LandBiomeCount; biome++)
            counts[biome] += columnCounts[start + biome] * change;
    }

    public void ApplyTo(Material material)
    {
        firstWeights.Apply(false, true);
        secondWeights.Apply(false, true);
        material.SetTexture("_BiomeWeights1", firstWeights);
        material.SetTexture("_BiomeWeights2", secondWeights);
        material.SetVector("_BiomeMapSize", new Vector4(width, height, width / 2 + 0.5f, height / 2 + 0.5f));
        var tiles = new Vector4[LandBiomeCount];
        var bottomTiles = new Vector4[LandBiomeCount];
        for (int biome = 0; biome < LandBiomeCount; biome++)
        {
            Block block = BlockPalette.Get(SurfaceBlocks[biome]);
            Block.Tile top = block.TexturePosition(Block.Direction.up);
            Block.Tile side = block.TexturePosition(Block.Direction.north);
            Block.Tile bottom = block.TexturePosition(Block.Direction.down);
            tiles[biome] = new Vector4(top.x, top.y, side.x, side.y);
            bottomTiles[biome] = new Vector4(bottom.x, bottom.y, 0, 0);
        }
        material.SetVectorArray("_BiomeTiles", tiles);
        material.SetVectorArray("_BiomeBottomTiles", bottomTiles);
        material.SetFloat("_BlendBiomes", 1f);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(firstWeights);
        UnityEngine.Object.Destroy(secondWeights);
    }
}
