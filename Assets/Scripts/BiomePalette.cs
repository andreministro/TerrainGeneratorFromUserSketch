using System;
using UnityEngine;

internal enum Biome : byte
{
    None,
    Water,
    HotDesert,
    AlpineTundra,
    TemperateForest,
    ColdDesert,
    Taiga,
    TropicalForest,
    Savannah
}

internal static class BiomePalette
{
    // Indexed by the three RGB bits, from black (000) to white (111).
    private static readonly Biome[] Colors =
    {
        Biome.Taiga, Biome.Water, Biome.TemperateForest, Biome.TropicalForest,
        Biome.Savannah, Biome.AlpineTundra, Biome.HotDesert, Biome.ColdDesert
    };

    internal static Color32 GetColor(Biome biome)
    {
        int index = Array.IndexOf(Colors, biome);
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(biome));

        return new Color32(
            (byte)((index & 4) != 0 ? 255 : 0),
            (byte)((index & 2) != 0 ? 255 : 0),
            (byte)((index & 1) != 0 ? 255 : 0),
            255);
    }

    internal static Biome Decode(Color color, out bool adjusted)
    {
        adjusted = color.a != 1f
            || !IsPaletteChannel(color.r) || !IsPaletteChannel(color.g) || !IsPaletteChannel(color.b);

        // Match the white background in the sketch canvas.
        if (color.a < 1f)
            color = Color.Lerp(Color.white, color, color.a);

        // The eight palette colors are the corners of the RGB cube.
        int index = (color.r >= 0.5f ? 4 : 0) | (color.g >= 0.5f ? 2 : 0) | (color.b >= 0.5f ? 1 : 0);
        return Colors[index];
    }

    private static bool IsPaletteChannel(float channel)
    {
        return channel == 0f || channel == 1f;
    }
}
