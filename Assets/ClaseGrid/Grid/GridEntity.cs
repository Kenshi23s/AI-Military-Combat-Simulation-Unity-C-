using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Entity))]
public class GridEntity : MonoBehaviour
{
	public event Action<GridEntity> OnMove = delegate {};
    Vector3 _previousPos;
    public bool OnGrid;

    public Entity Owner { get; private set; }

    public SpatialGrid3D SpatialGrid { get; private set; }

    private void Awake()
    {
        Owner = GetComponent<Entity>();       
    }

    private void Start()
    {
        SpatialGrid = FindObjectOfType<SpatialGrid3D>();
        SpatialGrid.AddEntity(this);
    }

    private void OnDestroy()
    {
        if (SpatialGrid != null)
            SpatialGrid.RemoveEntity(this);
    }


    public void LookGrid()
    {
        if (SpatialGrid == null)
        {
            SpatialGrid = FindObjectOfType<SpatialGrid3D>();
            SpatialGrid.AddEntity(this);
        }
       
    }
    public IEnumerable<Entity> GetEntitiesInRange(float range)
    {
        //creo una "caja" con las dimensiones deseadas, y luego filtro segun distancia para formar el círculo
        if (!OnGrid) return new List<Entity>();

        float sqrDistance = range * range;
        return SpatialGrid.Query(
            transform.position + new Vector3(-range, -range, -range),
            transform.position + new Vector3(range, range, range),
            x => {
                var position3d = x - transform.position;
                return position3d.sqrMagnitude < sqrDistance;
            })
            .Select(x => x.Owner);
    }

    public void SetSpatialGrid(SpatialGrid3D spatialGrid) 
    {
        SpatialGrid = spatialGrid;
    }

    private void LateUpdate()
    {
        if (transform.position != _previousPos)
        {
            OnMove(this);
            _previousPos = transform.position;
        }
    }
}
