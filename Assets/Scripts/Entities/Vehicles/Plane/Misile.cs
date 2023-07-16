using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FacundoColomboMethods;
using System.Linq;
using System;

[RequireComponent(typeof(NewPhysicsMovement))]
[RequireComponent(typeof(GridEntity))]
public class Misile : Entity, IMilitary
{  
    NewPhysicsMovement _movement;

    public Transform target { get; private set; }

    public MilitaryTeam Team { get; protected set; }

    public bool InCombat => false;

    [SerializeField] ParticleHold explosionParticle;
    [SerializeField] ParticleHold explosionParticleonGround;

    [NonSerialized] public MisileStats myStats;

    protected GridEntity _gridEntity;
    [Serializable]
    public struct MisileStats
    {
        [NonSerialized]
        public GameObject owner;
        public MilitaryTeam Team;
        public float acceleration;
        public float maxSpeed;
        public float timeBeforeExplosion;
        public int damage;
        public float explosionRadius;
        public Vector3 initialVelocity;
    }

    private void Awake()
    {
        _gridEntity = GetComponent<GridEntity>();
        _movement = GetComponent<NewPhysicsMovement>();

        GetComponent<Collider>().isTrigger = true;
        enabled = false;
       
    }

    void Start()
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

        Team = newStats.Team;
        target = newTarget;
        transform.parent = null;
        enabled = true;
    }

    void SetMovementStats()
    {
        _movement.Acceleration = myStats.acceleration;
        _movement.MaxSpeed = myStats.maxSpeed;
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
        //esto tendria que golpear a todos, pq si por ejemplo hay un civil en esa area
        //chau, cago y muere
        var damagables = _gridEntity.GetEntitiesInRange(myStats.explosionRadius)
            .OfType<IMilitary>()
            .Where(x => x.Team != Team)
            .OfType<IDamagable>();

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
        if (_gridEntity.SpatialGrid!=null)     
            _gridEntity.SpatialGrid.RemoveEntity(_gridEntity);
         
    }

    private void FixedUpdate()
    {
        if (target == null)       
            _movement.AccelerateTowards(_movement.Forward);    
        else
            _movement.AccelerateTowardsTarget(target.transform.position);
          
    }

    private void OnDrawGizmos()
    {
        if (target == null) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(target.position, myStats.explosionRadius);
        Gizmos.DrawLine(transform.position, target.position);
    }

  
}
