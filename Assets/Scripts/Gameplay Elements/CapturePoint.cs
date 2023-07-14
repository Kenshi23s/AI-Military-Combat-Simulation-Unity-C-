using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(DebugableObject))]
public class CapturePoint : MonoBehaviour
{
    public enum ZoneStates
    {
        Disputed,
        Neutral,
        BeingTaken,
        Taken
    }

    [SerializeField, Min(1)] float _waitingFramesTilSearch = 30;
    [SerializeField, Min(0)] float _zoneRadius = 15;
    [SerializeField] DebugableObject _debug;

    #region Events
    public UnityEvent onPointOwnerChange;

    public UnityEvent onProgressChange;

    public UnityEvent<ILookup<Team,Entity>> onEntitiesAroundUpdate;
    #endregion

    public float TakeProgress { get; private set; }


    [SerializeField]
    SpatialGrid3D _targetGrid;

    [field: SerializeField] public float ProgressRequiredForCapture { get; private set; }
    float captureProgress = 0;

    public float CaptureProgress
    {
        get => captureProgress;
        private set
        {
            captureProgress = Mathf.Clamp(value, -ProgressRequiredForCapture, ProgressRequiredForCapture);
        }
    }

    public float ZoneProgressNormalized 
    {
        get
        {
            if (captureProgress == 0) return 0.5f;

            return Mathf.Abs(captureProgress) / (ProgressRequiredForCapture * 2);
        }
    }
        
  
    public ZoneStates CurrentCaptureState { get; private set; }
    
    public Team takenBy = Team.None;
    public Team beingTakenBy { get; private set; }

    List<Entity> _combatEntitiesAround = new List<Entity>();

   
    void Awake()
    {
        _debug = GetComponent<DebugableObject>();
        _debug.AddGizmoAction(DrawRadius);
    }


    private void Start()
    {
        _targetGrid = FindObjectOfType<SpatialGrid3D>();
        CapturePointManager.instance.AddZone(this);
        StartCoroutine(SearchEntitiesAround());
    }

  
    IEnumerator SearchEntitiesAround()
    {
        while (true) 
        {
            for (int i = 0; i < _waitingFramesTilSearch; i++) yield return null;


            _combatEntitiesAround = SphereQuery()
                .OfType<Entity>()
                .ToList();

            _debug.Log("Combat Entities Around Zone: " + _combatEntitiesAround.Count);

            //divido la lista entre equipo rojo y verde con el lookup
            //si pasan el predicado, accedo a esos items con [true] y si no
            //accedo a los otros items con [false]
            var split = _combatEntitiesAround
                .Where(x => x.MyTeam != Team.None)
                .ToLookup(x => x.MyTeam);


            
            onEntitiesAroundUpdate?.Invoke(split);

            if (!split[Team.Red].Any() || !split[Team.Blue].Any()) 
            {
                _debug.Log("No hay unidades en el area");
                continue;
            }
          

            //rojo                    
            if (split[Team.Red].Any() && split[Team.Blue].Any())
            {
                CurrentCaptureState = ZoneStates.Disputed;
                _debug.Log("Esta en disputa, hay unidades de ambos equipos");
                continue;
            }
            string debug = "Esta siendo tomada por";
            if (split[Team.Red].Any())
            {
                debug += "El equipo rojo";
                CaptureProgress += Time.deltaTime * split[Team.Red].Count();
                beingTakenBy = Team.Red;
            }                    
            else
            {
                debug += "El equipo azul";
                CaptureProgress -= Time.deltaTime * split[Team.Blue].Count();
                beingTakenBy = Team.Blue;
            }
            _debug.Log(debug);

            onProgressChange?.Invoke();
            CheckCaptureProgress();
        }    
    }

    void CheckCaptureProgress()
    {
        var aux = takenBy;
        switch (beingTakenBy)
        {
            case Team.Red:
                if (captureProgress >= ProgressRequiredForCapture)
                    takenBy = Team.Red;
                break;
            case Team.Blue:
                if (captureProgress <= -ProgressRequiredForCapture)
                    takenBy = Team.Blue;
                break;
            default:
                takenBy = Team.None;
                break;
        }

        if (takenBy != aux) onPointOwnerChange?.Invoke();
    }

    private void DrawRadius()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _zoneRadius);
    }

    IEnumerable<GridEntity> SphereQuery() 
    {
        //creo una "caja" con las dimensiones deseadas, y luego filtro segun distancia para formar el c�rculo
        return _targetGrid.Query(
            transform.position + new Vector3(-_zoneRadius, -_zoneRadius, -_zoneRadius),
            transform.position + new Vector3(_zoneRadius, _zoneRadius, _zoneRadius),
            pos => {
                var distance = pos - transform.position;
                return distance.sqrMagnitude < _zoneRadius * _zoneRadius;
            });
    }
}
