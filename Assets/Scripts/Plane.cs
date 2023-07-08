using IA2;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum PlaneStates
{
    FLY_AROUND,
    PURSUIT_TARGET,
    FLEE_FROM_PURSUITER,
    ABANDONED
}

[RequireComponent(typeof(ShootComponent))]
public class Plane : Vehicle
{

    EventFSM<PlaneStates> _planeFSM;
    public Plane targetPlane;
    public Plane beingChasedBy;
   
    ShootComponent shootComponent;

    //para que el avion que es perseguido se mueva de manera "dinamica" en la dogfight
    //estaria bueno cambiarlo cada x seg con una corrutina cuando se esta en ese estado
    Vector3 _evadingDir;

    [SerializeField] float _cd_OnRandomDir;

    [SerializeField, Min(0)] float _unitsBehindPlane = 30f;
    [SerializeField, Min(0)] float _collisionCheckDistance = 30f;


    public override void VehicleAwake()
    {
        shootComponent = GetComponent<ShootComponent>();
        _debug.AddGizmoAction(DrawTowardsTarget);
        health.OnKilled += () => _planeFSM.SendInput(PlaneStates.ABANDONED);
    }

    private void Start()
    {
        _planeFSM = CreateFSM();
    }
    private void Update()
    {
        _planeFSM.Update();
    }
    private void LateUpdate()
    {
        _planeFSM.LateUpdate();
    }
    private void FixedUpdate()
    {
        _planeFSM.FixedUpdate();
        Moved();
    }

    #region ShootingLogic
    [Header("Shooting Parameters")]
    [SerializeField] bool isShooting;
    [SerializeField] float bulletsPerBurst;
    [SerializeField] float burstCD = 3;
    [SerializeField] float BulletCD = 0.3f;
    [SerializeField] Transform shootPos;

    IEnumerator ShootCoroutine()
    {
        isShooting = true;
        for (int i = 0; i < bulletsPerBurst; i++)
        {
            shootComponent.Shoot(shootPos);
            yield return new WaitForSeconds(BulletCD);
        }
        yield return new WaitForSeconds(burstCD);
        isShooting = false;
    }
    #endregion

    #region Useful Methods

    public void SetChaser(Plane newChaser)
    {
        if (newChaser == null)
        {
            beingChasedBy = null;
            _debug.Log("Me dejaron de seguir, vuelvo a volar");
            _planeFSM.SendInput(PlaneStates.FLY_AROUND);
        }
        else
        {
            _debug.Log("Me setearon un perseguidor, me tomo el palo");
            beingChasedBy = newChaser;
            _planeFSM.SendInput(PlaneStates.FLEE_FROM_PURSUITER);
        }

      
    }

    Vector3 GroundInFront()
    {
        if (Physics.Raycast(transform.position, transform.forward, _collisionCheckDistance + _movement.CurrentSpeed, PlanesManager.instance.groundMask))
        {
            _debug.Log("Tengo piso adelante, levanto vuelo");
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

    /// <summary>
    /// obtengo los aviones cercanos que no sean yo y que no esten "abandonados"
    /// </summary>
    /// <returns></returns>
    IEnumerable<Plane> GetNearbyPlanes()
    {
        var z = GetEntitiesInRange(_fov.viewRadius)
            .OfType<Plane>()
            .Where(x => x != this)
            .Where(x => x._planeFSM.CurrentKey != PlaneStates.ABANDONED);
        
        return z;
    }
    #endregion 

    #region PlaneStates

    EventFSM<PlaneStates> CreateFSM()
    {
        var flyAround = FlyAround();
        var pursuitTarget = PursuitTarget();
        var fleeDogFight = FleeFromDogFight();
        var abandonPlane = AbandonPlane();

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

        StateConfigurer.Create(abandonPlane).Done();

        return new EventFSM<PlaneStates>(flyAround);
    }

    State<PlaneStates> FlyAround()
    {
        State<PlaneStates> state = new State<PlaneStates>("FlyAround");

        state.OnEnter += (x) =>
        {
            _debug.Log($"entre a {state.Name} desde {x}");
        };
        state.OnUpdate += () =>
        {
            //obtengo los aviones cercanos que no sean de mi equipo y esten en mi fov
            var col = GetNearbyPlanes()
            .Where(x => x != this)
            .Where(x => 
            {
                Debug.Log(x);
                return _fov.IN_FOV(x.transform.position,PlanesManager.instance.groundMask);

            })
            .Where(x => x.myTeam != myTeam);
            if (col.Any())
            {
                _debug.Log($"Aviones enemigos a la vista, elijo el mas cercano de{col.Count()}");
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
            var col = GetNearbyPlanes().Where(x => x.myTeam == myTeam).Where(x=>x!=this);

            //si hay alguno y estoy en zona de combate, hago flocking
            if (col.Any() && onGrid)
            {
                var EndPromise = col.ToArray();
                dir += EndPromise.Flocking(flockingParameters);
                _debug.Log($"hago flocking con {EndPromise.Length} aviones aliados");
            }
                
            else if (!onGrid)
            {
                _debug.Log("No estoy en la grilla, me pego la vuelta hacia alla");

                //sino estoy en zona de combate me pego la vuelta
                Vector3 dirToCenter = SpatialGrid.GetMidleOfGrid() - transform.position;
                dir += dirToCenter.normalized;
            }

            //si estoy cerca del suelo levanto hacia arriba
            dir += GroundInFront();

            if (dir == Vector3.zero)
            {
                dir = _movement.Velocity;
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
            if (targetPlane == null)
            {
                _debug.Log("No hay blanco de disparo, paso de nuevo a FLY_AROUND");
                _planeFSM.SendInput(PlaneStates.FLY_AROUND);
                return;
            }
            else
            {
                _debug.Log("Hay Blanco, Le digo que lo voy a seguir");
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

            if (!_fov.IN_FOV(targetPlane.transform.position, _loseSightRadius, PlanesManager.instance.groundMask))
            {
                targetPlane.beingChasedBy = null;
                _planeFSM.SendInput(PlaneStates.FLY_AROUND);
                return;
            }
        };


        //fisica avion
        state.OnFixedUpdate += () =>
        {
            Vector3 dir = Vector3.zero;

            Vector3 awayDir = new Vector3(-targetPlane.transform.forward.x, 0, -targetPlane.transform.forward.z);
            Vector3 targetPos = targetPlane.transform.position + awayDir * _unitsBehindPlane;

            dir += (targetPos - transform.position).normalized;

            if (!onGrid)
            {
                Vector3 dirToCenter = SpatialGrid.GetMidleOfGrid() - transform.position;
                Debug.Log("no estoy en la grilla, voy hacia ella");
                dir += dirToCenter.normalized;
            }
               
            //despues tener una variable para la "distancia del suelo"

            dir += GroundInFront();

            if (dir == Vector3.zero)
            {
                dir = _movement.Velocity;
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

            if (!onGrid)
            {
                _debug.Log("No estoy en la grilla, me pego la vuelta hacia alla");

                //sino estoy en zona de combate me pego la vuelta
                Vector3 dirToCenter = SpatialGrid.GetMidleOfGrid() - transform.position;
                dir += dirToCenter.normalized;
            }

            dir += GroundInFront();

            if (dir == Vector3.zero)
            {
                dir = _movement.Velocity;
                dir.y = 0;
            }

            _movement.AccelerateTowards(dir);
        };

        state.OnExit += (x) =>
        {
            StopCoroutine(RandomEvasionDir());
        };

        return state;
    }

    State<PlaneStates> AbandonPlane()
    {
        State<PlaneStates> state = new State<PlaneStates>("FleeFromFight");
        state.OnEnter += (x) =>
        {
           _movement.UseGravity(true);
        };

        return state;
    }

    #endregion

    

    private void OnCollisionEnter(Collision collision)
    {
        //if (_movement._rb.velocity.magnitude > 20f)
        //{
        //    _debug.Log("choque yendo muy rapido, asi que explote C:");
        //    Destroy(gameObject);
        //}
      
    }


    
    void DrawTowardsTarget()
    {
        if (targetPlane!=null)
        {
            Gizmos.color = Color.red;
            Vector3 dir = targetPlane.transform.position - transform.position;
            DrawArrow.ForGizmo(transform.position, dir.normalized, Color.red, 2);

            Vector3 awayDir = new Vector3(-targetPlane.transform.forward.x, 0, -targetPlane.transform.forward.z);
            Vector3 pursuitTargetPos = targetPlane.transform.position + awayDir * _unitsBehindPlane;

            Gizmos.DrawWireSphere(pursuitTargetPos, 3f);
        }
    }

}
