using System;
using UnityEditor;
using UnityEngine;

internal class SketchTextureImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        bool isUserSketch = assetPath.StartsWith("Assets/UserSketches/", StringComparison.Ordinal)
            && assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        if (assetPath != "Assets/savedSketch.png" && !isUserSketch)
            return;

        var importer = (TextureImporter)assetImporter;
        importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, 4096);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
    }
}
