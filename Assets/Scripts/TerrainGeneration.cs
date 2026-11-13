using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimplexNoise;

public class TerrainGeneration
{
    private readonly struct ColumnElevation
    {
        public readonly Biome Biome;
        public readonly int StoneHeight;
        public readonly int SurfaceHeight;

        public ColumnElevation(Biome biome, int stoneHeight, int surfaceHeight)
        {
            Biome = biome;
            StoneHeight = stoneHeight;
            SurfaceHeight = surfaceHeight;
        }
    }

    private readonly ReadImage sketch;
    private readonly BiomeNoiseProfiles noise;
    private readonly float noiseOffsetX;
    private readonly float noiseOffsetZ;
    private readonly BiomeHeightBlend heightBlend;
    private RectInt elevationCacheBounds;
    private ColumnElevation[] columnElevations;

    public TerrainGeneration(ReadImage sketch) : this(sketch, BiomeNoiseProfiles.Default)
    {
    }

    internal TerrainGeneration(ReadImage sketch, BiomeNoiseProfiles noise, int cachedSketchWidth = 0, int biomeBlendWidth = 0)
    {
        this.sketch = sketch;
        this.noise = noise;
        var random = new System.Random(sketch.seed);
        noiseOffsetX = random.Next(-100000, 100000);
        noiseOffsetZ = random.Next(-100000, 100000);

        if (biomeBlendWidth > 0 && sketch.mapWidth > 0 && sketch.mapHeight > 0)
            heightBlend = new BiomeHeightBlend(sketch, biomeBlendWidth, SampleSurfaceHeight);

        if (cachedSketchWidth > 0)
        {
            int minimum = -(cachedSketchWidth / 2);
            ResetElevationCache(new RectInt(minimum, minimum, cachedSketchWidth, cachedSketchWidth));
        }
    }

    internal void ResetElevationCache(RectInt bounds)
    {
        elevationCacheBounds = bounds;
        int count = bounds.width * bounds.height;
        if (columnElevations == null || columnElevations.Length < count)
            columnElevations = new ColumnElevation[count];
        else
            System.Array.Clear(columnElevations, 0, count);
    }

    /*float buildingFrequency = 0.7f;
    float buildingDensity = 7;

    float houseFrequency = 0.8f;
    float houseDensity = 8;*/


    public Chunk ChunkGeneration(Chunk chunk)
    {
        // Include nearby columns so decorations can extend across chunk boundaries.
        for (int x = chunk.pos.x - 2; x < chunk.pos.x + Chunk.chunkSize + 2; x++)
        {
            for (int z = chunk.pos.z - 2; z < chunk.pos.z + Chunk.chunkSize + 2; z++)
            {
                switch (sketch.GetBiome(x, z))
                {
                    case Biome.Water:
                        ChunkColumnGenBlue(chunk, x, z);
                        break;
                    case Biome.HotDesert:
                        ChunkColumnGenYellow(chunk, x, z);
                        break;
                    case Biome.AlpineTundra:
                        ChunkColumnGenMagenta(chunk, x, z);
                        break;
                    case Biome.TemperateForest:
                        ChunkColumnGenGreen(chunk, x, z);
                        break;
                    case Biome.ColdDesert:
                        ChunkColumnGenWhite(chunk, x, z);
                        break;
                    case Biome.Taiga:
                        ChunkColumnGenBlack(chunk, x, z);
                        break;
                    case Biome.TropicalForest:
                        ChunkColumnGenCyan(chunk, x, z);
                        break;
                    case Biome.Savannah:
                        ChunkColumnGenRed(chunk, x, z);
                        break;
                }
            }
        }

        ClearChunkPadding(chunk);
        return chunk;
    }

    private void ClearChunkPadding(Chunk chunk)
    {
        int minX = -(sketch.mapWidth / 2) - chunk.pos.x;
        int minZ = -(sketch.mapHeight / 2) - chunk.pos.z;
        int endX = minX + sketch.mapWidth;
        int endZ = minZ + sketch.mapHeight;
        int size = Chunk.chunkSize;
        if (minX <= 0 && minZ <= 0 && endX >= size && endZ >= size)
            return;

        // Decorations near the sketch edge can extend into the padding of a partial chunk.
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                if (x >= minX && x < endX && z >= minZ && z < endZ)
                    continue;

                for (int y = 0; y < size; y++)
                    chunk.SetBlockId(x, y, z, BlockId.Unset);
            }
        }
    }

    //WATER 
    public Chunk ChunkColumnGenBlue(Chunk chunk, int x, int z)
    {
        ColumnElevation elevation = GetColumnElevation(x, z, Biome.Water, noise.water);
        int stoneHeight = elevation.StoneHeight;
        int waterHeight = elevation.SurfaceHeight;

        for (int y = chunk.pos.y; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            if (y <= stoneHeight)
            {
                SetBlock(x, y, z, BlockId.Stone, chunk);
            }
            else if (y <= waterHeight)
            {
                SetBlock(x, y, z, BlockId.Water, chunk);
            }
            else
            {
                SetBlock(x, y, z, BlockId.Air, chunk);
            }
        }
        return chunk;

    } 

    //TEMPERATE FOREST 
    public Chunk ChunkColumnGenGreen(Chunk chunk, int x, int z)
    {
        LandBiomeNoise profile = noise.temperateForest;
        TemperateForestPlants plants = noise.vegetation.temperateForest;
        ColumnElevation elevation = GetColumnElevation(x, z, Biome.TemperateForest, profile.height);
        int stoneHeight = elevation.StoneHeight;
        int dirtHeight = elevation.SurfaceHeight;

        for (int y = chunk.pos.y - 8; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            bool solid = IsSolidLand(x, y, z, dirtHeight, profile.caveFrequency, profile.caveCutoff);

            if (solid && y <= stoneHeight)
            {
                SetBlock(x, y, z, BlockId.Stone, chunk);
            }
            else if (solid)
            {
                SetBlock(x, y, z, BlockId.GrassNormal, chunk);

                if (y == dirtHeight && SpawnsPlant(x, z, plants.trees))
                {
                    CreateNormalTree(x, y + 1, z, chunk);
                }
                else if (y == dirtHeight && SpawnsPlant(x, z, plants.redFlowers))
                {
                    SetBlock(x, y + 1, z, BlockId.RedFlower, chunk, false);
                }
                else if (y == dirtHeight && SpawnsPlant(x, z, plants.blueFlowers))
                {
                    SetBlock(x, y + 1, z, BlockId.BlueFlower, chunk, false);
                }
                else if (y == dirtHeight && SpawnsPlant(x, z, plants.pinkFlowers))
                {
                    SetBlock(x, y + 1, z, BlockId.PinkFlower, chunk, false);
                }
            }
            else
            {
                SetBlock(x, y, z, BlockId.Air, chunk);
            }
        }

        return chunk;

    }

    //ALPINE TUNDRA
    public Chunk ChunkColumnGenMagenta(Chunk chunk, int x, int z)
    {
        MountainBiomeNoise profile = noise.alpineTundra;
        ColumnElevation elevation = GetColumnElevation(x, z, Biome.AlpineTundra, profile.height);
        int stoneHeight = elevation.StoneHeight;
        int dirtHeight = elevation.SurfaceHeight;

        float snowFrequency = 0.9f;
        int snowDensity = 100;

        for (int y = chunk.pos.y - 8; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            bool solid = IsSolidLand(x, y, z, dirtHeight, profile.caveFrequency, profile.caveCutoff);
            if (solid && y <= stoneHeight)
            {
                SetBlock(x, y, z, BlockId.Stone, chunk);
            }
            else if (solid)
            {
                SetBlock(x, y, z, BlockId.DirtGrass, chunk);

                if (y == dirtHeight && y >= profile.snowline
                    && GetNoise(x, 0, z, snowFrequency, 100) < snowDensity)
                {
                    SetBlock(x, y + 1, z, BlockId.Snow, chunk);
                }
            }
            else
            {
                SetBlock(x, y, z, BlockId.Air, chunk);
            }
        }

        return chunk;
    }

    //HOT DESERT
    public Chunk ChunkColumnGenYellow(Chunk chunk, int x, int z)
    {
        LandBiomeNoise profile = noise.hotDesert;
        HotDesertPlants plants = noise.vegetation.hotDesert;
        ColumnElevation elevation = GetColumnElevation(x, z, Biome.HotDesert, profile.height);
        int stoneHeight = elevation.StoneHeight;
        int sandHeight = elevation.SurfaceHeight;


        for (int y = chunk.pos.y; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            bool solid = IsSolidLand(x, y, z, sandHeight, profile.caveFrequency, profile.caveCutoff);
            if (solid && y <= stoneHeight)
            {
                SetBlock(x, y, z, BlockId.Stone, chunk);
            }
            else if (solid)
            {
                SetBlock(x, y, z, BlockId.Sand, chunk, true);

                if (y == sandHeight && SpawnsPlant(x, z, plants.cacti))
                {
                    CreateCacti(x, y + 1, z, chunk);
                }
                else if (y == sandHeight && SpawnsPlant(x, z, plants.dryShrubs))
                {
                    SetBlock(x, y + 1, z, BlockId.DryBush, chunk, true);
                }
            }
            else
            {
                SetBlock(x, y, z, BlockId.Air, chunk);
            }
        }


        return chunk;
    }

    //COLD DESERT
    public Chunk ChunkColumnGenWhite(Chunk chunk, int x, int z)
    {
        LandBiomeNoise profile = noise.coldDesert;
        ColumnElevation elevation = GetColumnElevation(x, z, Biome.ColdDesert, profile.height);
        int stoneHeight = elevation.StoneHeight;
        int snowHeight = elevation.SurfaceHeight;


        for (int y = chunk.pos.y; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            bool solid = IsSolidLand(x, y, z, snowHeight, profile.caveFrequency, profile.caveCutoff);
            if (solid && y <= stoneHeight)
            {
                SetBlock(x, y, z, BlockId.Stone, chunk);
            }
            else if (solid)
            {
                SetBlock(x, y, z, BlockId.Snow, chunk);

                if (y == snowHeight && SpawnsPlant(x, z, noise.vegetation.coldDesertFrozenShrubs))
                {
                    SetBlock(x, y + 1, z, BlockId.FrozenBush, chunk, true);
                }
            }
            else
            {
                SetBlock(x, y, z, BlockId.Air, chunk);
            }
        }
        return chunk;
    }

    //TAIGA
    public Chunk ChunkColumnGenBlack(Chunk chunk, int x, int z)
    {
        MountainBiomeNoise profile = noise.taiga;
        ColumnElevation elevation = GetColumnElevation(x, z, Biome.Taiga, profile.height);
        int stoneHeight = elevation.StoneHeight;
        int dirtHeight = elevation.SurfaceHeight;

        float snowFrequency = 0.9f;
        int snowDensity = 100;

        for (int y = chunk.pos.y - 8; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            bool solid = IsSolidLand(x, y, z, dirtHeight, profile.caveFrequency, profile.caveCutoff);
            if (solid && y <= stoneHeight)
            {
                SetBlock(x, y, z, BlockId.Stone, chunk);
            }
            else if (solid)
            {
                SetBlock(x, y, z, BlockId.GrassDark, chunk);

                if (y == dirtHeight && y >= profile.snowline
                    && GetNoise(x, 0, z, snowFrequency, 100) < snowDensity)
                {
                    SetBlock(x, y + 1, z, BlockId.Snow, chunk);
                }
                else if (y == dirtHeight && SpawnsPlant(x, z, noise.vegetation.taigaTrees))
                {
                    CreateTaigaTree(x, y + 1, z, chunk);
                }
            }
            else
            {
                SetBlock(x, y, z, BlockId.Air, chunk);
            }
        }
        return chunk;
    }

    //TROPICAL FOREST
    public Chunk ChunkColumnGenCyan(Chunk chunk, int x, int z)
    {
        LandBiomeNoise profile = noise.tropicalForest;
        TropicalForestPlants plants = noise.vegetation.tropicalForest;
        ColumnElevation elevation = GetColumnElevation(x, z, Biome.TropicalForest, profile.height);
        int stoneHeight = elevation.StoneHeight;
        int dirtHeight = elevation.SurfaceHeight;

        for (int y = chunk.pos.y - 8; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            bool solid = IsSolidLand(x, y, z, dirtHeight, profile.caveFrequency, profile.caveCutoff);

            if (solid && y <= stoneHeight)
            {
                SetBlock(x, y, z, BlockId.Stone, chunk);
            }
            else if (solid)
            {
                SetBlock(x, y, z, BlockId.GrassTropical, chunk);

                if (y == dirtHeight && SpawnsPlant(x, z, plants.trees))
                {
                    CreateTropicalTree(x, y + 1, z, chunk);
                }
                else if (y == dirtHeight && SpawnsPlant(x, z, plants.bushes))
                {
                    CreateTropicalBush(x, y + 1, z, chunk);
                }
            }
            else
            {
                SetBlock(x, y, z, BlockId.Air, chunk);
            }
        }

        return chunk;
    }

    //SAVANNAH
    public Chunk ChunkColumnGenRed(Chunk chunk, int x, int z)
    {
        LandBiomeNoise profile = noise.savannah;
        ColumnElevation elevation = GetColumnElevation(x, z, Biome.Savannah, profile.height);
        int stoneHeight = elevation.StoneHeight;
        int dirtHeight = elevation.SurfaceHeight;

        for (int y = chunk.pos.y - 8; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            bool solid = IsSolidLand(x, y, z, dirtHeight, profile.caveFrequency, profile.caveCutoff);

            if (solid && y <= stoneHeight)
            {
                SetBlock(x, y, z, BlockId.Stone, chunk);
            }
            else if (solid)
            {
                SetBlock(x, y, z, BlockId.GrassDry, chunk);

                if (y == dirtHeight && SpawnsPlant(x, z, noise.vegetation.savannahTrees))
                {
                    CreateSavannahTree(x, y + 1, z, chunk);
                }
            }
            else
            {
                SetBlock(x, y, z, BlockId.Air, chunk);
            }
        }
        return chunk;
    }

    void CreateNormalTree(int x, int y, int z, Chunk chunk)
    {
        //create leaves
        for (int xi = -2; xi <= 2; xi++)
        {
            for (int yi = 4; yi <= 8; yi++)
            {
                for (int zi = -2; zi <= 2; zi++)
                {
                    SetBlock(x + xi, y + yi, z + zi, BlockId.NormalLeaf, chunk, true);
                }
            }
        }
        //create trunk
        for (int yt = 0; yt < 6; yt++)
        {
            SetBlock(x, y + yt, z, BlockId.NormalWood, chunk, true);
        }
    }

    void CreateTaigaTree(int x, int y, int z, Chunk chunk)
    {
        //create leaves
        for (int xi = -2; xi <= 2; xi++)
        {
            for (int yi = 4; yi <= 13; yi++)
            {
                for (int zi = -2; zi <= 2; zi++)
                {
                    SetBlock(x + xi, y + yi, z + zi, BlockId.DarkLeaf, chunk, true);
                }
            }
        }

        //create trunk
        for (int yt = 0; yt < 12; yt++)
        {
            SetBlock(x, y + yt, z, BlockId.NormalWood, chunk, true);
        }
    }

    void CreateSavannahTree(int x, int y, int z, Chunk chunk)
    {
        //create leaves
        for (int xi = -4; xi <= 4; xi++)
        {
            for (int yi = 8; yi <= 8; yi++)
            {
                for (int zi = -4; zi <= 4; zi++)
                {
                    SetBlock(x + xi, y + yi, z + zi, BlockId.DryLeaf, chunk, true);
                }
            }
        }

        //create trunk
        for (int yt = 0; yt < 8; yt++)
        {
            SetBlock(x, y + yt, z, BlockId.DryWood, chunk, true);
        }
    }

    void CreateTropicalTree(int x, int y, int z, Chunk chunk)
    {
        //create leaves
        for (int xi = -3; xi <= 3; xi++)
        {
            for (int yi = 7; yi <= 9; yi++)
            {
                for (int zi = -3; zi <= 3; zi++)
                {
                    SetBlock(x + xi, y + yi, z + zi, BlockId.TropicalLeaf, chunk, true);
                }
            }
        }

        //create trunk
        for (int yt = 0; yt < 9; yt++)
        {
            SetBlock(x, y + yt, z, BlockId.TropicalWood, chunk, true);
        }
    }

    void CreateTropicalBush(int x, int y, int z, Chunk chunk)
    {
        //create leaves
        for (int xi = -1; xi <= 1; xi++)
        {
            for (int yi = 0; yi <= 1; yi++)
            {
                for (int zi = -1; zi <= 1; zi++)
                {
                    SetBlock(x + xi, y + yi, z + zi, BlockId.TropicalLeaf, chunk, true);
                }
            }
        }

        //create trunk
        for (int yt = 0; yt < 1; yt++)
        {
            SetBlock(x, y + yt, z, BlockId.TropicalWood, chunk, true);
        }
    }


    void CreateCacti(int x, int y, int z, Chunk chunk)
    {
        for(int yt = 0; yt < 4; yt++)
        {
            SetBlock(x, y + yt, z, BlockId.Cactus, chunk, true);
        }
    }

 /*   void CreateBuilding(int x, int y, int z, Chunk chunk)
    {
        for (int xi = -2; xi <= 2; xi++)
        {
            for (int yi = 0; yi <= 6; yi++)
            {
                for (int zi = -2; zi <= 2; zi++)
                {
                    SetBlock(x + xi, y + yi, z + zi, new BlockBuilding(), chunk, true);
                }
            }
        }

        for(int xt = -1; xt <= 1; xt++)
        {
            for(int yt = 6; yt <= 7; yt++)
            {
                for(int zt = -1; zt <= 1; zt++)
                {
                    SetBlock(x + xt, y + yt, z + zt, new BlockConcrete(), chunk, true);
                }
            }
        }
    }

    void CreateHouse(int x, int y, int z, Chunk chunk)
    {
        //walls
        for (int xi = -1; xi <= 1; xi++)
        {
            for (int yi = 0; yi <= 3; yi++)
            {
                for (int zi = -1; zi <= 1; zi++)
                {
                    SetBlock(x + xi, y + yi, z + zi, new BlockBrick(), chunk, true);
                }
            }
        }

        //roof
        for (int xi = -2; xi <= 2; xi++)
        {
            for (int yi = 3; yi <= 3; yi++)
            {
                for (int zi = -2; zi <= 2; zi++)
                {
                    SetBlock(x + xi, y + yi, z + zi, new BlockRoof(), chunk, true);
                }
            }
        }

        for (int xi = -1; xi <= 1; xi++)
        {
            for (int yi = 4; yi <= 4; yi++)
            {
                for (int zi = -1; zi <= 1; zi++)
                {
                    SetBlock(x + xi, y + yi, z + zi, new BlockRoof(), chunk, true);
                }
            }
        }
    }*/ 

    public static void SetBlock(int x, int y, int z, Block block, Chunk chunk, bool replaceBlocks = false)
    {
        x -= chunk.pos.x;
        y -= chunk.pos.y;
        z -= chunk.pos.z;

        if (Chunk.InRange(x) && Chunk.InRange(y) && Chunk.InRange(z))
        {
           if (replaceBlocks || chunk.IsUnset(x, y, z))
                chunk.SetBlock(x, y, z, block);
        }
    }

    private static void SetBlock(int x, int y, int z, BlockId block, Chunk chunk, bool replaceBlocks = false)
    {
        x -= chunk.pos.x;
        y -= chunk.pos.y;
        z -= chunk.pos.z;

        if (Chunk.InRange(x) && Chunk.InRange(y) && Chunk.InRange(z)
            && (replaceBlocks || chunk.IsUnset(x, y, z)))
            chunk.SetBlockId(x, y, z, block);
    }

    private ColumnElevation GetColumnElevation(int x, int z, Biome biome, in TerrainHeightNoise profile)
    {
        int columnX = x - elevationCacheBounds.xMin;
        int columnZ = z - elevationCacheBounds.yMin;
        if ((uint)columnX >= (uint)elevationCacheBounds.width || (uint)columnZ >= (uint)elevationCacheBounds.height)
            return GetBlendedElevation(x, z, biome, profile);

        ref ColumnElevation elevation = ref columnElevations[columnZ * elevationCacheBounds.width + columnX];
        // An empty entry has Biome.None. Key by biome to support direct column calls as well.
        if (elevation.Biome != biome)
            elevation = GetBlendedElevation(x, z, biome, profile);
        return elevation;
    }

    private ColumnElevation GetBlendedElevation(int x, int z, Biome biome, in TerrainHeightNoise profile)
    {
        ColumnElevation original = SampleColumnElevation(x, z, biome, profile);
        // Let land slope toward the water while preserving the water surface and bed.
        if (heightBlend == null || biome == Biome.Water || sketch.GetBiome(x, z) != biome
            || !heightBlend.TryGetSurface(x, z, out int surfaceHeight))
            return original;

        int offset = surfaceHeight - original.SurfaceHeight;
        return new ColumnElevation(biome, original.StoneHeight + offset, surfaceHeight);
    }

    private int SampleSurfaceHeight(int x, int z, Biome biome)
    {
        return SampleColumnElevation(x, z, biome, GetHeightProfile(biome)).SurfaceHeight;
    }

    private TerrainHeightNoise GetHeightProfile(Biome biome) => biome switch
    {
        Biome.Water => noise.water,
        Biome.HotDesert => noise.hotDesert.height,
        Biome.AlpineTundra => noise.alpineTundra.height,
        Biome.TemperateForest => noise.temperateForest.height,
        Biome.ColdDesert => noise.coldDesert.height,
        Biome.Taiga => noise.taiga.height,
        Biome.TropicalForest => noise.tropicalForest.height,
        _ => noise.savannah.height
    };

    internal int GetOverviewSurface(int x, int z, out BlockId surface)
    {
        Biome biome = sketch.GetBiome(x, z);
        surface = biome switch
        {
            Biome.Water => BlockId.Water,
            Biome.HotDesert => BlockId.Sand,
            Biome.AlpineTundra => BlockId.DirtGrass,
            Biome.TemperateForest => BlockId.GrassNormal,
            Biome.ColdDesert => BlockId.Snow,
            Biome.Taiga => BlockId.GrassDark,
            Biome.TropicalForest => BlockId.GrassTropical,
            _ => BlockId.GrassDry
        };
        int elevation = GetColumnElevation(x, z, biome, GetHeightProfile(biome)).SurfaceHeight;
        if ((biome == Biome.AlpineTundra && elevation >= noise.alpineTundra.snowline)
            || (biome == Biome.Taiga && elevation >= noise.taiga.snowline))
        {
            surface = BlockId.Snow;
            elevation++;
        }
        return elevation;
    }

    private ColumnElevation SampleColumnElevation(int x, int z, Biome biome, in TerrainHeightNoise profile)
    {
        int broadHeight = profile.baseElevation + GetNoise(x, 0, z, profile.hillFrequency, profile.hillHeightRange);
        int stoneHeight = Mathf.Max(broadHeight, profile.minimumElevation)
            + GetNoise(x, 0, z, profile.detailFrequency, profile.detailHeightRange);
        int surfaceHeight = stoneHeight + profile.surfaceThickness
            + GetNoise(x, 100, z, profile.surfaceFrequency, profile.surfaceHeightRange);
        return new ColumnElevation(biome, stoneHeight, surfaceHeight);
    }

    // Canopy IDs distinguish the tree silhouettes in the distant mesh.
    internal BlockId GetOverviewPlant(int x, int z, int highestVoxel)
    {
        Biome biome = sketch.GetBiome(x, z);
        BlockId plant = biome switch
        {
            Biome.TemperateForest when SpawnsPlant(x, z, noise.vegetation.temperateForest.trees) => BlockId.NormalLeaf,
            Biome.Taiga when SpawnsPlant(x, z, noise.vegetation.taigaTrees) => BlockId.DarkLeaf,
            Biome.TropicalForest when SpawnsPlant(x, z, noise.vegetation.tropicalForest.trees) => BlockId.TropicalLeaf,
            Biome.Savannah when SpawnsPlant(x, z, noise.vegetation.savannahTrees) => BlockId.DryLeaf,
            Biome.HotDesert when SpawnsPlant(x, z, noise.vegetation.hotDesert.cacti) => BlockId.Cactus,
            Biome.HotDesert when SpawnsPlant(x, z, noise.vegetation.hotDesert.dryShrubs) => BlockId.DryBush,
            Biome.ColdDesert when SpawnsPlant(x, z, noise.vegetation.coldDesertFrozenShrubs) => BlockId.FrozenBush,
            _ => BlockId.Unset
        };
        if (plant == BlockId.Unset)
            return plant;

        int ground = GetColumnElevation(x, z, biome, GetHeightProfile(biome)).SurfaceHeight;
        if (ground < -Chunk.chunkSize || ground >= highestVoxel
            || (biome == Biome.Taiga && ground >= noise.taiga.snowline))
            return BlockId.Unset;
        return plant;
    }

    private bool SpawnsPlant(int x, int z, in PlantSpawnNoise plant)
    {
        return plant.spawnCutoff > 0 && GetNoise(x, 0, z, plant.frequency, 100) < plant.spawnCutoff;
    }

    private bool IsSolidLand(int x, int y, int z, int surfaceHeight, float caveFrequency, int caveCutoff)
    {
        // Leave a solid roof under the generated surface so cave noise cannot open vertical holes.
        const int caveRoofThickness = 4;
        return y <= surfaceHeight && (y > surfaceHeight - caveRoofThickness
            || GetNoise(x, y, z, caveFrequency, 100) > caveCutoff);
    }

    private int GetNoise(int x, int y, int z, float scale, int max)
    {
        float value = Noise.Generate(x * scale + noiseOffsetX, y * scale, z * scale + noiseOffsetZ);
        return Mathf.FloorToInt((value + 1f) * (max / 2f));
    }
}
