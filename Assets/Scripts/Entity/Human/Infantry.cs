using IA2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[RequireComponent(typeof(NewAIMovement))]
[RequireComponent(typeof(FOVAgent))]
[RequireComponent(typeof(ShootComponent))]
public class Infantry : GridEntity,InitializeUnit
{
    public enum INFANTRY_STATES
    {
        WaitingOrders,
        MoveTowards,
        FollowLeader,
        Die,
        FireAtWill
    }
    [field : SerializeField] public Transform Center { get; private set; }
    public bool inCombat { get; private set; }
    

    [SerializeField] float _timeBeforeSelectingTarget;

    public Fireteam myFireteam { get; private set; }
    NewAIMovement _infantry_AI;
    FOVAgent _fov;
    #region ShootingLogic
    ShootComponent gun;
    [SerializeField] Transform _shootPos;
    #endregion
    public EventFSM<INFANTRY_STATES> infantry_FSM;

    public Entity actualTarget { get; private set; }

    public Vector3 Destination { get; private set; }


    public void InitializeUnit(Team newTeam)
    {
        MyTeam = newTeam;
        SetFSM();
    }

    protected override void EntityAwake()
    {
        _infantry_AI = GetComponent<NewAIMovement>();
        _fov = GetComponent<FOVAgent>();
        gun = GetComponent<ShootComponent>();
    }
    #region States
    void SetFSM()
    {
        var moveTowards = MoveTowards();
        var fireAtWill = FireAtWill();
    }
   
    State<INFANTRY_STATES> MoveTowards()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("MoveTowards");

        state.OnEnter += (x) =>
        {
            _infantry_AI.SetDestination(Destination);
           
            StartCoroutine(LookForTargets());
        };

        state.OnExit += (x) => 
        { 
            StopCoroutine(LookForTargets());
            _infantry_AI.CancelMovement();
        };

        return state;
    }

    State<INFANTRY_STATES> FollowLeader()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("FollowLeader");

        state.OnEnter += (x) =>
        {
            if (!myFireteam.IsNearLeader(this))
            {
                _infantry_AI.SetDestination(myFireteam.Leader.transform.position);
            }
            

            StartCoroutine(LookForTargets());
        };

        state.OnExit += (x) =>
        {
            StopCoroutine(LookForTargets());
            _infantry_AI.CancelMovement();
        };

        return state;
    }

    State<INFANTRY_STATES> FireAtWill()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("FireAtWill");

        state.OnEnter += (x) =>
        {
            StartCoroutine(SetTarget());
            if (myFireteam.Leader != this) return;
            
            var enemiesAlive = LookForEnemiesAlive().ToArray();
            if (enemiesAlive.Length > myFireteam.fireteamMembers.Count)
            {
                Vector3 middlePoint = enemiesAlive.Aggregate(Vector3.zero, (x, y) => x += y.transform.position) / enemiesAlive.Length;
                middlePoint.y = transform.position.y;
                myFireteam.RequestSupport(middlePoint);
            }
         
        };


        state.OnExit += (x) =>
        {
            StopCoroutine(SetTarget());
        };

        return state;
    }

    #endregion

    #region Metodos Utiles
    IEnumerator LookForTargets()
    {
        while (true)
        {
            var z = GetEntitiesAround()                               
           .Where(x => x.MyTeam != MyTeam)
           .Where(x => _fov.IN_FOV(x.transform.position));

            if (z.Any()) infantry_FSM.SendInput(INFANTRY_STATES.FireAtWill);

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
         
            
            actualTarget = LookForEnemiesAlive().Minimum(GetWeakestAndNearest);


            if (actualTarget != null)
            {
                transform.forward = actualTarget.transform.position - transform.position;
                gun.Shoot(_shootPos);

            }
            else
            {
                //si soy el lider
                if (myFireteam.Leader == this)
                {
                    //pregunto si alguno de mis miembros tiene un enemigo cerca con vida
                  
                    if (myFireteam.AlliesWithEnemiesNearby(this,out Entity ally))
                    {
                        Destination = ally.transform.position;
                        infantry_FSM.SendInput(INFANTRY_STATES.MoveTowards);
                    }
                }
                else
                {
                    infantry_FSM.SendInput(INFANTRY_STATES.FollowLeader);
                }
            }



            yield return new WaitForSeconds(_timeBeforeSelectingTarget);
        }
    }



    IEnumerable<Entity> LookForEnemiesAlive()
    {
        return GetEntitiesAround().Where(x => x.MyTeam != MyTeam)
                  .Where(x => x.health.isAlive);
    }
    #endregion
    
    float GetWeakestAndNearest(Entity entity)
    {
        float result = 0;
        result += Vector3.Distance(transform.position, entity.transform.position);
        result += entity.health.life;
        return result;
    }

    #region Transitions
    public void MoveTowardsTransition(Vector3 posToGo)
    {
        if (!inCombat)
            infantry_FSM.SendInput(INFANTRY_STATES.MoveTowards);
    }

    public void FollowLeaderTransition()
    {
        if (!inCombat)
            infantry_FSM.SendInput(INFANTRY_STATES.FollowLeader);
    }

   
    #endregion
}
