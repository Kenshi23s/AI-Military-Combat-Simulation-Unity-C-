using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Rigidbody rotation and vector projection helpers.</summary>
public static class JExtensionMethods
{
    /// <summary>Moves and rotates rb to targetRotation around point.</summary>
    public static void RotateToAroundPoint(this Rigidbody rb, Quaternion targetRotation, Vector3 point)
    {
        Vector3 offsetToPoint = point - rb.position;
        Quaternion rotationNeeded = targetRotation * Quaternion.Inverse(rb.rotation);
        rb.MovePosition(rb.position + rotationNeeded * -offsetToPoint + offsetToPoint);
        rb.MoveRotation(targetRotation);
    }

    /// <summary>Moves and rotates rb by rotation around point.</summary>
    public static void RotateAroundPoint(this Rigidbody rb, Quaternion rotation, Vector3 point)
    {
        Quaternion targetRotation = rb.rotation * rotation;
        Vector3 offsetToPoint = point - rb.position;
        Quaternion rotationNeeded = targetRotation * Quaternion.Inverse(rb.rotation);
        rb.MovePosition(rb.position + rotationNeeded * -offsetToPoint + offsetToPoint);
        rb.MoveRotation(targetRotation);
    }

    /// <summary>Returns direction projected on the plane of planeNormal, normalized.</summary>
    /// <param name="planeNormal">Unit normal of the plane.</param>
    /// <remarks>Vector3.ProjectOnPlane divides by the squared length of the normal. A unit normal skips that division.</remarks>
    public static Vector3 ProjectDirectionOnPlane(this Vector3 direction, Vector3 planeNormal)
    {
        return (direction - planeNormal * Vector3.Dot(direction, planeNormal)).normalized;
    }
}
