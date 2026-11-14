using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class BiomeHeightBlend
{
    private const int TileShift = 4;
    private const int TileSize = 1 << TileShift;
    private const int CachedTileCount = 256;
    private const int Unblended = int.MinValue;

    private readonly ReadImage sketch;
    private readonly int width;
    private readonly Func<int, int, Biome, int> sampleSurface;
    private readonly RectInt sketchBounds;
    private readonly int firstTileX;
    private readonly int firstTileZ;
    private readonly int tileColumns;
    private readonly byte[] biomeMasks;
    private readonly SampleTotals[] sums;
    private readonly Dictionary<Vector2Int, int[]> tiles = new Dictionary<Vector2Int, int[]>();
    private readonly Queue<Vector2Int> tileOrder = new Queue<Vector2Int>();

    public BiomeHeightBlend(ReadImage sketch, int width, Func<int, int, Biome, int> sampleSurface)
    {
        this.sketch = sketch;
        this.width = width;
        this.sampleSurface = sampleSurface;
        sketchBounds = new RectInt(-(sketch.mapWidth / 2), -(sketch.mapHeight / 2), sketch.mapWidth, sketch.mapHeight);
        firstTileX = sketchBounds.xMin >> TileShift;
        firstTileZ = sketchBounds.yMin >> TileShift;
        tileColumns = ((sketchBounds.xMax - 1) >> TileShift) - firstTileX + 1;
        int tileRows = ((sketchBounds.yMax - 1) >> TileShift) - firstTileZ + 1;
        biomeMasks = new byte[tileColumns * tileRows];
        int paddedSide = TileSize + 2 * width + 1;
        sums = new SampleTotals[paddedSide * paddedSide];
    }

    public bool TryGetSurface(int x, int z, out int height)
    {
        height = Unblended;
        if (!sketchBounds.Contains(new Vector2Int(x, z)))
            return false;

        var tile = new Vector2Int(x >> TileShift, z >> TileShift);
        if (!tiles.TryGetValue(tile, out int[] heights))
        {
            heights = BuildTile(tile.x * TileSize, tile.y * TileSize);
            if (tiles.Count == CachedTileCount)
                tiles.Remove(tileOrder.Dequeue());
            tiles.Add(tile, heights);
            tileOrder.Enqueue(tile);
        }

        if (heights == null)
            return false;
        int localX = x - tile.x * TileSize;
        int localZ = z - tile.y * TileSize;
        height = heights[localZ * TileSize + localX];
        return height != Unblended;
    }

    private int[] BuildTile(int originX, int originZ)
    {
        int minX = Mathf.Max(sketchBounds.xMin, originX - width);
        int minZ = Mathf.Max(sketchBounds.yMin, originZ - width);
        int endX = Mathf.Min(sketchBounds.xMax, originX + TileSize + width);
        int endZ = Mathf.Min(sketchBounds.yMax, originZ + TileSize + width);
        if (!MayContainMultipleBiomes(minX, minZ, endX, endZ))
            return null;

        int stride = endX - minX + 1;
        BuildSummedArea(minX, minZ, endX, endZ, stride);

        int[] heights = null;
        int outputMinX = Mathf.Max(sketchBounds.xMin, originX);
        int outputMinZ = Mathf.Max(sketchBounds.yMin, originZ);
        int outputEndX = Mathf.Min(sketchBounds.xMax, originX + TileSize);
        int outputEndZ = Mathf.Min(sketchBounds.yMax, originZ + TileSize);
        for (int z = outputMinZ; z < outputEndZ; z++)
        {
            int z0 = Mathf.Max(z - width, minZ) - minZ;
            int z1 = Mathf.Min(z + width + 1, endZ) - minZ;
            for (int x = outputMinX; x < outputEndX; x++)
            {
                int x0 = Mathf.Max(x - width, minX) - minX;
                int x1 = Mathf.Min(x + width + 1, endX) - minX;
                SampleTotals total = sums[z1 * stride + x1] - sums[z1 * stride + x0]
                    - sums[z0 * stride + x1] + sums[z0 * stride + x0];

                // Biome IDs have zero variance when every sample belongs to the same biome.
                if ((long)total.Count * total.BiomeSquared == (long)total.Biome * total.Biome)
                    continue;

                if (heights == null)
                {
                    heights = new int[TileSize * TileSize];
                    Array.Fill(heights, Unblended);
                }
                heights[(z - originZ) * TileSize + x - originX] = (int)Math.Round((double)total.Height / total.Count);
            }
        }
        return heights;
    }

    private void BuildSummedArea(int minX, int minZ, int endX, int endZ, int stride)
    {
        // A summed-area table makes each local average independent of the blend width.
        Array.Clear(sums, 0, stride);
        for (int z = minZ; z < endZ; z++)
        {
            int row = (z - minZ + 1) * stride;
            sums[row] = default;
            SampleTotals running = default;
            for (int x = minX; x < endX; x++)
            {
                Biome biome = sketch.GetBiome(x, z);
                if (biome != Biome.None)
                {
                    int id = (int)biome;
                    running.Count++;
                    running.Height += sampleSurface(x, z, biome);
                    running.Biome += id;
                    running.BiomeSquared += id * id;
                }
                int index = row + x - minX + 1;
                sums[index] = sums[index - stride] + running;
            }
        }
    }

    private bool MayContainMultipleBiomes(int minX, int minZ, int endX, int endZ)
    {
        int mask = 0;
        for (int z = minZ >> TileShift; z <= (endZ - 1) >> TileShift; z++)
        {
            for (int x = minX >> TileShift; x <= (endX - 1) >> TileShift; x++)
            {
                mask |= GetBiomeMask(x, z);
                if ((mask & (mask - 1)) != 0)
                    return true;
            }
        }
        return false;
    }

    private byte GetBiomeMask(int tileX, int tileZ)
    {
        int index = (tileZ - firstTileZ) * tileColumns + tileX - firstTileX;
        byte mask = biomeMasks[index];
        if (mask != 0)
            return mask;

        int x0 = Mathf.Max(sketchBounds.xMin, tileX * TileSize);
        int z0 = Mathf.Max(sketchBounds.yMin, tileZ * TileSize);
        int x1 = Mathf.Min(sketchBounds.xMax, (tileX + 1) * TileSize);
        int z1 = Mathf.Min(sketchBounds.yMax, (tileZ + 1) * TileSize);
        for (int z = z0; z < z1; z++)
        {
            for (int x = x0; x < x1; x++)
            {
                Biome biome = sketch.GetBiome(x, z);
                if (biome != Biome.None)
                    mask |= (byte)(1 << ((int)biome - 1));
            }
        }
        biomeMasks[index] = mask;
        return mask;
    }

    private struct SampleTotals
    {
        public long Height;
        public int Count;
        public int Biome;
        public int BiomeSquared;

        public static SampleTotals operator +(SampleTotals left, SampleTotals right) => new SampleTotals
        {
            Height = left.Height + right.Height,
            Count = left.Count + right.Count,
            Biome = left.Biome + right.Biome,
            BiomeSquared = left.BiomeSquared + right.BiomeSquared
        };

        public static SampleTotals operator -(SampleTotals left, SampleTotals right) => new SampleTotals
        {
            Height = left.Height - right.Height,
            Count = left.Count - right.Count,
            Biome = left.Biome - right.Biome,
            BiomeSquared = left.BiomeSquared - right.BiomeSquared
        };
    }
}
