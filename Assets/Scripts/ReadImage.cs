using UnityEngine;

public class ReadImage : MonoBehaviour
{
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
            biomes[i] = BiomePalette.Decode(colorMapSketch[i], out bool adjusted);
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
}
