using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registers its Entity in the SpatialGrid3D of the scene.
/// Raises OnMove on each frame where the transform position changed.
/// </summary>
[RequireComponent(typeof(Entity))]
public class GridEntity : MonoBehaviour
{
    // Grid shared by every GridEntity, found once with FindObjectOfType.
    private static SpatialGrid3D _sceneGrid;

    /// <summary>Raised in LateUpdate when the transform position changed.</summary>
    public event Action<GridEntity> OnMove = delegate { };

    /// <summary>true while the entity is inside a cell of SpatialGrid.</summary>
    public bool OnGrid;

    /// <summary>The Entity on the same GameObject.</summary>
    public Entity Owner { get; private set; }

    /// <summary>The grid that holds this entity.</summary>
    public SpatialGrid3D SpatialGrid { get; private set; }

    private Vector3 _previousPos;

    private void Awake()
    {
        Owner = GetComponent<Entity>();
    }

    // true after Start. The first registration waits for Start so the grid has run its Awake.
    private bool _started;

    private void Start()
    {
        _started = true;
        LookGrid();
    }

    private void OnEnable()
    {
        if (_started)
            LookGrid();
    }

    // A disabled entity leaves the grid: pooled missiles and dead soldiers stop appearing in queries.
    private void OnDisable()
    {
        if (SpatialGrid != null)
            SpatialGrid.RemoveEntity(this);
    }

    private void OnDestroy()
    {
        if (SpatialGrid != null)
            SpatialGrid.RemoveEntity(this);
    }

    private void LateUpdate()
    {
        if (transform.position != _previousPos)
        {
            OnMove(this);
            _previousPos = transform.position;
        }
    }

    /// <summary>
    /// Adds this entity to the scene's SpatialGrid3D when SpatialGrid is null.
    /// </summary>
    public void LookGrid()
    {
        if (SpatialGrid != null)
            return;

        if (_sceneGrid == null)
            _sceneGrid = FindObjectOfType<SpatialGrid3D>();

        if (_sceneGrid == null)
        {
            Debug.LogWarning(name + ": no hay ninguna SpatialGrid3D en la escena.");
            return;
        }

        _sceneGrid.AddEntity(this);
    }

    /// <summary>
    /// Returns the Owner of each GridEntity within range of this entity.
    /// The result includes this entity's Owner.
    /// </summary>
    /// <param name="range">Radius of the sphere around this entity's position.</param>
    /// <returns>A new list with the entities in range. Empty while OnGrid is false.</returns>
    public IEnumerable<Entity> GetEntitiesInRange(float range)
    {
        if (!OnGrid || SpatialGrid == null)
            return Array.Empty<Entity>();

        Vector3 center = transform.position;
        Vector3 extents = new Vector3(range, range, range);
        float sqrRange = range * range;

        // The grid returns the cube around the center. The filter keeps the sphere.
        var entities = SpatialGrid.Query(center - extents, center + extents,
            pos => (pos - center).sqrMagnitude < sqrRange);

        var owners = new List<Entity>();
        foreach (GridEntity entity in entities)
            owners.Add(entity.Owner);

        return owners;
    }

    /// <summary>Sets the grid that holds this entity.</summary>
    public void SetSpatialGrid(SpatialGrid3D spatialGrid)
    {
        SpatialGrid = spatialGrid;
    }
}
