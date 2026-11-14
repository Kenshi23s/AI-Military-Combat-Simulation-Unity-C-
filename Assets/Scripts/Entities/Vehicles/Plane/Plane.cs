using IA2;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using FacundoColomboMethods;

public enum PlaneStates
{
    FLY_AROUND,
    PURSUIT_TARGET,
    FLEE_FROM_PURSUITER,
    AIRSTRIKE,
    ABANDONED
}

/// <summary>
/// Vehicle that flies over the grid, fights enemy planes and runs air strikes for its Fireteams.
/// </summary>
[SelectionBase]
public class Plane : Vehicle
{
    /// <summary>The enemy plane this plane pursues.</summary>
    [field : Header("Plane Variables"),SerializeField]
    public Plane targetPlane { get; private set; }

    [SerializeField] private float _cd_OnRandomDir;
    [SerializeField] private float RandomAngleToStart = 90;
    [SerializeField, Min(0)] private float _unitsBehindPlane = 30f;
    [SerializeField, Min(0)] private float _collisionCheckDistance = 30f;
    [SerializeField] private int _ExplosionDamageOnDeath = 50;

        [SerializeField]
    private Transform[] misilePos;
    [SerializeField] private Misile.MisileStats misileStats;
    [SerializeField]private float misileCD;
    [SerializeField] private float minimumDistanceForStrike;

        [Header("Shooting Parameters")]
    [SerializeField] private float bulletsPerBurst;
    [SerializeField] private float burstCD = 3;
    [SerializeField] private float BulletCD = 0.3f;
    [SerializeField] private Transform shootPos;

    [Header("VFX")]
    [SerializeField] private ParticleHolder ExplosionParticle;

    private EventFSM<PlaneStates> _planeFSM;

    //para que el avion que es perseguido se mueva de manera "dinamica" en la dogfight
    //estaria bueno cambiarlo cada x seg con una corrutina cuando se esta en ese estado
    private Vector3 _evadingDir;

    // Reused by FlyAround to collect the allied planes.
    private readonly List<Plane> _alliesBuffer = new List<Plane>();

    private Vector3 airStrikeCordinates;
    private int keyExplosionParticle;

    // Seconds before the air strike pivot is destroyed.
    private const float AirStrikePivotLifetime = 120f;
    private const float explosionRadius = 20f;

    /// <summary>Current state of the FSM.</summary>
    public PlaneStates actualState => _planeFSM.CurrentKey;

    // true in PURSUIT_TARGET and FLEE_FROM_PURSUITER.
    public override bool InCombat => actualState == PlaneStates.PURSUIT_TARGET || actualState == PlaneStates.FLEE_FROM_PURSUITER;

    /// <summary>The enemy plane that pursues this plane.</summary>
    [SerializeField]
    public Plane beingChasedBy { get; private set; }

    public override void VehicleAwake()
    {
        _gridEntity.LookGrid();

        DebugEntity.AddGizmoAction(DrawTowardsTarget); DebugEntity.AddGizmoAction(DrawAirstrikeZone);
   

        Vector3 dir = _gridEntity.SpatialGrid.GetMidleOfGrid() - transform.position;
        transform.forward = new Vector3(0, 0, dir.z);
        _planeFSM = CreateFSM();
        
        misileStats.owner = gameObject;
        Health.OnKilled += () => _planeFSM.SendInput(PlaneStates.ABANDONED);
    }

    #region UnityCalls
    private void Start()
    {
        //Vector3 dir = _gridEntity.SpatialGrid.GetMidleOfGrid() - transform.position;
        ////dir = dir.RandomDirFrom(RandomAngleToStart);
        //transform.forward = new Vector3(0, 0, dir.z);

        keyExplosionParticle = ParticlePool.instance.CreateVFXPool(ExplosionParticle);

    }

    private void Update() => _planeFSM.Update();

    private void FixedUpdate() => _planeFSM.FixedUpdate();

    private void OnDestroy()
    {
        if (_gridEntity.SpatialGrid != null)
            _gridEntity.SpatialGrid.RemoveEntity(_gridEntity);
    }

    #endregion

    #region ShootingLogic


    private IEnumerator ShootBullets()
    {
        while (true)
        {         
            for (int i = 0; i < bulletsPerBurst; i++)
            {
                _shootComponent.Shoot(shootPos);
                yield return new WaitForSeconds(BulletCD);
            }
            yield return new WaitForSeconds(burstCD);     
        }       
    }

    private IEnumerator ShootMisiles()
    {
        while (true)
        {

            yield return new WaitForSeconds(misileCD); if (targetPlane == null) break;
            DebugEntity.Log($"Tiro Misil a {targetPlane.name}");
            ShootMisile(targetPlane.transform);          
        }
    }

    #endregion

    #region Useful Methods

    /// <summary>
    /// Sets beingChasedBy to newChaser.
    /// A plane sends the FSM to FLEE_FROM_PURSUITER. null sends it to FLY_AROUND.
    /// </summary>
    public void SetChaser(Plane newChaser)
    {
        if (newChaser == null)
        {
            beingChasedBy = null;
            DebugEntity.Log("Me dejaron de seguir, vuelvo a volar");
          
            _planeFSM.SendInput(PlaneStates.FLY_AROUND);
        }
        else
        {
            DebugEntity.Log("Me setearon un perseguidor, me tomo el palo");
            beingChasedBy = newChaser;
            _planeFSM.SendInput(PlaneStates.FLEE_FROM_PURSUITER);
        }

      
    }

    private Vector3 GroundInFront()
    {
        if (Physics.Raycast(transform.position, transform.forward, _collisionCheckDistance + _movement.CurrentSpeed, PlanesManager.instance.groundMask))
        {
            DebugEntity.Log("Tengo piso adelante, levanto vuelo");
            return Vector3.up;
        }            
        return Vector3.zero;
    }

    private IEnumerator RandomEvasionDir()
    {
        while (_planeFSM.CurrentKey == PlaneStates.FLEE_FROM_PURSUITER)
        {
            _evadingDir = Random.insideUnitSphere;
            yield return new WaitForSeconds(Random.Range(0, _cd_OnRandomDir));
        }
    }

    private void ShootMisile(Transform target)
    {
        var misile = ProjectilePool.instance.GetMisile();

        misile.transform.position = misilePos.PickRandom().transform.position;

        misileStats.initialVelocity = _movement.Velocity;
        misileStats.Team = Team;

        misile.ShootMisile(misileStats, target);
    }

    /// <summary>
    /// obtengo los aviones cercanos que no sean yo y que no esten "abandonados"
    /// </summary>
    /// <returns></returns>
    private IEnumerable<Plane> GetNearbyPlanes()
    {
        var z = _gridEntity.GetEntitiesInRange(_fov.ViewRadius)
            .OfType<Plane>()
            .Where(x => x != this)
            .Where(x => x._planeFSM.CurrentKey != PlaneStates.ABANDONED);
        
        return z;
    }

    /// <summary>
    /// Sends the plane to New_AirStrikePosition.
    /// The plane shoots one missile at the point and goes back to FLY_AROUND.
    /// The FSM accepts the call only in FLY_AROUND.
    /// </summary>
    public void CallAirStrike(Vector3 New_AirStrikePosition)
    {
        airStrikeCordinates = New_AirStrikePosition;
        _planeFSM.SendInput(PlaneStates.AIRSTRIKE);
    }
    #endregion

    #region PlaneStates

    private EventFSM<PlaneStates> CreateFSM()
    {
        var flyAround = FlyAround();
        var pursuitTarget = PursuitTarget();
        var fleeDogFight = FleeFromDogFight();
        var abandonPlane = AbandonPlane();
        var airStrike = AirStrike();

        StateConfigurer.Create(flyAround)
            .SetTransition(PlaneStates.PURSUIT_TARGET, pursuitTarget)
            .SetTransition(PlaneStates.FLEE_FROM_PURSUITER, fleeDogFight)
            .SetTransition(PlaneStates.AIRSTRIKE, airStrike)
            .SetTransition(PlaneStates.ABANDONED, abandonPlane)
            .Done();

        StateConfigurer.Create(pursuitTarget)
            .SetTransition(PlaneStates.FLEE_FROM_PURSUITER, fleeDogFight)
            .SetTransition(PlaneStates.ABANDONED, abandonPlane)
            .SetTransition(PlaneStates.FLY_AROUND, flyAround)
            .Done();

        StateConfigurer.Create(fleeDogFight)
           .SetTransition(PlaneStates.ABANDONED, abandonPlane)
           .SetTransition(PlaneStates.FLY_AROUND, flyAround)
           .Done();

        StateConfigurer.Create(airStrike)
           .SetTransition(PlaneStates.ABANDONED, abandonPlane)
           .SetTransition(PlaneStates.FLY_AROUND, flyAround)
           .SetTransition(PlaneStates.FLEE_FROM_PURSUITER, fleeDogFight)
           .Done();


        StateConfigurer.Create(abandonPlane).Done();

        return new EventFSM<PlaneStates>(flyAround);
    }

    private State<PlaneStates> FlyAround()
    {
        State<PlaneStates> state = new State<PlaneStates>("FlyAround");

        state.OnEnter += (x) =>
        {
            DebugEntity.Log($"entre a {state.Name} desde {x}");
        };
        state.OnUpdate += () =>
        {
            // Un solo recorrido: el enemigo mas cercano que no este siendo perseguido y este en mi fov.
            // Cada IN_FOV hace un raycast, asi que se evalua una vez por avion.
            Plane nearestEnemy = null;
            float nearestSqrDistance = float.MaxValue;
            int enemiesInSight = 0;

            foreach (Plane plane in GetNearbyPlanes())
            {
                if (plane.Team == Team || plane.beingChasedBy != null)
                    continue;

                if (!_fov.IN_FOV(plane.transform.position, PlanesManager.instance.groundMask))
                    continue;

                enemiesInSight++;
                float sqrDistance = Vector3.SqrMagnitude(plane.transform.position - transform.position);
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearestEnemy = plane;
                }
            }

            if (nearestEnemy == null)
                return;

            if (DebugEntity.canDebug)
                DebugEntity.Log($"Aviones enemigos a la vista, elijo el mas cercano de {enemiesInSight}");

            targetPlane = nearestEnemy;
            _planeFSM.SendInput(PlaneStates.PURSUIT_TARGET);
        };

        state.OnFixedUpdate += () =>
        {
            Vector3 dir = Vector3.zero;

            //agarro los aviones mas cercanos de mi equipo
            _alliesBuffer.Clear();
            foreach (Plane plane in GetNearbyPlanes())
            {
                if (plane.Team == Team)
                    _alliesBuffer.Add(plane);
            }

            //si hay alguno y estoy en zona de combate, hago flocking
            if (_alliesBuffer.Count > 0 && _gridEntity.OnGrid)
            {
                dir += _alliesBuffer.Flocking(_flockingParameters);

                if (DebugEntity.canDebug)
                    DebugEntity.Log($"hago flocking con {_alliesBuffer.Count} aviones aliados");
            }
                
            else if (!_gridEntity.OnGrid)
            {
                DebugEntity.Log("No estoy en la grilla, me pego la vuelta hacia alla");

                //sino estoy en zona de combate me pego la vuelta
                Vector3 dirToCenter = _gridEntity.SpatialGrid.GetMidleOfGrid() - transform.position;
                dir += dirToCenter.normalized;
            }

            //si estoy cerca del suelo levanto hacia arriba
            dir += GroundInFront();

            if (dir == Vector3.zero)
            {
                dir = _movement.Velocity != Vector3.zero ? _movement.Forward : transform.forward;
                dir.y = 0;
            }

            _movement.AccelerateTowards(dir);
        };

        return state;
    }

    private State<PlaneStates> PursuitTarget()
    {
        State<PlaneStates> state = new State<PlaneStates>("DogFight");

        state.OnEnter += (x) =>
        {
            StopAllCoroutines();
            if (targetPlane == null)
            {
                DebugEntity.Log("No hay blanco de disparo, paso de nuevo a FLY_AROUND");
                _planeFSM.SendInput(PlaneStates.FLY_AROUND);
                return;
            }
            else
            {
                DebugEntity.Log("Hay Blanco, Le digo que lo voy a seguir");
                StartCoroutine(ShootBullets());
                StartCoroutine(ShootMisiles());

                targetPlane.SetChaser(this);
            }
        };

        //decision
        state.OnUpdate += () =>
        {
            if (targetPlane == null)
            {
                _planeFSM.SendInput(PlaneStates.FLY_AROUND); 
                return;
            }


            if (Vector3.SqrMagnitude(transform.position - targetPlane.transform.position) > _loseSightRadius * _loseSightRadius)
            {
                targetPlane.beingChasedBy = null;
                _planeFSM.SendInput(PlaneStates.FLY_AROUND);
                return;
            }
        };


        //fisica avion
        state.OnFixedUpdate += () =>
        {
            if (targetPlane == null) return;
            Vector3 dir = Vector3.zero;

            Vector3 awayDir = new Vector3(-targetPlane.transform.forward.x, 0, -targetPlane.transform.forward.z);
            Vector3 targetPos = targetPlane.transform.position + awayDir * _unitsBehindPlane;

            dir += (targetPos - transform.position).normalized;

            if (!_gridEntity.OnGrid)
            {
                Vector3 dirToCenter = _gridEntity.SpatialGrid.GetMidleOfGrid() - transform.position;
                Debug.Log("no estoy en la grilla, voy hacia ella");
                dir += dirToCenter.normalized;
            }
               
            //despues tener una variable para la "distancia del suelo"

            dir += GroundInFront();

            if (dir == Vector3.zero)
            {
                dir = _movement.Velocity != Vector3.zero ? _movement.Forward : transform.forward;
                dir.y = 0;
            }

            _movement.AccelerateTowards(dir);
        };

        state.OnExit += (x) =>
        {
            if (targetPlane != null)
            {
                targetPlane.SetChaser(null);
                targetPlane = null;
            }
            StopAllCoroutines();
            //StopCoroutine(ShootBullets());
            //StopCoroutine(ShootMisiles());

        };

        return state;
    }

    private State<PlaneStates> FleeFromDogFight()
    {
        State<PlaneStates> state = new State<PlaneStates>("FleeFromFight");
        state.OnEnter += (x) =>
        {
            if (beingChasedBy == null)            
                _planeFSM.SendInput(PlaneStates.FLY_AROUND);

            StartCoroutine(RandomEvasionDir());        
        };

        state.OnFixedUpdate += () =>
        {
            Vector3 dir = Vector3.zero;

            dir += beingChasedBy.Evade();
            dir += _evadingDir;

            if (!_gridEntity.OnGrid)
            {
                DebugEntity.Log("No estoy en la grilla, me pego la vuelta hacia alla");

                //sino estoy en zona de combate me pego la vuelta
                Vector3 dirToCenter = _gridEntity.SpatialGrid.GetMidleOfGrid() - transform.position;
                dir += dirToCenter.normalized;
            }

            dir += GroundInFront();

            if (dir == Vector3.zero)
            {
                dir = _movement.Velocity != Vector3.zero ? _movement.Forward : transform.forward;
                dir.y = 0;
            }

            _movement.AccelerateTowards(dir);
        };

        state.OnExit += (x) => StopCoroutine(RandomEvasionDir());

        return state;
    }

    private State<PlaneStates> AbandonPlane()
    {
        State<PlaneStates> state = new State<PlaneStates>("Die");
        state.OnEnter += (x) =>
        {
           _movement.UseGravity(true);
           _movement.AccelerateTowards(Vector3.down);
        };

        return state;
    }

    private State<PlaneStates> AirStrike()
    {
        State<PlaneStates> state = new State<PlaneStates>("AirStrike");

        state.OnEnter += (x) => DebugEntity.Log($"Voy a bombardear {airStrikeCordinates}");

        state.OnUpdate += () =>
        {
            // La distancia se mide en el plano horizontal: el avion mantiene su altura.
            Vector3 toStrike = airStrikeCordinates - transform.position;
            toStrike.y = 0;

            if (toStrike.sqrMagnitude >= minimumDistanceForStrike * minimumDistanceForStrike)
                return;

            // Un solo misil por pedido de apoyo, guiado a un pivot en el punto del bombardeo.
            Transform strikePivot = new GameObject($"Pivot {nameof(airStrikeCordinates)} [{gameObject.name}]").transform;
            strikePivot.position = airStrikeCordinates;
            Destroy(strikePivot.gameObject, AirStrikePivotLifetime);

            ShootMisile(strikePivot);
            _planeFSM.SendInput(PlaneStates.FLY_AROUND);
        };

        state.OnFixedUpdate += () =>
        {
            // Vuela hacia el punto a su altura actual y esquiva el piso.
            Vector3 target = airStrikeCordinates;
            target.y = transform.position.y;

            Vector3 dir = (target - transform.position).normalized + GroundInFront();
            _movement.AccelerateTowards(dir);
        };

        return state;
    }

    #endregion

    private void DrawTowardsTarget()
    {
        if (targetPlane == null) return;
        
         Gizmos.color = Color.red;
         Vector3 dir = targetPlane.transform.position - transform.position;
         DrawArrow.ForGizmo(transform.position, dir.normalized, Color.red, 2);

         Vector3 awayDir = new Vector3(-targetPlane.transform.forward.x, 0, -targetPlane.transform.forward.z);
         Vector3 pursuitTargetPos = targetPlane.transform.position + awayDir * _unitsBehindPlane;

         Gizmos.DrawWireSphere(pursuitTargetPos, 3f);
        
    }

    private void DrawAirstrikeZone()
    {
        if (airStrikeCordinates == Vector3.zero) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(airStrikeCordinates, 3f);
        Gizmos.DrawLine(airStrikeCordinates, airStrikeCordinates + Vector3.up * minimumDistanceForStrike);
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_planeFSM.CurrentKey != PlaneStates.ABANDONED) return;

        _gridEntity.GetEntitiesInRange(explosionRadius)
        .Distinct()
        .ToList()
        .ForEach(x => x.TakeDamage(_ExplosionDamageOnDeath));

         var x = ParticlePool.instance.GetVFX(keyExplosionParticle);
         x.transform.position = transform.position;
         x.transform.localScale = explosionRadius.ToVector();
         Destroy(gameObject);
        
    }
}
