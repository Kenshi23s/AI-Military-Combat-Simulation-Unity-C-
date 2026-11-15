using FacundoColomboMethods;
using UnityEngine;

/// <summary>
/// Field of view of an agent: a cone of ViewRadius and _viewAngle from the eyes.
/// IN_FOV checks distance, angle and line of sight to a point.
/// </summary>
[RequireComponent(typeof(DebugableObject))]
public class FOVAgent : MonoBehaviour
{
    /// <summary>Maximum distance of the field of view.</summary>
    [field: SerializeField] public float ViewRadius { get; private set; }

    [SerializeField, Range(0, 360)] private float _viewAngle;
    [SerializeField] private Transform _eyes;

    // Cosine of half of _viewAngle. A point is inside the cone when the cosine of its angle is greater or equal.
    private float _cosHalfViewAngle;

    /// <summary>Sets ViewRadius.</summary>
    public void SetFov(float newRadius)
    {
        ViewRadius = newRadius;
    }

    /// <summary>Sets ViewRadius and the view angle from stats.</summary>
    public void ApplyStats(VisionStats stats)
    {
        ViewRadius = stats.ViewRadius;
        _viewAngle = stats.ViewAngle;
        UpdateCosHalfViewAngle();
    }

    private void Awake()
    {
        GetComponent<DebugableObject>().AddGizmoAction(FovGizmos);
        if (_eyes == null)
        {
            _eyes = transform;
            Debug.LogWarning(name + ": La variable EYES DE Fov no fue asignada, se recomienda asignar para evitar problemas a futuro, por ahora EYES es el transform del componente");
        }

        UpdateCosHalfViewAngle();
    }

    private void OnValidate()
    {
        UpdateCosHalfViewAngle();
    }

    /// <summary>
    /// Returns true when target is inside the field of view.
    /// AI_Manager.WallMask blocks the line of sight.
    /// </summary>
    public bool IN_FOV(Vector3 target)
    {
        return IN_FOV(target, AI_Manager.instance.WallMask);
    }

    /// <summary>
    /// Returns true when target is inside the field of view.
    /// mask blocks the line of sight.
    /// </summary>
    /// <remarks>The raycast runs only for the points inside the cone.</remarks>
    public bool IN_FOV(Vector3 target, LayerMask mask)
    {
        Vector3 dir = target - _eyes.position;
        float sqrDistance = dir.sqrMagnitude;

        if (sqrDistance > ViewRadius * ViewRadius)
            return false;

        // Same check as Vector3.Angle(forward, dir) <= _viewAngle / 2, without the arc cosine.
        float distance = Mathf.Sqrt(sqrDistance);
        if (Vector3.Dot(_eyes.forward, dir) < _cosHalfViewAngle * distance)
            return false;

        return ColomboMethods.InLineOffSight(_eyes.position, target, mask);
    }

    /// <summary>Draws the radius and the two edges of the field of view.</summary>
    public void FovGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_eyes.position, ViewRadius);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, ViewRadius);

        Vector3 lineA = GetVectorFromAngle(_viewAngle / 2 + transform.eulerAngles.y);
        Vector3 lineB = GetVectorFromAngle(-_viewAngle / 2 + transform.eulerAngles.y);

        Gizmos.DrawLine(_eyes.position, _eyes.position + lineA * ViewRadius);
        Gizmos.DrawLine(_eyes.position, _eyes.position + lineB * ViewRadius);
    }

    private void UpdateCosHalfViewAngle()
    {
        _cosHalfViewAngle = Mathf.Cos(_viewAngle * 0.5f * Mathf.Deg2Rad);
    }

    // Unit vector on the XZ plane for a rotation of angle degrees around the Y axis.
    // 0° points to +Z (forward) and 90° points to +X (right), same as transform.eulerAngles.y.
    private Vector3 GetVectorFromAngle(float angle) => new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
}
