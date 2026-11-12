using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public partial class TerrainManager
{
    private const int FullDetailMaxSize = 256;
    private const int MaxDetailRadius = 16;
    private const int InitialDetailRadius = 3;

    [SerializeField, Range(1, MaxDetailRadius)]
    [Tooltip("Number of detailed chunk columns to keep on each side of the active first-person view on large sketches.")]
    private int detailRadius = 10;
    [SerializeField]
    [Tooltip("Voxel spacing of the overview mesh. Very Fine uses Fine on the largest sketches to bound mesh memory.")]
    private OverviewDetail overviewDetail = OverviewDetail.Standard;

    private OverviewDetail buildOverviewDetail;
    private TerrainOverview overview;
    private TerrainGeneration streamGeneration;
    private Transform explorationTarget;
    private Coroutine streamRoutine;
    private RectInt residentWindow;
    private RectInt? meshingWindow;

    internal bool UsesStreaming => sketchWidth > FullDetailMaxSize;
    internal bool HasTerrain => overview != null || chunks.Count > 0;
    internal bool IsStreamingDetail => streamRoutine != null;
    internal int OverviewVersion { get; private set; }
    private int TopChunk => (sketchWidth <= 64 ? 1 : 3) * Chunk.chunkSize;
    private int MinVoxel => -(sketchWidth / 2);
    private int MaxVoxel => MinVoxel + sketchWidth - 1;

    internal IEnumerable<Bounds> GetOverviewBounds() => overview?.GetBounds();

    internal void SetExplorationTarget(Transform target)
    {
        if (explorationTarget == target)
            return;
        explorationTarget = target;
        if (UsesStreaming)
            UpdateDetailVisibility();
    }

    internal bool CanWalkTo(Vector3 position)
    {
        if (IsBuilding || !HasTerrain)
            return false;

        Vector2Int voxel = GetHorizontalVoxel(position);
        return voxel.x >= MinVoxel && voxel.x <= MaxVoxel && voxel.y >= MinVoxel && voxel.y <= MaxVoxel
            && (!UsesStreaming || residentWindow.Contains(GetColumn(voxel)));
    }

    internal Vector3 ClampToSketch(Vector3 position)
    {
        Vector3 local = chunkRoot.InverseTransformPoint(position);
        local.x = Mathf.Clamp(local.x, MinVoxel, MaxVoxel);
        local.z = Mathf.Clamp(local.z, MinVoxel, MaxVoxel);
        return chunkRoot.TransformPoint(local);
    }

    internal bool TryGetGround(Vector3 position, out Vector3 ground)
    {
        ground = default;
        if (!CanWalkTo(position))
            return false;

        Vector2Int voxel = GetHorizontalVoxel(position);
        if (meshingWindow.HasValue && !meshingWindow.Value.Contains(GetColumn(voxel)))
            return false;

        for (int y = TopChunk + Chunk.chunkSize - 1; y >= -Chunk.chunkSize; y--)
        {
            if (GetBlock(voxel.x, y, voxel.y) is BlockAir)
                continue;
            ground = chunkRoot.TransformPoint(new Vector3(voxel.x, y + 0.5f, voxel.y));
            return true;
        }
        return false;
    }

    internal bool IsBelowTerrain(Vector3 position)
    {
        return chunkRoot.InverseTransformPoint(position).y < -Chunk.chunkSize - 2f;
    }

    internal Vector3 GetFlyStart()
    {
        int y = 10;
        if (streamGeneration != null)
            y = Mathf.Max(y, streamGeneration.GetOverviewSurface(0, 0, out _) + 8);
        return chunkRoot.TransformPoint(new Vector3(0f, y, 0f));
    }

    private void BeginOverviewBuild(TerrainGeneration generation, bool regenerateTerrain)
    {
        settingsRebuildAt = float.PositiveInfinity;
        buildOverviewDetail = overviewDetail;
        if (regenerateTerrain)
            StopStreaming();
        var nextOverview = new TerrainOverview(chunkRoot,
            chunkPrefab.GetComponent<MeshRenderer>().sharedMaterial, sketchWidth, TopChunk, buildOverviewDetail);
        buildRoutine = StartCoroutine(BuildOverview(nextOverview, generation, regenerateTerrain));
    }

    private IEnumerator BuildOverview(TerrainOverview nextOverview, TerrainGeneration generation, bool regenerateTerrain)
    {
        bool finished = false;
        try
        {
            yield return null;
            var work = Stopwatch.StartNew();
            for (int tile = 0; tile < nextOverview.TileCount; tile++)
            {
                nextOverview.AddTile(tile, generation);
                if (work.Elapsed.TotalMilliseconds >= buildBudgetMilliseconds)
                {
                    yield return null;
                    work.Restart();
                }
            }

            nextOverview.Finish();
            overview?.Dispose();
            overview = nextOverview;
            streamGeneration = generation;
            OverviewVersion++;
            if (regenerateTerrain)
                BuildVersion++;
            UpdateDetailVisibility();
            finished = true;
        }
        finally
        {
            buildRoutine = null;
            if (!finished)
                nextOverview.Dispose();
        }
    }

    private void UpdateStreaming()
    {
        if (overview == null || IsBuilding || explorationTarget == null || streamRoutine != null)
            return;

        Vector2Int focus = GetColumn(GetHorizontalVoxel(explorationTarget.position));
        int min = GetChunkOrigin(MinVoxel) / Chunk.chunkSize;
        int max = GetChunkOrigin(MaxVoxel) / Chunk.chunkSize;
        int radius = Mathf.Clamp(detailRadius, 1, MaxDetailRadius);
        int x = Mathf.Clamp(focus.x, min, max);
        int z = Mathf.Clamp(focus.y, min, max);
        // Load a playable area first when entering detail or jumping to a distant location.
        if (!residentWindow.Contains(new Vector2Int(x, z)))
            radius = Mathf.Min(radius, InitialDetailRadius);
        var window = new RectInt();
        window.SetMinMax(new Vector2Int(Mathf.Max(min, x - radius), Mathf.Max(min, z - radius)),
            new Vector2Int(Mathf.Min(max, x + radius) + 1, Mathf.Min(max, z + radius) + 1));
        if (window != residentWindow)
            streamRoutine = StartCoroutine(StreamWindow(window));
    }

    private IEnumerator StreamWindow(RectInt window)
    {
        bool finished = false;
        try
        {
            yield return null;
            var work = Stopwatch.StartNew();
            var remesh = new List<Chunk>();
            int size = Chunk.chunkSize;
            // Generation samples two neighboring voxels for decorations that cross chunk boundaries.
            streamGeneration.ResetElevationCache(new RectInt(window.xMin * size - 2, window.yMin * size - 2,
                window.width * size + 4, window.height * size + 4));
            foreach (Vector2Int column in window.allPositionsWithin)
            {
                bool entering = !residentWindow.Contains(column);
                if (!entering && !HasChangedNeighbor(column, window))
                    continue;

                for (int y = -size; y <= TopChunk; y += size)
                {
                    var position = new WorldPosition(column.x * size, y, column.y * size);
                    if (entering)
                    {
                        Chunk chunk = CreateChunk(position.x, y, position.z, streamGeneration);
                        chunk.enabled = false;
                        chunk.ShowDetail(false);
                        remesh.Add(chunk);
                    }
                    else
                    {
                        remesh.Add(chunks[position]);
                    }
                    if (work.Elapsed.TotalMilliseconds >= buildBudgetMilliseconds)
                    {
                        yield return null;
                        work.Restart();
                    }
                }
            }

            // All incoming blocks are available. Cull only against the new window.
            meshingWindow = window;
            foreach (Chunk chunk in remesh)
            {
                chunk.RebuildMesh();
                if (work.Elapsed.TotalMilliseconds >= buildBudgetMilliseconds)
                {
                    yield return null;
                    work.Restart();
                }
            }
            meshingWindow = null;

            foreach (Vector2Int column in residentWindow.allPositionsWithin)
            {
                if (window.Contains(column))
                    continue;
                for (int y = -size; y <= TopChunk; y += size)
                {
                    var position = new WorldPosition(column.x * size, y, column.y * size);
                    Chunk chunk = chunks[position];
                    chunks.Remove(position);
                    chunk.ShowDetail(false);
                    Destroy(chunk.gameObject);
                }
            }
            foreach (Chunk chunk in remesh)
                chunk.enabled = true;
            residentWindow = window;
            UpdateDetailVisibility();
            finished = true;
        }
        finally
        {
            meshingWindow = null;
            streamRoutine = null;
            if (!finished)
                ClearStreamedChunks();
        }
    }

    private bool HasChangedNeighbor(Vector2Int column, RectInt window)
    {
        return Changed(column + Vector2Int.left) || Changed(column + Vector2Int.right)
            || Changed(column + Vector2Int.down) || Changed(column + Vector2Int.up);

        bool Changed(Vector2Int neighbor) => window.Contains(neighbor) != residentWindow.Contains(neighbor);
    }

    private void UpdateDetailVisibility()
    {
        bool exploring = explorationTarget != null;
        overview?.ShowOutside(exploring ? residentWindow : default);
        foreach (Chunk chunk in chunks.Values)
            chunk.ShowDetail(exploring && residentWindow.Contains(GetColumn(chunk.pos)));
    }

    private void StopStreaming()
    {
        if (!UsesStreaming)
            return;
        if (streamRoutine != null)
            StopCoroutine(streamRoutine);
        streamRoutine = null;
        ClearStreamedChunks();
    }

    private void ClearStreamedChunks()
    {
        foreach (Chunk chunk in chunks.Values)
        {
            if (chunk == null)
                continue;
            chunk.ShowDetail(false);
            Destroy(chunk.gameObject);
        }
        chunks.Clear();
        residentWindow = default;
        overview?.ShowOutside(default);
    }

    private Vector2Int GetHorizontalVoxel(Vector3 position)
    {
        Vector3 local = chunkRoot.InverseTransformPoint(position);
        return new Vector2Int(Mathf.FloorToInt(local.x + 0.5f), Mathf.FloorToInt(local.z + 0.5f));
    }

    private static Vector2Int GetColumn(Vector2Int voxel)
    {
        return new Vector2Int(GetChunkOrigin(voxel.x) / Chunk.chunkSize, GetChunkOrigin(voxel.y) / Chunk.chunkSize);
    }

    private static Vector2Int GetColumn(WorldPosition position)
    {
        return new Vector2Int(position.x / Chunk.chunkSize, position.z / Chunk.chunkSize);
    }

    private void OnDestroy() => overview?.Dispose();
}
