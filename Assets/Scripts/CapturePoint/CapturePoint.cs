using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Linq;
using IA2;
using System.Collections.ObjectModel;

/// <summary>State of a CapturePoint.</summary>
public enum CaptureState
{
    Inactive,
    BeingCaptured,
    Disputed
}

/// <summary>
/// Cylinder zone that a team captures by keeping more units inside it.
/// CaptureProgress goes from -ProgressRequiredForCapture to ProgressRequiredForCapture, one sign per team.
/// </summary>
/// <remarks>
/// <para>The units inside are queried from the SpatialGrid3D every _searchTime seconds.</para>
/// <para>A new state applies after it holds for StateChangeDelay seconds.</para>
/// <para>Each unit of the capturing team adds _captureSpeed of progress per second.</para>
/// </remarks>
[RequireComponent(typeof(DebugableObject))]
public class CapturePoint : MonoBehaviour
{
    [field: SerializeField] public float ProgressRequiredForCapture { get; private set; } = 100f;

    public UnityEvent<MilitaryTeam> OnPointOwnerChange;
    public UnityEvent OnPointNeutralized;
    public UnityEvent<float> OnProgressChange;
    public UnityEvent<Dictionary<MilitaryTeam, IMilitary[]>> OnTeamsInPointUpdate;
    [SerializeField, Min(0)] private float _searchTime = 0.2f;
    [SerializeField, Min(0)] private float _pointRadius = 15, _pointHeight = 8;
    [SerializeField, Min(0)] private float _captureSpeed = 5f;
    private DebugableObject _debug;

    private HashSet<ICapturePointEntity> _entitiesInPoint = new HashSet<ICapturePointEntity>();
    private Dictionary<MilitaryTeam, IMilitary[]> _teamSplit = new Dictionary<MilitaryTeam, IMilitary[]>();

    private float _captureProgress = 0;

    private SpatialGrid3D _targetGrid;

    private EventFSM<CaptureState> _fsm;

    // State that CheckState waits to apply, and the seconds it has held.
    private CaptureState _pendingState = CaptureState.Inactive;
    private MilitaryTeam _pendingCapturer = MilitaryTeam.None;
    private float _pendingTime;

    // Seconds a new state has to hold before the point switches to it.
    private const float StateChangeDelay = 0.3f;
    public float CaptureProgress
    {
        get => _captureProgress;
        private set
        {
            _captureProgress = Mathf.Clamp(value, -ProgressRequiredForCapture, ProgressRequiredForCapture);
        }
    }

    public CaptureState CurrentState { get; private set; } = CaptureState.Inactive;

    public MilitaryTeam BeingCapturedBy { get; private set; } = MilitaryTeam.None;
    public MilitaryTeam CapturedBy { get; private set; } = MilitaryTeam.None;

    public bool IsBeingCaptured => BeingCapturedBy != MilitaryTeam.None;
    public bool IsCaptured => CapturedBy != MilitaryTeam.None;
    public bool IsFullyCaptured => Mathf.Abs(_captureProgress) >= ProgressRequiredForCapture;

    public event Action OnDisputeStart, OnDisputeEnd,OnStopCapture;
    public event Action<MilitaryTeam> OnCaptureComplete;
    public event Action<MilitaryTeam> OnBeingCaptured;

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
            e.PointStay();

        CheckState();

        _fsm.Update();
    }

    private bool CanTeamCapture(MilitaryTeam team)
    {
        return UnitsOf(team) > 0 && (!IsFullyCaptured || CapturedBy != team);
    }

    #region FSM States
    private State<CaptureState> CreateInactiveState()
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

    private State<CaptureState> CreateBeingCapturedState()
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

    private State<CaptureState> CreateDisputedState()
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

    private IEnumerator UpdateEntitiesInPoint()
    {
        while (true)
        {

            var previousQuery = _entitiesInPoint;

            _entitiesInPoint = PointQuery().Where(x => x.CanCapture).ToHashSet();

            // Calcular quien entro
            foreach (var e in _entitiesInPoint?.Except(previousQuery)) 
                e.PointEnter(this);

            // Calcular quien salio
            foreach (var e in previousQuery?.Except(_entitiesInPoint))
                e.PointExit(this);

            // Consigo los que son militares y divido la lista por equipos con ToLookup.
            _teamSplit = _entitiesInPoint
                .OfType<IMilitary>()
                .ToLookup(x => x.Team)
                .ToDictionary(x => x.Key, x => x.ToArray())
                .EmptyIfNull();
            //IA2-LINQ

            OnTeamsInPointUpdate?.Invoke(_teamSplit);
            yield return new WaitForSeconds(_searchTime);
        }
    }

    private void UpdateCaptureProgress()
    {
        int sign = BeingCapturedBy == MilitaryTeam.Red ? 1 : -1;

        // Each unit of the capturing team adds _captureSpeed per second.
        CaptureProgress += Time.deltaTime * sign * _captureSpeed * UnitsOf(BeingCapturedBy);
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

    // Applies the state the units in the point ask for, once it holds for StateChangeDelay seconds.
    private void CheckState()
    {
        GetDesiredState(out CaptureState desiredState, out MilitaryTeam desiredCapturer);

        // Already in that state: nothing pending.
        if (desiredState == CurrentState && desiredCapturer == BeingCapturedBy)
        {
            _pendingTime = 0f;
            return;
        }

        // A different request restarts the wait.
        if (desiredState != _pendingState || desiredCapturer != _pendingCapturer)
        {
            _pendingState = desiredState;
            _pendingCapturer = desiredCapturer;
            _pendingTime = 0f;
        }

        _pendingTime += Time.deltaTime;
        if (_pendingTime < StateChangeDelay)
            return;

        _pendingTime = 0f;

        // BeingCapturedBy is set before the transition, so OnBeingCaptured carries the new team.
        BeingCapturedBy = desiredCapturer;
        SendInputToFSM(desiredState);
    }

    // Both teams in the point: Disputed.
    // One team that can capture: BeingCaptured by that team.
    // Any other case: Inactive.
    private void GetDesiredState(out CaptureState state, out MilitaryTeam capturer)
    {
        int red = UnitsOf(MilitaryTeam.Red);
        int blue = UnitsOf(MilitaryTeam.Blue);

        if (red > 0 && blue > 0)
        {
            state = CaptureState.Disputed;
            capturer = MilitaryTeam.None;
        }
        else if (CanTeamCapture(MilitaryTeam.Red))
        {
            state = CaptureState.BeingCaptured;
            capturer = MilitaryTeam.Red;
        }
        else if (CanTeamCapture(MilitaryTeam.Blue))
        {
            state = CaptureState.BeingCaptured;
            capturer = MilitaryTeam.Blue;
        }
        else
        {
            state = CaptureState.Inactive;
            capturer = MilitaryTeam.None;
        }
    }

    /// <summary>Returns the number of units of team in the point.</summary>
    public int UnitsOf(MilitaryTeam team)
    {
        return _teamSplit.TryGetValue(team, out IMilitary[] units) ? units.Length : 0;
    }

    private bool CheckNeutralization() 
    {
        if (!IsCaptured || CapturedBy == BeingCapturedBy)
            return false;

        if (BeingCapturedBy == MilitaryTeam.Red)
            return _captureProgress >= 0;

        if (BeingCapturedBy == MilitaryTeam.Blue)
            return _captureProgress <= 0;

        return false;
    }

    private MilitaryTeam GetOppositeTeam(MilitaryTeam team)
    {
        if (team == MilitaryTeam.Blue)
            return MilitaryTeam.Red;

        if (team == MilitaryTeam.Red)
            return MilitaryTeam.Blue;

        return MilitaryTeam.None;
    }

    private bool CheckCaptureFinish() 
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
    private IEnumerable<ICapturePointEntity> PointQuery()
    {
        // Creo una "caja" con las dimensiones deseadas, y luego filtro segun distancia para formar el cilindro.
        var col = _targetGrid.Query(
            transform.position + new Vector3(-_pointRadius, 0, -_pointRadius),
            transform.position + new Vector3(_pointRadius, _pointHeight, _pointRadius),
            pos => {
                // Horizontal distance: the cylinder is vertical.
                var distance = pos - transform.position;
                distance.y = 0;
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
