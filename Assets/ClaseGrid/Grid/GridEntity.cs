using System;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class GridEntity : Entity
{
	public event Action<GridEntity> OnMove = delegate {};
    Vector3 _previousPos;
    public bool OnGrid;

    SpatialGrid3D _spatialGrid;
    public SpatialGrid3D SpatialGrid => _spatialGrid;


    public IEnumerable<GridEntity> GetEntitiesInRange(float range) 
    {
        //creo una "caja" con las dimensiones deseadas, y luego filtro segun distancia para formar el círculo
        if (!OnGrid) return new List<GridEntity>();
       
        float sqrDistance = range * range;
        return _spatialGrid.Query(
            transform.position + new Vector3(-range, -range, -range),
            transform.position + new Vector3(range, range, range),
            x => {
                var position3d = x - transform.position;
                return position3d.sqrMagnitude < sqrDistance;
            });
    }

    public void SetSpatialGrid(SpatialGrid3D spatialGrid) 
    {
        _spatialGrid = spatialGrid;
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
