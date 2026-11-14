using UnityEngine;

/// <summary>Distance, field of view and line of sight checks between two points.</summary>
public static class ViewHelper
{
    /// <summary>Returns true when target is within viewRadius of origin.</summary>
    public static bool IsNear(Vector3 origin, Vector3 target, float viewRadius)
    {
        return (target - origin).sqrMagnitude <= viewRadius * viewRadius;
    }

    /// <summary>Returns true when the segment from origin to target hits nothing in obstacleLayer.</summary>
    public static bool IsInLineOfSight(Vector3 origin, Vector3 target, LayerMask obstacleLayer)
    {
        Vector3 dir = target - origin;
        return !Physics.Raycast(origin, dir, dir.magnitude, obstacleLayer);
    }

    /// <summary>
    /// Returns true when a ray from origin along dir reaches targetCollider before any obstacle in obstacleLayer.
    /// </summary>
    /// <param name="hitPos">The point on targetCollider, or the point on the obstacle.</param>
    public static bool IsColliderInLineOfSight(Vector3 origin, Vector3 dir, LayerMask obstacleLayer, Collider targetCollider, out Vector3 hitPos)
    {
        targetCollider.Raycast(new Ray(origin, dir), out RaycastHit hitInfo, float.MaxValue);

        if (!Physics.Raycast(origin, dir, out RaycastHit obstacleHitInfo, hitInfo.distance, obstacleLayer))
        {
            hitPos = hitInfo.point;
            return true;
        }

        hitPos = obstacleHitInfo.point;
        return false;
    }

    /// <summary>Returns true when the angle between forward and dir is at most half of viewAngle.</summary>
    public static bool IsInFOV(Vector3 forward, Vector3 dir, float viewAngle)
    {
        return Vector3.Angle(forward, dir) <= viewAngle / 2;
    }

    /// <summary>Returns true when the angle between forward and dir is at most half of viewAngle.</summary>
    /// <param name="alignment">Dot product of the normalized forward and dir.</param>
    public static bool IsInFOV(Vector3 forward, Vector3 dir, float viewAngle, out float alignment)
    {
        float minViewDotProduct = Mathf.Cos(viewAngle / 2 * Mathf.Deg2Rad);
        alignment = Vector3.Dot(forward.normalized, dir.normalized);
        return alignment >= minViewDotProduct;
    }

    /// <summary>Returns true when target is inside the view cone of viewAngle from origin along forward.</summary>
    public static bool IsInFOV(Vector3 origin, Vector3 forward, Vector3 target, float viewAngle)
    {
        return Vector3.Angle(forward, target - origin) <= viewAngle / 2;
    }

    /// <summary>Returns true when target is inside the view cone of viewAngle from origin along forward.</summary>
    /// <param name="alignment">Dot product of the normalized forward and the direction to target.</param>
    public static bool IsInFOV(Vector3 origin, Vector3 forward, Vector3 target, float viewAngle, out float alignment)
    {
        float minViewDotProduct = Mathf.Cos(viewAngle / 2 * Mathf.Deg2Rad);
        alignment = Vector3.Dot(forward.normalized, (target - origin).normalized);
        return alignment >= minViewDotProduct;
    }

    /// <summary>Returns true when target is within viewRadius, inside viewAngle and in line of sight from origin.</summary>
    /// <remarks>The check uses a single point of the target.</remarks>
    public static bool IsInSight(Vector3 origin, Vector3 forward, Vector3 target, float viewRadius, float viewAngle, LayerMask obstacleLayer)
    {
        if (!IsNear(origin, target, viewRadius)) return false;

        if (IsInFOV(forward, target - origin, viewAngle)) return IsInLineOfSight(origin, target, obstacleLayer);

        return false;
    }

    /// <summary>Draws the radius and the two edges of a field of view as gizmos.</summary>
    public static void DrawFieldOfView(Vector3 origin, Vector3 forward, float viewRadius, float viewAngle)
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(origin, viewRadius);

        float halfViewAngle = viewAngle / 2;

        Quaternion leftRayRotation = Quaternion.AngleAxis(-halfViewAngle, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(halfViewAngle, Vector3.up);

        Gizmos.DrawRay(origin, leftRayRotation * forward * viewRadius);
        Gizmos.DrawRay(origin, rightRayRotation * forward * viewRadius);
    }
}
