using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FacundoColomboMethods;
using System.Linq;
using System;

[RequireComponent(typeof(NewPhysicsMovement))]
public class Misile : GridEntity
{
    NewPhysicsMovement _movement;

    public Transform target { get; private set; }

 
    
    public MisileStats myStats;
    [System.Serializable]
    public struct MisileStats
    {
        [NonSerialized]
        public GameObject owner;
        public float acceleration;
        public float maxSpeed;
        public float rotationSpeed;
        public float timeBeforeExplosion;
        public int damage;
        public float explosionRadius;
    }

    private void Awake()
    {
        _movement = GetComponent<NewPhysicsMovement>();
        GetComponent<Collider>().isTrigger = true;
        enabled = false;
       
    }

    public void ShootMisile(MisileStats newStats, Transform target)
    {
        myStats = newStats;
        SetMovementStats();
        transform.parent = null;
        enabled = true;
    }

    void SetMovementStats()
    {
        _movement.Acceleration = myStats.acceleration;
        _movement.MaxSpeed = myStats.maxSpeed;
        _movement.RotationSpeed = myStats.rotationSpeed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == myStats.owner) return;
        Explosion();
    }

    void Explosion()
    {
        var damagables = GetEntitiesInRange(myStats.explosionRadius)
            .OfType<Entity>()
            .Select(x => x.GetComponent<IDamagable>())
            .Where(x  => x != null);

        foreach (var entity in damagables) 
            entity.TakeDamage(myStats.damage);

        Destroy(gameObject);
    }


    IEnumerator CountdownForExplosion()
    {
        yield return new WaitForSeconds(myStats.timeBeforeExplosion);
        Explosion();
    }
    private void OnDestroy()
    {     
        SpatialGrid.RemoveEntity(this);
    }

    private void FixedUpdate()
    {
        Vector3 dirToGo = target != null 
            ? target.transform.position - transform.position 
            : _movement.Forward;
        _movement.AccelerateTowards(dirToGo);
    }

    private void OnDrawGizmos()
    {
        if (target == null) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(target.position, myStats.explosionRadius);
        Gizmos.DrawLine(transform.position, target.position);
    }

   
}
