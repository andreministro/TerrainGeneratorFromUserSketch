using UnityEngine;

public class ReadImage : MonoBehaviour
{
    private static (byte[] Png, int Seed)? queuedSketch;

    [SerializeField]
    public Texture2D image;

    public TerrainManager world;
    public int seed;

    private Texture2D runtimeSketch;
    private Biome[] biomes;

    public int mapWidth;
    public int mapHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetQueuedSketch()
    {
        queuedSketch = null;
    }

    internal static void QueueSketch(byte[] png, int seed)
    {
        queuedSketch = (png, seed);
    }

    public void DisplayImage()
    {
        if (world.IsBuilding || world.HasTerrain)
            return;

        if (queuedSketch.HasValue)
        {
            var (png, queuedSeed) = queuedSketch.Value;
            queuedSketch = null;

            runtimeSketch = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false)
            {
                name = "savedSketch",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            if (!runtimeSketch.LoadImage(png))
            {
                Debug.LogError("The saved sketch PNG could not be loaded.", this);
                return;
            }

            image = runtimeSketch;
            seed = queuedSeed;
        }

        if (image == null)
        {
            Debug.LogError("Open the sketch scene and save a sketch, or assign a PNG to ImageReader.", this);
            return;
        }

        if (!TerrainManager.IsSupportedSketchSize(image.width, image.height))
        {
            Debug.LogError($"Use a square sketch between 1 × 1 and {TerrainManager.MaxSketchSize} × {TerrainManager.MaxSketchSize}.", this);
            return;
        }

        mapWidth = image.width;
        mapHeight = image.height;
        Color32[] pixels = image == runtimeSketch ? image.GetPixels32() : ReadImportedPixels(image);
        biomes = new Biome[pixels.Length];
        int adjustedPixelCount = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            biomes[i] = BiomePalette.Decode(pixels[i], out bool adjusted);
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

    private static Color32[] ReadImportedPixels(Texture2D source)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(
            source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, mipChain: false);
        try
        {
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            return readable.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            Destroy(readable);
        }
    }

    internal Biome GetBiome(int worldX, int worldZ)
    {
        int x = worldX + mapWidth / 2;
        int z = worldZ + mapHeight / 2;
        if (biomes == null || (uint)x >= (uint)mapWidth || (uint)z >= (uint)mapHeight)
            return Biome.None;

        return biomes[z * mapWidth + x];
    }

    private void OnDestroy()
    {
        if (runtimeSketch != null)
            Destroy(runtimeSketch);
    }
}
