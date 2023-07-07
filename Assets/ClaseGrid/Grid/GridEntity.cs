using System;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
public class GridEntity : MonoBehaviour
{
	public event Action<GridEntity> OnMove = delegate {};
	public Vector3 velocity = new Vector3(0, 0, 0);
    public bool onGrid;
    Renderer _rend;
    public SpatialGrid3D _spatialGrid { get; private set; }

    private void Awake()
    {
        _rend = GetComponent<Renderer>();
    }

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
    public void Moved() 
    {
        OnMove(this);
    }
}
