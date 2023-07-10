using System;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class GridEntity : Entity
{
	public event Action<GridEntity> OnMove = delegate {};
	[NonSerialized]public Vector3 velocity = new Vector3(0, 0, 0);
    public bool onGrid;

    SpatialGrid3D _spatialGrid;
    public SpatialGrid3D SpatialGrid => _spatialGrid;


    public IEnumerable<GridEntity> GetEntitiesInRange(float range) 
    {
        //creo una "caja" con las dimensiones deseadas, y luego filtro segun distancia para formar el círculo
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

    /// <summary>
    /// Llama al evento OnMove().
    /// </summary>
    protected void Moved() 
    {
        OnMove(this);
    }
}
