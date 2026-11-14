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
    private int[] occupiedColumnsByChunk;
    private int chunkCount;
    private int firstChunk;
    private Texture2D paintedMask;
    internal bool HasPaintedPixels { get; private set; }
    internal bool HasBlankPixels { get; private set; }

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
        HasPaintedPixels = false;
        HasBlankPixels = false;
        int size = Chunk.chunkSize;
        int min = -(mapWidth / 2);
        firstChunk = TerrainManager.GetChunkOrigin(min) / size;
        chunkCount = TerrainManager.GetChunkOrigin(min + mapWidth - 1) / size - firstChunk + 1;
        occupiedColumnsByChunk = new int[chunkCount * chunkCount];
        int adjustedPixelCount = 0;
        int origin = min - firstChunk * size;
        for (int i = 0; i < pixels.Length; i++)
        {
            Biome biome = BiomePalette.Decode(pixels[i], out bool adjusted);
            biomes[i] = biome;
            if (adjusted)
                adjustedPixelCount++;
            if (biome == Biome.None)
            {
                HasBlankPixels = true;
                continue;
            }

            int chunkX = (i % mapWidth + origin) / size;
            int chunkZ = (i / mapWidth + origin) / size;
            HasPaintedPixels = true;
            occupiedColumnsByChunk[chunkZ * chunkCount + chunkX]++;
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

    internal bool HasPaintedChunk(int originX, int originZ)
    {
        int x = originX / Chunk.chunkSize - firstChunk;
        int z = originZ / Chunk.chunkSize - firstChunk;
        return (uint)x < (uint)chunkCount && (uint)z < (uint)chunkCount
            && occupiedColumnsByChunk[z * chunkCount + x] > 0;
    }

    internal int CountPainted(RectInt area)
    {
        int size = Chunk.chunkSize;
        int originX = TerrainManager.GetChunkOrigin(area.xMin);
        int originZ = TerrainManager.GetChunkOrigin(area.yMin);
        if (area.xMax <= originX + size && area.yMax <= originZ + size)
        {
            int chunkX = originX / size - firstChunk;
            int chunkZ = originZ / size - firstChunk;
            if ((uint)chunkX >= (uint)chunkCount || (uint)chunkZ >= (uint)chunkCount)
                return 0;
            int count = occupiedColumnsByChunk[chunkZ * chunkCount + chunkX];
            if (count == 0)
                return 0;
            if (count == size * size)
                return area.width * area.height;
        }

        int painted = 0;
        for (int z = area.yMin; z < area.yMax; z++)
        for (int x = area.xMin; x < area.xMax; x++)
            if (GetBiome(x, z) != Biome.None)
                painted++;
        return painted;
    }

    internal Vector2Int FindPaintedColumn(Vector2Int near)
    {
        if (GetBiome(near.x, near.y) != Biome.None || !HasPaintedPixels)
            return near;

        Vector2Int closest = near;
        long bestDistance = long.MaxValue;
        int minimum = -(mapWidth / 2);
        for (int i = 0; i < biomes.Length; i++)
        {
            if (biomes[i] == Biome.None)
                continue;
            int x = i % mapWidth + minimum;
            int z = i / mapWidth + minimum;
            long dx = (long)x - near.x;
            long dz = (long)z - near.y;
            long distance = dx * dx + dz * dz;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                closest = new Vector2Int(x, z);
            }
        }
        return closest;
    }

    internal Texture2D GetPaintedMask()
    {
        if (paintedMask == null)
        {
            paintedMask = new Texture2D(mapWidth, mapHeight, TextureFormat.R8, false, true)
            {
                name = "Painted Sketch Area",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var data = paintedMask.GetRawTextureData<byte>();
            for (int i = 0; i < biomes.Length; i++)
                data[i] = biomes[i] == Biome.None ? (byte)0 : (byte)255;
            paintedMask.Apply(false, true);
        }
        return paintedMask;
    }

    private void OnDestroy()
    {
        if (runtimeSketch != null)
            Destroy(runtimeSketch);
        if (paintedMask != null)
            Destroy(paintedMask);
    }
}
