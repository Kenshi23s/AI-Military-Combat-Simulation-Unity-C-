using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IA2;
using System;

[RequireComponent(typeof(AI_Movement))]
[RequireComponent(typeof(FOVAgent))]
public class Medic : Infantry
{
    public enum MedicInputs
    {
        IDLE,
        FOLLOW_LEADER,
        RUN_TO,
        SHOOT_TARGET,
        DIE
    }

    AI_Movement _ai;
    FOVAgent _fov;
    Fireteam _fireteam;
    public EventFSM<MedicInputs> _fsm;

    public Entity _shootTarget;
    public Infantry _healTarget;
    public Vector3 coverPosition;

    Vector3 runToTarget;
    Action onTargetReached = delegate { };


    State<MedicInputs> idle, followLeader, runTo, shootTarget, die;

    private void Awake()
    {
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
            // Dejar de moverse

            // Cambiar a animacion de idle.
        };

        return idle;
    }

    State<MedicInputs> CreateFollowLeaderState()
    {
        var followLeader = new State<MedicInputs>("FOLLOW_LEADER");

        followLeader.OnEnter += _ =>
        {
            // Pasar a animacion de correr

            // Empezar corutina para calcular camino hacia el lider cada X cantidad de segundos 
        };

        return followLeader;
    }

    State<MedicInputs> CreateRunToState()
    {
        var runTo = new State<MedicInputs>("RUN_TO");

        followLeader.OnEnter += _ =>
        {
            // Empezar a calcular camino hacia posicion, y cuando se haya calculado
            // empezar a moverse y pasar a animacion de correr.
        };

        followLeader.OnUpdate += () =>
        {
            // Avisar que nos movimos.
            Moved();
        };

        return runTo;
    }

    State<MedicInputs> CreateShootTargetState()
    {
        var shootTarget = new State<MedicInputs>("SHOOT_TARGET");

        shootTarget.OnEnter += _ => 
        {
            // Dejar de moverse

            // Pasar a animacion de disparar
        };

        shootTarget.OnUpdate += () =>
        {
            // Logica de disparo y recarga
        };

        return shootTarget;
    }

    State<MedicInputs> CreateDieState() 
    {
        var die = new State<MedicInputs>("DIE");

        die.OnEnter += _ =>
        {
            // Dejar de moverse

            // Pasar a animacion de morir
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
