using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IA2;
using System;

[RequireComponent(typeof(NewAIMovement))]
[RequireComponent(typeof(FOVAgent))]
public class Medic : GridEntity
{
    public enum MedicInputs
    {
        IDLE,
        FOLLOW_LEADER,
        RUN_TO,
        SHOOT_TARGET,
        DIE
    }

    NewAIMovement _ai;
    FOVAgent _fov;
    Fireteam _fireteam;
    public EventFSM<MedicInputs> _fsm;

    public Entity _shootTarget;
    public Infantry _healTarget;
    public Vector3 coverPosition;

    Vector3 runToTarget;
    Action onTargetReached = delegate { };

    Animator _anim;

    State<MedicInputs> idle, followLeader, runTo, shootTarget, die;

    private void Awake()
    {
        _anim = GetComponent<Animator>();

        idle = CreateIdleState();
        followLeader = CreateFollowLeaderState();
        runTo = CreateRunToState();
        shootTarget = CreateShootTargetState();
        die = CreateDieState();

        ConfigureTransitions();

        _fsm = new EventFSM<MedicInputs>(idle);
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

            _anim.SetBool("Running", true);
        };

        runTo.OnUpdate += () =>
        {
            // Avisar que nos movimos.
            Moved();
        };

        runTo.OnExit += _ =>
        {
            _anim.SetBool("Running", false);
        };

        return runTo;
    }

    State<MedicInputs> CreateShootTargetState()
    {
        var shootTarget = new State<MedicInputs>("SHOOT_TARGET");

        shootTarget.OnEnter += _ => 
        {
            // Pasar a animacion de disparar
            _anim.SetBool("Shooting", true);

            // Dejar de moverse
        };

        shootTarget.OnUpdate += () =>
        {
            // Logica de disparo y recarga
        };

        shootTarget.OnExit += _ => 
        {
            _anim.SetBool("Shooting", false);
        };

        return shootTarget;
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
        StateConfigurer.Create(idle)
            .SetTransition(MedicInputs.RUN_TO, runTo)
            .SetTransition(MedicInputs.FOLLOW_LEADER, followLeader)
            .SetTransition(MedicInputs.SHOOT_TARGET, shootTarget)
            .SetTransition(MedicInputs.DIE, die)
            .Done();

        StateConfigurer.Create(followLeader)
            .SetTransition(MedicInputs.RUN_TO, runTo)
            .SetTransition(MedicInputs.SHOOT_TARGET, shootTarget)
            .SetTransition(MedicInputs.IDLE, idle)
            .SetTransition(MedicInputs.DIE, die)
            .Done();

        StateConfigurer.Create(runTo)
            .SetTransition(MedicInputs.FOLLOW_LEADER, followLeader)
            .SetTransition(MedicInputs.SHOOT_TARGET, shootTarget)
            .SetTransition(MedicInputs.IDLE, idle)
            .SetTransition(MedicInputs.DIE, die)
            .Done();

        StateConfigurer.Create(shootTarget)
            .SetTransition(MedicInputs.RUN_TO, runTo)
            .SetTransition(MedicInputs.FOLLOW_LEADER, followLeader)
            .SetTransition(MedicInputs.IDLE, idle)
            .SetTransition(MedicInputs.DIE, die)
            .Done();

        StateConfigurer.Create(die).Done();
    }

    // Update is called once per frame
    void Update() => _fsm.Update();

    private void FixedUpdate() => _fsm.FixedUpdate();

    private void SendInputToFSM(MedicInputs inp) => _fsm.SendInput(inp);
}
