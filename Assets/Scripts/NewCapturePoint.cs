using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using IA2;

[RequireComponent(typeof(CapturePoint))]
public class NewCapturePoint : MonoBehaviour
{
    public enum ZoneStates
    {
        Neutral,
        Disputed,
        BeingTaken,
        Taken
    }

    [SerializeField, Min(1)] float _waitingTimeTilSearch = 0.2f;
    [SerializeField, Min(0)] float _zoneRadius = 15, _zoneHeight = 8;
    DebugableObject _debug;

    public UnityEvent OnPointOwnerChange;
    public UnityEvent OnProgressChange;

    public UnityEvent<Dictionary<MilitaryTeam, IMilitary[]>> OnEntitiesAroundUpdate;

    public event Action<MilitaryTeam> OnCaptureComplete;

    public ZoneStates CurrentCaptureState { get; private set; }

    public MilitaryTeam TakenBy { get; private set; } = MilitaryTeam.None;
    public MilitaryTeam BeingTakenBy { get; private set; } = MilitaryTeam.None;

    EventFSM<ZoneStates> _fsm;

    private void Awake()
    {
        var neutral = CreateNeutralState();
        var disputed = CreateDisputedState();
        var beingTaken = CreateBeingTakenState();
        var taken = CreateTakenState();

        #region Transitions
        StateConfigurer.Create(neutral)
            .SetTransition(ZoneStates.BeingTaken, beingTaken)
            .Done();

        StateConfigurer.Create(disputed)
            .SetTransition(ZoneStates.BeingTaken, beingTaken)
            .Done();

        StateConfigurer.Create(beingTaken)
            .SetTransition(ZoneStates.Neutral, neutral)
            .SetTransition(ZoneStates.Disputed, disputed)
            .SetTransition(ZoneStates.Taken, taken)
            .Done();

        StateConfigurer.Create(taken)
            .SetTransition(ZoneStates.BeingTaken, beingTaken)
            .Done();
        #endregion

        _fsm = new EventFSM<ZoneStates>(neutral);
    }

    State<ZoneStates> CreateNeutralState()
    {
        var neutral = new State<ZoneStates>("Neutral");

        neutral.OnEnter += _ =>
        {
            CurrentCaptureState = ZoneStates.Neutral;

        };

        neutral.OnUpdate += () =>
        {
            // Hacer query de quienes entraron de los dos equipos

            // Si entro alguien, pasar a being taken
        };

        neutral.OnExit += _ =>
        {

        };

        return neutral;
    }

    State<ZoneStates> CreateDisputedState() {
        var disputed = new State<ZoneStates>("Disputed");

        disputed.OnEnter += _ =>
        {
            CurrentCaptureState = ZoneStates.Disputed;
        };

        disputed.OnUpdate += () =>
        {
            // Hacer query de quienes entraron de los dos equipos
        };

        disputed.OnExit += _ =>
        {

        };

        return disputed;
    }

    State<ZoneStates> CreateBeingTakenState()
    {
        var beingTaken = new State<ZoneStates>("BeingTaken");

        beingTaken.OnEnter += _ =>
        {
            CurrentCaptureState = ZoneStates.BeingTaken;

            // Hacer un chequeo para ver si transicionar a disputed o a neutral

            // Si no hay transicion, setear beingtakenby a el equipo correspondiente

            // Mientras se este en being taken, se actualiza mas frequentemente el query
        };

        beingTaken.OnUpdate += () =>
        {
            // Si se fueron los del equipo actual, pasar a neutral
            // Si entraron del equipo enemigo, pasar a disputed
            // Si se termino de capturar, pasar a taken.
        };

        beingTaken.OnExit += _ =>
        {
            BeingTakenBy = MilitaryTeam.None;
        };

        return beingTaken;
    }

    State<ZoneStates> CreateTakenState()
    {
        var taken = new State<ZoneStates>("Taken");

        taken.OnEnter += _ =>
        {
            CurrentCaptureState = ZoneStates.Taken;

        };

        taken.OnUpdate += () =>
        {
            // Si entran del equipo enemigo, pasar a being taken
        };

        taken.OnExit += _ =>
        {

        };

        return taken;
    }


    private void SendInputToFSM(ZoneStates inp)
    {
        _fsm.SendInput(inp);
    }
}
