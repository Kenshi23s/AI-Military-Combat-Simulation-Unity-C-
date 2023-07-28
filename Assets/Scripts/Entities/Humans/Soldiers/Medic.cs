using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IA2;
using System;
using System.Linq;

[RequireComponent(typeof(NewAIMovement))]
public class Medic : Soldier
{
    public enum MedicInputs
    {
        IDLE,
        FOLLOW_LEADER,
        RUN_TO,
        SHOOT,
        HEAL,
        DIE
    }

    NewAIMovement _ai;
    Fireteam _fireteam;
    public EventFSM<MedicInputs> _fsm;

    public Entity ShootTarget;
    public Infantry HealTarget;

    Vector3 _runToPosition;
    Action _onTargetReached = delegate { };

    Animator _anim;

    State<MedicInputs> _idle, _followLeader, _runTo, _shoot, _heal, _die;


    protected override void SoldierAwake()
    {
        _anim = GetComponent<Animator>();

        _idle = CreateIdleState();
        _followLeader = CreateFollowLeaderState();
        _runTo = CreateRunToState();
        _shoot = CreateShootState();
        _heal = CreateHealState();
        _die = CreateDieState();

        ConfigureTransitions();

    }

    void Start()
    {
        _fsm = new EventFSM<MedicInputs>(_idle);
    }


    State<MedicInputs> CreateIdleState()
    {
        var idle = new State<MedicInputs>("IDLE");

        idle.OnEnter += _ =>
        {
            _anim.SetBool("Idle", true);

            // Dejar de moverse

        };

        return idle;
    }

    State<MedicInputs> CreateFollowLeaderState()
    {
        var followLeader = new State<MedicInputs>("FOLLOW_LEADER");

        followLeader.OnEnter += _ =>
        {
            // Pasar a animacion de correr
            _anim.SetBool("Running", true);

            // Empezar corutina para calcular camino hacia el lider cada X cantidad de segundos 
        };

        followLeader.OnExit += _ =>
        {
            _anim.SetBool("Running", false);
        };

        return followLeader;
    }

    State<MedicInputs> CreateRunToState()
    {
        var runTo = new State<MedicInputs>("RUN_TO");


        runTo.OnEnter += _ =>
        {
            // Empezar a calcular camino hacia posicion, y cuando se haya calculado
            // empezar a moverse y pasar a animacion de correr.
            //_ai.SetDestination(_runToPosition, pathFound =>
            //{
            //    if (pathFound)
            //    {
            //        _anim.SetBool("Running", true);
            //        moving = true;
            //    }
            //    else
            //    {
            //        _fsm.SendInput(MedicInputs.FOLLOW_LEADER);
            //    }
            //});
        };

        runTo.OnUpdate += () =>
        {

        };

        runTo.OnExit += _ =>
        {
            _anim.SetBool("Running", false);
        };

        return runTo;
    }

    State<MedicInputs> CreateShootState()
    {
        var shoot = new State<MedicInputs>("SHOOT_TARGET");

        shoot.OnEnter += _ =>
        {
            // Pasar a animacion de disparar
            _anim.SetBool("Shooting", true);

            // Dejar de moverse
        };

        shoot.OnUpdate += () =>
        {
            // Logica de disparo y recarga
        };

        shoot.OnExit += _ =>
        {
            _anim.SetBool("Shooting", false);
        };

        return shoot;
    }

    [SerializeField] float _healTime = 0.4f;

    State<MedicInputs> CreateHealState()
    {
        var heal = new State<MedicInputs>("DIE");

        float timer = 0;

        heal.OnEnter += _ =>
        {
            timer = 0;
            // Pasar a animacion de curacion
            _anim.SetBool("Healing", true);

            // Dejar de 
        };

        heal.OnUpdate += () =>
        {
            timer += Time.deltaTime;

            // Despues de cierta cantidad de segundos, terminar de curar
            if (timer >= _healTime)
            {

            }

        };

        heal.OnExit += _ =>
        {
            _anim.SetBool("Healing", false);
        };

        return heal;
    }

    State<MedicInputs> CreateDieState()
    {
        var die = new State<MedicInputs>("DIE");

        die.OnEnter += _ =>
        {
            // Pasar a animacion de morir
            _anim.SetTrigger("Die");

            // Dejar de moverse
        };

        die.OnUpdate += () =>
        {
            // Despues de cierta cantidad de segundos, meter en un pool o destruir
        };

        return die;
    }

    void ConfigureTransitions()
    {
        StateConfigurer.Create(_idle)
            .SetTransition(MedicInputs.RUN_TO, _runTo)
            .SetTransition(MedicInputs.FOLLOW_LEADER, _followLeader)
            .SetTransition(MedicInputs.SHOOT, _shoot)
            .SetTransition(MedicInputs.DIE, _die)
            .Done();

        StateConfigurer.Create(_followLeader)
            .SetTransition(MedicInputs.RUN_TO, _runTo)
            .SetTransition(MedicInputs.SHOOT, _shoot)
            .SetTransition(MedicInputs.IDLE, _idle)
            .SetTransition(MedicInputs.DIE, _die)
            .Done();

        StateConfigurer.Create(_runTo)
            .SetTransition(MedicInputs.FOLLOW_LEADER, _followLeader)
            .SetTransition(MedicInputs.SHOOT, _shoot)
            .SetTransition(MedicInputs.IDLE, _idle)
            .SetTransition(MedicInputs.DIE, _die)
            .Done();

        StateConfigurer.Create(_shoot)
            .SetTransition(MedicInputs.RUN_TO, _runTo)
            .SetTransition(MedicInputs.FOLLOW_LEADER, _followLeader)
            .SetTransition(MedicInputs.IDLE, _idle)
            .SetTransition(MedicInputs.DIE, _die)
            .Done();

        StateConfigurer.Create(_heal)
            .SetTransition(MedicInputs.RUN_TO, _runTo)
            .SetTransition(MedicInputs.FOLLOW_LEADER, _followLeader)
            .SetTransition(MedicInputs.IDLE, _idle)
            .SetTransition(MedicInputs.DIE, _die)
            .Done();

        StateConfigurer.Create(_die).Done();
    }


    float _scanForShootTargetsTime = 1f, _scanForHealTargetsTime = 1f;

    IEnumerator ScanForShootTargets()
    {
        while (true)
        {
            var z = _gridEntity.GetEntitiesInRange(_fovAgent.ViewRadius)
               .Where(x => _fovAgent.IN_FOV(x.transform.position))
               .OfType<IMilitary>()
               .Where(x => x.Team != Team);

            if (z.Any()) _fsm.SendInput(MedicInputs.SHOOT);

            yield return new WaitForSeconds(_scanForShootTargetsTime);
        }
    }

    IEnumerator ScanForHealTargets()
    {
        while (true)
        {
            var z = _gridEntity.GetEntitiesInRange(_fovAgent.ViewRadius)
               .OfType<IMilitary>()
               .Where(x => x.Team == Team);

            if (z.Any()) _fsm.SendInput(MedicInputs.RUN_TO);

            yield return new WaitForSeconds(_scanForHealTargetsTime);
        }
    }

    // Update is called once per frame
    void Update() => _fsm.Update();

    private void FixedUpdate() => _fsm.FixedUpdate();

    private void SendInputToFSM(MedicInputs inp) => _fsm.SendInput(inp);

    
}

   
