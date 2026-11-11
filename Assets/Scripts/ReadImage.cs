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

public class ReadImage : MonoBehaviour
{
    // Indexed by the three RGB bits, from black (000) to white (111).
    private static readonly Biome[] Palette =
    {
        Biome.Taiga, Biome.Water, Biome.TemperateForest, Biome.TropicalForest,
        Biome.Savannah, Biome.AlpineTundra, Biome.HotDesert, Biome.ColdDesert
    };

    [SerializeField]
    public Texture2D image;

    public TerrainManager world;
    public int seed;

    private Biome[] biomes;

    public int mapWidth;
    public int mapHeight;

    public void DisplayImage()
    {
        //Prepare input texture
        Texture2D duplicateTexture(Texture2D image)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        image.width,
                        image.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(image, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(image.width, image.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.filterMode = FilterMode.Point;
            readableText.wrapMode = TextureWrapMode.Clamp;
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

        Texture2D copy = duplicateTexture(image);

        Color[] colorMapSketch = copy.GetPixels();

        mapWidth = image.width;
        mapHeight = image.height;

        biomes = new Biome[colorMapSketch.Length];
        int adjustedPixelCount = 0;
        for (int i = 0; i < colorMapSketch.Length; i++)
        {
            biomes[i] = DecodeBiome(colorMapSketch[i], out bool adjusted);
            if (adjusted)
                adjustedPixelCount++;
        }

        if (adjustedPixelCount > 0)
        {
            Debug.LogWarning(
                $"Mapped {adjustedPixelCount} of {biomes.Length} off-palette or translucent pixels in sketch '{image.name}' to the nearest biome colors.",
                this);
        }

        world.CreateFromSketch(mapWidth, mapHeight);
    }

    internal Biome GetBiome(int worldX, int worldZ)
    {
        int x = worldX + mapWidth / 2;
        int z = worldZ + mapHeight / 2;
        if (biomes == null || (uint)x >= (uint)mapWidth || (uint)z >= (uint)mapHeight)
            return Biome.None;

        return biomes[z * mapWidth + x];
    }

    private static Biome DecodeBiome(Color color, out bool adjusted)
    {
        adjusted = color.a != 1f
            || !IsPaletteChannel(color.r) || !IsPaletteChannel(color.g) || !IsPaletteChannel(color.b);

        // Match the white background in the sketch canvas.
        if (color.a < 1f)
            color = Color.Lerp(Color.white, color, color.a);

        // The eight palette colors are the corners of the RGB cube.
        int index = (color.r >= 0.5f ? 4 : 0) | (color.g >= 0.5f ? 2 : 0) | (color.b >= 0.5f ? 1 : 0);
        return Palette[index];
    }

    private static bool IsPaletteChannel(float channel)
    {
        return channel == 0f || channel == 1f;
    }
}
