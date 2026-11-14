# TerrainGeneratorFromUserSketch
Project for a Master Thesis: Terrain Generator based on a User sketch

Open `SketchScene` and enter Play Mode. Choose a square canvas size from 1×1 to 512×512, select **Start drawing**, and paint with the biome brushes. **Undo** removes the last stroke. Enter a seed if you want a specific terrain variation. **Save** exports a new PNG and opens the generated terrain.

In the Unity Editor, each save adds a new PNG asset to `Assets/UserSketches` immediately. Player builds save to `UserSketches` in the application's persistent data folder. To use an existing PNG, assign it to `ImageReader` in `TerrainScene` and enter Play Mode there.

Press **K** to switch between the orthographic view and the first-person fly camera. **L** opens the first-person player view, **O** returns to the overview, and **P** opens the size chooser for a new sketch.
