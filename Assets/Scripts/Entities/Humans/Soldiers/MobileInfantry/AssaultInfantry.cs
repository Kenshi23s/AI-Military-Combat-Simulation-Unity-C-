using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IA2;
using AlignmentType = NewPhysicsMovement.AlignmentType;

/// <summary>
/// MobileInfantry that shoots the nearest enemy in its FOVAgent.
/// The Leader requests support when the enemies outnumber the Fireteam.
/// </summary>
public class AssaultInfantry : MobileInfantry
{
    [SerializeField] private float _timeBeforeSelectingTarget;

    private State<ASSAULT_INFANTRY_STATES> _awaitingOrders, _leaderMoveTo, _followLeader, _fireAtWill, _die;

    private WaitForSeconds _searchTargetWait, _selectTargetWait;

    // Running coroutines. Each state stops the ones it started.
    private Coroutine _lookForTargets, _findPoint, _followLeaderRoutine, _setTarget;

    // Seconds between two enemy searches in LookForTargets.
    private const float _searchTargetWaitTime = 0.5f;

    /// <summary>The enemy the soldier shoots at.</summary>
    public Entity ShootTarget { get; private set; }

    /// <summary>The FSM of the soldier.</summary>
    public EventFSM<ASSAULT_INFANTRY_STATES> FSM { get; private set; }

    /// <summary>States of the FSM.</summary>
    public enum ASSAULT_INFANTRY_STATES
    {
        AWAITING_ORDERS,
        LEADER_MOVE_TO,
        FOLLOW_LEADER,
        FIRE_AT_WILL,
        DIE
    }

    protected override void Awake()
    {
        base.Awake();

        _searchTargetWait = new WaitForSeconds(_searchTargetWaitTime);
        _selectTargetWait = new WaitForSeconds(_timeBeforeSelectingTarget);

        Health.OnTakeDamage += (x) =>
        {
            if (LookForEnemiesAlive().Any())
                FSM.SendInput(ASSAULT_INFANTRY_STATES.FIRE_AT_WILL);
        };

        Health.OnKilled += () => FSM.SendInput(ASSAULT_INFANTRY_STATES.DIE);
    }

    private void Update()
    {
        FSM.Update();
    }

    #region States
    protected override void CreateFSM()
    {
        _awaitingOrders = CreateAwaitingOrdersState();
        _leaderMoveTo = CreateLeaderMoveToState();
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

    private State<ASSAULT_INFANTRY_STATES> CreateAwaitingOrdersState()
    {
        State<ASSAULT_INFANTRY_STATES> state = new State<ASSAULT_INFANTRY_STATES>("WaitingOrders");

        state.OnEnter += (x) =>
        {
            Movement.ManualMovement.Alignment = AlignmentType.Velocity;
            StopMoving(); Anim.SetBool("Running", false);

            DebugEntity.Log("Espero Ordenes");

            RestartCoroutine(ref _lookForTargets, LookForTargets());

            if (!IsLeader && !IsCapturing) return;

            RestartCoroutine(ref _findPoint, Fireteam.FindNearestUntakenPoint());

            DebugEntity.Log("Busco la zona mas cercana");
        };

        state.OnExit += (x) =>
        {
            StopTrackedCoroutine(ref _lookForTargets);
            StopTrackedCoroutine(ref _findPoint);
        };

        return state;
    }

    private State<ASSAULT_INFANTRY_STATES> CreateLeaderMoveToState()
    {
        State<ASSAULT_INFANTRY_STATES> state = new State<ASSAULT_INFANTRY_STATES>("MoveTowards");

        state.OnEnter += (x) =>
        {
            if (Vector3.SqrMagnitude(Destination - transform.position) < _minDistanceFromDestination * _minDistanceFromDestination)
            {
                Movement.ManualMovement.Alignment = AlignmentType.Velocity;
                FSM.SendInput(ASSAULT_INFANTRY_STATES.AWAITING_ORDERS);
                return;
            }

            StopMoving();

            DebugEntity.Log("Me muevo hacia posicion x");
            // Al llegar, o si se traba y no puede llegar, vuelve a esperar ordenes.
            Movement.SetDestination(Destination,
                onDestinationReached: () => FSM.SendInput(ASSAULT_INFANTRY_STATES.AWAITING_ORDERS),
                onFailed: () => FSM.SendInput(ASSAULT_INFANTRY_STATES.AWAITING_ORDERS));

            Anim.SetBool("Running", true);
            RestartCoroutine(ref _lookForTargets, LookForTargets());
        };

        state.OnExit += (x) =>
        {
            Anim.SetBool("Running", false);
            StopTrackedCoroutine(ref _lookForTargets);
            StopMoving();
        };

        return state;
    }

    private State<ASSAULT_INFANTRY_STATES> CreateFollowLeaderState()
    {
        State<ASSAULT_INFANTRY_STATES> state = new State<ASSAULT_INFANTRY_STATES>("FollowLeader");

        state.OnEnter += (x) =>
        {
            Movement.ManualMovement.Alignment = AlignmentType.Velocity;
            if (Fireteam.IsNearLeader(this, _minDistanceFromDestination))
            {
                FSM.SendInput(ASSAULT_INFANTRY_STATES.AWAITING_ORDERS);
                return;
            }

            DebugEntity.Log("Sigo al lider");

            Anim.SetBool("Running", true);

            RestartCoroutine(ref _lookForTargets, LookForTargets());
            RestartCoroutine(ref _followLeaderRoutine, FollowLeaderRoutine());
        };

        state.OnExit += (x) =>
        {
            Anim.SetBool("Running", false);
            StopTrackedCoroutine(ref _lookForTargets);
            StopTrackedCoroutine(ref _followLeaderRoutine);
            Movement.CancelMovement();
        };

        return state;
    }

    private State<ASSAULT_INFANTRY_STATES> CreateFireAtWillState()
    {
        State<ASSAULT_INFANTRY_STATES> state = new State<ASSAULT_INFANTRY_STATES>("FireAtWill");

        state.OnEnter += (x) =>
        {
            Movement.ManualMovement.Alignment = AlignmentType.Custom;
            Movement.CancelMovement();

            Anim.SetBool("Running", true);
            Anim.SetBool("Shooting", true);

            InCombat = true;

            DebugEntity.Log("Entro en combate");

            RestartCoroutine(ref _setTarget, SetTarget());

            if (!IsLeader) return;

            var enemiesAlive = LookForEnemiesAlive().ToArray();

            if (enemiesAlive.Length > Fireteam.FireteamMembers.Count)
            {
                Vector3 middlePoint = enemiesAlive.Aggregate(Vector3.zero, (x, y) => x += y.transform.position) / enemiesAlive.Length;
                middlePoint.y = transform.position.y;
                Fireteam.RequestSupport(middlePoint);
            }
        };

        state.OnUpdate += () =>
        {
            if (ShootTarget == null) return;

            Movement.ManualMovement.Alignment = AlignmentType.Custom;

            Vector3 dir = ShootTarget.AimPoint - transform.position;
            Movement.ManualMovement.CustomAlignment = GetTargetRotation(dir);
        };

        state.OnExit += (x) =>
        {
            InCombat = false;
            Anim.SetBool("Shooting", false);
            StopTrackedCoroutine(ref _setTarget);
        };

        return state;
    }

    private State<ASSAULT_INFANTRY_STATES> CreateDieState()
    {
        State<ASSAULT_INFANTRY_STATES> state = new State<ASSAULT_INFANTRY_STATES>("Die");

        state.OnEnter += (x) =>
        {
            // Disabling the components keeps their coroutines running, so they are stopped here.
            StopAllCoroutines();

            Movement.ManualMovement.Alignment = AlignmentType.Custom;
            InCombat = false;

            var colliders = FList.Create(GetComponent<Collider>()) + GetComponentsInChildren<Collider>();

            foreach (var item in colliders.Where(x => x != null)) item.enabled = false;

            foreach (var item in GetComponents<MonoBehaviour>()) item.enabled = false;

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

    /// <summary>Stops the NewAIMovement.</summary>
    public void StopMoving()
    {
        Movement.CancelMovement();
    }

    #region Metodos Utiles

    // Sends FIRE_AT_WILL when an enemy is in sight. Checks every _searchTargetWaitTime seconds.
    private IEnumerator LookForTargets()
    {
        // The first check waits one frame. A state change inside StartCoroutine would run before the handle is stored.
        yield return null;

        while (true)
        {
            if (LookForEnemiesAlive().Any())
            {
                FSM.SendInput(ASSAULT_INFANTRY_STATES.FIRE_AT_WILL);
                yield break;
            }

            yield return _searchTargetWait;
        }
    }

    private IEnumerable<Soldier> LookForEnemiesAlive()
    {
        return GetMilitaryAround()
            .Where(x => x.Team != Team)
            .Where(x => x.Health.IsAlive)
            .Where(x => _fovAgent.IN_FOV(x.transform.position));
    }

    // Shoots the nearest enemy every _timeBeforeSelectingTarget seconds.
    private IEnumerator SetTarget()
    {
        // The first check waits one frame. A state change inside StartCoroutine would run before the handle is stored.
        yield return null;

        while (true)
        {
            ShootTarget = LookForEnemiesAlive().Minimum(GetSqrDistance);

            if (ShootTarget != null)
            {
                Vector3 dir = ShootTarget.AimPoint - transform.position;
                ShootComponent.Shoot(_shootPos, dir, CheckIfDifferentTeam);
            }
            else if (IsLeader)
            {
                // pregunto si alguno de mis miembros tiene un enemigo cerca con vida
                if (Fireteam.AlliesWithEnemiesNearby(this, out Entity ally))
                {
                    Destination = ally.transform.position;
                    FSM.SendInput(ASSAULT_INFANTRY_STATES.LEADER_MOVE_TO);
                }
            }
            else
            {
                FSM.SendInput(ASSAULT_INFANTRY_STATES.FOLLOW_LEADER);
            }

            yield return _selectTargetWait;
        }
    }

    private Quaternion GetTargetRotation(Vector3 dir)
    {
        Vector3 gunForward = _shootPos.forward; gunForward.y = 0;
        dir.y = 0;

        Quaternion fromToRotation = Quaternion.FromToRotation(gunForward.normalized, dir.normalized);

        return transform.rotation * fromToRotation;
    }

    #endregion

    private bool CheckIfDifferentTeam(RaycastHit hit)
    {
        if (hit.transform.TryGetComponent<IMilitary>(out var x))
            return x.Team != Team;

        return true;
    }

    private float GetSqrDistance(Entity entity)
    {
        return Vector3.SqrMagnitude(transform.position - entity.transform.position);
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
