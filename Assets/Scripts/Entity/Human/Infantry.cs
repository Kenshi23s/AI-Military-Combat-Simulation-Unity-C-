using IA2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[RequireComponent(typeof(NewAIMovement))]
[RequireComponent(typeof(FOVAgent))]
[RequireComponent(typeof(ShootComponent))]
[RequireComponent(typeof(DebugableObject))]
[SelectionBase]
public class Infantry : GridEntity, InitializeUnit
{
    public enum INFANTRY_STATES
    {
        WAITING_ORDERS,
        MOVE_TOWARDS,
        FOLLOW_LEADER,
        DIE,
        FIRE_AT_WILL
    }
    [field: SerializeField] public Transform Center { get; private set; }
    public bool InCombat { get; private set; }


    [SerializeField] Animator _anim;

    public Fireteam MyFireteam { get; private set; }

    [SerializeField] float _timeBeforeSelectingTarget;

    NewAIMovement _infantry_AI;
    FOVAgent _fov;
    #region ShootingLogic
    ShootComponent _gun;
    [SerializeField] Transform _shootPos;
    #endregion
    public EventFSM<INFANTRY_STATES> Infantry_FSM { get; private set; }

    public Entity ActualTarget { get; private set; }

    public Vector3 Destination { get; private set; }

    [field: SerializeField] public float MinDistanceFromDestination { get; private set; }

 
    public void InitializeUnit(Team newTeam)
    {     
        MyTeam = newTeam;
        SetFSM();
        InCombat = false;
    }

    public override void GridEntityStart()
    {
        Health.OnTakeDamage += (x) =>
        {
            Infantry_FSM.SendInput(INFANTRY_STATES.FIRE_AT_WILL);
        };
    }

    public void SetFireteam(Fireteam MyFireteam)
    {
        this.MyFireteam = MyFireteam;
    }

    protected override void EntityAwake()
    {
        _infantry_AI = GetComponent<NewAIMovement>();
        _fov = GetComponent<FOVAgent>();
        _gun = GetComponent<ShootComponent>();
     
    }

    #region States
    void SetFSM()
    {
        var waitOrders = WaitingOrders();
        var moveTowards = MoveTowards();
        var followLeader = FollowLeader();
        var fireAtWill = FireAtWill();
        var die = Die();

        StateConfigurer.Create(waitOrders)
            .SetTransition(INFANTRY_STATES.MOVE_TOWARDS, moveTowards)
            .SetTransition(INFANTRY_STATES.FOLLOW_LEADER, followLeader)
            .SetTransition(INFANTRY_STATES.FIRE_AT_WILL, fireAtWill)
            .SetTransition(INFANTRY_STATES.DIE,die)
            .Done();

        StateConfigurer.Create(moveTowards)
           .SetTransition(INFANTRY_STATES.WAITING_ORDERS, waitOrders)
           .SetTransition(INFANTRY_STATES.FOLLOW_LEADER, followLeader)
           .SetTransition(INFANTRY_STATES.FIRE_AT_WILL, fireAtWill)
           .SetTransition(INFANTRY_STATES.DIE, die)
           .Done();

        StateConfigurer.Create(followLeader)
         .SetTransition(INFANTRY_STATES.WAITING_ORDERS, waitOrders)
         .SetTransition(INFANTRY_STATES.MOVE_TOWARDS, moveTowards)
         .SetTransition(INFANTRY_STATES.FIRE_AT_WILL, fireAtWill)
         .SetTransition(INFANTRY_STATES.DIE, die)
         .Done();

        StateConfigurer.Create(fireAtWill)
         .SetTransition(INFANTRY_STATES.WAITING_ORDERS, waitOrders)
         .SetTransition(INFANTRY_STATES.FOLLOW_LEADER, followLeader)
         .SetTransition(INFANTRY_STATES.MOVE_TOWARDS, moveTowards)
         .SetTransition(INFANTRY_STATES.DIE, die)
         .Done();

        StateConfigurer.Create(die).Done();


        Infantry_FSM = new EventFSM<INFANTRY_STATES>(waitOrders);
    }


    State<INFANTRY_STATES> WaitingOrders()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("WaitingOrders");

        state.OnEnter += (x) =>
        {
         
            StopMoving();
            _anim.SetBool("Running", false);

            DebugEntity.Log("Espero Ordenes");
       

            StartCoroutine(LookForTargets());

            if (MyFireteam.Leader != this) return;
            StartCoroutine(MyFireteam.LookForNearestZone());
            DebugEntity.Log("Busco la zona mas cercana");
        };

        state.OnExit += (x) =>
        {
            StopCoroutine(LookForTargets());           
        };

        return state;
    }

    State<INFANTRY_STATES> MoveTowards()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("MoveTowards");

        state.OnEnter += (x) =>
        {
            if (MinDistanceFromDestination > Vector3.Distance(Destination,transform.position))
            {
                Infantry_FSM.SendInput(INFANTRY_STATES.WAITING_ORDERS);
                return;
            }
            StopMoving();
            DebugEntity.Log("Me muevo hacia posicion x");

            _infantry_AI.SetDestination(Destination);
            _infantry_AI.OnDestinationReached += () =>
            {
                Infantry_FSM.SendInput(INFANTRY_STATES.WAITING_ORDERS);
            };
            _anim.SetBool("Running", true);

            StartCoroutine(LookForTargets());
        };

        state.OnExit += (x) => 
        {
            _anim.SetBool("Running", false);
            StopCoroutine(LookForTargets());
            StopMoving();
        };

        return state;
    }

    State<INFANTRY_STATES> FollowLeader()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("FollowLeader");

        state.OnEnter += (x) =>
        {
            if (MyFireteam.IsNearLeader(this, MinDistanceFromDestination))
            {
                Infantry_FSM.SendInput(INFANTRY_STATES.WAITING_ORDERS);
                return;
            }

             DebugEntity.Log("Sigo al lider");
            
            _anim.SetBool("Running", true);
  
            StartCoroutine(LookForTargets());
            StartCoroutine(FollowLeaderRoutine());
        };


        state.OnExit += (x) =>
        {
            _anim.SetBool("Running", false);
            StopCoroutine(LookForTargets());
            StopCoroutine(FollowLeaderRoutine());
            _infantry_AI.CancelMovement();
        };

        return state;
    }

    IEnumerator FollowLeaderRoutine()
    {
        GoToLeader();
        while (true)
        {
            for (int i = 0; i < 120; i++)
            {
                yield return null;
            }
            if (MyFireteam.IsNearLeader(this, MinDistanceFromDestination)) continue;

            GoToLeader();
        }
       
    }

    void GoToLeader()
    {

        _infantry_AI.SetDestination(MyFireteam.Leader.transform.position);
        _infantry_AI.OnDestinationReached += () =>
        {
            Infantry_FSM.SendInput(INFANTRY_STATES.WAITING_ORDERS);
        };
    }

    State<INFANTRY_STATES> FireAtWill()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("FireAtWill");

        state.OnEnter += (x) =>
        {
            _anim.SetBool("Running", false);
            _anim.SetBool("Shooting",true);
            InCombat=true;
            DebugEntity.Log("Sigo al lider");
            StartCoroutine(SetTarget());
            if (MyFireteam.Leader != this) return;
            
            var enemiesAlive = LookForEnemiesAlive().ToArray();
            if (enemiesAlive.Length > MyFireteam.FireteamMembers.Count)
            {
                Vector3 middlePoint = enemiesAlive.Aggregate(Vector3.zero, (x, y) => x += y.transform.position) / enemiesAlive.Length;
                middlePoint.y = transform.position.y;
                MyFireteam.RequestSupport(middlePoint);
            }
         
        };


        state.OnExit += (x) =>
        {
            InCombat = false;
            _anim.SetBool("Shooting", false);
            StopCoroutine(SetTarget());
        };

        return state;
    }

    State<INFANTRY_STATES> Die()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("Die");

        state.OnEnter += (x) =>
        {
            InCombat = false;
            _anim.SetBool("Die",true);
            DebugEntity.Log("Mori");
            MyFireteam.RemoveMember(this);

        };

        return state;
    }

    #endregion


    public void StopMoving()
    {
        _infantry_AI.CancelMovement();
    }
    #region Metodos Utiles
    IEnumerator LookForTargets()
    {
        while (true)
        {
            var z = GetEntitiesAround()                               
           .Where(x => x.MyTeam != MyTeam)
           .Where(x => _fov.IN_FOV(x.transform.position));

            if (z.Any()) Infantry_FSM.SendInput(INFANTRY_STATES.FIRE_AT_WILL);

            for (int i = 0; i < 30; i++)
                yield return null;
        }
    }


   public IEnumerable<Entity> GetEntitiesAround()
    {
        var col = GetEntitiesInRange(_fov.viewRadius)
         .Where(x => x != this)
         .OfType<Entity>()
         .Where(x => x.GetType() != typeof(Civilian))
         .Where(x => x != null);

        return col;
    }

    IEnumerator SetTarget()
    {
        while (true)
        {      
            ActualTarget = LookForEnemiesAlive().Minimum(GetWeakestAndNearest);

            if (ActualTarget != null)
            {
                transform.forward = ActualTarget.transform.position - transform.position;
                _gun.Shoot(_shootPos);
            }
            else
            {
                //si soy el lider
                if (MyFireteam.Leader == this)
                {
                    //pregunto si alguno de mis miembros tiene un enemigo cerca con vida
                  
                    if (MyFireteam.AlliesWithEnemiesNearby(this,out Entity ally))
                    {
                        Destination = ally.transform.position;
                        Infantry_FSM.SendInput(INFANTRY_STATES.MOVE_TOWARDS);
                    }
                }
                else
                {
                    Infantry_FSM.SendInput(INFANTRY_STATES.FOLLOW_LEADER);
                }
            }



            yield return new WaitForSeconds(_timeBeforeSelectingTarget);
        }
    }

    IEnumerable<Entity> LookForEnemiesAlive()
    {
        return GetEntitiesAround().Where(x => x.MyTeam != MyTeam)
                  .Where(x => x.Health.isAlive);
    }
    #endregion
    
    float GetWeakestAndNearest(Entity entity)
    {
        float result = 0;
        result += Vector3.Distance(transform.position, entity.transform.position);
        result += entity.Health.life;
        return result;
    }

    #region Transitions
    public void MoveTowardsTransition(Vector3 posToGo)
    {
        if (!InCombat)
        {
            Destination = posToGo;
            Infantry_FSM.SendInput(INFANTRY_STATES.MOVE_TOWARDS);
        }
           
    }

    public void FollowLeaderTransition()
    {
        if (!InCombat)
            Infantry_FSM.SendInput(INFANTRY_STATES.FOLLOW_LEADER);
    }

    public void WaitOrdersTransition()
    {
        if (!InCombat)
            Infantry_FSM.SendInput(INFANTRY_STATES.WAITING_ORDERS);
    }




    #endregion
}
