using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IA2;
using System;
using System.Linq;

[RequireComponent(typeof(NewAIMovement))]
public class MedicInfantry : MobileInfantry
{
    public enum MEDIC_INFANTRY_STATES
    {
        AWAITING_ORDERS,
        RUN_TO_HEAL,
        LEADER_MOVE_TO,
        FOLLOW_LEADER,
        HEAL,
        DIE
    }

    EventFSM<MEDIC_INFANTRY_STATES> _fsm;

    public MobileInfantry HealTarget;

    State<MEDIC_INFANTRY_STATES> _awaitingOrders, _leaderMoveTo, _followLeader, _runToHeal, _heal, _die;

    float _queryHealTargetsTime = 1f;

    // El porcentaje de vida requerido para considerar que una tropa ya no necesita curacion.
    float _minHealthNeeded = 0.9f;

    // La velocidad de curacion en health points por segundo
    [SerializeField] float _healSpeed = 40f;

    [SerializeField] float _maxHealDistance = 2.5f, _idealHealDistance = 1.5f;

    [SerializeField] ParticleSystem _healParticles;

    protected override void Awake()
    {
        base.Awake();
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


    void Start()
    {
        StartCoroutine(QueryForHealTarget());
    }


    IEnumerator QueryForHealTarget()
    {
        float timer = _queryHealTargetsTime;

        // Esperar a que no haya objetivo de curacion
        // y que el tiempo de query haya pasado
        var wait = new WaitUntil(() => {
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
               .Minimum(x => Vector3.SqrMagnitude(x.transform.position - transform.position));
            
            yield return wait;
        }
    }

    // Devuelve verdadero si el HealthTarget deberia seguir siendo el mismo.
    // Si murio, por ejemplo, devuelve falso. 
    bool CheckHealTarget() 
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

    // Update is called once per frame
    void Update()
    {
        _fsm.Update();
    }

    #region States and Transitions
    State<MEDIC_INFANTRY_STATES> CreateAwaitingOrdersState()
    {
        var awaitingOrders = new State<MEDIC_INFANTRY_STATES>("AWAITING_ORDERS");

        awaitingOrders.OnEnter += _ =>
        {
            StartCoroutine(Fireteam.FindNearestUntakenPoint());
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
                {
                    SendInputToFSM(MEDIC_INFANTRY_STATES.FOLLOW_LEADER);
                    return;
                }

                return;
            }

            // Transiciones si es lider. Se prioriza ir al punto, solo que el Fireteam es el que le da la orden.
            if (HealTarget)
            {

                SendInputToFSM(MEDIC_INFANTRY_STATES.RUN_TO_HEAL);
            }
        };

        awaitingOrders.OnExit += _ =>
        {
            StopCoroutine(Fireteam.FindNearestUntakenPoint());

        };

        return awaitingOrders;
    }

    State<MEDIC_INFANTRY_STATES> CreateLeaderMoveToState()
    {
        var leaderMoveTo = new State<MEDIC_INFANTRY_STATES>("LEADER_MOVE_TO");

        leaderMoveTo.OnEnter += (x) =>
        {
            Movement.SetDestination(Destination, () =>
            {
                _fsm.SendInput(MEDIC_INFANTRY_STATES.AWAITING_ORDERS);
            });

            Anim.SetBool("Running", true);
        };

        leaderMoveTo.OnExit += (x) =>
        {
            Anim.SetBool("Running", false);
            Movement.CancelMovement();
        };

        return leaderMoveTo;
    }

    State<MEDIC_INFANTRY_STATES> CreateFollowLeaderState()
    {
        var followLeader = new State<MEDIC_INFANTRY_STATES>("LEADER_MOVE_TO");

        followLeader.OnEnter += (x) =>
        {
            if (Fireteam.IsNearLeader(this, _minDistanceFromDestination))
            {
                _fsm.SendInput(MEDIC_INFANTRY_STATES.AWAITING_ORDERS);
                return;
            }

            StartCoroutine(FollowLeaderRoutine());

            Anim.SetBool("Running", true);
        };

        followLeader.OnUpdate += () =>
        {
            // Si hay un heal target cerca priorizar ir a el antes de seguir al lider.
            if (HealTarget)
            {
                SendInputToFSM(MEDIC_INFANTRY_STATES.RUN_TO_HEAL);
                return;
            }

            // Si esta lo suficientemente cerca del lider, pasar a await orders 
            if (Fireteam.IsNearLeader(this, _minDistanceFromDestination))
            {
                SendInputToFSM(MEDIC_INFANTRY_STATES.AWAITING_ORDERS);
                return;
            }
        };

        followLeader.OnExit += (x) =>
        {
            StopCoroutine(FollowLeaderRoutine());
            
            Anim.SetBool("Running", false);
            Movement.CancelMovement();
        };

        return followLeader;
    }

    State<MEDIC_INFANTRY_STATES> CreateRunToHealState()
    {
        var runTo = new State<MEDIC_INFANTRY_STATES>("RUN_TO");
        float recalculatePathTime = 2;
        float currentTime = 0;
        bool calculatingPath = false;
        float sqrIdealHealDistance = _idealHealDistance * _idealHealDistance;

        void GoToHealTarget() 
        {
            // Empezar a calcular camino hacia posicion, y cuando se haya calculado
            // empezar a moverse y pasar a animacion de correr.
            calculatingPath = true;

            Movement.SetDestination(HealTarget.transform.position, 
                onFinishCalculating: pathFound =>
                {
                    calculatingPath = false;

                    if (pathFound)
                    {
                        // Si se encontro camino, empezamos a correr
                        Anim.SetBool("Running", true);
                    }
                    else
                    {
                        // Si no se encontro camino, esperamos y volvemos a calcular
                        Anim.SetBool("Running", false);
                        Movement.CancelMovement();
                    }
                });
        }

        runTo.OnEnter += _ =>
        {
            currentTime = 0;
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
            if (Vector3.SqrMagnitude(HealTarget.transform.position - transform.position) <= sqrIdealHealDistance)
            {
                Debug.Log("Entrando al estado de Heal");
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
            Anim.SetBool("Running", false);
            Movement.CancelMovement();
        };

        return runTo;
    }

    State<MEDIC_INFANTRY_STATES> CreateHealState()
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
            // TRANSICIONES
            // Si ya no hay un objetivo a quien curar, pasar a idle.
            if (!HealTarget)
            {
                SendInputToFSM(MEDIC_INFANTRY_STATES.AWAITING_ORDERS);
                return;
            }

            // Si el objetivo se aleja, correr hacia el
            if (Vector3.SqrMagnitude(HealTarget.transform.position - transform.position) > sqrMaxHealDistance)
            {
                SendInputToFSM(MEDIC_INFANTRY_STATES.RUN_TO_HEAL);
                return;
            }
            //

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

    State<MEDIC_INFANTRY_STATES> CreateDieState()
    {
        var die = new State<MEDIC_INFANTRY_STATES>("DIE");

        die.OnEnter += _ =>
        {
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
        };

        return die;
    }

    void ConfigureTransitions()
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

    private void SendInputToFSM(MEDIC_INFANTRY_STATES inp) => _fsm.SendInput(inp);

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