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

    public EventFSM<MEDIC_INFANTRY_STATES> FSM;

    public MobileInfantry HealTarget;

    State<MEDIC_INFANTRY_STATES> _idle, _runTo, _heal, _die;

    float _queryHealTargetsTime = 1f;

    // El porcentaje de vida requerido para considerar que una tropa ya no necesita curacion.
    float _minHealthNeeded = 0.9f;

    // El tiempo maximo que un medico se puede pasar curando a alguien.
    [SerializeField] float _maxTimeHealing = 10;
    float _timeHealing;

    // La velocidad de curacion en health points por segundo
    [SerializeField] float _healSpeed = 30f;


    [SerializeField] float _maxHealDistance = 2.5f;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void CreateFSM() 
    {
        _idle = CreateIdleState();
        _runTo = CreateRunToState();
        _heal = CreateHealState();
        _die = CreateDieState();

        ConfigureTransitions();

        FSM = new EventFSM<MEDIC_INFANTRY_STATES>(_idle);
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

        // Si el objetivo ya esta al maximo de vida, deberia cambiar
        if (HealTarget.Life >= HealTarget.MaxLife)
        {
            HealTarget = null;
            return false;
        }

        // Si ya paso el tiempo maximo de curacion y el objetivo ya tiene el minimo de vida necesario, deberia cambiar.
        if (_timeHealing >= _maxTimeHealing && HealTarget.NormalizedLife > _minHealthNeeded)
        {
            HealTarget = null;
            return false;
        }

        // Si no, el heal target actual deberia permanecer siendo el mismo
        return true;
    }

    // Update is called once per frame
    void Update() => FSM.Update();

    private void FixedUpdate() => FSM.FixedUpdate();

    #region States and Transitions
    State<MEDIC_INFANTRY_STATES> CreateIdleState()
    {
        var idle = new State<MEDIC_INFANTRY_STATES>("IDLE");

        idle.OnEnter += _ =>
        {

        };

        idle.OnUpdate += () => 
        {
            if (HealTarget)
            {
                SendInputToFSM(MEDIC_INFANTRY_STATES.RUN_TO_HEAL);
                return;
            }
        };

        idle.OnExit += _ =>
        {
        };

        return idle;
    }

    State<MEDIC_INFANTRY_STATES> CreateRunToState()
    {
        var runTo = new State<MEDIC_INFANTRY_STATES>("RUN_TO");
        float recalculatePathTime = 2;
        float currentTime = 0;
        bool calculatingPath = false;

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
            Debug.Log("Medico - Distancia a objetivo = " + (Vector3.Distance(HealTarget.transform.position, transform.position)));
            Debug.Log("Medico - Distancia maxima = " + _maxHealDistance);
            if (Vector3.Distance(HealTarget.transform.position, transform.position) <= _maxHealDistance)
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

    [SerializeField] ParticleSystem _healParticles;

    State<MEDIC_INFANTRY_STATES> CreateHealState()
    {
        var heal = new State<MEDIC_INFANTRY_STATES>("HEAL");
        float floatHealAmount = 0;

        heal.OnEnter += _ =>
        {
            // Pasar a animacion de curacion
            Anim.SetBool("Healing", true);
            floatHealAmount = 0;

            // Reproducir particulas de curacion
            _healParticles.Play();

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
            if (Vector3.Distance(HealTarget.transform.position, transform.position) > _maxHealDistance)
            {
                SendInputToFSM(MEDIC_INFANTRY_STATES.RUN_TO_HEAL);
                return;
            }

            //

            floatHealAmount += _healSpeed * Time.deltaTime;

            int intHealAmount = Mathf.FloorToInt(floatHealAmount);
            floatHealAmount -= intHealAmount;

            HealTarget.Heal(intHealAmount);

            _timeHealing += Time.deltaTime;
        };

        heal.OnExit += _ =>
        {
            _timeHealing = 0;
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

            GetComponent<Rigidbody>().useGravity = false;

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
        StateConfigurer.Create(_idle)
            .SetTransition(MEDIC_INFANTRY_STATES.RUN_TO_HEAL, _runTo)
            .SetTransition(MEDIC_INFANTRY_STATES.DIE, _die)
            .Done();

        StateConfigurer.Create(_runTo)
            .SetTransition(MEDIC_INFANTRY_STATES.AWAITING_ORDERS, _idle)
            .SetTransition(MEDIC_INFANTRY_STATES.HEAL, _heal)
            .SetTransition(MEDIC_INFANTRY_STATES.DIE, _die)
            .Done();

        StateConfigurer.Create(_heal)
            .SetTransition(MEDIC_INFANTRY_STATES.RUN_TO_HEAL, _runTo)
            .SetTransition(MEDIC_INFANTRY_STATES.AWAITING_ORDERS, _idle)
            .SetTransition(MEDIC_INFANTRY_STATES.DIE, _die)
            .Done();

        StateConfigurer.Create(_die).Done();
    }
    #endregion

    private void SendInputToFSM(MEDIC_INFANTRY_STATES inp) => FSM.SendInput(inp);

    #region Transitions
    public override void LeaderMoveTo(Vector3 pos)
    {
        if (InCombat)
            return;

        Destination = pos;
        FSM.SendInput(MEDIC_INFANTRY_STATES.LEADER_MOVE_TO);
    }

    public override void FollowLeader()
    {
        if (InCombat)
            return;

        FSM.SendInput(MEDIC_INFANTRY_STATES.FOLLOW_LEADER);
    }

    public override void AwaitOrders()
    {
        if (InCombat)
            return;

        FSM.SendInput(MEDIC_INFANTRY_STATES.AWAITING_ORDERS);
    }
    #endregion
}