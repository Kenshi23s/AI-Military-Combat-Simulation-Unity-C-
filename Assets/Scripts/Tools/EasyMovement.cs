using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Weights and neighborhood of the flocking behaviours.</summary>
[System.Serializable]
public struct FlockingParameters
{
    [SerializeField, Range(0f, 3f)] public float AlignmentForce;
    [SerializeField, Range(0f, 3f)] public float _separationForce;
    [SerializeField, Range(0f, 3f)] public float _cohesionForce;

    // Set by the owner in Awake.
    [NonSerialized] public Transform myTransform;

    // Radius of the neighborhood. Only the neighbors within viewRadius of myTransform count.
    [NonSerialized] public float viewRadius;
}

/// <summary>Entity that takes part in flocking, pursuit and evade.</summary>
public interface IFlockableEntity
{
    /// <summary>Returns the position of the entity.</summary>
    public Vector3 GetPosition();

    /// <summary>Returns the velocity of the entity.</summary>
    public Vector3 GetVelocity();
}

/// <summary>Steering helpers: flocking, pursuit, evade and Rigidbody shortcuts.</summary>
public static class EasyMovement
{
    /// <summary>Adds dir scaled by force and Time.deltaTime to the velocity of rb.</summary>
    public static Rigidbody MoveTowards(this Rigidbody rb, Vector3 dir, float force)
    {
        rb.velocity = rb.velocity + dir.normalized * force * Time.deltaTime;
        return rb;
    }

    /// <summary>Limits the speed of rb to maxSpeed.</summary>
    public static Rigidbody ClampVelocity(this Rigidbody rb, float maxSpeed)
    {
        rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
        return rb;
    }

    /// <summary>Returns the weighted sum of GroupAlignment, Cohesion and Separation.</summary>
    public static Vector3 Flocking(this IEnumerable<IFlockableEntity> targets, FlockingParameters parameters)
    {
        Vector3 actualforce = Vector3.zero;

        actualforce += targets.GroupAlignment(parameters) * parameters.AlignmentForce;
        actualforce += targets.Cohesion(parameters) * parameters._cohesionForce;
        actualforce += targets.Separation(parameters) * parameters._separationForce;

        return actualforce;
    }

    /// <summary>Returns the steering force from velocity to desired scaled by steeringForce.</summary>
    public static Vector3 CalculateSteering(this Vector3 velocity, Vector3 desired, float steeringForce) => (desired - velocity) * steeringForce;

    #region Flocking
    /// <summary>Returns the normalized average velocity of the targets within viewRadius.</summary>
    public static Vector3 GroupAlignment(this IEnumerable<IFlockableEntity> targets, FlockingParameters parameters)
    {
        Vector3 desired = Vector3.zero;
        int count = 0;
        Vector3 myPos = parameters.myTransform.position;
        float sqrViewRadius = parameters.viewRadius * parameters.viewRadius;

        foreach (var item in targets)
        {
            if ((item.GetPosition() - myPos).sqrMagnitude > sqrViewRadius)
                continue;

            desired += item.GetVelocity();
            count++;
        }

        if (count == 0)
            return Vector3.zero;

        return (desired / count).normalized;
    }

    /// <summary>Returns the normalized direction to the average position of the targets within viewRadius.</summary>
    public static Vector3 Cohesion(this IEnumerable<IFlockableEntity> targets, FlockingParameters parameters)
    {
        Vector3 desired = Vector3.zero;
        int count = 0;
        Vector3 myPos = parameters.myTransform.position;
        float sqrViewRadius = parameters.viewRadius * parameters.viewRadius;

        foreach (var item in targets)
        {
            if ((item.GetPosition() - myPos).sqrMagnitude > sqrViewRadius)
                continue;

            desired += item.GetPosition();
            count++;
        }

        if (count == 0)
            return Vector3.zero;

        return (desired / count - myPos).normalized;
    }

    /// <summary>Returns the normalized direction away from the targets within viewRadius.</summary>
    public static Vector3 Separation(this IEnumerable<IFlockableEntity> targets, FlockingParameters parameters)
    {
        Vector3 desired = Vector3.zero;
        Vector3 myPos = parameters.myTransform.position;
        float sqrViewRadius = parameters.viewRadius * parameters.viewRadius;

        foreach (var item in targets)
        {
            Vector3 dist = item.GetPosition() - myPos;

            if (dist.sqrMagnitude <= sqrViewRadius)
                desired += dist;
        }

        return (-desired).normalized;
    }
    #endregion

    /// <summary>Returns the normalized direction from target to its position in the next frame.</summary>
    public static Vector3 Pursuit(this IFlockableEntity target)
    {
        Vector3 finalPos = target.GetPosition() + target.GetVelocity() * Time.deltaTime;
        Vector3 desired = finalPos - target.GetPosition();
        desired.Normalize();

        return desired;
    }

    /// <summary>Returns the normalized direction from the position of target in the next frame back to target.</summary>
    public static Vector3 Evade(this IFlockableEntity target)
    {
        Vector3 finalPos = target.GetPosition() + target.GetVelocity() * Time.deltaTime;
        Vector3 desired = target.GetPosition() - finalPos;
        desired.Normalize();

        return desired;
    }
}
