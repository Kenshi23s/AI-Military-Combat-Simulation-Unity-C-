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

[SelectionBase]
[RequireComponent(typeof(ShootComponent))]
public class Plane : Vehicle
{

    EventFSM<PlaneStates> _planeFSM;
    public PlaneStates actualState => _planeFSM.CurrentKey;
    [field : Header("Plane Variables"),SerializeField]
    public Plane targetPlane { get; private set; }
    [SerializeField] 
    public Plane beingChasedBy { get; private set; }

    //para que el avion que es perseguido se mueva de manera "dinamica" en la dogfight
    //estaria bueno cambiarlo cada x seg con una corrutina cuando se esta en ese estado
    Vector3 _evadingDir;

    [SerializeField] float _cd_OnRandomDir;
    [SerializeField] float RandomAngleToStart = 90;
    [SerializeField, Min(0)] float _unitsBehindPlane = 30f;
    [SerializeField, Min(0)] float _collisionCheckDistance = 30f;

    #region Misile
    [SerializeField]
    Transform[] misilePos;
    [SerializeField] Misile.MisileStats misileStats;
    [SerializeField]float misileCD;

    #endregion

    #region AirStrike
    Vector3 airStrikeCordinates;
    [SerializeField] float minimumDistanceForStrike;
    #endregion

    #region ShootingBullets
    [Header("Shooting Parameters")]
    [SerializeField] float bulletsPerBurst;
    [SerializeField] float burstCD = 3;
    [SerializeField] float BulletCD = 0.3f;
    [SerializeField] Transform shootPos;
    ShootComponent shootComponent;
    #endregion


    [Header("VFX")]
    [SerializeField] ParticleHolder ExplosionParticle;
    int keyExplosionParticle;
    const float explosionRadiusParticle = 20f;

    public override void VehicleAwake()
    {
        _gridEntity.LookGrid();

        shootComponent = GetComponent<ShootComponent>();
        DebugEntity.AddGizmoAction(DrawTowardsTarget); DebugEntity.AddGizmoAction(DrawAirstrikeZone);
   

        Vector3 dir = _gridEntity.SpatialGrid.GetMidleOfGrid() - transform.position;
        transform.forward = new Vector3(0, 0, dir.z);
        _planeFSM = CreateFSM();
        
        misileStats.owner = gameObject;

        Health.OnKilled += () => _planeFSM.SendInput(PlaneStates.ABANDONED);
    }

    #region UnityCalls
    void Start()
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


    IEnumerator ShootBullets()
    {
        while (true)
        {         
            for (int i = 0; i < bulletsPerBurst; i++)
            {
                shootComponent.Shoot(shootPos);
                yield return new WaitForSeconds(BulletCD);
            }
            yield return new WaitForSeconds(burstCD);     
        }       
    }

    IEnumerator ShootMisiles()
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

    Vector3 GroundInFront()
    {
        if (Physics.Raycast(transform.position, transform.forward, _collisionCheckDistance + _movement.CurrentSpeed, PlanesManager.instance.groundMask))
        {
            DebugEntity.Log("Tengo piso adelante, levanto vuelo");
            return Vector3.up;
        }            
        return Vector3.zero;
    }

    IEnumerator RandomEvasionDir()
    {
        while (_planeFSM.CurrentKey == PlaneStates.FLEE_FROM_PURSUITER)
        {
            _evadingDir = Random.insideUnitSphere;
            yield return new WaitForSeconds(Random.Range(0, _cd_OnRandomDir));
        }
    }

    void ShootMisile(Transform target)
    {
        var misile = ProjectilePool.instance.GetMisile();

        misile.transform.position = misilePos.PickRandom().transform.position;

        misileStats.initialVelocity = _movement.Velocity;

        misile.ShootMisile(misileStats, target);
    }

    /// <summary>
    /// obtengo los aviones cercanos que no sean yo y que no esten "abandonados"
    /// </summary>
    /// <returns></returns>
    IEnumerable<Plane> GetNearbyPlanes()
    {
        var z = _gridEntity.GetEntitiesInRange(_fov.ViewRadius)
            .OfType<Plane>()
            .Where(x => x != this)
            .Where(x => x._planeFSM.CurrentKey != PlaneStates.ABANDONED);
        
        return z;
    }

    public void CallAirStrike(Vector3 New_AirStrikePosition)
    {
        airStrikeCordinates = New_AirStrikePosition;
        _planeFSM.SendInput(PlaneStates.AIRSTRIKE);
    }
    #endregion

    #region PlaneStates

    EventFSM<PlaneStates> CreateFSM()
    {
        var flyAround = FlyAround();
        var pursuitTarget = PursuitTarget();
        var fleeDogFight = FleeFromDogFight();
        var abandonPlane = AbandonPlane();
        var airStrike = AirStrike();

        StateConfigurer.Create(flyAround)
            .SetTransition(PlaneStates.PURSUIT_TARGET, pursuitTarget)
            .SetTransition(PlaneStates.FLEE_FROM_PURSUITER, fleeDogFight)
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

    State<PlaneStates> FlyAround()
    {
        State<PlaneStates> state = new State<PlaneStates>("FlyAround");

        state.OnEnter += (x) =>
        {
            DebugEntity.Log($"entre a {state.Name} desde {x}");
        };
        state.OnUpdate += () =>
        {
            //obtengo los aviones cercanos que no sean de mi equipo y esten en mi fov
            var col = GetNearbyPlanes()
            .Where(x => x != this)
            .Where(x => x.Team != Team)
            .Where(x => x.beingChasedBy == null)
            .Where(x => _fov.IN_FOV(x.transform.position, PlanesManager.instance.groundMask));
            if (col.Any())
            {
                DebugEntity.Log($"Aviones enemigos a la vista, elijo el mas cercano de{col.Count()}");
                //si encuentro alguno, obtengo el mas cercano
                targetPlane = col.Minimum((x) => Vector3.Distance(x.transform.position, transform.position));
                //y empiezo la persecucion
                _planeFSM.SendInput(PlaneStates.PURSUIT_TARGET);
            }
        };

        state.OnFixedUpdate += () =>
        {
            Vector3 dir = Vector3.zero;
            //agarro los aviones mas cercanos de mi equipo
            var col = GetNearbyPlanes().Where(x => x.Team == Team).Where(x => x != this);

            //si hay alguno y estoy en zona de combate, hago flocking
            if (col.Any() && _gridEntity.OnGrid)
            {
                var endPromise = col.ToArray();
                dir += endPromise.Flocking(_flockingParameters);
                
                DebugEntity.Log($"hago flocking con {endPromise.Length} aviones aliados");
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

    State<PlaneStates> PursuitTarget()
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


            if (Vector3.Distance(transform.position,targetPlane.transform.position) > _loseSightRadius)
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

    State<PlaneStates> FleeFromDogFight()
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

    State<PlaneStates> AbandonPlane()
    {
        State<PlaneStates> state = new State<PlaneStates>("Die");
        state.OnEnter += (x) =>
        {
           _movement.UseGravity(true);
           _movement.AccelerateTowards(Vector3.down);
        };

        return state;
    }

    State<PlaneStates> AirStrike()
    {
        State<PlaneStates> state = new State<PlaneStates>("AirStrike");


        state.OnUpdate += () =>
        {
            if (Vector3.Distance(transform.position, airStrikeCordinates) < minimumDistanceForStrike)
            {
                GameObject AirStrikePos = new GameObject($"Pivot {nameof(airStrikeCordinates)} [{gameObject.name}]");
                Transform WorldReference = Instantiate(AirStrikePos, airStrikeCordinates, Quaternion.identity).transform;
                Destroy(WorldReference,120f); ShootMisile(WorldReference);

            }
        };

        state.OnFixedUpdate += () => _movement.AccelerateTowardsTarget(airStrikeCordinates);

        return state;
    }

    #endregion

    void DrawTowardsTarget()
    {
        if (targetPlane == null) return;
        
         Gizmos.color = Color.red;
         Vector3 dir = targetPlane.transform.position - transform.position;
         DrawArrow.ForGizmo(transform.position, dir.normalized, Color.red, 2);

         Vector3 awayDir = new Vector3(-targetPlane.transform.forward.x, 0, -targetPlane.transform.forward.z);
         Vector3 pursuitTargetPos = targetPlane.transform.position + awayDir * _unitsBehindPlane;

         Gizmos.DrawWireSphere(pursuitTargetPos, 3f);
        
    }

    void DrawAirstrikeZone()
    {
        if (airStrikeCordinates == Vector3.zero) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(airStrikeCordinates, 3f);
        Gizmos.DrawLine(airStrikeCordinates, airStrikeCordinates + Vector3.up * minimumDistanceForStrike);
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_planeFSM.CurrentKey == PlaneStates.ABANDONED)
        {
            var x = ParticlePool.instance.GetVFX(keyExplosionParticle);
            x.transform.position = transform.position;
            x.transform.localScale = explosionRadiusParticle.ToVector();
            Destroy(gameObject);
        }
    }
}
