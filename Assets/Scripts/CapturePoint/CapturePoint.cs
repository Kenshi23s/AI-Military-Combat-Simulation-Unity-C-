using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Linq;
using IA2;
using System.Collections.ObjectModel;

public enum CaptureState
{
    Inactive,
    BeingCaptured,
    Disputed
}

[RequireComponent(typeof(DebugableObject))]
public class CapturePoint : MonoBehaviour
{
    #region Variables
    [SerializeField, Min(0)] float _searchTime = 0.2f;
    [SerializeField, Min(0)] float _pointRadius = 15, _pointHeight = 8;
    [SerializeField, Min(0)] float _captureSpeed = 5f;
    #endregion
    DebugableObject _debug;

    HashSet<ICapturePointEntity> _entitiesInPoint = new HashSet<ICapturePointEntity>();
    Dictionary<MilitaryTeam, IMilitary[]> _teamSplit = new Dictionary<MilitaryTeam, IMilitary[]>();

    [field: SerializeField] public float ProgressRequiredForCapture { get; private set; } = 100f;


    float _captureProgress = 0;
    public float CaptureProgress
    {
        get => _captureProgress;
        private set
        {
            _captureProgress = Mathf.Clamp(value, -ProgressRequiredForCapture, ProgressRequiredForCapture);
        }
    }

    SpatialGrid3D _targetGrid;

    public CaptureState CurrentState { get; private set; } = CaptureState.Inactive;

    public MilitaryTeam BeingCapturedBy { get; private set; } = MilitaryTeam.None;
    public MilitaryTeam CapturedBy { get; private set; } = MilitaryTeam.None;

    #region Questions
    public bool IsBeingCaptured => BeingCapturedBy != MilitaryTeam.None;
    public bool IsCaptured => CapturedBy != MilitaryTeam.None;
    public bool IsFullyCaptured => Mathf.Abs(_captureProgress) >= ProgressRequiredForCapture;
    #endregion

    #region Events

    #region UnityEvents
    public UnityEvent<MilitaryTeam> OnPointOwnerChange;
    public UnityEvent OnPointNeutralized;
    public UnityEvent<float> OnProgressChange;
    public UnityEvent<Dictionary<MilitaryTeam, IMilitary[]>> OnTeamsInPointUpdate;
    #endregion

    #region C# Events
    public event Action OnDisputeStart, OnDisputeEnd,OnStopCapture;
    public event Action<MilitaryTeam> OnCaptureComplete;
    public event Action<MilitaryTeam> OnBeingCaptured;
    #endregion

    #endregion

    EventFSM<CaptureState> _fsm;

    private void Awake()
    {
        _debug = GetComponent<DebugableObject>();
        _debug.AddGizmoAction(DrawCapturePoint);

        var inactive = CreateInactiveState();
        var disputed = CreateDisputedState();
        var beingCaptured = CreateBeingCapturedState();

        #region Transitions
        StateConfigurer.Create(inactive)
            .SetTransition(CaptureState.BeingCaptured, beingCaptured)
            .SetTransition(CaptureState.Disputed, disputed)
            .Done();

        StateConfigurer.Create(beingCaptured)
            .SetTransition(CaptureState.Inactive, inactive)
            .SetTransition(CaptureState.BeingCaptured, beingCaptured)
            .SetTransition(CaptureState.Disputed, disputed)
            .Done();

        StateConfigurer.Create(disputed)
            .SetTransition(CaptureState.Inactive, inactive)
            .SetTransition(CaptureState.BeingCaptured, beingCaptured)
            .Done();
        #endregion

        _fsm = new EventFSM<CaptureState>(inactive);
    }

    private void Start()
    {
        _targetGrid = FindObjectOfType<SpatialGrid3D>();
        CapturePointManager.instance.Add(this);
        StartCoroutine(UpdateEntitiesInPoint());
    }

    private void Update()
    {
        foreach (var e in _entitiesInPoint)
            e.ZoneStay();

        CheckState();

        _fsm.Update();
    }

    bool CanTeamCapture(MilitaryTeam team)
    {
        return _teamSplit[team].Any() && (!IsFullyCaptured || CapturedBy != team);
    }


    #region FSM States
    State<CaptureState> CreateInactiveState()
    {
        var inactive = new State<CaptureState>("Neutral");

        inactive.OnEnter += _ =>
        {

            CurrentState = CaptureState.Inactive;
        };

        inactive.OnUpdate += () =>
        {

        };

        inactive.OnExit += _ =>
        {

        };

        return inactive;
    }

    State<CaptureState> CreateBeingCapturedState()
    {
        var beingCaptured = new State<CaptureState>("BeingTaken");

        beingCaptured.OnEnter += _ =>
        {
            CurrentState = CaptureState.BeingCaptured;
            OnBeingCaptured?.Invoke(BeingCapturedBy);
        };

        beingCaptured.OnUpdate += () =>
        {
            UpdateCaptureProgress();
        };

        beingCaptured.OnExit += _ =>
        {
            OnStopCapture?.Invoke();
        };

        return beingCaptured;
    }

    State<CaptureState> CreateDisputedState()
    {
        var disputed = new State<CaptureState>("Disputed");

        disputed.OnEnter += _ =>
        {
            CurrentState = CaptureState.Disputed;
            OnDisputeStart?.Invoke();
        };

        disputed.OnUpdate += () =>
        {

        };

        disputed.OnExit += _ =>
        {
            OnDisputeEnd?.Invoke();
        };

        return disputed;
    }
    #endregion

    IEnumerator UpdateEntitiesInPoint()
    {
        while (true)
        {

            var previousQuery = _entitiesInPoint;

            _entitiesInPoint = PointQuery().Where(x => x.CanCapture).ToHashSet();

            // Calcular quien entro
            foreach (var e in _entitiesInPoint?.Except(previousQuery)) 
                e.ZoneEnter(this);

            // Calcular quien salio
            foreach (var e in previousQuery?.Except(_entitiesInPoint))
                e.ZoneExit(this);

            // Consigo los que son militares y divido la lista por equipos con ToLookup.
            _teamSplit = _entitiesInPoint
                .OfType<IMilitary>()
                .ToLookup(x => x.Team)
                .ToDictionary(x => x.Key, x => x.ToArray())
                .EmptyIfNull();

            OnTeamsInPointUpdate?.Invoke(_teamSplit);
            yield return new WaitForSeconds(_searchTime);
        }
    }

    void UpdateCaptureProgress()
    {
        int sign = BeingCapturedBy == MilitaryTeam.Red ? 1 : -1;

        CaptureProgress += Time.deltaTime * sign * _captureSpeed * _teamSplit.Count();
        OnProgressChange?.Invoke(CaptureProgress);

        if (CheckNeutralization())
        {
            CapturedBy = MilitaryTeam.None;
            OnPointOwnerChange?.Invoke(CapturedBy);
        }

        if (CheckCaptureFinish())
        {
            CapturedBy = BeingCapturedBy;

            OnCaptureComplete?.Invoke(CapturedBy);
            OnPointOwnerChange?.Invoke(CapturedBy);
        }
    }

    void CheckState()
    {
        // Chequear si esta siendo disputada.
        if (_teamSplit[MilitaryTeam.Red].Any() && _teamSplit[MilitaryTeam.Blue].Any() && CurrentState != CaptureState.Disputed)
        {
            SendInputToFSM(CaptureState.Disputed);
            return;
        }

        // Chequear si esta siendo capturada por el equipo rojo.
        if (CanTeamCapture(MilitaryTeam.Red) && BeingCapturedBy != MilitaryTeam.Red)
        {
            BeingCapturedBy = MilitaryTeam.Red;
            SendInputToFSM(CaptureState.BeingCaptured);
            return;
        }

        // Chequear si esta siendo capturada por el equipo azul.
        if (CanTeamCapture(MilitaryTeam.Blue) && BeingCapturedBy != MilitaryTeam.Blue)
        {
            BeingCapturedBy = MilitaryTeam.Blue;
            SendInputToFSM(CaptureState.BeingCaptured);
            return;
        }

        // Si estaba siendo capturada, pasar al estado inactivo
        if (IsBeingCaptured)
        {
            BeingCapturedBy = MilitaryTeam.None;
            SendInputToFSM(CaptureState.Inactive);
            return;
        }
    }

    bool CheckNeutralization() 
    {
        if (!IsCaptured || CapturedBy == BeingCapturedBy)
            return false;

        if (BeingCapturedBy == MilitaryTeam.Red)
            return _captureProgress >= 0;

        if (BeingCapturedBy == MilitaryTeam.Blue)
            return _captureProgress <= 0;

        return false;
    }

    MilitaryTeam GetOppositeTeam(MilitaryTeam team)
    {
        if (team == MilitaryTeam.Blue)
            return MilitaryTeam.Red;

        if (team == MilitaryTeam.Red)
            return MilitaryTeam.Blue;

        return MilitaryTeam.None;
    }

    bool CheckCaptureFinish() 
    {
        if (CapturedBy == BeingCapturedBy)
            return false;

        if (BeingCapturedBy == MilitaryTeam.Red)
            return _captureProgress >= ProgressRequiredForCapture;

        if (BeingCapturedBy == MilitaryTeam.Blue)
            return _captureProgress <= -ProgressRequiredForCapture;

        return false; 
    }

    // Hace un query cilindrico de objetos de tipo ICapturePointEntity.
    IEnumerable<ICapturePointEntity> PointQuery()
    {
        // Creo una "caja" con las dimensiones deseadas, y luego filtro segun distancia para formar el cilindro.
        var col = _targetGrid.Query(
            transform.position + new Vector3(-_pointRadius, 0, -_pointRadius),
            transform.position + new Vector3(_pointRadius, _pointHeight, _pointRadius),
            pos => {
                var distance = pos - transform.position;
                distance.y = transform.position.y;
                return distance.sqrMagnitude < _pointRadius * _pointRadius;
            });

        return col.Select(x => x.Owner).OfType<ICapturePointEntity>();
    }

    private void DrawCapturePoint()
    {
        Gizmos.color = new Color(243, 58, 106, 255) / 255;
        DrawCylinder.ForGizmo(transform.position, Quaternion.identity, _pointHeight, _pointRadius);
    }

    private void SendInputToFSM(CaptureState inp)
    {
        _fsm.SendInput(inp);
    }
}
