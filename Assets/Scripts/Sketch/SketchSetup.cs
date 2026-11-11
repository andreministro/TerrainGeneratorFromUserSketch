using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SketchSetup : MonoBehaviour
{
    private const string TerrainScene = "TerrainScene";
    private const string SketchFolder = "UserSketches";

    [SerializeField] private GameObject sizePanel;
    [SerializeField] private Slider sizeSlider;
    [SerializeField] private Text sizeLabel;
    [SerializeField] private Text maximumSizeLabel;
    [SerializeField] private GameObject painterControls;
    [SerializeField] private Sketch sketch;
    [SerializeField] private InputField seedInput;
    [SerializeField] private Button saveButton;
    [SerializeField] private Text saveStatus;

    private void Awake()
    {
        sizeSlider.wholeNumbers = true;
        sizeSlider.minValue = 1;
        sizeSlider.maxValue = TerrainManager.MaxSketchSize;
        sizeSlider.onValueChanged.AddListener(UpdateSizeLabel);
        UpdateSizeLabel(sizeSlider.value);
        maximumSizeLabel.text = TerrainManager.MaxSketchSize.ToString();
    }

    private void UpdateSizeLabel(float value)
    {
        int size = Mathf.RoundToInt(value);
        sizeLabel.text = $"{size} × {size}";
    }

    public void OpenPainter()
    {
        if (!sizePanel.activeSelf)
            return;

        sketch.OpenCanvas(Mathf.RoundToInt(sizeSlider.value));
        sketch.gameObject.SetActive(true);
        painterControls.SetActive(true);
        sizePanel.SetActive(false);
    }

    public void SaveAndGenerate()
    {
        if (!saveButton.interactable)
            return;

        int seed = 0;
        if (!string.IsNullOrWhiteSpace(seedInput.text) && !int.TryParse(seedInput.text, out seed))
        {
            saveStatus.text = "Enter a seed from\n−2147483648 to 2147483647.";
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(TerrainScene))
        {
            saveStatus.text = "The terrain scene is unavailable.";
            Debug.LogError("Add TerrainScene to the build scene list.", this);
            return;
        }

        string root = Application.isEditor ? Application.dataPath : Application.persistentDataPath;
        string directory = Path.Combine(root, SketchFolder);
        string path = GetNewSketchPath(directory);
        byte[] png = sketch.EncodePng();

        try
        {
            Directory.CreateDirectory(directory);
            WriteSketch(path, png);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            saveStatus.text = "Could not save the sketch. See the Console.";
            Debug.LogError($"Could not save the sketch to {path}. {exception.Message}", this);
            return;
        }

#if UNITY_EDITOR
        string assetPath = $"Assets/{SketchFolder}/{Path.GetFileName(path)}";
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) == null)
        {
            saveStatus.text = "Sketch saved. Unity could not import it.";
            Debug.LogError($"Could not import the saved sketch at {assetPath}.", this);
            return;
        }
#endif

        Debug.Log($"Saved sketch to {path}.", this);
        saveButton.interactable = false;
        saveStatus.text = "Opening terrain…";
        ReadImage.QueueSketch(png, seed);
        SceneManager.LoadSceneAsync(TerrainScene);
    }

    private static string GetNewSketchPath(string directory)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        string name = $"Sketch-{timestamp}";
        string path = Path.Combine(directory, $"{name}.png");
        for (int suffix = 2; File.Exists(path) || File.Exists(path + ".meta"); suffix++)
            path = Path.Combine(directory, $"{name}-{suffix}.png");

        return path;
    }

    private static void WriteSketch(string path, byte[] png)
    {
        string temporaryPath = Path.Combine(Path.GetDirectoryName(path), $".sketch-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(temporaryPath, png);
            File.Move(temporaryPath, path);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
