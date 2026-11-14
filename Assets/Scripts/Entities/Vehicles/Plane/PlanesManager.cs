using UnityEngine;

/// <summary>Shared settings of the planes: flight height, combat zone, missile prefab and ground layers.</summary>
public class PlanesManager : MonoSingleton<PlanesManager>
{
    /// <summary>Minimum height of the optimal flight.</summary>
    public float maxHeight;
    public float DistanceX;
    public float maxDistanceZ;
    public float Scale;

    /// <summary>Prefab of the missiles.</summary>
    [field: SerializeField] public Misile MisilePrefab { get; private set; }

    /// <summary>Layers of the ground for the planes and the missiles.</summary>
    public LayerMask groundMask;

    /// <summary>Returns true when Position is above maxHeight.</summary>
    public bool inOptimalHeight(Vector3 Position)
    {
        return Position.y > maxHeight;
    }

    protected override void SingletonAwake()
    {
    }

    /// <summary>Returns true for every plane.</summary>
    public bool InCombatZone(Plane plane) { return true; }
}
