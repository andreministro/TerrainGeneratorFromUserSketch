using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimplexNoise;

public class TerrainGeneration
{
    public Chunk scriptRef;

    float mountainCaveFrequency = 0.055f;
    int mountainCaveSize = 20;

    float normalCaveFrequency = 0.025f;
    int normalCaveSize = 7;

    float TforestTreeFrequency = 0.6f;
    int TforestTreeDensity = 4;

    float taigaTreeFrequency = 0.6f;
    int taigaTreeDensity = 3;

    float tropicalTreeFrequency = 0.9f;
    int tropicalTreeDensity = 11;

    float savannahTreeFrequency = 0.35f;
    int savannahTreeDensity = 3;

    float cactiFrequency = 0.7f;
    int cactiDensity = 6;

    float shrubFrequency = 0.9f;
    float shrubDensity = 10;

    float flowerRedFrequency = 0.7f;
    float flowerRedDensity = 8;

    float flowerBlueFrequency = 0.73f;
    float flowerBlueDensity = 6;

    float flowerPinkFrequency = 0.71f;
    float flowerPinkDensity = 7;

    /*float buildingFrequency = 0.7f;
    float buildingDensity = 7;

    float houseFrequency = 0.8f;
    float houseDensity = 8;*/


    public Chunk ChunkGeneration(Chunk chunk)
    {
        GameObject imgReader = GameObject.Find("ImageReader");
        ReadImage readImage = imgReader.GetComponent<ReadImage>();
        for (int x = chunk.pos.x - 2; x < chunk.pos.x + Chunk.chunkSize + 2; x++)
        {
            for (int z = chunk.pos.z - 2; z < chunk.pos.z + Chunk.chunkSize + 2; z++)
            {
                foreach (Vector2 p in readImage.bluePos) //WATER
                {
                    if (p.x == x && p.y == z)
                    {
                        chunk = ChunkColumnGenBlue(chunk, x, z);
                    }
                }

                foreach (Vector2 p in readImage.yellowPos) //HOT DESERT
                {
                    if (p.x == x && p.y == z)
                    {
                        chunk = ChunkColumnGenYellow(chunk, x, z);

                    }
                }

                foreach (Vector2 p in readImage.magentaPos) //ALPINE TUNDRA
                {
                    if (p.x == x && p.y == z)
                    {
                        chunk = ChunkColumnGenMagenta(chunk, x, z);
                    }
                } 

                foreach (Vector2 p in readImage.greenPos) //TEMPERATE FOREST
                {
                    if (p.x == x && p.y == z)
                    {
                        chunk = ChunkColumnGenGreen(chunk, x, z);
                    }
                }

                foreach (Vector2 p in readImage.whitePos) //COLD DESERT
                {
                    if (p.x == x && p.y == z)
                    {
                        chunk = ChunkColumnGenWhite(chunk, x, z);

                    }
                }

                foreach (Vector2 p in readImage.blackPos) //TAIGA
                {
                    if (p.x == x && p.y == z)
                    {
                        chunk = ChunkColumnGenBlack(chunk, x, z);

                    }
                }

                foreach (Vector2 p in readImage.cyanPos) //TROPICAL FOREST
                {
                    if (p.x == x && p.y == z)
                    {
                        chunk = ChunkColumnGenCyan(chunk, x, z);

                    }
                }

                foreach (Vector2 p in readImage.redPos) //SAVANNAH
                {
                    if (p.x == x && p.y == z)
                    {
                        chunk = ChunkColumnGenRed(chunk, x, z);

                    }
                }
            }
        }
        return chunk;
    }

    //WATER 
    public Chunk ChunkColumnGenBlue(Chunk chunk, int x, int z)
    {
        //Base stone layer 
        float stoneBaseHeight = -30; //Base stone layer height
        float stoneBaseNoise = 0; //Scale of the noise to add to the base height, 0.05 --> peaks 25 blocks apart
        float stoneBaseNoiseHeight = 4; //Height of that noise, 4 --> max diff between peak and valley is 4

        //Mountain variables, Larger height and less frequency
        float stoneMountainHeight = 4;
        float stoneMountainFrequency = 0.000000001f;
        float stoneMinHeight = -15; //Lowest stone is allowed to go

        //Water Variables
        float waterBaseHeight = 3; // minimum depth on top of the stone 
        float waterNoise = 0.0001f; // little more messy then stoneBaseNoise
        float waterNoiseHeight = 2;

        int stoneHeight = Mathf.FloorToInt(stoneBaseHeight);
        stoneHeight += GetNoise(x, 0, z, stoneMountainFrequency, Mathf.FloorToInt(stoneMountainHeight));

        if (stoneHeight < stoneMinHeight)
            stoneHeight = Mathf.FloorToInt(stoneMinHeight); //after the mountain noise, raise everything under minimum to minmum

        stoneHeight += GetNoise(x, 0, z, stoneBaseNoise, Mathf.FloorToInt(stoneBaseNoiseHeight)); // apply base noise

        int waterHeight = stoneHeight + Mathf.FloorToInt(waterBaseHeight);
        waterHeight += GetNoise(x, 100, z, waterNoise, Mathf.FloorToInt(waterNoiseHeight));

        for (int y = chunk.pos.y; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            if (y <= stoneHeight)
            {
                SetBlock(x, y, z, new Block(), chunk);
            }
            else if (y <= waterHeight)
            {
                SetBlock(x, y, z, new BlockWater(), chunk);
            }
            else
            {
                SetBlock(x, y, z, new BlockAir(), chunk);
            }
        }
        return chunk;

    } 

    //TEMPERATE FOREST 
    public Chunk ChunkColumnGenGreen(Chunk chunk, int x, int z)
    {
        //Base stone layer 
        float stoneBaseHeight = -28; //Base stone layer height
        float stoneBaseNoise = 0.05f; //Scale of the noise to add to the base height, 0.05 --> peaks 25 blocks apart
        float stoneBaseNoiseHeight = 4; //Height of that noise, 4 --> max diff between peak and valley is 4

        //Mountain variables, Larger height and less frequency
        float stoneMountainHeight = 20;
        float stoneMountainFrequency = 0.0008f;
        float stoneMinHeight = -12; //Lowest stone is allowed to go

        //Dirt Variables
        float dirtBaseHeight = 3; // minimum depth on top of the stone 
        float dirtNoise = 0.04f; // little more messy then stoneBaseNoise
        float dirtNoiseHeight = 3;

        int stoneHeight = Mathf.FloorToInt(stoneBaseHeight);
        stoneHeight += GetNoise(x, 0, z, stoneMountainFrequency, Mathf.FloorToInt(stoneMountainHeight));

        if (stoneHeight < stoneMinHeight)
            stoneHeight = Mathf.FloorToInt(stoneMinHeight); //after the mountain noise, raise everything under minimum to minmum

        stoneHeight += GetNoise(x, 0, z, stoneBaseNoise, Mathf.FloorToInt(stoneBaseNoiseHeight)); // apply base noise

        int dirtHeight = stoneHeight + Mathf.FloorToInt(dirtBaseHeight);
        dirtHeight += GetNoise(x, 100, z, dirtNoise, Mathf.FloorToInt(dirtNoiseHeight));

        for (int y = chunk.pos.y - 8; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            int caveChance = GetNoise(x, y, z, normalCaveFrequency, 100);

            if (y <= stoneHeight && normalCaveSize < caveChance )
            {
                SetBlock(x, y, z, new Block(), chunk);
            }
            else if (y <= dirtHeight && normalCaveSize < caveChance)
            {
                SetBlock(x, y, z, new BlockGrassNormal(), chunk);

                if (y == dirtHeight && GetNoise(x, 0 , z, TforestTreeFrequency, 100) < TforestTreeDensity)
                {
                    CreateNormalTree(x, y + 1, z, chunk);
                }
                else if(y == dirtHeight && GetNoise(x, 0, z, flowerRedFrequency, 100) < flowerRedDensity)
                {
                    SetBlock(x, y + 1, z, new BlockRedFlower(), chunk, false);
                }
                else if (y == dirtHeight && GetNoise(x, 0, z, flowerBlueFrequency, 100) < flowerBlueDensity)
                {
                    SetBlock(x, y + 1, z, new BlockBlueFlower(), chunk, false);
                }
                else if (y == dirtHeight && GetNoise(x, 0, z, flowerPinkFrequency, 100) < flowerPinkDensity)
                {
                    SetBlock(x, y + 1, z, new BlockPinkFlower(), chunk, false);
                }
            }
            else
            {
                SetBlock(x, y, z, new BlockAir(), chunk);
            }
        }

        return chunk;

    }

    //ALPINE TUNDRA
    public Chunk ChunkColumnGenMagenta(Chunk chunk, int x, int z)
    {
        //Base stone layer 
        float stoneBaseHeight = -24; //Base stone layer height
        float stoneBaseNoise = 0.05f; //Scale of the noise to add to the base height, 0.05 --> peaks 25 blocks apart
        float stoneBaseNoiseHeight = 4; //Height of that noise, 4 --> max diff between peak and valley is 4

        //Mountain variables, Larger height and less frequency
        float stoneMountainHeight = 48;
        float stoneMountainFrequency = 0.048f;
        float stoneMinHeight = -8; //Lowest stone is allowed to go

        //Dirt Variables
        float dirtBaseHeight = 1; // minimum depth on top of the stone 
        float dirtNoise = 0.04f; // little more messy then stoneBaseNoise
        float dirtNoiseHeight = 3;

        //Snow Variables
        float snowBaseHeight = 3;
        float snowNoise = 0.03f;
        float snowNoiseHeight = 1;


        // Stone height
        int stoneHeight = Mathf.FloorToInt(stoneBaseHeight);
        stoneHeight += GetNoise(x, 0, z, stoneMountainFrequency, Mathf.FloorToInt(stoneMountainHeight));

        if (stoneHeight < stoneMinHeight)
            stoneHeight = Mathf.FloorToInt(stoneMinHeight); //after the mountain noise, raise everything under minimum to minmum

        stoneHeight += GetNoise(x, 0, z, stoneBaseNoise, Mathf.FloorToInt(stoneBaseNoiseHeight)); // apply base noise

        int dirtHeight = stoneHeight + Mathf.FloorToInt(dirtBaseHeight);
        dirtHeight += GetNoise(x, 100, z, dirtNoise, Mathf.FloorToInt(dirtNoiseHeight));

        int snowHeight = dirtHeight + Mathf.FloorToInt(snowBaseHeight);
        snowHeight += GetNoise(x, 100, z, snowNoise, Mathf.FloorToInt(snowNoiseHeight));

        float snowFrequency = 0.9f;
        int snowDensity = 100;

        for (int y = chunk.pos.y - 8; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            int caveChance = GetNoise(x, y, z, mountainCaveFrequency, 100);
            if (y <= stoneHeight && mountainCaveSize < caveChance)
            {
                SetBlock(x, y, z, new Block(), chunk);
            }
            else if (y <= dirtHeight && mountainCaveSize < caveChance)
            {
                SetBlock(x, y, z, new BlockDirtGrass(), chunk);

                if (y == dirtHeight && y >= 20 && GetNoise(x, 0, z, snowFrequency, 100) < snowDensity && mountainCaveSize < caveChance)
                {
                    SetBlock(x, y + 1, z, new BlockSnow(), chunk);
                }
            }
            else
            {
                SetBlock(x, y, z, new BlockAir(), chunk);
            }
        }

        return chunk;
    }

    //HOT DESERT
    public Chunk ChunkColumnGenYellow(Chunk chunk, int x, int z)
    {
        //Base stone layer 
        float stoneBaseHeight = -28; //Base stone layer height
        float stoneBaseNoise = 0.05f; //Scale of the noise to add to the base height, 0.05 --> peaks 25 blocks apart
        float stoneBaseNoiseHeight = 4; //Height of that noise, 4 --> max diff between peak and valley is 4

        //Mountain variables, Larger height and less frequency
        float stoneMountainHeight = 20; //38
        float stoneMountainFrequency = 0.0008f;
        float stoneMinHeight = -12; //Lowest stone is allowed to go

        //Sand Variables
        float sandBaseHeight = 3; // minimum depth on top of the stone 
        float sandNoise = 0.04f; // little more messy then stoneBaseNoise
        float sandNoiseHeight = 3;

        int stoneHeight = Mathf.FloorToInt(stoneBaseHeight);
        stoneHeight += GetNoise(x, 0, z, stoneMountainFrequency, Mathf.FloorToInt(stoneMountainHeight));

        if (stoneHeight < stoneMinHeight)
            stoneHeight = Mathf.FloorToInt(stoneMinHeight); //after the mountain noise, raise everything under minimum to minmum

        stoneHeight += GetNoise(x, 0, z, stoneBaseNoise, Mathf.FloorToInt(stoneBaseNoiseHeight)); // apply base noise

        int sandHeight = stoneHeight + Mathf.FloorToInt(sandBaseHeight);
        sandHeight += GetNoise(x, 100, z, sandNoise, Mathf.FloorToInt(sandNoiseHeight));


        for (int y = chunk.pos.y; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            int caveChance = GetNoise(x, y, z, normalCaveFrequency, 100);
            if (y <= stoneHeight && normalCaveSize < caveChance)
            {
                SetBlock(x, y, z, new Block(), chunk);
            }
            else if (y <= sandHeight && normalCaveSize < caveChance)
            {
                SetBlock(x, y, z, new BlockSand(), chunk, true);

                if(y == sandHeight && GetNoise(x, 0, z, cactiFrequency, 100) < cactiDensity)
                {
                    CreateCacti(x, y + 1, z, chunk);
                }
                else if(y == sandHeight && GetNoise(x, 0, z, shrubFrequency, 100) < shrubDensity)
                {
                    SetBlock(x, y + 1, z, new BlockDryBush(), chunk, true);
                }
            }
            else
            {
                SetBlock(x, y, z, new BlockAir(), chunk);
            }
        }


        return chunk;
    }

    //COLD DESERT
    public Chunk ChunkColumnGenWhite(Chunk chunk, int x, int z)
    {
        //Base stone layer 
        float stoneBaseHeight = -28; //Base stone layer height
        float stoneBaseNoise = 0.05f; //Scale of the noise to add to the base height, 0.05 --> peaks 25 blocks apart
        float stoneBaseNoiseHeight = 4; //Height of that noise, 4 --> max diff between peak and valley is 4

        //Mountain variables, Larger height and less frequency
        float stoneMountainHeight = 20; //38
        float stoneMountainFrequency = 0.0008f;
        float stoneMinHeight = -12; //Lowest stone is allowed to go

        //Sand Variables
        float snowBaseHeight = 3; // minimum depth on top of the stone 
        float snowNoise = 0.04f; // little more messy then stoneBaseNoise
        float snowNoiseHeight = 3;

        int stoneHeight = Mathf.FloorToInt(stoneBaseHeight);
        stoneHeight += GetNoise(x, 0, z, stoneMountainFrequency, Mathf.FloorToInt(stoneMountainHeight));

        if (stoneHeight < stoneMinHeight)
            stoneHeight = Mathf.FloorToInt(stoneMinHeight); //after the mountain noise, raise everything under minimum to minmum

        stoneHeight += GetNoise(x, 0, z, stoneBaseNoise, Mathf.FloorToInt(stoneBaseNoiseHeight)); // apply base noise

        int snowHeight = stoneHeight + Mathf.FloorToInt(snowBaseHeight);
        snowHeight += GetNoise(x, 100, z, snowNoise, Mathf.FloorToInt(snowNoiseHeight));


        for (int y = chunk.pos.y; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            int caveChance = GetNoise(x, y, z, normalCaveFrequency, 100);
            if (y <= stoneHeight && normalCaveSize < caveChance)
            {
                SetBlock(x, y, z, new Block(), chunk);
            }
            else if (y <= snowHeight && normalCaveSize < caveChance)
            {
                SetBlock(x, y, z, new BlockSnow(), chunk);

                if (y == snowHeight && GetNoise(x, 0, z, shrubFrequency, 100) < shrubDensity)
                {
                    SetBlock(x, y + 1, z, new BlockFrozenBush(), chunk, true);
                }
            }
            else
            {
                SetBlock(x, y, z, new BlockAir(), chunk);
            }
        }
        return chunk;
    }

    //TAIGA
    public Chunk ChunkColumnGenBlack(Chunk chunk, int x, int z)
    {
        //Base stone layer 
        float stoneBaseHeight = -24; //Base stone layer height
        float stoneBaseNoise = 0.05f; //Scale of the noise to add to the base height, 0.05 --> peaks 25 blocks apart
        float stoneBaseNoiseHeight = 4; //Height of that noise, 4 --> max diff between peak and valley is 4

        //Mountain variables, Larger height and less frequency
        float stoneMountainHeight = 48;
        float stoneMountainFrequency = 0.048f;
        float stoneMinHeight = -8; //Lowest stone is allowed to go

        //Dirt Variables
        float dirtBaseHeight = 1; // minimum depth on top of the stone 
        float dirtNoise = 0.04f; // little more messy then stoneBaseNoise
        float dirtNoiseHeight = 3;

        //Snow Variables
        float snowBaseHeight = 3;
        float snowNoise = 0.03f;
        float snowNoiseHeight = 1;


        // Stone height
        int stoneHeight = Mathf.FloorToInt(stoneBaseHeight);
        stoneHeight += GetNoise(x, 0, z, stoneMountainFrequency, Mathf.FloorToInt(stoneMountainHeight));

        if (stoneHeight < stoneMinHeight)
            stoneHeight = Mathf.FloorToInt(stoneMinHeight); //after the mountain noise, raise everything under minimum to minmum

        stoneHeight += GetNoise(x, 0, z, stoneBaseNoise, Mathf.FloorToInt(stoneBaseNoiseHeight)); // apply base noise

        int dirtHeight = stoneHeight + Mathf.FloorToInt(dirtBaseHeight);
        dirtHeight += GetNoise(x, 100, z, dirtNoise, Mathf.FloorToInt(dirtNoiseHeight));

        int snowHeight = dirtHeight + Mathf.FloorToInt(snowBaseHeight);
        snowHeight += GetNoise(x, 100, z, snowNoise, Mathf.FloorToInt(snowNoiseHeight));

        float snowFrequency = 0.9f;
        int snowDensity = 100;

        for (int y = chunk.pos.y - 8; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            int caveChance = GetNoise(x, y, z, mountainCaveFrequency, 100);
            if (y <= stoneHeight && mountainCaveSize < caveChance)
            {
                SetBlock(x, y, z, new Block(), chunk);
            }
            else if (y <= dirtHeight && mountainCaveSize < caveChance)
            {
                SetBlock(x, y, z, new BlockGrassDark(), chunk);

                if (y == dirtHeight && y >= 20 && GetNoise(x, 0, z, snowFrequency, 100) < snowDensity && mountainCaveSize < caveChance)
                {
                    SetBlock(x, y + 1, z, new BlockSnow(), chunk);
                }
                else if (y == dirtHeight && GetNoise(x, 0, z, taigaTreeFrequency, 100) < taigaTreeDensity)
                {
                    CreateTaigaTree(x, y + 1, z, chunk);
                }
            }
            else
            {
                SetBlock(x, y, z, new BlockAir(), chunk);
            }
        }
        return chunk;
    }

    //TROPICAL FOREST
    public Chunk ChunkColumnGenCyan(Chunk chunk, int x, int z)
    {
        //Base stone layer 
        float stoneBaseHeight = -28; //Base stone layer height
        float stoneBaseNoise = 0.05f; //Scale of the noise to add to the base height, 0.05 --> peaks 25 blocks apart
        float stoneBaseNoiseHeight = 4; //Height of that noise, 4 --> max diff between peak and valley is 4

        //Mountain variables, Larger height and less frequency
        float stoneMountainHeight = 20; //38
        float stoneMountainFrequency = 0.0008f;
        float stoneMinHeight = -12; //Lowest stone is allowed to go

        //Dirt Variables
        float dirtBaseHeight = 3; // minimum depth on top of the stone 
        float dirtNoise = 0.04f; // little more messy then stoneBaseNoise
        float dirtNoiseHeight = 3;

        int stoneHeight = Mathf.FloorToInt(stoneBaseHeight);
        stoneHeight += GetNoise(x, 0, z, stoneMountainFrequency, Mathf.FloorToInt(stoneMountainHeight));

        if (stoneHeight < stoneMinHeight)
            stoneHeight = Mathf.FloorToInt(stoneMinHeight); //after the mountain noise, raise everything under minimum to minmum

        stoneHeight += GetNoise(x, 0, z, stoneBaseNoise, Mathf.FloorToInt(stoneBaseNoiseHeight)); // apply base noise

        int dirtHeight = stoneHeight + Mathf.FloorToInt(dirtBaseHeight);
        dirtHeight += GetNoise(x, 100, z, dirtNoise, Mathf.FloorToInt(dirtNoiseHeight));

        float bushFrequency = 0.8f;
        int bushDensity = 15;

        for (int y = chunk.pos.y - 8; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            int caveChance = GetNoise(x, y, z, normalCaveFrequency, 100);

            if (y <= stoneHeight && normalCaveSize < caveChance)
            {
                SetBlock(x, y, z, new Block(), chunk);
            }
            else if (y <= dirtHeight && normalCaveSize < caveChance)
            {
                SetBlock(x, y, z, new BlockGrassTropical(), chunk);

                if (y == dirtHeight && GetNoise(x, 0, z, tropicalTreeFrequency, 100) < tropicalTreeDensity)
                {
                    CreateTropicalTree(x, y + 1, z, chunk);
                }
                else if(y == dirtHeight && GetNoise(x, 0, z, bushFrequency, 100) < bushDensity && normalCaveSize < caveChance)
                {
                    CreateTropicalBush(x, y + 1, z, chunk);
                }
            }
            else
            {
                SetBlock(x, y, z, new BlockAir(), chunk);
            }
        }

        return chunk;
    }

    //SAVANNAH
    public Chunk ChunkColumnGenRed(Chunk chunk, int x, int z)
    {
        //Base stone layer 
        float stoneBaseHeight = -28; //Base stone layer height
        float stoneBaseNoise = 0.05f; //Scale of the noise to add to the base height, 0.05 --> peaks 25 blocks apart
        float stoneBaseNoiseHeight = 4; //Height of that noise, 4 --> max diff between peak and valley is 4

        //Mountain variables, Larger height and less frequency
        float stoneMountainHeight = 20; //38
        float stoneMountainFrequency = 0.0008f;
        float stoneMinHeight = -12; //Lowest stone is allowed to go

        //Dirt Variables
        float dirtBaseHeight = 3; // minimum depth on top of the stone 
        float dirtNoise = 0.04f; // little more messy then stoneBaseNoise
        float dirtNoiseHeight = 3;

        int stoneHeight = Mathf.FloorToInt(stoneBaseHeight);
        stoneHeight += GetNoise(x, 0, z, stoneMountainFrequency, Mathf.FloorToInt(stoneMountainHeight));

        if (stoneHeight < stoneMinHeight)
            stoneHeight = Mathf.FloorToInt(stoneMinHeight); //after the mountain noise, raise everything under minimum to minmum

        stoneHeight += GetNoise(x, 0, z, stoneBaseNoise, Mathf.FloorToInt(stoneBaseNoiseHeight)); // apply base noise

        int dirtHeight = stoneHeight + Mathf.FloorToInt(dirtBaseHeight);
        dirtHeight += GetNoise(x, 100, z, dirtNoise, Mathf.FloorToInt(dirtNoiseHeight));

        for (int y = chunk.pos.y - 8; y < chunk.pos.y + Chunk.chunkSize; y++)
        {
            int caveChance = GetNoise(x, y, z, normalCaveFrequency, 100);

            if (y <= stoneHeight && normalCaveSize < caveChance)
            {
                SetBlock(x, y, z, new Block(), chunk);
            }
            else if (y <= dirtHeight && normalCaveSize < caveChance)
            {
                SetBlock(x, y, z, new BlockGrassDry(), chunk);

                if (y == dirtHeight && GetNoise(x, 0, z, savannahTreeFrequency, 100) < savannahTreeDensity)
                {
                    CreateSavannahTree(x, y + 1, z, chunk);
                }
            }
            else
            {
                SetBlock(x, y, z, new BlockAir(), chunk);
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
                    SetBlock(x + xi, y + yi, z + zi, new BlockNormalLeaf(), chunk, true);
                }
            }
        }
        //create trunk
        for (int yt = 0; yt < 6; yt++)
        {
            SetBlock(x, y + yt, z, new BlockNormalWood(), chunk, true);
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
                    SetBlock(x + xi, y + yi, z + zi, new BlockDarkLeaf(), chunk, true);
                }
            }
        }

        //create trunk
        for (int yt = 0; yt < 12; yt++)
        {
            SetBlock(x, y + yt, z, new BlockNormalWood(), chunk, true);
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
                    SetBlock(x + xi, y + yi, z + zi, new BlockDryLeaf(), chunk, true);
                }
            }
        }

        //create trunk
        for (int yt = 0; yt < 8; yt++)
        {
            SetBlock(x, y + yt, z, new BlockDryWood(), chunk, true);
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
                    SetBlock(x + xi, y + yi, z + zi, new BlockTropicalLeaf(), chunk, true);
                }
            }
        }

        //create trunk
        for (int yt = 0; yt < 9; yt++)
        {
            SetBlock(x, y + yt, z, new BlockTropicalWood(), chunk, true);   
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
                    SetBlock(x + xi, y + yi, z + zi, new BlockTropicalLeaf(), chunk, true);
                }
            }
        }

        //create trunk
        for (int yt = 0; yt < 1; yt++)
        {
            SetBlock(x, y + yt, z, new BlockTropicalWood(), chunk, true);
        }
    }


    void CreateCacti(int x, int y, int z, Chunk chunk)
    {
        for(int yt = 0; yt < 4; yt++)
        {
            SetBlock(x, y + yt, z, new BlockCactus(), chunk, true);
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
           if (replaceBlocks || chunk.blocks[x, y, z] == null)
                chunk.SetBlock(x, y, z, block);
        }
    }

    public static int GetNoise(int x, int y, int z, float scale, int max)
    {
        GameObject imgReader = GameObject.Find("ImageReader");
        ReadImage readImage = imgReader.GetComponent<ReadImage>();
        int seed = readImage.seed;

        System.Random prng = new System.Random(seed);

        float offsetX = prng.Next(-100000, 100000);
        float offsetY = 0;//prng.Next(-100000, 100000);
        float offsetZ = prng.Next(-100000, 100000);

        return Mathf.FloorToInt((Noise.Generate(x * scale + offsetX, y * scale + offsetY, z * scale + offsetZ) + 1f) * (max / 2f));
    }
}