using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IA2;
using System;
using System.Linq;

/// <summary>
/// MobileInfantry that heals the nearest wounded AssaultInfantry of its team.
/// Healing has priority over following the Leader.
/// </summary>
[RequireComponent(typeof(NewAIMovement))]
public class MedicInfantry : MobileInfantry
{
    /// <summary>The ally the medic heals.</summary>
    public MobileInfantry HealTarget;

    // La velocidad de curacion en health points por segundo
    [SerializeField] private float _healSpeed = 40f;

    [SerializeField] private float _maxHealDistance = 2.5f, _idealHealDistance = 1.5f;

    [SerializeField] private ParticleSystem _healParticles;

    private EventFSM<MEDIC_INFANTRY_STATES> _fsm;

    private State<MEDIC_INFANTRY_STATES> _awaitingOrders, _leaderMoveTo, _followLeader, _runToHeal, _heal, _die;

    private float _queryHealTargetsTime = 1f;

    // El porcentaje de vida requerido para considerar que una tropa ya no necesita curacion.
    private float _minHealthNeeded = 0.9f;

    // Running coroutines. Each state stops the ones it started.
    private Coroutine _findPoint, _followLeaderRoutine;

    // Wounded ally the medic could not reach, skipped by QueryForHealTarget until _ignoreHealTargetUntil.
    private MobileInfantry _ignoredHealTarget;
    private float _ignoreHealTargetUntil;

    // Extra distance over DestinationArriveDistance at which healing starts.
    private const float HealStartMargin = 0.25f;

    // Failed attempts to reach HealTarget before the medic ignores it.
    private const int MaxReachFailures = 2;

    // Seconds the medic ignores an unreachable HealTarget.
    private const float UnreachableIgnoreSeconds = 5f;

    /// <summary>States of the FSM.</summary>
    public enum MEDIC_INFANTRY_STATES
    {
        AWAITING_ORDERS,
        RUN_TO_HEAL,
        LEADER_MOVE_TO,
        FOLLOW_LEADER,
        HEAL,
        DIE
    }

    protected override void Awake()
    {
        base.Awake();

        Health.OnKilled += () => SendInputToFSM(MEDIC_INFANTRY_STATES.DIE);
    }

    protected override void CreateFSM()
    {
        _awaitingOrders = CreateAwaitingOrdersState();
        _leaderMoveTo = CreateLeaderMoveToState();
        _followLeader = CreateFollowLeaderState();
        _runToHeal = CreateRunToHealState();
        _heal = CreateHealState();
        _die = CreateDieState();

        ConfigureTransitions();

        _fsm = new EventFSM<MEDIC_INFANTRY_STATES>(_awaitingOrders);
    }

    private void Start()
    {
        StartCoroutine(QueryForHealTarget());
    }

    private void Update()
    {
        _fsm?.Update();
    }

    private IEnumerator QueryForHealTarget()
    {
        float timer = _queryHealTargetsTime;

        // Esperar a que no haya objetivo de curacion
        // y que el tiempo de query haya pasado
        var wait = new WaitUntil(() =>
        {
            timer -= Time.deltaTime;

            if (timer <= 0 && !CheckHealTarget())
            {
                timer = _queryHealTargetsTime;
                return true;
            }

            return false;
        });

        while (true)
        {
            // Conseguir a el aliado herido mas cercano.
            HealTarget = _gridEntity?.GetEntitiesInRange(_fovAgent.ViewRadius)
               .OfType<AssaultInfantry>()
               .Where(x => x.Team == Team)
               .Where(x => x.IsAlive && x.Life < x.MaxLife)
               .Where(x => x != _ignoredHealTarget || Time.time >= _ignoreHealTargetUntil)
               .Minimum(x => Vector3.SqrMagnitude(x.transform.position - transform.position));

            yield return wait;
        }
    }

    // Squared distance to target on the ground plane. The pivots of the medic and the target are at different heights.
    private float HorizontalSqrDistanceTo(Transform target)
    {
        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude;
    }

    // Devuelve verdadero si el HealthTarget deberia seguir siendo el mismo.
    // Si murio, por ejemplo, devuelve falso.
    private bool CheckHealTarget()
    {
        // Si no existe un heal target, seguimos haciendo query hasta encontrar uno.
        if (!HealTarget)
            return false;

        // Si esta muerto, el heal target actual deberia cambiar
        if (!HealTarget.IsAlive)
        {
            HealTarget = null;
            return false;
        }

        // Si el objetivo ya tiene el minimo de vida necesario, deberia cambiar.
        if (HealTarget.NormalizedLife > _minHealthNeeded)
        {
            HealTarget = null;
            return false;
        }

        // Si no, el heal target actual deberia permanecer siendo el mismo
        return true;
    }

    #region States and Transitions
    private State<MEDIC_INFANTRY_STATES> CreateAwaitingOrdersState()
    {
        var awaitingOrders = new State<MEDIC_INFANTRY_STATES>("AWAITING_ORDERS");

        awaitingOrders.OnEnter += _ =>
        {
            // Solo el lider (o quien esta capturando) le da ordenes al Fireteam, igual que AssaultInfantry.
            if (IsLeader || IsCapturing)
                RestartCoroutine(ref _findPoint, Fireteam.FindNearestUntakenPoint());
        };

        awaitingOrders.OnUpdate += () =>
        {
            // Transiciones si no es lider. Se prioriza curar
            if (!IsLeader)
            {
                if (HealTarget)
                {
                    SendInputToFSM(MEDIC_INFANTRY_STATES.RUN_TO_HEAL);
                    return;
                }

                if (!Fireteam.IsNearLeader(this, _minDistanceFromDestination))
                    SendInputToFSM(MEDIC_INFANTRY_STATES.FOLLOW_LEADER);

                return;
            }

            // Transiciones si es lider. Se prioriza ir al punto, solo que el Fireteam es el que le da la orden.
            if (HealTarget)
                SendInputToFSM(MEDIC_INFANTRY_STATES.RUN_TO_HEAL);
        };

        awaitingOrders.OnExit += _ => StopTrackedCoroutine(ref _findPoint);

        return awaitingOrders;
    }

    private State<MEDIC_INFANTRY_STATES> CreateLeaderMoveToState()
    {
        var leaderMoveTo = new State<MEDIC_INFANTRY_STATES>("LEADER_MOVE_TO");

        leaderMoveTo.OnEnter += (x) =>
        {
            // Al llegar, o si se traba y no puede llegar, vuelve a esperar ordenes.
            Movement.SetDestination(Destination,
                onDestinationReached: () => _fsm.SendInput(MEDIC_INFANTRY_STATES.AWAITING_ORDERS),
                onFailed: () => _fsm.SendInput(MEDIC_INFANTRY_STATES.AWAITING_ORDERS));

            Anim.SetBool("Running", true);
        };

        leaderMoveTo.OnExit += (x) =>
        {
            Anim.SetBool("Running", false);
            Movement.CancelMovement();
        };

        return leaderMoveTo;
    }

    private State<MEDIC_INFANTRY_STATES> CreateFollowLeaderState()
    {
        var followLeader = new State<MEDIC_INFANTRY_STATES>("FOLLOW_LEADER");

        followLeader.OnEnter += (x) =>
        {
            if (Fireteam.IsNearLeader(this, _minDistanceFromDestination))
            {
                _fsm.SendInput(MEDIC_INFANTRY_STATES.AWAITING_ORDERS);
                return;
            }

            RestartCoroutine(ref _followLeaderRoutine, FollowLeaderRoutine());

            Anim.SetBool("Running", true);
        };

        followLeader.OnUpdate += () =>
        {
            // Si hay un heal target cerca priorizar ir a el antes de seguir al lider.
            // FollowLeaderRoutine frena al medico cuando esta cerca del lider.
            if (HealTarget)
                SendInputToFSM(MEDIC_INFANTRY_STATES.RUN_TO_HEAL);
        };

        followLeader.OnExit += (x) =>
        {
            StopTrackedCoroutine(ref _followLeaderRoutine);

            Anim.SetBool("Running", false);
            Movement.CancelMovement();
        };

        return followLeader;
    }

    private State<MEDIC_INFANTRY_STATES> CreateRunToHealState()
    {
        var runTo = new State<MEDIC_INFANTRY_STATES>("RUN_TO");
        float recalculatePathTime = 2;
        float currentTime = 0;
        bool calculatingPath = false;
        int reachFailures = 0;

        // The medic stops at DestinationArriveDistance from the destination, so healing starts at that distance or farther.
        float healStartDistance = Mathf.Min(Mathf.Max(_idealHealDistance, Movement.DestinationArriveDistance) + HealStartMargin, _maxHealDistance);
        float sqrHealStartDistance = healStartDistance * healStartDistance;
        float sqrMaxHealDistance = _maxHealDistance * _maxHealDistance;

        // Counts a failed attempt to reach HealTarget. After MaxReachFailures the medic ignores it for a while.
        void OnReachFailed()
        {
            calculatingPath = false;
            Anim.SetBool("Running", false);

            if (++reachFailures < MaxReachFailures)
                return;

            DebugEntity.Log("No puedo llegar al herido, lo ignoro un rato");
            _ignoredHealTarget = HealTarget;
            _ignoreHealTargetUntil = Time.time + UnreachableIgnoreSeconds;
            HealTarget = null;
        }

        void GoToHealTarget()
        {
            // Empezar a calcular camino hacia posicion, y cuando se haya calculado
            // empezar a moverse y pasar a animacion de correr.
            calculatingPath = true;

            Movement.SetDestination(HealTarget.transform.position,
                onDestinationReached: () =>
                {
                    // Llego al destino: si el herido sigue cerca, empieza a curar.
                    if (HealTarget && HorizontalSqrDistanceTo(HealTarget.transform) <= sqrMaxHealDistance)
                        SendInputToFSM(MEDIC_INFANTRY_STATES.HEAL);
                },
                onFinishCalculating: pathFound =>
                {
                    if (pathFound)
                    {
                        // Si se encontro camino, empezamos a correr
                        calculatingPath = false;
                        reachFailures = 0;
                        Anim.SetBool("Running", true);
                    }
                    else
                    {
                        // Si no se encontro camino, esperamos y volvemos a calcular
                        Movement.CancelMovement();
                        OnReachFailed();
                    }
                },
                // Si se traba, frena y el timer de este estado vuelve a calcular el camino.
                onFailed: OnReachFailed);
        }

        runTo.OnEnter += _ =>
        {
            currentTime = 0;
            reachFailures = 0;
            GoToHealTarget();
        };

        runTo.OnUpdate += () =>
        {
            if (!HealTarget)
            {
                // Si no se encontro camino volvemos a idle
                SendInputToFSM(MEDIC_INFANTRY_STATES.AWAITING_ORDERS);
                return;
            }

            // Si llegamos, pasar a estado de curacion
            if (HorizontalSqrDistanceTo(HealTarget.transform) <= sqrHealStartDistance)
            {
                DebugEntity.Log("Entrando al estado de Heal");
                SendInputToFSM(MEDIC_INFANTRY_STATES.HEAL);
                return;
            }

            if (calculatingPath)
                return;

            currentTime += Time.deltaTime;
            if (currentTime >= recalculatePathTime)
            {
                currentTime = 0;
                GoToHealTarget();
            }
        };

        runTo.OnExit += _ =>
        {
            calculatingPath = false;
            Anim.SetBool("Running", false);
            Movement.CancelMovement();
        };

        return runTo;
    }

    private State<MEDIC_INFANTRY_STATES> CreateHealState()
    {
        var heal = new State<MEDIC_INFANTRY_STATES>("HEAL");
        float floatHealAmount = 0;
        float sqrMaxHealDistance = _maxHealDistance * _maxHealDistance;

        heal.OnEnter += _ =>
        {
            // Pasar a animacion de curacion
            Anim.SetBool("Healing", true);
            floatHealAmount = 0;

            // Reproducir particulas de curacion
            _healParticles.Play();
            Movement.ManualMovement.Alignment = NewPhysicsMovement.AlignmentType.Target;
            Movement.ManualMovement.AlignmentTarget = HealTarget.transform;
        };

        heal.OnUpdate += () =>
        {
            // Si ya no hay un objetivo a quien curar, pasar a idle.
            if (!HealTarget)
            {
                SendInputToFSM(MEDIC_INFANTRY_STATES.AWAITING_ORDERS);
                return;
            }

            // Si el objetivo se aleja, correr hacia el
            if (HorizontalSqrDistanceTo(HealTarget.transform) > sqrMaxHealDistance)
            {
                SendInputToFSM(MEDIC_INFANTRY_STATES.RUN_TO_HEAL);
                return;
            }

            floatHealAmount += _healSpeed * Time.deltaTime;

            // Transformamos el valor de curacion de flotante a entero
            int intHealAmount = Mathf.FloorToInt(floatHealAmount);
            floatHealAmount -= intHealAmount;

            HealTarget.Heal(intHealAmount);
        };

        heal.OnExit += _ =>
        {
            Movement.ManualMovement.Alignment = NewPhysicsMovement.AlignmentType.Velocity;
            Anim.SetBool("Healing", false);
            _healParticles.Stop();
        };

        return heal;
    }

    private State<MEDIC_INFANTRY_STATES> CreateDieState()
    {
        var die = new State<MEDIC_INFANTRY_STATES>("DIE");

        die.OnEnter += _ =>
        {
            // Disabling the components keeps their coroutines running, so they are stopped here.
            StopAllCoroutines();

            Movement.ManualMovement.Alignment = NewPhysicsMovement.AlignmentType.Custom;
            InCombat = false;

            var colliders = FList.Create(GetComponent<Collider>()) + GetComponentsInChildren<Collider>();

            foreach (var item in colliders.Where(x => x != null))
                item.enabled = false;

            foreach (var item in GetComponents<MonoBehaviour>())
                item.enabled = false;

            Movement.CancelMovement();
            Movement.ManualMovement.UseGravity(false);
            Movement.ManualMovement.DeactivateMovement();

            Anim.SetBool("Die", true);
            DebugEntity.Log("Mori");
            Fireteam.RemoveMember(this);
        };

        return die;
    }

    private void ConfigureTransitions()
    {
        StateConfigurer.Create(_awaitingOrders)
            .SetTransition(MEDIC_INFANTRY_STATES.RUN_TO_HEAL, _runToHeal)
            .SetTransition(MEDIC_INFANTRY_STATES.LEADER_MOVE_TO, _leaderMoveTo)
            .SetTransition(MEDIC_INFANTRY_STATES.FOLLOW_LEADER, _followLeader)
            .SetTransition(MEDIC_INFANTRY_STATES.DIE, _die)
            .Done();

        StateConfigurer.Create(_leaderMoveTo)
            .SetTransition(MEDIC_INFANTRY_STATES.AWAITING_ORDERS, _awaitingOrders)
            .SetTransition(MEDIC_INFANTRY_STATES.DIE, _die)
            .Done();

        StateConfigurer.Create(_followLeader)
            .SetTransition(MEDIC_INFANTRY_STATES.AWAITING_ORDERS, _awaitingOrders)
            .SetTransition(MEDIC_INFANTRY_STATES.LEADER_MOVE_TO, _leaderMoveTo)
            .SetTransition(MEDIC_INFANTRY_STATES.RUN_TO_HEAL, _runToHeal)
            .SetTransition(MEDIC_INFANTRY_STATES.DIE, _die)
            .Done();

        StateConfigurer.Create(_runToHeal)
            .SetTransition(MEDIC_INFANTRY_STATES.AWAITING_ORDERS, _awaitingOrders)
            .SetTransition(MEDIC_INFANTRY_STATES.HEAL, _heal)
            .SetTransition(MEDIC_INFANTRY_STATES.DIE, _die)
            .Done();

        StateConfigurer.Create(_heal)
            .SetTransition(MEDIC_INFANTRY_STATES.RUN_TO_HEAL, _runToHeal)
            .SetTransition(MEDIC_INFANTRY_STATES.AWAITING_ORDERS, _awaitingOrders)
            .SetTransition(MEDIC_INFANTRY_STATES.DIE, _die)
            .Done();

        StateConfigurer.Create(_die).Done();
    }
    #endregion

    private void SendInputToFSM(MEDIC_INFANTRY_STATES inp) => _fsm?.SendInput(inp);

    public override void LeaderMoveTo(Vector3 pos)
    {
        Destination = pos;
        SendInputToFSM(MEDIC_INFANTRY_STATES.LEADER_MOVE_TO);
    }

    public override void FollowLeader()
    {
        SendInputToFSM(MEDIC_INFANTRY_STATES.FOLLOW_LEADER);
    }

    public override void AwaitOrders()
    {
        SendInputToFSM(MEDIC_INFANTRY_STATES.AWAITING_ORDERS);
    }
}
