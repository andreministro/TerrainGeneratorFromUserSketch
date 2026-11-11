using System;
using UnityEngine;

[Serializable]
internal struct TerrainHeightNoise
{
    [Header("Stone Elevation")]
    [Tooltip("Starting stone elevation in blocks, before any noise is added.")]
    public int baseElevation;
    [Tooltip("Floor for the broad elevation. Fine detail is added afterward.")]
    public int minimumElevation;
    [Min(0f), Tooltip("Higher values bring hills and mountains closer together.")]
    public float hillFrequency;
    [Min(0), Tooltip("Height range of the broad terrain features, in blocks.")]
    public int hillHeightRange;
    [Min(0f), Tooltip("Higher values bring small bumps closer together.")]
    public float detailFrequency;
    [Min(0), Tooltip("Height range of the fine terrain detail, in blocks.")]
    public int detailHeightRange;

    [Header("Surface Layer")]
    [Min(0), Tooltip("Base thickness of the water, soil, sand, or snow above the stone.")]
    public int surfaceThickness;
    [Min(0f), Tooltip("Higher values make the surface thickness vary over shorter distances.")]
    public float surfaceFrequency;
    [Min(0), Tooltip("Range of the extra thickness added by surface noise, in blocks.")]
    public int surfaceHeightRange;
}

[Serializable]
internal struct LandBiomeNoise
{
    public TerrainHeightNoise height;

    [Header("Caves")]
    [Min(0f), Tooltip("Higher values make cave features vary over shorter distances.")]
    public float caveFrequency;
    [Range(0, 100), Tooltip("Higher values remove more stone and surface blocks.")]
    public int caveCutoff;
}

[Serializable]
internal struct MountainBiomeNoise
{
    public TerrainHeightNoise height;

    [Header("Caves")]
    [Min(0f), Tooltip("Higher values make cave features vary over shorter distances.")]
    public float caveFrequency;
    [Range(0, 100), Tooltip("Higher values remove more stone and surface blocks.")]
    public int caveCutoff;

    [Header("Snow")]
    [Tooltip("Minimum ground elevation, in voxels, for snow to form above the surface.")]
    public int snowline;
}

[Serializable]
internal struct BiomeNoiseProfiles
{
    public TerrainHeightNoise water;
    public LandBiomeNoise temperateForest;
    public MountainBiomeNoise alpineTundra;
    public LandBiomeNoise hotDesert;
    public MountainBiomeNoise taiga;
    public LandBiomeNoise coldDesert;
    public LandBiomeNoise tropicalForest;
    public LandBiomeNoise savannah;

    public static BiomeNoiseProfiles Default
    {
        get
        {
            var lowlands = new LandBiomeNoise
            {
                height = new TerrainHeightNoise
                {
                    baseElevation = -28,
                    minimumElevation = -12,
                    hillFrequency = 0.0008f,
                    hillHeightRange = 20,
                    detailFrequency = 0.05f,
                    detailHeightRange = 4,
                    surfaceThickness = 3,
                    surfaceFrequency = 0.04f,
                    surfaceHeightRange = 3
                },
                caveFrequency = 0.025f,
                caveCutoff = 7
            };
            var mountains = new MountainBiomeNoise
            {
                height = lowlands.height,
                caveFrequency = 0.055f,
                caveCutoff = 20,
                snowline = 20
            };
            mountains.height.baseElevation = -24;
            mountains.height.minimumElevation = -8;
            mountains.height.hillFrequency = 0.048f;
            mountains.height.hillHeightRange = 48;
            mountains.height.surfaceThickness = 1;

            return new BiomeNoiseProfiles
            {
                water = new TerrainHeightNoise
                {
                    baseElevation = -30,
                    minimumElevation = -15,
                    hillFrequency = 0.000000001f,
                    hillHeightRange = 4,
                    detailFrequency = 0f,
                    detailHeightRange = 4,
                    surfaceThickness = 3,
                    surfaceFrequency = 0.0001f,
                    surfaceHeightRange = 2
                },
                temperateForest = lowlands,
                alpineTundra = mountains,
                hotDesert = lowlands,
                taiga = mountains,
                coldDesert = lowlands,
                tropicalForest = lowlands,
                savannah = lowlands
            };
        }
    }
}
