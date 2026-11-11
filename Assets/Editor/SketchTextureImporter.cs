using UnityEditor;
using UnityEngine;

internal class SketchTextureImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (assetPath != "Assets/savedSketch.png")
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
    }
}
