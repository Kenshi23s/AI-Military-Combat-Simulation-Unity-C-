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

    [SerializeField] ParticleHold explosionParticle;
    [SerializeField] ParticleHold explosionParticleonGround;

    [NonSerialized] public MisileStats myStats;
    [System.Serializable]
    public struct MisileStats
    {
        [NonSerialized]
        public GameObject owner;
        public Team myTeam;
        public float acceleration;
        public float maxSpeed;
        public float rotationSpeed;
        public float timeBeforeExplosion;
        public int damage;
        public float explosionRadius;
        public Vector3 initialVelocity;
    }

    private void Awake()
    {
        _movement = GetComponent<NewPhysicsMovement>();

        GetComponent<Collider>().isTrigger = true;
        enabled = false;
       
    }

    public override void GridEntityStart()
    {
        explosionParticle.key = ParticlePool.instance.CreateVFXPool(explosionParticle.particle);
        explosionParticleonGround.key = ParticlePool.instance.CreateVFXPool(explosionParticleonGround.particle);
    }

    private void LateUpdate()
    {
        _movement.MaxSpeed += Time.deltaTime;
        _movement.Acceleration += Time.deltaTime / 2;
    }

    public void ShootMisile(MisileStats newStats, Transform newTarget)
    {
        myStats = newStats;
        SetMovementStats();
        StartCoroutine(CountdownForExplosion());
        MyTeam = newStats.myTeam;
        target = newTarget;
        transform.parent = null;
        enabled = true;
    }

    void SetMovementStats()
    {
        _movement.Acceleration = myStats.acceleration;
        _movement.MaxSpeed = myStats.maxSpeed;
        //_movement.RotationSpeed = myStats.rotationSpeed;
        _movement.Velocity = myStats.initialVelocity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == myStats.owner) return;

        ParticleHold particleToSpawn = explosionParticle;
        if (other.gameObject.layer == PlanesManager.instance.groundMask)       
            particleToSpawn = explosionParticleonGround;

       ParticleHolder x = ParticlePool.instance.GetVFX(particleToSpawn.key);

        x.transform.localScale = myStats.explosionRadius.ToVector();
        x.transform.position=transform.position;
        Explosion();
    }

    void Explosion()
    {
        var damagables = GetEntitiesInRange(myStats.explosionRadius)
            .OfType<Entity>()
            .Where(x => x.MyTeam != myStats.myTeam)
            .Select(x => x.GetComponent<IDamagable>())
            .Where(x  => x != null);


        foreach (var entity in damagables) 
            entity.TakeDamage(myStats.damage);

        Destroy(gameObject);
    }


    IEnumerator CountdownForExplosion()
    {
        yield return new WaitForSeconds(myStats.timeBeforeExplosion);
        ParticleHolder x = ParticlePool.instance.GetVFX(explosionParticle.key);
        Explosion();
    }
    private void OnDestroy()
    {
        if (SpatialGrid!=null)
        {
            SpatialGrid.RemoveEntity(this);
        }
    
    }

    private void FixedUpdate()
    {
        if (target==null)
        {
            _movement.AccelerateTowards(_movement.Forward);
        }
        else
        {
            _movement.AccelerateTowardsTarget(target.transform.position);
        }     
    }

    private void OnDrawGizmos()
    {
        if (target == null) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(target.position, myStats.explosionRadius);
        Gizmos.DrawLine(transform.position, target.position);
    }

  
}
