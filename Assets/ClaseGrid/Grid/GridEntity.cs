using System;
using UnityEngine;

//[ExecuteInEditMode]
public class GridEntity : MonoBehaviour
{
	public event Action<GridEntity> OnMove = delegate {};
	public Vector3 velocity = new Vector3(0, 0, 0);
    public bool onGrid;
    Renderer _rend;

    private void Awake()
    {
        _rend = GetComponent<Renderer>();
    }

    void Update() {
      
	}
}
