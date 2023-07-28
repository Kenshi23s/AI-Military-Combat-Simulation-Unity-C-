using IA2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using static NewPhysicsMovement;

[RequireComponent(typeof(NewAIMovement))]
public class Infantry : Soldier, ICapturePointEntity
{
    public enum INFANTRY_STATES
    {
        WAITING_ORDERS,
        MOVE_TOWARDS,
        FOLLOW_LEADER,
        DIE,
        FIRE_AT_WILL
    }

    #region Zone Variables
    public bool CanCapture => Health.IsAlive;

    public CapturePoint Zone { get; protected set; }

    public event Action OnZoneEnter = delegate { };
    public event Action OnZoneStay = delegate { };
    public event Action OnZoneExit = delegate { };
    #endregion

    [SerializeField] Animator _anim;

    public Fireteam MyFireteam { get; private set; }

    [SerializeField] float _timeBeforeSelectingTarget;

    NewAIMovement _infantry_AI;

    #region ShootingLogic
    [SerializeField] Transform _shootPos;
    #endregion

    public Entity ActualTarget { get; private set; }

    public Vector3 Destination { get; private set; }

    [field: SerializeField] public float MinDistanceFromDestination { get; private set; }

    #region States

    public EventFSM<INFANTRY_STATES> Infantry_FSM { get; private set; }

    protected State<INFANTRY_STATES> waitOrders, moveTowards, followLeader, fireAtWill,die;

    const int waitingFramesTilSearch = 30;
    #endregion

    public void InitializeUnit(MilitaryTeam newTeam)
    {     
        Team = newTeam;

        SetFSM();

        InCombat = false;
    }

    #region CaptureZone

    public void ZoneEnter(CapturePoint zone)
    {
        Zone = zone;
        DebugEntity.Log("ZoneEnter");
        OnZoneEnter();
    }

    public void ZoneStay()
    {
        DebugEntity.Log("ZoneStay");
        OnZoneStay();
    }

    public void ZoneExit(CapturePoint zone)
    {
        Zone = null;
        DebugEntity.Log("ZoneExit");
        OnZoneExit();
    }

    #endregion

    public void SetFireteam(Fireteam MyFireteam) => this.MyFireteam = MyFireteam;

    void Start()
    {
        Health.OnTakeDamage += (x) =>
        {
            if (LookForEnemiesAlive().Any())
            {
                Infantry_FSM.SendInput(INFANTRY_STATES.FIRE_AT_WILL);
            }

        };

        Health.OnKilled += () => Infantry_FSM.SendInput(INFANTRY_STATES.DIE);

      
    }


    protected override void SoldierAwake()
    {
        _infantry_AI = GetComponent<NewAIMovement>();     
    }

    #region States
    void SetFSM()
    {
         waitOrders = WaitingOrders();
         moveTowards = MoveTowards();
         followLeader = FollowLeader();
         fireAtWill = FireAtWill();
         die = Die();

        ConfigureStates();
        
        Infantry_FSM = new EventFSM<INFANTRY_STATES>(waitOrders);
    }

    protected virtual void ConfigureStates()
     {
        StateConfigurer.Create(waitOrders)
            .SetTransition(INFANTRY_STATES.MOVE_TOWARDS, moveTowards)
            .SetTransition(INFANTRY_STATES.FOLLOW_LEADER, followLeader)
            .SetTransition(INFANTRY_STATES.FIRE_AT_WILL, fireAtWill)
            .SetTransition(INFANTRY_STATES.DIE, die)
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

        StateConfigurer.Create(die)
            .Done();
    }

    protected virtual State<INFANTRY_STATES> WaitingOrders()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("WaitingOrders");

        state.OnEnter += (x) =>
        {
            _infantry_AI.ManualMovement.Alignment = AlignmentType.Velocity;
            StopMoving(); _anim.SetBool("Running", false);

            DebugEntity.Log("Espero Ordenes");
       
            StartCoroutine(LookForTargets());

            if (MyFireteam.Leader != this && !IsCapturing) return;

            StartCoroutine(MyFireteam.FindNearestUntakenPoint());

            DebugEntity.Log("Busco la zona mas cercana");
        };

        state.OnExit += (x) => StopCoroutine(LookForTargets());
        state.OnExit += (x) => StopCoroutine(MyFireteam.FindNearestUntakenPoint());

        return state;
    }

    protected virtual State<INFANTRY_STATES> MoveTowards()
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
            _infantry_AI.SetDestination(Destination, () =>
            {
                Infantry_FSM.SendInput(INFANTRY_STATES.WAITING_ORDERS);
            });
           
            _anim.SetBool("Running", true);
            StartCoroutine(LookForTargets());
        };

        state.OnExit += (x) => 
        {
            _anim.SetBool("Running", false);
            StopCoroutine(LookForTargets()); StopMoving();
        };

        return state;
    }

    protected virtual State<INFANTRY_STATES> FollowLeader()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("FollowLeader");

        state.OnEnter += (x) =>
        {
            _infantry_AI.ManualMovement.Alignment = AlignmentType.Velocity;
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
            for (int frames = 0; frames < 120; frames++) yield return null;

            if (MyFireteam.IsNearLeader(this, MinDistanceFromDestination)) 
            {
                DebugEntity.Log("El lider esta muy cerca, no es necesario calcular camino");
                continue;
            }

            GoToLeader();
        }
       
    }

    void GoToLeader()
    {
        _infantry_AI.SetDestination(MyFireteam.Leader.transform.position, () =>
        {
            Infantry_FSM.SendInput(INFANTRY_STATES.WAITING_ORDERS);
            DebugEntity.Log("Llegue al lider");
        });
     
    }

    protected virtual State<INFANTRY_STATES> FireAtWill()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("FireAtWill");

        state.OnEnter += (x) =>
        {

            _infantry_AI.CancelMovement();

            _anim.SetBool("Running", true);
            _anim.SetBool("Shooting", true);

            InCombat = true;

            DebugEntity.Log("Entro en combate");

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

    protected virtual State<INFANTRY_STATES> Die()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("Die");

        state.OnEnter += (x) =>
        {
            _infantry_AI.ManualMovement.Alignment = AlignmentType.Custom;
            InCombat = false;


            var colliders = FList.Create(GetComponent<Collider>()) + GetComponentsInChildren<Collider>();

            foreach (var item in colliders.Where(x => x != null)) item.enabled = false;

            foreach (var item in this.GetComponents<MonoBehaviour>()) item.enabled = false;

            GetComponent<Rigidbody>().useGravity = false;

            _infantry_AI.CancelMovement();
            _infantry_AI.ManualMovement.UseGravity(false);
            _infantry_AI.ManualMovement.DeactivateMovement();
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
            var z = LookForEnemiesAlive();                               
                   

            if (z.Any())
            {
                Infantry_FSM.SendInput(INFANTRY_STATES.FIRE_AT_WILL);
                break;
            } 

            for (int i = 0; i < waitingFramesTilSearch; i++)
                yield return null;
        }
    }


    public IEnumerable<Soldier> GetMilitaryAround()
    {
         var col = _gridEntity.GetEntitiesInRange(_fovAgent.ViewRadius)
          .Where(x => x != this)
          .OfType<Soldier>();
    
         return col;
    }

    IEnumerable<Soldier> LookForEnemiesAlive()
    {
        return GetMilitaryAround().Where(x => x.Team != Team)
                  .Where(x => x.Health.IsAlive)
                  .Where(x => _fovAgent.IN_FOV(x.transform.position));
    }

    IEnumerator SetTarget()
    {
        while (true)
        {      
            ActualTarget = LookForEnemiesAlive().Minimum(GetWeakestAndNearest);

            if (ActualTarget != null)
            {
                _infantry_AI.ManualMovement.AlignmentTarget = ActualTarget.transform;
                _infantry_AI.ManualMovement.Alignment = AlignmentType.Target;

                Vector3 dir = ActualTarget.transform.position - transform.position;
                _shootComponent.Shoot(_shootPos, dir, CheckIfDifferentTeam);
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
                    Infantry_FSM.SendInput(INFANTRY_STATES.FOLLOW_LEADER);         
            }
            yield return new WaitForSeconds(_timeBeforeSelectingTarget);
        }
    }


    #endregion

    bool CheckIfDifferentTeam(RaycastHit hit)
    {
        if (hit.transform.TryGetComponent<IMilitary>(out var x))       
            return x.Team != Team;
        
        return true;
    }
    float GetWeakestAndNearest(Entity entity)
    {
        float result = 0;
        result += Vector3.Distance(transform.position, entity.transform.position);
      
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
