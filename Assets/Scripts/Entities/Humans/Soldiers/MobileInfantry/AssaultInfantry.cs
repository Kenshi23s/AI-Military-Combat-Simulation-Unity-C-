using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IA2;
using AlignmentType = NewPhysicsMovement.AlignmentType;

public class AssaultInfantry : MobileInfantry
{
    public Entity ShootTarget { get; private set; }
    [SerializeField] float _timeBeforeSelectingTarget;

    [field: SerializeField] public float MinDistanceFromDestination { get; private set; }

    public enum ASSAULT_INFANTRY_STATES
    {
        AWAITING_ORDERS,
        LEADER_MOVE_TO,
        FOLLOW_LEADER,
        FIRE_AT_WILL,
        DIE
    }

    public EventFSM<ASSAULT_INFANTRY_STATES> FSM { get; private set; }

    State<ASSAULT_INFANTRY_STATES> _awaitingOrders, _leaderMoveTo, _followLeader, _fireAtWill, _die;

    const float _searchTargetWaitTime = 0.5f;

    protected override void Awake()
    {
        base.Awake();

        Health.OnTakeDamage += (x) =>
        {
            if (LookForEnemiesAlive().Any())
            {
                FSM.SendInput(ASSAULT_INFANTRY_STATES.FIRE_AT_WILL);
            }

        };

        Health.OnKilled += () => FSM.SendInput(ASSAULT_INFANTRY_STATES.DIE);
    }

    #region States
    protected override void CreateFSM()
    {
        _awaitingOrders = CreateAwaitingOrdersState();
        _leaderMoveTo = CreateMoveTowardsState();
        _followLeader = CreateFollowLeaderState();
        _fireAtWill = CreateFireAtWillState();
        _die = CreateDieState();

        ConfigureStateTransitions();

        FSM = new EventFSM<ASSAULT_INFANTRY_STATES>(_awaitingOrders);
    }

    protected virtual void ConfigureStateTransitions()
    {
        StateConfigurer.Create(_awaitingOrders)
            .SetTransition(ASSAULT_INFANTRY_STATES.LEADER_MOVE_TO, _leaderMoveTo)
            .SetTransition(ASSAULT_INFANTRY_STATES.FOLLOW_LEADER, _followLeader)
            .SetTransition(ASSAULT_INFANTRY_STATES.FIRE_AT_WILL, _fireAtWill)
            .SetTransition(ASSAULT_INFANTRY_STATES.DIE, _die)
            .Done();



        StateConfigurer.Create(_leaderMoveTo)
            .SetTransition(ASSAULT_INFANTRY_STATES.AWAITING_ORDERS, _awaitingOrders)
            .SetTransition(ASSAULT_INFANTRY_STATES.FOLLOW_LEADER, _followLeader)
            .SetTransition(ASSAULT_INFANTRY_STATES.FIRE_AT_WILL, _fireAtWill)
            .SetTransition(ASSAULT_INFANTRY_STATES.DIE, _die)
            .Done();


        StateConfigurer.Create(_followLeader)
            .SetTransition(ASSAULT_INFANTRY_STATES.AWAITING_ORDERS, _awaitingOrders)
            .SetTransition(ASSAULT_INFANTRY_STATES.LEADER_MOVE_TO, _leaderMoveTo)
            .SetTransition(ASSAULT_INFANTRY_STATES.FIRE_AT_WILL, _fireAtWill)
            .SetTransition(ASSAULT_INFANTRY_STATES.DIE, _die)
            .Done();


        StateConfigurer.Create(_fireAtWill)
            .SetTransition(ASSAULT_INFANTRY_STATES.AWAITING_ORDERS, _awaitingOrders)
            .SetTransition(ASSAULT_INFANTRY_STATES.FOLLOW_LEADER, _followLeader)
            .SetTransition(ASSAULT_INFANTRY_STATES.LEADER_MOVE_TO, _leaderMoveTo)
            .SetTransition(ASSAULT_INFANTRY_STATES.DIE, _die)
            .Done();

        StateConfigurer.Create(_die)
            .Done();
    }

    protected virtual State<ASSAULT_INFANTRY_STATES> CreateAwaitingOrdersState()
    {
        State<ASSAULT_INFANTRY_STATES> state = new State<ASSAULT_INFANTRY_STATES>("WaitingOrders");

        state.OnEnter += (x) =>
        {
            Movement.ManualMovement.Alignment = AlignmentType.Velocity;
            StopMoving(); Anim.SetBool("Running", false);

            DebugEntity.Log("Espero Ordenes");

            StartCoroutine(LookForTargets());

            if (Fireteam.Leader != this && !IsCapturing) return;

            StartCoroutine(Fireteam.FindNearestUntakenPoint());

            DebugEntity.Log("Busco la zona mas cercana");
        };

        state.OnExit += (x) => StopCoroutine(LookForTargets());
        state.OnExit += (x) => StopCoroutine(Fireteam.FindNearestUntakenPoint());

        return state;
    }

    protected virtual State<ASSAULT_INFANTRY_STATES> CreateMoveTowardsState()
    {
        State<ASSAULT_INFANTRY_STATES> state = new State<ASSAULT_INFANTRY_STATES>("MoveTowards");

        state.OnEnter += (x) =>
        {
            if (MinDistanceFromDestination > Vector3.Distance(Destination, transform.position))
            {
                FSM.SendInput(ASSAULT_INFANTRY_STATES.AWAITING_ORDERS);
                return;
            }

            StopMoving();

            DebugEntity.Log("Me muevo hacia posicion x");
            Movement.SetDestination(Destination, () =>
            {
                FSM.SendInput(ASSAULT_INFANTRY_STATES.AWAITING_ORDERS);
            });

            Anim.SetBool("Running", true);
            StartCoroutine(LookForTargets());
        };

        state.OnExit += (x) =>
        {
            Anim.SetBool("Running", false);
            StopCoroutine(LookForTargets()); StopMoving();
        };

        return state;
    }

    protected virtual State<ASSAULT_INFANTRY_STATES> CreateFollowLeaderState()
    {
        State<ASSAULT_INFANTRY_STATES> state = new State<ASSAULT_INFANTRY_STATES>("FollowLeader");

        state.OnEnter += (x) =>
        {
            Movement.ManualMovement.Alignment = AlignmentType.Velocity;
            if (Fireteam.IsNearLeader(this, MinDistanceFromDestination))
            {
                FSM.SendInput(ASSAULT_INFANTRY_STATES.AWAITING_ORDERS);
                return;
            }

            DebugEntity.Log("Sigo al lider");

            Anim.SetBool("Running", true);

            StartCoroutine(LookForTargets());
            StartCoroutine(FollowLeaderRoutine());
        };


        state.OnExit += (x) =>
        {
            Anim.SetBool("Running", false);
            StopCoroutine(LookForTargets());
            StopCoroutine(FollowLeaderRoutine());
            Movement.CancelMovement();
        };

        return state;
    }

    protected virtual State<ASSAULT_INFANTRY_STATES> CreateFireAtWillState()
    {
        State<ASSAULT_INFANTRY_STATES> state = new State<ASSAULT_INFANTRY_STATES>("FireAtWill");

        state.OnEnter += (x) =>
        {

            Movement.CancelMovement();

            Anim.SetBool("Running", true);
            Anim.SetBool("Shooting", true);

            InCombat = true;

            DebugEntity.Log("Entro en combate");

            StartCoroutine(SetTarget());

            if (Fireteam.Leader != this) return;

            var enemiesAlive = LookForEnemiesAlive().ToArray();

            if (enemiesAlive.Length > Fireteam.FireteamMembers.Count)
            {
                Vector3 middlePoint = enemiesAlive.Aggregate(Vector3.zero, (x, y) => x += y.transform.position) / enemiesAlive.Length;
                middlePoint.y = transform.position.y;
                Fireteam.RequestSupport(middlePoint);
            }
        };


        state.OnExit += (x) =>
        {

            InCombat = false;
            Anim.SetBool("Shooting", false);
            StopCoroutine(SetTarget());
        };

        return state;
    }

    protected virtual State<ASSAULT_INFANTRY_STATES> CreateDieState()
    {
        State<ASSAULT_INFANTRY_STATES> state = new State<ASSAULT_INFANTRY_STATES>("Die");

        state.OnEnter += (x) =>
        {
            Movement.ManualMovement.Alignment = AlignmentType.Custom;
            InCombat = false;


            var colliders = FList.Create(GetComponent<Collider>()) + GetComponentsInChildren<Collider>();

            foreach (var item in colliders.Where(x => x != null)) item.enabled = false;

            foreach (var item in this.GetComponents<MonoBehaviour>()) item.enabled = false;

            GetComponent<Rigidbody>().useGravity = false;

            Movement.CancelMovement();
            Movement.ManualMovement.UseGravity(false);
            Movement.ManualMovement.DeactivateMovement();
            Anim.SetBool("Die", true);

            DebugEntity.Log("Mori");
            Fireteam.RemoveMember(this);

        };

        return state;
    }
    #endregion

    IEnumerator FollowLeaderRoutine()
    {
        GoToLeader();
        while (true)
        {
            for (int frames = 0; frames < 120; frames++) yield return null;

            if (Fireteam.IsNearLeader(this, MinDistanceFromDestination))
            {
                DebugEntity.Log("El lider esta muy cerca, no es necesario calcular camino");
                continue;
            }

            GoToLeader();
        }

    }

    void GoToLeader()
    {
        Movement.SetDestination(Fireteam.Leader.transform.position, () =>
        {
            FSM.SendInput(ASSAULT_INFANTRY_STATES.AWAITING_ORDERS);
            DebugEntity.Log("Llegue al lider");
        });

    }

    public void StopMoving()
    {
        Movement.CancelMovement();
    }

    #region Metodos Utiles

    IEnumerator LookForTargets()
    {
        while (true)
        {
            var z = LookForEnemiesAlive();


            if (z.Any())
            {
                FSM.SendInput(ASSAULT_INFANTRY_STATES.FIRE_AT_WILL);
                break;
            }

            for (int i = 0; i < _searchTargetWaitTime; i++)
                yield return null;
        }
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
            ShootTarget = LookForEnemiesAlive().Minimum(GetWeakestAndNearest);

            if (ShootTarget != null)
            {
                Movement.ManualMovement.AlignmentTarget = ShootTarget.transform;
                Movement.ManualMovement.Alignment = AlignmentType.Target;

                Vector3 dir = ShootTarget.transform.position - transform.position;
                ShootComponent.Shoot(_shootPos, dir, CheckIfDifferentTeam);
            }
            else
            {
                //si soy el lider
                if (Fireteam.Leader == this)
                {
                    //pregunto si alguno de mis miembros tiene un enemigo cerca con vida

                    if (Fireteam.AlliesWithEnemiesNearby(this, out Entity ally))
                    {
                        Destination = ally.transform.position;
                        FSM.SendInput(ASSAULT_INFANTRY_STATES.LEADER_MOVE_TO);
                    }
                }
                else
                    FSM.SendInput(ASSAULT_INFANTRY_STATES.FOLLOW_LEADER);
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
    public override void LeaderMoveTo(Vector3 pos)
    {
        if (InCombat)
            return;

        Destination = pos;
        FSM.SendInput(ASSAULT_INFANTRY_STATES.LEADER_MOVE_TO);
    }

    public override void FollowLeader()
    {
        if (InCombat)
            return;

        FSM.SendInput(ASSAULT_INFANTRY_STATES.FOLLOW_LEADER);
    }

    public override void AwaitOrders()
    {
        if (InCombat)
            return;

        FSM.SendInput(ASSAULT_INFANTRY_STATES.AWAITING_ORDERS);
    }
    #endregion

}
