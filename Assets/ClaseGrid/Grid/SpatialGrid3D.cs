using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3D grid of cells that stores each GridEntity in the cell of its position.
/// Query returns the entities inside a box by visiting only the cells that the box overlaps.
/// </summary>
/// <remarks>
/// <para>AddEntity, RemoveEntity and UpdateEntity run in O(1).</para>
/// <para>Query runs in O(c + e), where c is the number of overlapped cells and e is the number of entities in them.</para>
/// </remarks>
public class SpatialGrid3D : MonoBehaviour
{
    #region Variables

    [SerializeField] bool takePositionFromTransform = false;

    /// <summary>World position of the grid's minimum corner.</summary>
    public float x, y, z;

    /// <summary>Size of each cell on each axis.</summary>
    [Min(0)]
    public float cellWidth, cellHeight, cellDepth;

    /// <summary>Number of cells on each axis.</summary>
    [Min(0)]
    public int width, height, depth;

    /// <summary>Cell coordinates of an entity outside the grid.</summary>
    public static readonly Vector3Int Outside = new Vector3Int(-1, -1, -1);

    /// <summary>Empty collection returned by Query when the box is outside the grid.</summary>
    public static readonly GridEntity[] Empty = new GridEntity[0];

    // Every entity registered with AddEntity, inside or outside the grid.
    readonly HashSet<GridEntity> _entities = new HashSet<GridEntity>();

    // Last known cell of each entity inside the grid.
    Dictionary<GridEntity, Vector3Int> lastPositions;

    // The entities in each cell.
    HashSet<GridEntity>[,,] buckets;

    Vector3 _gridCenter;

    #endregion

    #region Unity calls

    private void OnValidate()
    {
#if UNITY_EDITOR
        runInEditMode = takePositionFromTransform;
#endif

        CalculateGridCenter();
    }

    private void Awake()
    {
        CalculateGridCenter();

        lastPositions = new Dictionary<GridEntity, Vector3Int>();
        buckets = new HashSet<GridEntity>[width, height, depth];

        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                for (int k = 0; k < depth; k++)
                    buckets[i, j, k] = new HashSet<GridEntity>();

        foreach (var entity in FindObjectsOfType<GridEntity>(false))
            AddEntity(entity);
    }

    public void Update()
    {
#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject) && takePositionFromTransform && transform.hasChanged)
            SetGridPosition();
#endif
    }

    void OnDestroy()
    {
        foreach (var entity in _entities)
        {
            if (entity == null)
                continue;

            entity.SetSpatialGrid(null);
            entity.OnGrid = false;
            entity.OnMove -= UpdateEntity;
        }

        _entities.Clear();
    }

    #endregion

    #region Entities

    /// <summary>
    /// Registers the entity in the grid and places it in the cell of its position.
    /// The grid updates the entity's cell on every OnMove event.
    /// </summary>
    public void AddEntity(GridEntity entity)
    {
        entity.SetSpatialGrid(this);

        // Unsubscribing first keeps a single subscription when AddEntity runs twice for the same entity.
        entity.OnMove -= UpdateEntity;
        entity.OnMove += UpdateEntity;
        _entities.Add(entity);

        UpdateEntity(entity);
    }

    /// <summary>
    /// Removes the entity from its cell and unregisters it from the grid.
    /// Sets OnGrid to false and the entity's SpatialGrid to null.
    /// </summary>
    public void RemoveEntity(GridEntity entity)
    {
        if (lastPositions != null && lastPositions.TryGetValue(entity, out Vector3Int lastPos))
        {
            buckets[lastPos.x, lastPos.y, lastPos.z].Remove(entity);
            lastPositions.Remove(entity);
        }

        _entities.Remove(entity);
        entity.OnGrid = false;
        entity.SetSpatialGrid(null);
        entity.OnMove -= UpdateEntity;
    }

    /// <summary>
    /// Moves the entity to the cell of its current position.
    /// Sets OnGrid to true when the position is inside the grid.
    /// </summary>
    public void UpdateEntity(GridEntity entity)
    {
        Vector3Int lastPos = lastPositions.TryGetValue(entity, out Vector3Int known) ? known : Outside;
        Vector3Int currentPos = GetPositionInGrid(entity.transform.position);

        if (lastPos == currentPos)
            return;

        if (IsInsideGrid(lastPos))
            buckets[lastPos.x, lastPos.y, lastPos.z].Remove(entity);

        if (IsInsideGrid(currentPos))
        {
            buckets[currentPos.x, currentPos.y, currentPos.z].Add(entity);
            lastPositions[entity] = currentPos;
            entity.OnGrid = true;
        }
        else
        {
            lastPositions.Remove(entity);
            entity.OnGrid = false;
        }
    }

    #endregion

    #region Queries

    /// <summary>
    /// Returns the entities inside the box between aabbFrom and aabbTo that pass filterByPosition.
    /// </summary>
    /// <param name="aabbFrom">One corner of the box.</param>
    /// <param name="aabbTo">The opposite corner of the box.</param>
    /// <param name="filterByPosition">Returns true for the entity positions to keep.</param>
    /// <returns>A new list with the matching entities. Empty when the box is outside the grid.</returns>
    public IEnumerable<GridEntity> Query(Vector3 aabbFrom, Vector3 aabbTo, Func<Vector3, bool> filterByPosition)
    {
        Vector3 from = Vector3.Min(aabbFrom, aabbTo);
        Vector3 to = Vector3.Max(aabbFrom, aabbTo);

        Vector3Int fromCell = GetPositionInGrid(from);
        Vector3Int toCell = GetPositionInGrid(to);

        // The box is outside the grid on at least one axis.
        if (toCell.x < 0 || toCell.y < 0 || toCell.z < 0 ||
            fromCell.x >= width || fromCell.y >= height || fromCell.z >= depth)
            return Empty;

        fromCell = Vector3Int.Max(fromCell, Vector3Int.zero);
        toCell = Vector3Int.Min(toCell, new Vector3Int(width - 1, height - 1, depth - 1));

        var results = new List<GridEntity>();

        for (int i = fromCell.x; i <= toCell.x; i++)
            for (int j = fromCell.y; j <= toCell.y; j++)
                for (int k = fromCell.z; k <= toCell.z; k++)
                {
                    foreach (GridEntity entity in buckets[i, j, k])
                    {
                        Vector3 pos = entity.transform.position;

                        bool insideBox =
                            from.x <= pos.x && pos.x <= to.x &&
                            from.y <= pos.y && pos.y <= to.y &&
                            from.z <= pos.z && pos.z <= to.z;

                        if (insideBox && filterByPosition(pos))
                            results.Add(entity);
                    }
                }

        return results;
    }

    /// <summary>Returns the cell coordinates of a world position.</summary>
    public Vector3Int GetPositionInGrid(Vector3 pos)
    {
        return new Vector3Int(
            Mathf.FloorToInt((pos.x - x) / cellWidth),
            Mathf.FloorToInt((pos.y - y) / cellHeight),
            Mathf.FloorToInt((pos.z - z) / cellDepth));
    }

    /// <summary>Returns true when the cell coordinates are inside the grid.</summary>
    public bool IsInsideGrid(Vector3Int position)
    {
        return 0 <= position.x && position.x < width &&
               0 <= position.y && position.y < height &&
               0 <= position.z && position.z < depth;
    }

    /// <summary>Returns the world position of the grid's center.</summary>
    public Vector3 GetMidleOfGrid() => _gridCenter;

    #endregion

    #region Grid position

    void SetGridPosition()
    {
        x = transform.position.x;
        y = transform.position.y;
        z = transform.position.z;

        CalculateGridCenter();
    }

    void CalculateGridCenter()
    {
        Vector3 dimensions = new Vector3(width * cellWidth, height * cellHeight, depth * cellDepth);
        _gridCenter = new Vector3(x, y, z) + dimensions / 2;
    }

    #endregion

    #region Graphic representation

    /// <summary>Hides every grid gizmo.</summary>
    public bool AreGizmosShutDown;

    /// <summary>Draws connections only between entities in the same cell.</summary>
    public bool activatedGrid;

    /// <summary>Logs the connection count of each entity on the next gizmo draw.</summary>
    public bool showLogs = true;

    private void OnDrawGizmos()
    {
        if (AreGizmosShutDown) return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_gridCenter, 50f);

        Gizmos.color = new Color(1, 1, 1, 0.1f);
        DrawGridLines();

        if (buckets == null) return;

        if (activatedGrid)
            DrawConnectionsInsideCells();
        else
            DrawAllConnections();

        showLogs = false;
    }

    void DrawGridLines()
    {
        float sizeX = cellWidth * width;
        float sizeY = cellHeight * height;
        float sizeZ = cellDepth * depth;

        // Lines along the X axis.
        for (int j = 0; j <= height; j++)
            for (int k = 0; k <= depth; k++)
            {
                var start = new Vector3(x, y + j * cellHeight, z + k * cellDepth);
                Gizmos.DrawLine(start, start + new Vector3(sizeX, 0, 0));
            }

        // Lines along the Y axis.
        for (int i = 0; i <= width; i++)
            for (int k = 0; k <= depth; k++)
            {
                var start = new Vector3(x + i * cellWidth, y, z + k * cellDepth);
                Gizmos.DrawLine(start, start + new Vector3(0, sizeY, 0));
            }

        // Lines along the Z axis.
        for (int i = 0; i <= width; i++)
            for (int j = 0; j <= height; j++)
            {
                var start = new Vector3(x + i * cellWidth, y + j * cellHeight, z);
                Gizmos.DrawLine(start, start + new Vector3(0, 0, sizeZ));
            }
    }

    // Draws a line between every pair of entities in the grid: O(n²).
    void DrawAllConnections()
    {
        var all = new List<GridEntity>();
        foreach (var bucket in buckets)
            all.AddRange(bucket);

        DrawConnections(all);
    }

    // Draws a line between every pair of entities that share a cell.
    void DrawConnectionsInsideCells()
    {
        foreach (var bucket in buckets)
            DrawConnections(bucket);
    }

    void DrawConnections(IReadOnlyCollection<GridEntity> group)
    {
        foreach (var entity in group)
        {
            foreach (var other in group)
            {
                if (other != entity)
                    Gizmos.DrawLine(entity.transform.position, other.transform.position);
            }

            if (showLogs)
                Debug.Log("tengo " + (group.Count - 1) + " conexiones por individuo");
        }
    }

    #endregion
}
