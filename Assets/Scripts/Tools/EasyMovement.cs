using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct FlockingParameters
{
    [NonSerialized] 
    public Transform myTransform;
    [NonSerialized]
    public float maxForce;
    [NonSerialized]
    public float viewRadius;

    [SerializeField,Range(0f,3f)]public float AlignmentForce;
    [SerializeField,Range(0f,3f)]public float _separationForce;
    [SerializeField,Range(0f,3f)]public float _cohesionForce;

}
public interface FlockableEntity
{
    public Vector3 GetPosition();
    public Vector3 GetVelocity();
}
public static class EasyMovement 
{

   public static Rigidbody MoveTowards(this Rigidbody rb,Vector3 dir, float force)
   {
      rb.velocity = rb.velocity + dir.normalized * force * Time.deltaTime;
      return rb;
   }

   public static Rigidbody ClampVelocity(this Rigidbody rb, float _maxSpeed)
   {
       rb.velocity= Vector3.ClampMagnitude(rb.velocity, _maxSpeed);
       return rb;
   }

    public static Vector3 Flocking(this IEnumerable<FlockableEntity> targets,FlockingParameters parameters)
    {
        Vector3 actualforce = Vector3.zero;

        actualforce += targets.GroupAlignment(parameters);
        actualforce += targets.Cohesion(parameters);
        actualforce += targets.Separation(parameters);

        return actualforce;
    }

    public static Vector3 CalculateSteering(this Vector3 velocity, Vector3 desired, float steeringForce) => (desired - velocity) * steeringForce;

    #region Flocking
    public static Vector3 GroupAlignment(this IEnumerable<FlockableEntity> targets, FlockingParameters parameters)
    {
        Vector3 desired = Vector3.zero;
        int count = 0;
        if (!targets.Any()) return desired;        
        
        var result = targets.Where(x => Vector3.Distance(x.GetPosition(), parameters.myTransform.position) <= parameters.viewRadius);
        if (!result.Any()) return desired;

       
        //todo lo de flocking se podria resumir mas con Flist y Linq
        //por el momento quedara asi pq hay otras cosas mas importantes que optimizar
        
        
        foreach (AI_Movement item in result)
        {                     
             desired += item.Velocity;
             count++;           
        }

        if (count <= 0)
            return desired;

        desired /= count;

        desired.Normalize();
        desired *= parameters.maxForce;

        return desired;
    }

    public static Vector3 Cohesion(this IEnumerable<FlockableEntity> targets, FlockingParameters parameters)
    {
        Vector3 desired = Vector3.zero;
        int count = 0;
        Vector3 myPos= parameters.myTransform.position;
        foreach (var item in targets)
        {
            Vector3 dist = item.GetPosition() - myPos;

            if (dist.magnitude <= parameters.viewRadius)
            {
                desired += item.GetPosition();
                count++;
            }
        }

        if (count <= 0)
            return desired;

        desired /= count;
        desired -= myPos;

        desired.Normalize();
        desired *= parameters.maxForce;

        return desired*parameters._cohesionForce;
    }

    public static Vector3 Separation(this IEnumerable<FlockableEntity> targets,FlockingParameters parameters)
    {
        Vector3 desired = Vector3.zero;
        foreach (var item in targets)
        {
            Vector3 dist = item.GetPosition() - parameters.myTransform.position;

            if (dist.magnitude <= parameters.viewRadius)
                desired += dist;
        }

        if (desired == Vector3.zero)
            return desired;

        desired = -desired;

        desired.Normalize();
        desired *= parameters.maxForce;

        return desired*parameters._separationForce;
    }
    #endregion



}
