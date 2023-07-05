using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ViewHelper
{
    public static bool IsNear(Vector3 origin, Vector3 target, float viewRadius) //Revisa si hay una distancia especifica entre el punto A y el punto B
    {
        float sqrViewRadius = viewRadius * viewRadius; // Toma el viewRadius al cuadrado para optimizar un poco el codigo.
        Vector3 dir = target - origin; //Toma el vector entre los dos objetos

        // Si esta dentro del rango devuelve true.
        // Este metodo esta apenas mas optimizado que hacer Vector3.Distance(origin, target) <= viewRadius
        return dir.sqrMagnitude <= sqrViewRadius;
    }

    //Revisa si hay linea de vision directa entre el objeto A y el B
    public static bool IsInLineOfSight(Vector3 origin, Vector3 target, LayerMask obstacleLayer) 
    {
        Vector3 dir = target - origin; //Toma el vector entre el punto A y el B

        //Tira un raycast desde el objeto A hacia el B
        return !Physics.Raycast(origin, dir, dir.magnitude, obstacleLayer);
    }

    //Revisa si hay linea de vision directa entre el objeto A y el B. Pero chequea si se golpeo el target collider
    public static bool IsColliderInLineOfSight(Vector3 origin, Vector3 dir, LayerMask obstacleLayer, Collider targetCollider, out Vector3 hitPos) 
    {

        targetCollider.Raycast(new Ray(origin, dir), out RaycastHit hitInfo, float.MaxValue);
        // Si no hay ningun obstaculo en el medio
        if (!Physics.Raycast(origin, dir, out RaycastHit obstacleHitInfo, hitInfo.distance, obstacleLayer))
        {
            hitPos = hitInfo.point;
            return true;
        }

        hitPos = obstacleHitInfo.point;
        return false;
    }

    public static bool IsInFOV(Vector3 forward, Vector3 dir, float viewAngle)
    {
        return Vector3.Angle(forward, dir) <= viewAngle / 2;
    }

    public static bool IsInFOV(Vector3 forward, Vector3 dir, float viewAngle, out float alignment)
    {
        float minViewDotProduct = Mathf.Cos(viewAngle / 2 * Mathf.Deg2Rad);
        alignment = Vector3.Dot(forward.normalized, dir.normalized);
        return alignment <= minViewDotProduct;
    }

    public static bool IsInFOV(Vector3 origin, Vector3 forward, Vector3 target, float viewAngle)
    {
        return Vector3.Angle(forward, target - origin) <= viewAngle / 2;
    }

    public static bool IsInFOV(Vector3 origin, Vector3 forward, Vector3 target, float viewAngle, out float alignment)
    {
        float minViewDotProduct = Mathf.Cos(viewAngle / 2 * Mathf.Deg2Rad);
        alignment = Vector3.Dot(forward.normalized, (target - origin).normalized);
        return alignment >= minViewDotProduct;
    }

    public static bool IsInSight(Vector3 origin, Vector3 forward, Vector3 target, float viewRadius, float viewAngle, LayerMask obstacleLayer) //Revisa si el objeto B está en el angulo de visión del objeto A
    {
        // Preguntar si el objeto B está dentro del rango de vision del objeto A
        if (!IsNear(origin, target, viewRadius)) return false; 

        Vector3 dir = target - origin; //Toma el vector entre los dos objetos

        // Revisar si esta dentro del angulo de vision
        // Si tiene una linea de vision directa, el objeto esta en vista, si no, no.
        // NOTA: Que no tenga una linea de vision directa no significa necesariamente que un objecto entero no este en vista, 
        // solo que el punto en el espacio no lo esta.
        if (IsInFOV(forward, dir, viewAngle)) return IsInLineOfSight(origin, target, obstacleLayer);

        return false; // Si está fuera de su angulo de vision devuelve false
    }

    public static void DrawFieldOfView(Vector3 origin, Vector3 forward, float viewRadius, float viewAngle)
    {
        Gizmos.color = Color.blue;
        //Dibujamos el radio de vision con una esfera
        Gizmos.DrawWireSphere(origin, viewRadius); 

        float halfViewAngle = viewAngle / 2;

        // Conseguimos la rotacion necesaria.
        Quaternion leftRayRotation = Quaternion.AngleAxis(-halfViewAngle, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(halfViewAngle, Vector3.up);

        Vector3 leftRayDirection = leftRayRotation * forward; 
        Vector3 rightRayDirection = rightRayRotation * forward;

        //Dibujar las lineas del angulo de vision
        Gizmos.DrawRay(origin, leftRayDirection * viewRadius);
        Gizmos.DrawRay(origin, rightRayDirection * viewRadius);
    }
}
