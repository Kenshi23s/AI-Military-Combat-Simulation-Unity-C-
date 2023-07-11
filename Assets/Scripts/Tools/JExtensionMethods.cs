using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class JExtensionMethods
{
    public static void RotateToAroundPoint(this Rigidbody rb, Quaternion targetRotation, Vector3 point)
    {
        Vector3 offsetToPoint = point - rb.position;
        Quaternion rotationNeeded = targetRotation * Quaternion.Inverse(rb.rotation);
        rb.MovePosition(rb.position + rotationNeeded * -offsetToPoint + offsetToPoint);
        rb.MoveRotation(targetRotation);
    }

    public static void RotateAroundPoint(this Rigidbody rb, Quaternion rotation, Vector3 point)
    {
        Quaternion targetRotation = rb.rotation * rotation;
        Vector3 offsetToPoint = point - rb.position;
        Quaternion rotationNeeded = targetRotation * Quaternion.Inverse(rb.rotation);
        rb.MovePosition(rb.position + rotationNeeded * -offsetToPoint + offsetToPoint);
        rb.MoveRotation(targetRotation);
    }

    // ¿Por qué no usar Vector3.ProjectOnPlane?
    // Ese método hace lo mismo pero no asume que el vector normal proporcionado es de longitud unitaria;
    // Divide el resultado por la longitud al cuadrado de la normal, que siempre es 1, por lo que no es necesario.
    public static Vector3 ProjectDirectionOnPlane(this Vector3 direction, Vector3 planeNormal)
    {
        return (direction - planeNormal * Vector3.Dot(direction, planeNormal)).normalized;
    }
}
