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
    [Range(0, 100), Tooltip("Higher values carve more blocks below the solid surface layer.")]
    public int caveCutoff;
}

[Serializable]
internal struct MountainBiomeNoise
{
    public TerrainHeightNoise height;

    [Header("Caves")]
    [Min(0f), Tooltip("Higher values make cave features vary over shorter distances.")]
    public float caveFrequency;
    [Range(0, 100), Tooltip("Higher values carve more blocks below the solid surface layer.")]
    public int caveCutoff;

    [Header("Snow")]
    [Tooltip("Minimum ground elevation, in voxels, for snow to form above the surface.")]
    public int snowline;
}

[Serializable]
internal struct PlantSpawnNoise
{
    [Min(0f), Tooltip("Higher values make the placement noise vary over shorter distances.")]
    public float frequency;
    [Range(0, 100), Tooltip("Higher values allow more placements. Zero disables this plant.")]
    public int spawnCutoff;

    public PlantSpawnNoise(float frequency, int spawnCutoff)
    {
        this.frequency = frequency;
        this.spawnCutoff = spawnCutoff;
    }
}

[Serializable]
internal struct TemperateForestPlants
{
    public PlantSpawnNoise trees;
    public PlantSpawnNoise redFlowers;
    public PlantSpawnNoise blueFlowers;
    public PlantSpawnNoise pinkFlowers;
}

[Serializable]
internal struct HotDesertPlants
{
    public PlantSpawnNoise cacti;
    public PlantSpawnNoise dryShrubs;
}

[Serializable]
internal struct TropicalForestPlants
{
    public PlantSpawnNoise trees;
    public PlantSpawnNoise bushes;
}

[Serializable]
internal struct BiomeVegetation
{
    public TemperateForestPlants temperateForest;
    public HotDesertPlants hotDesert;
    public PlantSpawnNoise taigaTrees;
    public PlantSpawnNoise coldDesertFrozenShrubs;
    public TropicalForestPlants tropicalForest;
    public PlantSpawnNoise savannahTrees;

    public static BiomeVegetation Default => new BiomeVegetation
    {
        temperateForest = new TemperateForestPlants
        {
            trees = new PlantSpawnNoise(0.46f, 5),
            redFlowers = new PlantSpawnNoise(0.7f, 6),
            blueFlowers = new PlantSpawnNoise(0.73f, 5),
            pinkFlowers = new PlantSpawnNoise(0.71f, 5)
        },
        hotDesert = new HotDesertPlants
        {
            cacti = new PlantSpawnNoise(0.3f, 3),
            dryShrubs = new PlantSpawnNoise(0.65f, 6)
        },
        taigaTrees = new PlantSpawnNoise(0.42f, 4),
        coldDesertFrozenShrubs = new PlantSpawnNoise(0.52f, 5),
        tropicalForest = new TropicalForestPlants
        {
            trees = new PlantSpawnNoise(0.65f, 5),
            bushes = new PlantSpawnNoise(0.8f, 7)
        },
        savannahTrees = new PlantSpawnNoise(0.35f, 3)
    };
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
    public BiomeVegetation vegetation;

    public static BiomeNoiseProfiles Default
    {
        get
        {
            // Y = 0 is the water datum. Land gains its character mainly through local relief.
            return new BiomeNoiseProfiles
            {
                water = new TerrainHeightNoise
                {
                    baseElevation = -6,
                    minimumElevation = -6,
                    surfaceThickness = 6
                },
                temperateForest = new LandBiomeNoise
                {
                    height = TerrainHeight(-1, 0, 0.012f, 12, 2, 2),
                    caveFrequency = 0.032f,
                    caveCutoff = 12
                },
                alpineTundra = new MountainBiomeNoise
                {
                    height = TerrainHeight(-1, 3, 0.017f, 28, 2, 1),
                    caveFrequency = 0.036f,
                    caveCutoff = 14,
                    snowline = 22
                },
                hotDesert = new LandBiomeNoise
                {
                    height = TerrainHeight(-2, -1, 0.009f, 7, 2, 2),
                    caveFrequency = 0.030f,
                    caveCutoff = 8
                },
                taiga = new MountainBiomeNoise
                {
                    height = TerrainHeight(-1, 1, 0.010f, 14, 2, 2),
                    caveFrequency = 0.034f,
                    caveCutoff = 10,
                    snowline = 15
                },
                coldDesert = new LandBiomeNoise
                {
                    height = TerrainHeight(-1, 0, 0.009f, 9, 2, 1),
                    caveFrequency = 0.029f,
                    caveCutoff = 12
                },
                tropicalForest = new LandBiomeNoise
                {
                    height = TerrainHeight(-1, 0, 0.014f, 17, 2, 2),
                    caveFrequency = 0.032f,
                    caveCutoff = 12
                },
                savannah = new LandBiomeNoise
                {
                    height = TerrainHeight(-2, -1, 0.007f, 5, 1, 2),
                    caveFrequency = 0.031f,
                    caveCutoff = 9
                },
                vegetation = BiomeVegetation.Default
            };
        }
    }

    private static TerrainHeightNoise TerrainHeight(int baseElevation, int minimumElevation,
        float hillFrequency, int hillHeightRange, int detailHeightRange, int surfaceThickness)
    {
        return new TerrainHeightNoise
        {
            baseElevation = baseElevation,
            minimumElevation = minimumElevation,
            hillFrequency = hillFrequency,
            hillHeightRange = hillHeightRange,
            detailFrequency = 0.055f,
            detailHeightRange = detailHeightRange,
            surfaceThickness = surfaceThickness,
            surfaceFrequency = 0.032f,
            surfaceHeightRange = 1
        };
    }
}
