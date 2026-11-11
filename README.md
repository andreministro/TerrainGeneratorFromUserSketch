# TerrainGeneratorFromUserSketch
Project for a Master Thesis: Terrain Generator based on a User sketch

Open `SketchScene` and enter Play Mode. Choose a square canvas size from 1×1 to 256×256, select **Start drawing**, and paint with the biome brushes. Enter a seed if you want a specific terrain variation. **Save** exports a new PNG and opens the generated terrain.

In the Unity Editor, each save adds a new PNG asset to `Assets/UserSketches` immediately. Player builds save to `UserSketches` in the application's persistent data folder. To use an existing PNG, assign it to `ImageReader` in `TerrainScene` and enter Play Mode there.

You'll be presented with an orthographic view of the terrain, press "K" to change to a First Person free camera, press "L" to change to a First Person player perspective camera, press "O" to go back to the orthogonal camera.
