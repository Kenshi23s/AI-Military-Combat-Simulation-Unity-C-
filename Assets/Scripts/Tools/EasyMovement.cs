using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct FlockingParameters
{
    //parametros para el flocking, rellenar en el awake
    [NonSerialized] 
    public Transform myTransform;
   
    [NonSerialized]
    public float viewRadius;
    //estas se pueden rellenar desde editor
    [SerializeField,Range(0f,3f)]public float AlignmentForce;
    [SerializeField,Range(0f,3f)]public float _separationForce;
    [SerializeField,Range(0f,3f)]public float _cohesionForce;

}
//si se quiere hacer flocking con algo, debe tener esta interfaz
public interface IFlockableEntity
{
    public Vector3 GetPosition();
    public Vector3 GetVelocity();
}
public static class EasyMovement 
{

   public static Rigidbody MoveTowards(this Rigidbody rb, Vector3 dir, float force)
   {
      rb.velocity = rb.velocity + dir.normalized * force * Time.deltaTime;
      return rb;
   }

   public static Rigidbody ClampVelocity(this Rigidbody rb, float maxSpeed)
   {
       rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
       return rb;
   }

    public static Vector3 Flocking(this IEnumerable<IFlockableEntity> targets, FlockingParameters parameters)
    {
        Vector3 actualforce = Vector3.zero;

        actualforce += targets.GroupAlignment(parameters)*parameters.AlignmentForce;
        actualforce += targets.Cohesion(parameters)*parameters._cohesionForce;
        actualforce += targets.Separation(parameters)*parameters._separationForce;

        return actualforce;
    }

    public static Vector3 CalculateSteering(this Vector3 velocity, Vector3 desired, float steeringForce) => (desired - velocity) * steeringForce;

    #region Flocking
    public static Vector3 GroupAlignment(this IEnumerable<IFlockableEntity> targets, FlockingParameters parameters)
    {
        Vector3 desired = Vector3.zero;
        //int count = 0;
        if (!targets.Any()) return desired;

        var result = targets.Where(x => Vector3.Distance(x.GetPosition(), parameters.myTransform.position) <= parameters.viewRadius);
        if (!result.Any()) return desired;


        //todo lo de flocking se podria resumir mas con Flist y Linq
        //por el momento quedara asi pq hay otras cosas mas importantes que optimizar

        desired = result.Aggregate(Vector3.zero, (x, y) => x + y.GetVelocity())/result.Count();
       
        //foreach (var item in result)
        //{                     
        //     desired += item.GetVelocity();
        //     count++;           
        //}

        //if (count <= 0)
        //    return desired;

        //desired /= count;

        desired.Normalize();
   

        return desired;
    }

    public static Vector3 Cohesion(this IEnumerable<IFlockableEntity> targets, FlockingParameters parameters)
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
       

        return desired;
    }

    public static Vector3 Separation(this IEnumerable<IFlockableEntity> targets,FlockingParameters parameters)
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
     

        return desired;
    }
    #endregion

    public static Vector3 Pursuit(this IFlockableEntity target)
    {
        Vector3 finalPos = target.GetPosition() + target.GetVelocity() * Time.deltaTime;
        Vector3 desired = finalPos - target.GetPosition();
        desired.Normalize();

        return desired;
    }
    public static Vector3 Evade(this IFlockableEntity target)
    {
        Vector3 finalPos = target.GetPosition() + target.GetVelocity() * Time.deltaTime;
        Vector3 desired = target.GetPosition() - finalPos;
        desired.Normalize();

        return desired;

    }


}
