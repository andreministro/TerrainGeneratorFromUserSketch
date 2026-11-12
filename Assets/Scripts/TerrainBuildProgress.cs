using UnityEngine;
using UnityEngine.UI;

public class TerrainBuildProgress : MonoBehaviour
{
    private const float SpinnerDelaySeconds = 0.12f;

    [SerializeField] private TerrainManager terrain;
    [SerializeField] private CameraController views;
    [SerializeField] private Font controlsFont;

    private Text controls;
    private TerrainLoadingSpinner spinner;
    private float busySince = float.PositiveInfinity;

    private void Awake()
    {
        var hint = new GameObject("Terrain Controls", typeof(RectTransform), typeof(Text), typeof(Shadow));
        hint.transform.SetParent(transform, false);
        controls = hint.GetComponent<Text>();
        controls.font = controlsFont;
        controls.fontSize = 16;
        controls.alignment = TextAnchor.LowerLeft;
        controls.color = Color.white;
        controls.raycastTarget = false;
        controls.rectTransform.anchorMin = Vector2.zero;
        controls.rectTransform.anchorMax = Vector2.right;
        controls.rectTransform.offsetMin = new Vector2(14f, 12f);
        controls.rectTransform.offsetMax = new Vector2(-14f, 62f);

        var icon = new GameObject("Terrain Loading", typeof(RectTransform), typeof(TerrainLoadingSpinner), typeof(Shadow));
        icon.transform.SetParent(transform, false);
        spinner = icon.GetComponent<TerrainLoadingSpinner>();
        spinner.raycastTarget = false;
        spinner.rectTransform.anchorMin = Vector2.one;
        spinner.rectTransform.anchorMax = Vector2.one;
        spinner.rectTransform.anchoredPosition = new Vector2(-30f, -30f);
        spinner.rectTransform.sizeDelta = new Vector2(28f, 28f);
        spinner.enabled = false;
    }

    private void LateUpdate()
    {
        controls.text = views.ControlHint;
        bool busy = terrain.IsBuilding || (terrain.IsStreamingDetail && !views.orthoCam.enabled);
        float now = Time.unscaledTime;
        busySince = busy ? Mathf.Min(busySince, now) : float.PositiveInfinity;
        spinner.enabled = now - busySince >= SpinnerDelaySeconds;
        if (spinner.enabled)
            spinner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -270f * now);
    }
}

internal sealed class TerrainLoadingSpinner : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper geometry)
    {
        geometry.Clear();
        const int segments = 24;
        Vector2 center = rectTransform.rect.center;
        float outer = rectTransform.rect.width * 0.45f;
        float inner = outer * 0.73f;
        for (int i = 0; i <= segments; i++)
        {
            float fraction = i / (float)segments;
            float angle = fraction * 300f * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Color32 tint = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.16f, fraction));
            geometry.AddVert(center + direction * inner, tint, Vector2.zero);
            geometry.AddVert(center + direction * outer, tint, Vector2.zero);
            if (i == 0)
                continue;

            int last = i * 2;
            geometry.AddTriangle(last - 2, last - 1, last + 1);
            geometry.AddTriangle(last - 2, last + 1, last);
        }
    }
}
