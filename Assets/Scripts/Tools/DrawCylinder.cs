using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DrawCylinder
{
    public static void ForGizmo(Vector3 position, Quaternion orientation, float height, float radius, bool drawFromBase = true)
    {
        Vector3 localUp = orientation * Vector3.up;
        Vector3 localRight = orientation * Vector3.right;
        Vector3 localForward = orientation * Vector3.forward;

        Vector3 basePositionOffset = drawFromBase ? Vector3.zero : (localUp * height * 0.5f);
        Vector3 basePosition = position - basePositionOffset;
        Vector3 topPosition = basePosition + localUp * height;

        Quaternion circleOrientation = orientation * Quaternion.Euler(90, 0, 0);

        Vector3 pointA = basePosition + localRight * radius;
        Vector3 pointB = basePosition + localForward * radius;
        Vector3 pointC = basePosition - localRight * radius;
        Vector3 pointD = basePosition - localForward * radius;

        Gizmos.DrawRay(pointA, localUp * height);
        Gizmos.DrawRay(pointB, localUp * height);
        Gizmos.DrawRay(pointC, localUp * height);
        Gizmos.DrawRay(pointD, localUp * height);

        DrawCircle(basePosition, circleOrientation, radius, 32);
        DrawCircle(topPosition, circleOrientation, radius, 32);
    }

    static void DrawCircle(Vector3 position, Quaternion rotation, float radius, int segments)
    {
        // If either radius or number of segments are less or equal to 0, skip drawing
        if (radius <= 0.0f || segments <= 0)
        {
            return;
        }

        // Single segment of the circle covers (360 / number of segments) degrees
        float angleStep = (360.0f / segments);

        // Result is multiplied by Mathf.Deg2Rad constant which transforms degrees to radians
        // which are required by Unity's Mathf class trigonometry methods

        angleStep *= Mathf.Deg2Rad;

        // lineStart and lineEnd variables are declared outside of the following for loop
        Vector3 lineStart = Vector3.zero;
        Vector3 lineEnd = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            // Line start is defined as starting angle of the current segment (i)
            lineStart.x = Mathf.Cos(angleStep * i);
            lineStart.y = Mathf.Sin(angleStep * i);
            lineStart.z = 0.0f;

            // Line end is defined by the angle of the next segment (i+1)
            lineEnd.x = Mathf.Cos(angleStep * (i + 1));
            lineEnd.y = Mathf.Sin(angleStep * (i + 1));
            lineEnd.z = 0.0f;

            // Results are multiplied so they match the desired radius
            lineStart *= radius;
            lineEnd *= radius;

            // Results are multiplied by the rotation quaternion to rotate them 
            // since this operation is not commutative, result needs to be
            // reassigned, instead of using multiplication assignment operator (*=)
            lineStart = rotation * lineStart;
            lineEnd = rotation * lineEnd;

            // Results are offset by the desired position/origin 
            lineStart += position;
            lineEnd += position;

            // Points are connected using DrawLine method and using the passed color
            Gizmos.DrawLine(lineStart, lineEnd);
        }
    }
}
