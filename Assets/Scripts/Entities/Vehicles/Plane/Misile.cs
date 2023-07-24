using System.Collections;
using UnityEngine;
using FacundoColomboMethods;
using System.Linq;
using System;

[RequireComponent(typeof(NewPhysicsMovement))]
[RequireComponent(typeof(GridEntity))]
public class Misile : Entity, IMilitary
{  
    NewPhysicsMovement _movement;
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

    public Transform target { get; private set; }

    public MilitaryTeam Team { get; protected set; }

    public bool InCombat => false;

    [SerializeField] ParticleHold explosionParticle;
    [SerializeField] ParticleHold explosionParticleonGround;

    [NonSerialized] public MisileStats myStats;

    protected GridEntity _gridEntity;

    Action<Misile> returnToPool;

    string ownerName;

    public event Action OnDeathInCombat;

    public void PoolObjectInitialize(Action<Misile> HowToReturn) => returnToPool = HowToReturn;

    #region UnityCalls
   
    protected override void EntityAwake()
    {
        _gridEntity = GetComponent<GridEntity>();
        Health.OnKilled += OnDeathInCombat;
        Health.OnKilled += Explosion;
        _movement = GetComponent<NewPhysicsMovement>();

        GetComponent<Collider>().isTrigger = true;
    }


    void Start()
    {
        explosionParticle.key = ParticlePool.instance.CreateVFXPool(explosionParticle.particle);
        explosionParticleonGround.key = ParticlePool.instance.CreateVFXPool(explosionParticleonGround.particle);
    }

    private void LateUpdate()
    {
        _movement.MaxSpeed += Time.deltaTime;
        _movement.Acceleration += Time.deltaTime * 0.5f;
    }

    private void OnDestroy()
    {
        if (_gridEntity.SpatialGrid != null)
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
    #endregion

    public void ShootMisile(MisileStats newStats, Transform newTarget)
    {
        myStats = newStats;
        ownerName = myStats.owner.name;
        SetMovementStats();
        StartCoroutine(CountdownForExplosion());

        Team = newStats.Team;
        target = newTarget;
        transform.parent = null;
        enabled = true;
    }

    void SetMovementStats()
    {
        Debug.Log("Movement Misile"+ _movement);
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
        x.transform.position = transform.position;

        Explosion();
    }

    void Explosion()
    { 
        var _damagables = _gridEntity.GetEntitiesInRange(myStats.explosionRadius)
        .Where(x => x != myStats.owner)
        .Where(x => x != null)
        .OfType<IDamagable>()
        .Where(FilterUnitsByTeam);

        foreach (var entity in _damagables)
        {
            GameManager.instance.DebugDamageFeed(ownerName,entity);
            entity.TakeDamage(myStats.damage);
        }
        returnToPool?.Invoke(this);
    }

    bool FilterUnitsByTeam(IDamagable x)
    {
        var cast = x as IMilitary;
        return cast == null || cast.Team != Team;
    }

    IEnumerator CountdownForExplosion()
    {
        yield return new WaitForSeconds(myStats.timeBeforeExplosion);
        ParticleHolder x = ParticlePool.instance.GetVFX(explosionParticle.key);
        Explosion();
    }
   

  
}
