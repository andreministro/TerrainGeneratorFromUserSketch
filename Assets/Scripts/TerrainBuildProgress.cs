using UnityEngine;
using UnityEngine.UI;

public class TerrainBuildProgress : MonoBehaviour
{
    [SerializeField] private TerrainManager terrain;
    [SerializeField] private GameObject panel;
    [SerializeField] private Text status;
    [SerializeField] private RectTransform fill;

    private int displayedSteps = -1;

    private void LateUpdate()
    {
        if (!terrain.IsBuilding)
        {
            if (panel.activeSelf)
                panel.SetActive(false);

            displayedSteps = -1;
            return;
        }

        int completed = terrain.CompletedBuildSteps;
        if (completed == displayedSteps)
            return;

        int total = terrain.TotalBuildSteps;
        float progress = (float)completed / total;
        string stage = completed < total / 2 ? "Generating blocks" : "Building meshes";
        status.text = $"{stage} · {Mathf.FloorToInt(progress * 100f)}%";
        fill.anchorMax = new Vector2(progress, 1f);
        panel.SetActive(true);
        displayedSteps = completed;
    }
}
