using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadImage : MonoBehaviour
{
    [SerializeField]
    public Texture2D image;

    public TerrainManager world;
    public int seed;

    [HideInInspector]
    public List<Vector2> bluePos = new List<Vector2>();
    [HideInInspector]
    public List<Vector2> yellowPos = new List<Vector2>();
    [HideInInspector]
    public List<Vector2> magentaPos = new List<Vector2>();
    [HideInInspector]
    public List<Vector2> greenPos = new List<Vector2>();
    [HideInInspector]
    public List<Vector2> whitePos = new List<Vector2>();
    [HideInInspector]
    public List<Vector2> redPos = new List<Vector2>();
    [HideInInspector]
    public List<Vector2> cyanPos = new List<Vector2>();
    [HideInInspector]
    public List<Vector2> blackPos = new List<Vector2>();

    public int mapWidth;
    public int mapHeight;

    public static Color coloryellow = new Color32(255, 255, 0, 255);

    public void DisplayImage()
    {
        //Prepare input texture
        Texture2D duplicateTexture(Texture2D image)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        image.width,
                        image.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(image, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(image.width, image.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.filterMode = FilterMode.Point;
            readableText.wrapMode = TextureWrapMode.Clamp;
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

        Texture2D copy = duplicateTexture(image);

        Color[] colorMapSketch = copy.GetPixels();

        mapWidth = image.width;
        mapHeight = image.height;

        //Verify positions for each pixel
        Vector2[] spawnPositions = new Vector2[colorMapSketch.Length];
        Vector2 startSpawnPos = new Vector2(-Mathf.Round(mapWidth / 2), -Mathf.Round(mapHeight / 2));
        Vector2 currentSpawnPos = startSpawnPos;

        int counter = 0;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                spawnPositions[counter] = currentSpawnPos;
                counter++;
                currentSpawnPos.x++;
            }

            currentSpawnPos.x = startSpawnPos.x;
            currentSpawnPos.y++;
        }
   
        int counterC = 0;

        //save positions for each pixel
        foreach (Vector2 pos in spawnPositions)
        {   
            Color c = colorMapSketch[counterC];
            if (c.Equals(Color.blue))
            {
                bluePos.Add(pos);
            }
            else if (c.Equals(coloryellow))
            {
                yellowPos.Add(pos);
            }
            else if (c.Equals(Color.magenta))
            {
                magentaPos.Add(pos);
            }
            else if (c.Equals(Color.green))
            {
                greenPos.Add(pos);
            }
            else if (c.Equals(Color.white))
            {
                whitePos.Add(pos);
            }
            else if (c.Equals(Color.black))
            {
                blackPos.Add(pos);
            }
            else if (c.Equals(Color.red))
            {
                redPos.Add(pos);
            }
            else if (c.Equals(Color.cyan))
            {
                cyanPos.Add(pos);
            }
            counterC++;
        }
        world.CreateFromSketch(mapWidth, mapHeight);
    }

}
