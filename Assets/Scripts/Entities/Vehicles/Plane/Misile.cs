using System.Collections;
using UnityEngine;
using FacundoColomboMethods;
using System.Linq;
using System;

[RequireComponent(typeof(NewPhysicsMovement))]
[RequireComponent(typeof(GridEntity))]
public class Misile : Entity, IMilitary
{  
    private NewPhysicsMovement _movement;
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

    public int TotalDamageDealt { get; private set; }

    [SerializeField] private ParticleHold explosionParticle;
    [SerializeField] private ParticleHold explosionParticleonGround;

    [NonSerialized] public MisileStats myStats;

    protected GridEntity _gridEntity;

    private Action<Misile> returnToPool;

    private string ownerName;

    // true after the explosion of the current launch. Blocks a second explosion and a second return to the pool.
    private bool _exploded;

    // Countdown of the current launch.
    private Coroutine _countdown;

    public event Action OnDeathInCombat;

    public event Action<IDamageable> onHit;

    public void PoolObjectInitialize(Action<Misile> HowToReturn) => returnToPool = HowToReturn;

    #region UnityCalls

    protected override void Awake()
    {
        base.Awake();

        _gridEntity = GetComponent<GridEntity>();

        // A lambda reads OnDeathInCombat when the missile dies. Subscribing the event directly would copy its value at Awake.
        Health.OnKilled += () => OnDeathInCombat?.Invoke();
        Health.OnKilled += Explosion;
        
        _movement = GetComponent<NewPhysicsMovement>();

        GetComponent<Collider>().isTrigger = true;
    }


    private void Start()
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

    /// <summary>
    /// Launches the missile at newTarget with newStats.
    /// The missile explodes on contact or after timeBeforeExplosion seconds.
    /// </summary>
    public void ShootMisile(MisileStats newStats, Transform newTarget)
    {
        myStats = newStats;
        ownerName = myStats.owner.name;
        Team = newStats.Team;
        target = newTarget;
        _exploded = false;

        SetMovementStats();

        if (_countdown != null)
            StopCoroutine(_countdown);
        _countdown = StartCoroutine(CountdownForExplosion());

        transform.parent = null;
        enabled = true;
    }

    private void SetMovementStats()
    {
        _movement.Acceleration = myStats.acceleration;
        _movement.MaxSpeed = myStats.maxSpeed;
        _movement.Velocity = myStats.initialVelocity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_exploded || other.gameObject == myStats.owner) return;

        // groundMask es una mascara de bits, layer es un indice: se compara el bit de la layer.
        bool hitGround = (PlanesManager.instance.groundMask.value & (1 << other.gameObject.layer)) != 0;

        Explode(hitGround ? explosionParticleonGround : explosionParticle);
    }

    // Spawns the explosion particle at the missile position and applies the explosion.
    private void Explode(ParticleHold particle)
    {
        if (_exploded) return;

        ParticleHolder vfx = ParticlePool.instance.GetVFX(particle.key);
        vfx.transform.localScale = myStats.explosionRadius.ToVector();
        vfx.transform.position = transform.position;

        Explosion();
    }

    // Damages the entities within explosionRadius and returns the missile to the pool once per launch.
    private void Explosion()
    {
        if (_exploded) return;
        _exploded = true;

        if (_countdown != null)
        {
            StopCoroutine(_countdown);
            _countdown = null;
        }

        var damagables = _gridEntity.GetEntitiesInRange(myStats.explosionRadius)
            .Where(x => x != null)
            .Where(x => x.gameObject != myStats.owner)
            .OfType<IDamageable>()
            .Where(FilterUnitsByTeam);

        foreach (var entity in damagables)
        {
            GameManager.instance.DebugDamageFeed(ownerName, entity);
            entity.TakeDamage(myStats.damage); onHit?.Invoke(entity);
        }

        returnToPool?.Invoke(this);
    }

    private bool FilterUnitsByTeam(IDamageable x)
    {
        var cast = x as IMilitary;
        return cast == null || cast.Team != Team;
    }

    private IEnumerator CountdownForExplosion()
    {
        yield return new WaitForSeconds(myStats.timeBeforeExplosion);
        _countdown = null;
        Explode(explosionParticle);
    }
}
