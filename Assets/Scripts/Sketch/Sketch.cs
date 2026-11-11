using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(MeshCollider), typeof(MeshRenderer))]
public class Sketch : MonoBehaviour
{
    private static readonly int MainTexture = Shader.PropertyToID("_MainTex");

    [SerializeField, Min(1)] private int resolution = 32;

    private MeshCollider canvasCollider;
    private Camera viewCamera;
    private Texture2D canvasTexture;
    private Color32? brushColor;
    private float brushSize = 0.1f;
    private bool strokeActive;
    private Vector2Int? lastPixel;

    private void Awake()
    {
        canvasCollider = GetComponent<MeshCollider>();
        viewCamera = Camera.main;
        canvasTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, mipChain: false)
        {
            name = "Sketch Canvas",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = canvasTexture.GetRawTextureData<Color32>();
        Color32 background = BiomePalette.GetColor(Biome.ColdDesert);
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = background;
        canvasTexture.Apply(updateMipmaps: false);

        var renderer = GetComponent<MeshRenderer>();
        var properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        properties.SetTexture(MainTexture, canvasTexture);
        renderer.SetPropertyBlock(properties);
    }

    private void LateUpdate()
    {
        UpdateStroke(Input.mousePosition, Input.GetMouseButtonDown(0), Input.GetMouseButton(0));
    }

    private void UpdateStroke(Vector2 screenPosition, bool pressed, bool held)
    {
        if (!held)
        {
            strokeActive = false;
            lastPixel = null;
            return;
        }

        if (!brushColor.HasValue || (!strokeActive && !pressed))
            return;

        bool overCanvas = TryGetCanvasPixel(screenPosition, out Vector2Int pixel);
        if (pressed)
        {
            strokeActive = overCanvas;
            lastPixel = null;
        }

        if (!overCanvas)
        {
            lastPixel = null;
            return;
        }

        if (lastPixel != pixel)
        {
            PaintStroke(lastPixel ?? pixel, pixel, brushColor.Value);
            lastPixel = pixel;
        }
    }

    private bool TryGetCanvasPixel(Vector2 screenPosition, out Vector2Int pixel)
    {
        pixel = default;
        if (viewCamera == null || !viewCamera.pixelRect.Contains(screenPosition)
            || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            return false;

        Ray ray = viewCamera.ScreenPointToRay(screenPosition);
        if (!canvasCollider.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            return false;

        Vector2 uv = hit.textureCoord;
        pixel = new Vector2Int(
            Mathf.Clamp(Mathf.FloorToInt(uv.x * resolution), 0, resolution - 1),
            Mathf.Clamp(Mathf.FloorToInt(uv.y * resolution), 0, resolution - 1));
        return true;
    }

    private void PaintStroke(Vector2Int start, Vector2Int end, Color32 color)
    {
        float radius = Mathf.Max(0.5f, brushSize * resolution * 0.5f);
        float radiusSquared = radius * radius;
        int minY = Mathf.Max(0, Mathf.CeilToInt(Mathf.Min(start.y, end.y) - radius));
        int maxY = Mathf.Min(resolution - 1, Mathf.FloorToInt(Mathf.Max(start.y, end.y) + radius));

        Vector2 from = start;
        Vector2 to = end;
        Vector2 delta = to - from;
        Vector2 normal = new Vector2(-delta.y, delta.x).normalized * radius;
        Vector2 firstSide = from + normal;
        Vector2 secondSide = from - normal;
        bool isDrag = start != end;

        // The stroke has two round ends joined by straight sides.
        // Its intersection with each row is one contiguous span.
        var pixels = canvasTexture.GetRawTextureData<Color32>();
        for (int y = minY; y <= maxY; y++)
        {
            float left = float.PositiveInfinity;
            float right = float.NegativeInfinity;
            IncludeBrushCap(from, y, radiusSquared, ref left, ref right);
            if (isDrag)
            {
                IncludeBrushCap(to, y, radiusSquared, ref left, ref right);
                if (delta.y != 0f)
                {
                    IncludeBrushSide(firstSide, delta, y, ref left, ref right);
                    IncludeBrushSide(secondSide, delta, y, ref left, ref right);
                }
            }

            if (left > right)
                continue;

            int minX = Mathf.Max(0, Mathf.CeilToInt(left));
            int maxX = Mathf.Min(resolution - 1, Mathf.FloorToInt(right));
            int row = y * resolution;
            for (int x = minX; x <= maxX; x++)
                pixels[row + x] = color;
        }

        canvasTexture.Apply(updateMipmaps: false);
    }

    private static void IncludeBrushCap(Vector2 center, int y, float radiusSquared, ref float left, ref float right)
    {
        float offsetY = y - center.y;
        float halfWidthSquared = radiusSquared - offsetY * offsetY;
        if (halfWidthSquared < 0f)
            return;

        float halfWidth = Mathf.Sqrt(halfWidthSquared);
        left = Mathf.Min(left, center.x - halfWidth);
        right = Mathf.Max(right, center.x + halfWidth);
    }

    private static void IncludeBrushSide(Vector2 start, Vector2 delta, int y, ref float left, ref float right)
    {
        float t = (y - start.y) / delta.y;
        if (t < 0f || t > 1f)
            return;

        float x = start.x + t * delta.x;
        left = Mathf.Min(left, x);
        right = Mathf.Max(right, x);
    }

    public void ChangeBrushSizeBig() => brushSize = 0.9f;
    public void ChangeBrushSizeMedium() => brushSize = 0.4f;
    public void ChangeBrushSizeSmall() => brushSize = 0.1f;
    public void ChangeBrushSizeTiny() => brushSize = 0.01f;

    public void ChangeBrushtoWater() => brushColor = BiomePalette.GetColor(Biome.Water);
    public void ChangeBrushtoTemperateForest() => brushColor = BiomePalette.GetColor(Biome.TemperateForest);
    public void ChangeBrushtoHotDesert() => brushColor = BiomePalette.GetColor(Biome.HotDesert);
    public void ChangeBrushtoAlpineTundra() => brushColor = BiomePalette.GetColor(Biome.AlpineTundra);
    public void ChangeBrushtoTaiga() => brushColor = BiomePalette.GetColor(Biome.Taiga);
    public void ChangeBrushtoColdDesert() => brushColor = BiomePalette.GetColor(Biome.ColdDesert);
    public void ChangeBrushtoSavannah() => brushColor = BiomePalette.GetColor(Biome.Savannah);
    public void ChangeBrushtoTropicalForest() => brushColor = BiomePalette.GetColor(Biome.TropicalForest);

    public void Save()
    {
        string path = Path.Combine(Application.dataPath, "savedSketch.png");
        File.WriteAllBytes(path, canvasTexture.EncodeToPNG());
        Debug.Log($"Saved sketch to {path}.", this);
    }

    private void OnDestroy()
    {
        if (canvasTexture != null)
            Destroy(canvasTexture);
    }
}
